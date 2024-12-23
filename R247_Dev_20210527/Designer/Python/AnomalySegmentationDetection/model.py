import tensorflow as tf 
from tensorflow import keras 
from tensorflow.keras.preprocessing.image import ImageDataGenerator
from tensorflow.keras.optimizers import Adam
from tensorflow.keras.applications.vgg16 import VGG16
from tensorflow.keras.applications.vgg19 import VGG19
from tensorflow.keras.applications.mobilenet_v2 import MobileNetV2
from tensorflow.keras import Input, Model, Sequential
from tensorflow.keras import layers
from sklearn.model_selection import train_test_split
from sklearn.decomposition import PCA
from skimage import io
from skimage.transform import resize
import numpy as np
import cv2
import matplotlib.pyplot as plt
import os
import random
from tqdm import tqdm
from random import randrange
import tensorflow_probability as tfp

from config import *
from vgg16 import *

class AnomalySegmentator(keras.Model):
  def __init__(self, init_layer=0, end_layer=None,LEAKYRELU_ALPHA=0.1):
    super(AnomalySegmentator, self).__init__()
    self.init_layer = init_layer
    self.end_layer = end_layer
    self.LEAKYRELU_ALPHA= LEAKYRELU_ALPHA

  def build_autoencoder(self, c0, cd):
    self.autoencoder = Sequential(
        [
         layers.InputLayer((self.map_shape[0] // 4, self.map_shape[1] // 4, c0)),
         layers.Conv2D((c0 + cd) // 2, (1,1), padding='same', activation=tf.keras.layers.LeakyReLU(alpha=self.LEAKYRELU_ALPHA)),
         layers.Conv2D(2 * cd, (1,1), padding='same', activation=tf.keras.layers.LeakyReLU(alpha=self.LEAKYRELU_ALPHA)),
         layers.Conv2D(cd, (1,1), padding='same'),
         layers.Conv2D(2 * cd, (1,1), padding='same', activation=tf.keras.layers.LeakyReLU(alpha=self.LEAKYRELU_ALPHA)),
         layers.Conv2D((c0 + cd) // 2, (1,1), padding='same', activation=tf.keras.layers.LeakyReLU(alpha=self.LEAKYRELU_ALPHA)),
         layers.Conv2D(c0, (1,1), padding='same', dtype='float32'),
        ]
    )
  
  def build_model(self, weights_file_path, input_shape):
    self.vgg = vgg16(weights_file_path, input_shape)
    self.features_list = [layer.output for layer in self.vgg.layers if 'conv' in layer.name][self.init_layer:self.end_layer]
    self.feature_extractor = Model(inputs = self.vgg.input, outputs = self.features_list)
    self.feature_extractor.trainable = False
    self.threshold = tf.Variable(0, trainable = False, dtype = tf.float32)
    self.map_shape = self.features_list[0].shape[1:-1]
    self.average_pooling = layers.AveragePooling2D(pool_size=(4, 4), strides=(4,4))
    self.c0 = sum([feature.shape[-1] for feature in self.features_list])
    self.cd = 40
    self.build_autoencoder(self.c0, self.cd)

  def call(self, x):
    features = self.feature_extractor(x)
    resized_features = [tf.image.resize(feature, self.map_shape) for feature in features]
    resized_features = tf.concat(resized_features, axis = -1)
    resized_features = tf.cast(self.average_pooling(resized_features), dtype='float32')
    autoencoder_output = self.autoencoder(resized_features)
    return tf.reduce_sum((autoencoder_output - resized_features)**2, axis = -1)

  def reconstruction_loss(self):
    @tf.function
    def _loss(y_true, y_pred):
      loss = tf.cast(tf.reduce_mean(y_pred, axis=(1,2)), tf.float32) / (tf.cast(tf.shape(y_pred)[0], tf.float32) * self.c0)
      return loss
    return _loss
  
  def compute_threshold(self, data_loader, dataset_type='tf.data', fpr = 0.05):
    error = []

    if dataset_type == "Sequence":
      for i in tqdm(range(len(data_loader))):
        x, y = data_loader[i]
        error.append(self(x))
      threshold = np.percentile(error, 100 - fpr)
      self.threshold = tf.Variable(threshold, trainable = False, dtype=tf.float32)
    else:
      total_data = data_loader.take(len(data_loader))
      for idx, (x, y) in enumerate(total_data):
        error.append(self(x))
        threshold = np.percentile(error, 100 - fpr)
      self.threshold = tf.Variable(threshold, trainable = False, dtype = tf.float32)
  
  def compute_pca(self, data_loader, dataset_type='tf.data'):
    extraction_per_sample = 20
    extractions = []

    if dataset_type == "Sequence":
      for i in tqdm(range(len(data_loader))):
        x, _ = data_loader[i]
        features = self.feature_extractor(x)
        resized_features = [tf.image.resize(feature, self.map_shape) for feature in features]
        resized_features = tf.concat(resized_features, axis = -1)
        resized_features = self.average_pooling(resized_features)
        for feature in resized_features:
          for _ in range(extraction_per_sample):
            row, col = randrange(feature.shape[0]), randrange(feature.shape[1])
            extraction = feature[row, col]
            extractions.append(extraction)
    else:
      total_data = data_loader.take(len(data_loader))
      for idx, (X, y) in enumerate(total_data):
        features = self.feature_extractor(X)
        resized_features = [tf.image.resize(feature, self.map_shape) for feature in features]
        resized_features = tf.concat(resized_features, axis = -1)
        resized_features = self.average_pooling(resized_features)

        for feature in resized_features:
          for _ in range(extraction_per_sample):
            row, col = random.randrange(feature.shape[0]), random.randrange(feature.shape[1])
            extraction = feature[row, col]
            extractions.append(extraction)
    
    if dataset_type == "Sequence":
      extractions = np.array(extractions)
    else:
      extractions = tf.convert_to_tensor(extractions)
    print(f"Extractions shape: {extractions.shape}")
    pca = PCA(0.9, svd_solver="full")
    pca.fit(extractions)
    self.cd = pca.n_components_
    self.build_autoencoder(self.c0, self.cd)
    print(f"Components with explainable variance 0.9 -> {self.cd}")