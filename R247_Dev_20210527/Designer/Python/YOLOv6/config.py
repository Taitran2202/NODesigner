import tensorflow as tf
import cv2
import os
import numpy as np
import sys
import random
from easydict import EasyDict as edict
import matplotlib.pyplot as plt
import shutil
import yaml
import colorsys
import tf2onnx
from pathlib import Path
import glob
import math
import json
import time
import xml.etree.ElementTree as ET
#import torch
#import tensorflow_addons as tfa
import warnings

np.random.seed(42)
tf.random.set_seed(42)

'''
------------------------- Configurations for object detection task -------------------------
'''
__C = edict()
cfg = __C

__C.YOLO = edict()
__C.YOLO.YOLOV6_TYPES = "YOLOv6n" #['YOLOv6n', 'YOLOv6t', 'YOLOv6s']
__C.YOLO.YOLOV6_SCALE = dict(
                          YOLOv6n= dict(
                              depth_multiple = 0.33,
                              width_multiple = 0.25
                          ),

                          YOLOv6t= dict(
                              depth_multiple = 0.25,
                              width_multiple = 0.5
                          ),

                          YOLOv6s= dict(
                              depth_multiple = 0.33,
                              width_multiple = 0.5
                          )
                        )

__C.YOLO.YOLOV6_BACKBONE_NUM_REPEATS = [1, 6, 12, 18, 6]
__C.YOLO.YOLOV6_BACKBONE_OUTPUTS_CHANNELS = [64, 128, 256, 512, 1024]
__C.YOLO.YOLOV6_NECK_NUM_REPEATS = [12, 12, 12, 12]
__C.YOLO.YOLOV6_NECK_OUTPUTS_CHANNELS = [256, 128, 128, 256, 256, 512]
__C.YOLO.STRIDES =[8, 16, 32]
__C.YOLO.ANCHORS = np.array([[12,  16], [19,   36], [40,   28],
                             [36,  75], [76,   55], [72,  146],
                             [142,110], [192, 243], [459, 401]], np.float32)
__C.YOLO.ANCHOR_PER_SCALE = 3
__C.YOLO.IOU_LOSS_THRESH = 0.5
__C.YOLO.CLASSES = '/content/drive/MyDrive/lib/dataset/object_detection/annotations/_classes.names'


__C.YOLO.TRAIN = edict()
__C.YOLO.TRAIN.MODEL_SIZE = (640, 640)
__C.YOLO.TRAIN.EPOCHS = 100
__C.YOLO.TRAIN.TRAIN_LOG_DIR = '/content/drive/MyDrive/lib_tensorflow/log'
__C.YOLO.TRAIN.ANNOT_PATH = "/content/drive/MyDrive/lib/dataset/object_detection/annotations/_annotations.txt"
__C.YOLO.TRAIN.SAVED_MODEL_DIR = "/content/drive/MyDrive/lib_tensorflow/saved_model"
__C.YOLO.TRAIN.WEIGHTS_PATH = "/content/drive/MyDrive/lib_tensorflow/checkpoint"
__C.YOLO.TRAIN.PRETRAINED_WEIGHT_PATH_FOR_YOLOV6 = ""
__C.YOLO.TRAIN.DATA_AUG = True
__C.YOLO.TRAIN.USE_MOSAIC_IMG = False
__C.YOLO.TRAIN.LR_INIT = 1e-2
__C.YOLO.TRAIN.LR_END = 1e-6
__C.YOLO.TRAIN.BATCH_SIZE = 16
__C.YOLO.TRAIN.LEARNING_RATE_LEVELS = 2
__C.YOLO.TRAIN.LEARNING_RATE_STEPS = 2
__C.YOLO.TRAIN.LABEL_SMOOTHING = 0.02
__C.YOLO.TRAIN.CONF_THRESHOLD = 0.4
__C.YOLO.TRAIN.IOU_THRESHOLD = 0.5
__C.YOLO.TRAIN.OPTIMIZERTYPE= 'adam'
__C.YOLO.TRAIN.VISUAL_LEARNING_PROCESS=True
__C.YOLO.TRAIN.WARMUP_LEARNING_RATE = 1e-6
__C.YOLO.TRAIN.WARMUP_EPOCHS = 2
__C.YOLO.TRAIN.WARMUP_STEPS = 0
__C.YOLO.TRAIN.SCALE_RANGE = (0.5, 0.5)
__C.YOLO.TRAIN.ADD_IMG_PATH = "/content/drive/MyDrive/lib/dataset/object_detection/train/"
__C.YOLO.TRAIN.TRANSFER = 'scratch'
__C.YOLO.TRAIN.LOSS_TYPE_FOR_BBOXES = 'siou'
__C.YOLO.TRAIN.RESULT_DIR = ""
__C.YOLO.TRAIN.IMG_DIR = ""

def get_config_from_json_file(filename):
    if filename is None:
        filename = './config/yolov6.yaml'
    with open(filename, 'r') as f:
        config_json = json.load(f)
        config = edict(config_json)
        get_config_from_edict(config)
def get_config_from_edict(edict):
    __C.YOLO.TRAIN.MODEL_SIZE          = (edict.INPUT_WIDTH,edict.INPUT_HEIGHT) or (640, 640)
    __C.YOLO.TRAIN.LR_INIT = edict.LR_INIT or 1e-3
    __C.YOLO.TRAIN.LR_END = edict.LR_END or 1e-6
    __C.YOLO.TRAIN.LEARNING_RATE_LEVELS = edict.LEARNING_RATE_LEVELS or 2
    __C.YOLO.TRAIN.LEARNING_RATE_STEPS = edict.LEARNING_RATE_STEPS or 2
    __C.YOLO.TRAIN.LABEL_SMOOTHING = edict.LABEL_SMOOTHING or 0.02
    __C.YOLO.TRAIN.WARMUP_LEARNING_RATE = edict.WARMUP_LEARNING_RATE or 1e-6
    __C.YOLO.TRAIN.WARMUP_EPOCHS = edict.WARMUP_EPOCHS or 2
    __C.YOLO.TRAIN.WARMUP_STEPS = edict.WARMUP_STEPS or 0
    #required parameters
    __C.YOLO.CLASSES = edict.Classes or ""
    __C.YOLO.TRAIN.EPOCHS = edict.EPOCHS or 20
    __C.YOLO.TRAIN.ANNOT_PATH = edict.AnnotationDir or ""
    __C.YOLO.TRAIN.SAVED_MODEL_FOLDER = edict.SavedModelDir or ''
    __C.YOLO.TRAIN.WEIGHTS_PATH = edict.WeightDir or ""
    __C.YOLO.TRAIN.PRETRAINED_WEIGHT_PATH_FOR_YOLOV6 = edict.PreTrainModelPath or ""
    __C.YOLO.TRAIN.TRAIN_LOG_DIR = edict.TrainLogDir or ''
    __C.YOLO.TRAIN.DATA_AUG = edict.DATA_AUG or True
    __C.YOLO.TRAIN.USE_MOSAIC_IMG = edict.UseMosaicImage or False
    __C.YOLO.TRAIN.BATCH_SIZE = edict.BATCH_SIZE or 8
    __C.YOLO.TRAIN.OPTIMIZERTYPE = edict.Optimizer or 'adam'
    __C.YOLO.TRAIN.TRANSFER = edict.TrainningType or 'transfer'
    __C.YOLO.TRAIN.VISUAL_LEARNING_PROCESS = edict.VisualizeLearningProcess or True
    __C.YOLO.TRAIN.RESULT_DIR = edict.ResultDir or ""
    __C.YOLO.TRAIN.IMG_DIR = edict.ImageDir or ""
    __C.YOLO.YOLOV6_TYPES  = edict.ModelType or "YOLOv6n"
    __C.YOLO.TRAIN.LOSS_TYPE_FOR_BBOXES = edict.LOSS_BBOXES_TYPE or 'siou'
    __C.YOLO.TRAIN.IMG_DIR = edict.ImageDir or ""