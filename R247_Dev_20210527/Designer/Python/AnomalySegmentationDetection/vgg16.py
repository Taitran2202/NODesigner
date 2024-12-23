import tensorflow as tf 
from tensorflow import keras 
from tensorflow.keras.preprocessing.image import ImageDataGenerator
from tensorflow.keras.optimizers import Adam
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

def vgg16(weights_file_path, input_shape):
  inputs = keras.Input(shape=input_shape, dtype='float32', name='input_vgg16')
  x = layers.Conv2D(64, (3,3), padding='same', activation='relu')(inputs)
  x = layers.Conv2D(64, (3,3), padding='same', activation='relu')(x)
  x = layers.MaxPool2D((2,2), (2,2))(x)
  x = layers.Conv2D(128, (3,3), padding='same', activation='relu')(x)
  x = layers.Conv2D(128, (3,3), padding='same', activation='relu')(x)
  x = layers.MaxPool2D((2,2), (2,2))(x)
  x = layers.Conv2D(256, (3,3), padding='same', activation='relu')(x)
  x = layers.Conv2D(256, (3,3), padding='same', activation='relu')(x)
  x = layers.Conv2D(256, (3,3), padding='same', activation='relu')(x)
  x = layers.MaxPool2D((2,2), (2,2))(x)
  x = layers.Conv2D(512, (3,3), padding='same', activation='relu')(x)
  x = layers.Conv2D(512, (3,3), padding='same', activation='relu')(x)
  x = layers.Conv2D(512, (3,3), padding='same', activation='relu')(x)
  x = layers.MaxPool2D((2,2), (2,2))(x)
  x = layers.Conv2D(512, (3,3), padding='same', activation='relu')(x)
  x = layers.Conv2D(512, (3,3), padding='same', activation='relu')(x)
  x = layers.Conv2D(512, (3,3), padding='same', activation='relu')(x)
  outputs = layers.MaxPool2D((2,2), (2,2))(x)
  model = keras.Model(inputs, outputs)
  model.load_weights(weights_file_path)
  return model