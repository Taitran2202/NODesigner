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
import matplotlib.pyplot as 

import os
import random
from tqdm import tqdm
from random import randrange
import tensorflow_probability as tfp
from config import *

#tf.Sequence

class CustomDataGen(tf.keras.utils.Sequence):
  def __init__(self, path, batch_size, input_size, shuffle=True, seed=None, subset="Training",mask=None):
    super(CustomDataGen, self)
    self.img_data_generator = ImageDataGenerator(rescale=1.0/255, data_format="channels_last",
                                                 zoom_range = 0.15,
                                                 width_shift_range = 0.1,
                                                 height_shift_range = 0.1,rotation_range=30)
    if(mask is not None):
      self.UseMask=True
      self.r1=mask["row1"]
      self.r2=mask["row2"]
      self.c1=mask["col1"]
      self.c2=mask["col2"]
    else:
      self.UseMask=False
    self.shuffle=shuffle
    if seed is None:
      random.randint(0, 2**32)
    self.batch_size = batch_size
    self.input_size = input_size
    self.x_paths = [os.fsdecode(file) for file in os.scandir(path)]
    self.x_train, self.x_val = train_test_split(self.x_paths, test_size=0.2, random_state=seed, shuffle=True)

    if subset == "Training":
      self.x = self.x_paths
    elif subset == "Validation":
      self.x = self.x_val
    
    self.num_samples = len(self.x)
    self.indexes = np.arange(self.num_samples)
    self.on_epoch_end()
  
  def __len__(self):
    return int(np.floor(self.num_samples / self.batch_size))
  def on_epoch_end(self):
    if self.shuffle:
      np.random.shuffle(self.indexes)
  def __data_generation(self, indexes):
    data_x = []
    for i,index in enumerate(indexes):
      image = cv2.imread(self.x[index])
      if(self.UseMask):
        image=image[self.r1:self.r2,self.c1:self.c2,:]
      image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
      image = self.img_data_generator.random_transform(image)
      
      nh, nw = self.input_size

      image_resized = cv2.resize(image, (nw, nh))

      image_resized = image_resized / 255.
      image_resized = image_resized*np.random.uniform(0.9,1.1)
      data_x.append(image_resized)

    data_x = np.array(data_x)

    return data_x, data_x
  def __getitem__(self, index):
    indexes = self.indexes[index * self.batch_size:(index + 1) * self.batch_size]
    return self.__data_generation(indexes)


# tf.dataset
AUTOTUNE = tf.data.AUTOTUNE

class Datagen:
  def __init__(self, img_paths, img_size, batch_size, subset="Training"):
    self.img_paths = img_paths
    self.img_size = img_size
    self.batch_size = batch_size
    self.subset = subset
    self.X_paths = [os.fsdecode(file) for file in os.scandir(img_paths)]
    self.X_train, self.X_val = train_test_split(self.X_paths, test_size=0.2, random_state = seed, shuffle = True)
    
    if subset == 'Training':            
        self.X = self.X_train
    elif subset == 'Validation':
        self.X = self.X_val
        
    self.n = len(self.X)
  
  def _build_data(self, img_path):
    image = tf.io.read_file(img_path)
    image = tf.image.decode_png(image, channels = 3)
    image = self.distortion_free_resize(image, self.img_size)
    image = tf.cast(image, tf.float32) / 255.0
    return image, image

  def distortion_free_resize(self, image, img_size):
    w, h = img_size
    image = tf.image.resize(image, size = (h, w), preserve_aspect_ratio = True)

    pad_height = h - tf.shape(image)[0]
    pad_width = w - tf.shape(image)[1]

    if pad_height % 2 != 0:
        height = pad_height // 2
        pad_height_top = height + 1
        pad_height_bottom = height
    else:
        pad_height_top = pad_height_bottom = pad_height // 2

    if pad_width % 2 != 0:
        width = pad_width // 2
        pad_width_left = width + 1
        pad_width_right = width
    else:
        pad_width_left = pad_width_right = pad_width // 2

    image = tf.pad(image,
                paddings = [
                    [pad_height_top, pad_height_bottom],
                    [pad_width_left, pad_width_right],
                    [0, 0],
                ])
    image = tf.transpose(image, perm = [1, 0, 2])
    return image

  def _set_shapes(self, x, y):
    x.set_shape([self.batch_size, *self.img_size, 3])
    y.set_shape([self.batch_size, *self.img_size, 3])
    return (x, y)

  
  def _get_dataset(self):
    trainDS = tf.data.Dataset.from_tensor_slices(list(self.X))
    trainDS = (trainDS.shuffle(self.n)
                    .map(self._build_data, num_parallel_calls=AUTOTUNE)
                    .cache()
                    .batch(self.batch_size)
                    .map(self._set_shapes, num_parallel_calls=AUTOTUNE)
                    .prefetch(AUTOTUNE)
            )
    return trainDS