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
from utils import *
from dataset import *
from loss import *
from common_modules import *


class YoloBody(layers.Layer):
  def __init__(self, training, input_tensor, data_format=None, **kwargs):
    super(YoloBody, self).__init__(**kwargs)
    self.training = training
    self.data_format = K.image_data_format() if not data_format else data_format
    self.input_tensor = input_tensor
    self.modules = self._get_model(input_tensor)

  def call(self, x):
    return self.modules(x)

  def _get_model(self, input_tensor):
    input_shape = input_tensor.shape
    input_tensor = keras.Input(shape=((input_shape[1], input_shape[2], input_shape[3])))
    if self.data_format == 'channels_first':
      input_tensor = tf.transpose(input_tensor, [0, 3, 1, 2])
    output = []
    route1, route2, x = darknet53(training=self.training, data_format=self.data_format, input_tensor=self.input_tensor)(input_tensor)
    route, x = yolo_conv_block(filters=512, training=self.training, data_format=self.data_format, output_tensor_prev=x)(x)
    output.append(x)

    x = conv2d_fixed_padding(filters=256, kernel_size=1, data_format=self.data_format)(route)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)
    upsample_size = route2.get_shape().as_list()
    x = upsample(out_shape=upsample_size, data_format=self.data_format)(x)
    axis = 1 if self.data_format == 'channels_first' else 3
    x = tf.concat([x, route2], axis=axis)
    route, x = yolo_conv_block(filters=256, training=self.training, data_format=self.data_format, output_tensor_prev=x)(x)
    output.append(x)

    x = conv2d_fixed_padding(filters=128, kernel_size=1, data_format=self.data_format)(route)
    x = batch_norm(training=self.training, data_format=self.data_format)(x)
    x = layers.LeakyReLU(alpha=cfg.LEAKY_RELU)(x)
    upsample_size = route1.get_shape().as_list()
    x = upsample(out_shape=upsample_size, data_format=self.data_format)(x)
    x = tf.concat([x, route1], axis=axis)
    route, x = yolo_conv_block(filters=128, training=self.training, data_format=self.data_format, output_tensor_prev=x)(x)
    output.append(x)

    return keras.Model(inputs=input_tensor, outputs=output)

class YoloHead(layers.Layer):
  def __init__(self, n_classes, model_size, anchors, data_format=None, **kwargs):
    super(YoloHead, self).__init__(**kwargs)
    self.n_classes = n_classes
    self.model_size = model_size
    self.data_format = K.image_data_format() if not data_format else data_format
    self.anchors = anchors
    self.detect_layer1 = detect_layer(n_classes=self.n_classes, anchors=self.anchors[6:9],
                           img_size=self.model_size, data_format=self.data_format, name="detect1")
    self.detect_layer2 = detect_layer(n_classes=self.n_classes, anchors=self.anchors[3:6],
                           img_size=self.model_size, data_format=self.data_format, name="detect2")
    self.detect_layer3 = detect_layer(n_classes=self.n_classes, anchors=self.anchors[0:3],
                           img_size=self.model_size, data_format=self.data_format, name="detect3")  
  
  def call(self, x):
    detect1_input, detect2_input, detect3_input = x
    conv_bbox1, detect1 = self.detect_layer1(detect1_input)
    conv_bbox2, detect2 = self.detect_layer2(detect2_input)
    conv_bbox3, detect3 = self.detect_layer3(detect3_input)
    detects = tf.concat([detect1, detect2, detect3], axis=1)
    return detects, [conv_bbox1, conv_bbox2, conv_bbox3], [detect1, detect2, detect3]

  
  

def get_yolo_model(n_classes, model_size, anchors, training):
  data_format = K.image_data_format()

  if data_format == 'channels_first':
    inputs = keras.Input(shape=(3, model_size[0], model_size[1]), name="yolo_input")
  else:
    inputs = keras.Input(shape=(model_size[0], model_size[1], 3), name="yolo_input")

  x = YoloBody(training, inputs, data_format, name="yolo_body")(inputs)

  outputs = YoloHead(n_classes, model_size, anchors, data_format, name="yolo_head")(x)
  
  
  return keras.Model(inputs=inputs, outputs=outputs)