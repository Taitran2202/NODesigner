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
from dataset import *
from loss import *
from common_modules import *

def load_weights(file_path, model):
  indexes_con2d = []
  indexes_bn = []
  indexes_residual_block_conv2d = []
  indexes_residual_block_bn = []
  index_yolo_head_kernel = []
  index_yolo_head_bias = []
  i = 0
  for var in model.variables:
    if "conv2d_fixed_padding" in var.name.split('/')[0]:
      indexes_con2d.append(i)
    if "batch_norm" in var.name.split("/")[0]:
      indexes_bn.append(i)
    if "residual_block" in var.name.split("/")[0]:
      if "conv2d_fixed_padding" in var.name.split("/")[1]:
        indexes_residual_block_conv2d.append(i)
      elif "batch_norm" in var.name.split("/")[1]:
        indexes_residual_block_bn.append(i)
    if "yolo_head" in var.name.split("/")[0]:
      if "kernel" in var.name.split("/")[-1]:
        index_yolo_head_kernel.append(i)
      else:
        index_yolo_head_bias.append(i)
    i += 1
  
  # get weigths
  with open(file_path, "rb") as f:
      np.fromfile(f, dtype=np.int32, count=5)
      weights = np.fromfile(f, dtype=np.float32)
  
  # load weights for darknet53
  ptr = 0
  for i in range(52):
    if i in indexes_con2d:
      # beta, gamma, mean, variance
      index_con2d = indexes_con2d.index(i)
      index_gamma = index_con2d * 4
      index_beta = index_gamma + 1
      index_mean = index_gamma + 2
      index_variance = index_gamma + 3

      shape = model.variables[indexes_bn[index_beta]].shape
      num_params = np.prod(shape)
      beta_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # num_params = model.variables[indexes_bn[index_beta]].shape[0]
      # beta_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      shape = model.variables[indexes_bn[index_gamma]].shape
      num_params = np.prod(shape)
      gamma_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # gamma_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      shape = model.variables[indexes_bn[index_mean]].shape
      num_params = np.prod(shape)
      mean_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # mean_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      shape = model.variables[indexes_bn[index_variance]].shape
      num_params = np.prod(shape)
      variance_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # variance_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      model.variables[indexes_bn[index_beta]].assign(beta_weigths)
      model.variables[indexes_bn[index_gamma]].assign(gamma_weigths)
      model.variables[indexes_bn[index_mean]].assign(mean_weigths)
      model.variables[indexes_bn[index_variance]].assign(variance_weigths)

      shape = model.variables[i].shape.as_list()
      num_params = np.prod(shape)
      conv_weights = weights[ptr:ptr + num_params].reshape(
                (shape[3], shape[2], shape[0], shape[1]))
      conv_weights = np.transpose(conv_weights, (2, 3, 1, 0))
      ptr += num_params
      model.variables[i].assign(conv_weights)
    if i in indexes_residual_block_conv2d:
      # beta, gamma, mean, variance
      index_residual_block_conv2d = indexes_residual_block_conv2d.index(i)
      index_gamma = index_residual_block_conv2d * 4
      index_beta = index_gamma + 1
      index_mean = index_gamma + 2
      index_variance = index_gamma + 3


      shape = model.variables[indexes_residual_block_bn[index_beta]].shape
      num_params = np.prod(shape)
      beta_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # num_params = model.variables[indexes_residual_block_bn[index_beta]].shape[0]
      # beta_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      shape = model.variables[indexes_residual_block_bn[index_gamma]].shape
      num_params = np.prod(shape)
      gamma_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # gamma_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      shape = model.variables[indexes_residual_block_bn[index_mean]].shape
      num_params = np.prod(shape)
      mean_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # mean_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      shape = model.variables[indexes_residual_block_bn[index_variance]].shape
      num_params = np.prod(shape)
      variance_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # variance_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      model.variables[indexes_residual_block_bn[index_beta]].assign(beta_weigths)
      model.variables[indexes_residual_block_bn[index_gamma]].assign(gamma_weigths)
      model.variables[indexes_residual_block_bn[index_mean]].assign(mean_weigths)
      model.variables[indexes_residual_block_bn[index_variance]].assign(variance_weigths)

      shape = model.variables[i].shape.as_list()
      num_params = np.prod(shape)
      conv_weights = weights[ptr:ptr + num_params].reshape(
                (shape[3], shape[2], shape[0], shape[1]))
      conv_weights = np.transpose(conv_weights, (2, 3, 1, 0))
      ptr += num_params
      model.variables[i].assign(conv_weights)
  
  # load weights for yolo block
  current_indexes_conv2d = indexes_con2d[6:]
  ranges = [range(0, 6), range(6, 13), range(13, 20)]
  for j in range(3):
    for i in ranges[j]:
      # beta, gamma, mean, variance
      index_con2d = i + 6
      index_gamma = index_con2d * 4
      index_beta = index_gamma + 1
      index_mean = index_gamma + 2
      index_variance = index_gamma + 3


      shape = model.variables[indexes_bn[index_beta]].shape
      num_params = np.prod(shape)
      beta_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # num_params = model.variables[indexes_bn[index_beta]].shape[0]
      # beta_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      shape = model.variables[indexes_bn[index_gamma]].shape
      num_params = np.prod(shape)
      gamma_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # gamma_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      shape = model.variables[indexes_bn[index_mean]].shape
      num_params = np.prod(shape)
      mean_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # mean_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      shape = model.variables[indexes_bn[index_variance]].shape
      num_params = np.prod(shape)
      variance_weigths = weights[ptr:ptr + num_params].reshape(shape)
      # variance_weigths = weights[ptr: ptr + num_params]
      ptr += num_params

      model.variables[indexes_bn[index_beta]].assign(beta_weigths)
      model.variables[indexes_bn[index_gamma]].assign(gamma_weigths)
      model.variables[indexes_bn[index_mean]].assign(mean_weigths)
      model.variables[indexes_bn[index_variance]].assign(variance_weigths)

      shape = model.variables[current_indexes_conv2d[i]].shape.as_list()
      num_params = np.prod(shape)
      conv_weights = weights[ptr:ptr + num_params].reshape(
                (shape[3], shape[2], shape[0], shape[1]))
      conv_weights = np.transpose(conv_weights, (2, 3, 1, 0))
      ptr += num_params
      model.variables[current_indexes_conv2d[i]].assign(conv_weights)
    

    num_params = model.variables[index_yolo_head_bias[j]].shape[0]
    bias_weigths = weights[ptr: ptr + num_params]
    ptr += num_params
    model.variables[index_yolo_head_bias[j]].assign(bias_weigths)

    shape = model.variables[index_yolo_head_kernel[j]].shape.as_list()
    num_params = np.prod(shape)
    conv_weights = weights[ptr:ptr + num_params].reshape(
              (shape[3], shape[2], shape[0], shape[1]))
    conv_weights = np.transpose(conv_weights, (2, 3, 1, 0))
    ptr += num_params
    model.variables[index_yolo_head_kernel[j]].assign(conv_weights)