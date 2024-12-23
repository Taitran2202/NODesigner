import tensorflow as tf
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from IPython.display import display
import cv2
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras import backend as K
from easydict import EasyDict as edict
import os
import shutil
import random
import math
from config import *
from model import *
from utils import *
from dataset import *
from loss import *

class upsample(layers.Layer):
  def __init__(self, out_shape, data_format, ):
    super(upsample, self).__init__()
    self.out_shape = out_shape
    self.data_format = data_format
  
  def call(self, x):
    if self.data_format == 'channels_first':
      x = tf.transpose(x, [0, 2, 3, 1])
      new_height = self.out_shape[3]
      new_width = self.out_shape[2]
    else:
      new_height = self.out_shape[2]
      new_width = self.out_shape[1]
    
    x = tf.image.resize(x, (new_height, new_width), tf.image.ResizeMethod.NEAREST_NEIGHBOR)

    if self.data_format == 'channels_first':
      x = tf.transpose(x, [0, 3, 1, 2])
    
    return x

class batch_norm(layers.Layer):
  def __init__(self, training, data_format):
    super(batch_norm, self).__init__()
    self.training = training
    self.data_format = data_format
    self.bn = layers.BatchNormalization(
        axis=1 if data_format == 'channels_first' else 3,
        momentum=cfg.BATCH_NORM_DECAY, epsilon=cfg.BATCH_NORM_EPSILON,
        scale=True, trainable=training
    )
  
  def call(self, x):
    return self.bn(x)


class fixed_padding(layers.Layer):
  def __init__(self, kernel_size, data_format):
    super(fixed_padding, self).__init__()
    self.kernel_size = kernel_size
    self.data_format = data_format
  
  def call(self, x):
    pad_total = self.kernel_size - 1
    pad_beg = pad_total // 2
    pad_end = pad_total - pad_beg

    if self.data_format == 'channels_first':
        padded_inputs = tf.pad(x, [[0, 0], [0, 0],
                                  [pad_beg, pad_end],
                                  [pad_beg, pad_end]])
    else:
        padded_inputs = tf.pad(x, [[0, 0], [pad_beg, pad_end],
                                        [pad_beg, pad_end], [0, 0]])
    return padded_inputs


class conv2d_fixed_padding(layers.Layer):
  def __init__(self, filters, kernel_size, data_format, strides=1): 
    super(conv2d_fixed_padding, self).__init__()  
    self.strides = strides
    self.kernel_size = kernel_size
    self.filters = filters
    self.data_format = data_format
    self.fp = fixed_padding(kernel_size=kernel_size, data_format=data_format)
    self.conv2d = layers.Conv2D(filters=self.filters, kernel_size=self.kernel_size,
                         strides=self.strides,
                         padding=("same" if self.strides == 1 else "valid"),
                         use_bias=False, data_format=self.data_format)

  def call(self, x):
    if self.strides > 1:
      x = self.fp(x)
    
    return self.conv2d(x)


class residual_block(layers.Layer):
  def __init__(self, filters, training, data_format, strides=1):
    super(residual_block, self).__init__()
    self.filters = filters
    self.strides = strides
    self.data_format = data_format
    self.cfp1 = conv2d_fixed_padding(filters=filters, kernel_size=1, strides=strides, data_format=data_format)
    self.cfp2 = conv2d_fixed_padding(filters=filters * 2, kernel_size=3, strides=strides, data_format=data_format)
    self.bn1 = batch_norm(training=training, data_format=data_format)
    self.bn2 = batch_norm(training=training, data_format=data_format)
    self.leakyReLu1 = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)
    self.leakyReLu2 = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)
  
  def call(self, x):
    inputs = x
    inputs = self.cfp1(inputs)
    inputs  = self.bn1(inputs)
    inputs = self.leakyReLu1(inputs)
    inputs = self.cfp2(inputs)
    inputs  = self.bn2(inputs)
    inputs = self.leakyReLu2(inputs)
    return inputs + x

class darknet53(layers.Layer):
  def __init__(self, training, data_format, input_tensor):
    super(darknet53, self).__init__()
    self.training = training
    self.data_format = data_format
    self.modules = self._get_model(input_tensor)

  def call(self, x):
    return self.modules(x)

  def _get_model(self, input_tensor):
    input_shape = input_tensor.shape
    input_tensor = keras.Input(shape=(input_shape[1], input_shape[2], input_shape[3]))
    x = conv2d_fixed_padding(filters=32, kernel_size=3,
                                  data_format=self.data_format)(input_tensor)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)
    x = conv2d_fixed_padding(filters=64, kernel_size=3, strides=2, data_format=self.data_format)(x)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)
    x = residual_block(filters=32, training=self.training, data_format=self.data_format)(x)
    x = conv2d_fixed_padding(filters=128, kernel_size=3, strides=2, data_format=self.data_format)(x)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)

    for _ in range(2):
      x = residual_block(filters=64, training=self.training, data_format=self.data_format)(x)
    
    x = conv2d_fixed_padding(filters=256, kernel_size=3, strides=2, data_format=self.data_format)(x)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)

    for _ in range(8):
      x = residual_block(filters=128, training=self.training, data_format=self.data_format)(x)
    
    route1 = x
    x = conv2d_fixed_padding(filters=512, kernel_size=3, strides=2, data_format=self.data_format)(x)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)

    for _ in range(8):
      x = residual_block(filters=256, training=self.training, data_format=self.data_format)(x)
    
    route2 = x
    x = conv2d_fixed_padding(filters=1024, kernel_size=3, strides=2, data_format=self.data_format)(x)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)

    for _ in range(4):
      x = residual_block(filters=512, training=self.training, data_format=self.data_format)(x)
    
    model = keras.Model(inputs=input_tensor, outputs=[route1, route2, x])

    return model


class yolo_conv_block(layers.Layer):
  def __init__(self, filters, training, data_format, output_tensor_prev):
    super(yolo_conv_block, self).__init__()
    self.filters = filters
    self.training = training
    self.data_format = data_format
    self.modules = self._get_model(output_tensor_prev)

  def call(self, x):
    return self.modules(x)
  
  def _get_model(self, output_tensor_prev):
    input_shape = output_tensor_prev.shape
    inputs = keras.Input(shape=(input_shape[1], input_shape[2], input_shape[3]))
    x = conv2d_fixed_padding(filters=self.filters, kernel_size=1, data_format=self.data_format)(inputs)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)
    x = conv2d_fixed_padding(filters=self.filters * 2, kernel_size=3, data_format=self.data_format)(x)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)
    x = conv2d_fixed_padding(filters=self.filters, kernel_size=1, data_format=self.data_format)(x)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)
    x = conv2d_fixed_padding(filters=self.filters * 2, kernel_size=3, data_format=self.data_format)(x)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)
    x = conv2d_fixed_padding(filters=self.filters, kernel_size=1, data_format=self.data_format)(x)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)
    route = x
    x = conv2d_fixed_padding(filters=self.filters * 2, kernel_size=3, data_format=self.data_format)(x)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    outputs = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)
    model = keras.Model(inputs=inputs, outputs=[route, outputs])

    return model

class detect_layer(layers.Layer):
  def __init__(self, n_classes, anchors, img_size, data_format, **kwargs):
    super(detect_layer, self).__init__(**kwargs)
    self.n_classes = n_classes
    self.img_size = img_size
    self.data_format = data_format
    self.anchors = anchors
    self.n_anchors = len(anchors)
    self.conv2d = layers.Conv2D(filters=self.n_anchors * (5 + self.n_classes), kernel_size=1, strides=1,
                      use_bias=True, data_format=self.data_format)

  def call(self, x):
    x = self.conv2d(x)


    shape = x.get_shape().as_list()
    grid_shape = shape[2:4] if self.data_format == 'channels_first' else shape[1:3]
    if self.data_format == 'channels_first':
      x = tf.transpose(x, [0, 2, 3, 1])

    conv_output = tf.reshape(x, [-1, grid_shape[0], grid_shape[1], self.n_anchors, (self.n_classes + 5)])
    
    x = tf.reshape(x, [-1, self.n_anchors * grid_shape[0] * grid_shape[1], self.n_classes + 5])
    strides = (self.img_size[0] // grid_shape[0], self.img_size[1] // grid_shape[1])

    box_centers, box_shapes, confidence, classes = tf.split(x, [2, 2, 1, self.n_classes], axis=-1)

    x_arr = tf.range(grid_shape[0], dtype=tf.float32)
    y_arr = tf.range(grid_shape[1], dtype=tf.float32)
    x_offset, y_offset = tf.meshgrid(x_arr, y_arr)
    x_offset = tf.reshape(x_offset, (-1, 1))
    y_offset = tf.reshape(y_offset, (-1, 1))
    x_y_offset = tf.concat([x_offset, y_offset], axis=-1)
    x_y_offset = tf.tile(x_y_offset, [1, self.n_anchors])
    x_y_offset = tf.reshape(x_y_offset, [1, -1, 2])
    box_centers = tf.nn.sigmoid(box_centers)
    box_centers = (box_centers + x_y_offset) * strides
    anchors = tf.tile(self.anchors, [grid_shape[0] * grid_shape[1], 1])
    box_shapes = tf.exp(box_shapes) * tf.cast(anchors, dtype=tf.float32)
    confidence = tf.nn.sigmoid(confidence)
    classes = tf.nn.sigmoid(classes)
    decode_outputs= tf.concat([box_centers, box_shapes, confidence, classes], axis=-1)
    return conv_output, decode_outputs