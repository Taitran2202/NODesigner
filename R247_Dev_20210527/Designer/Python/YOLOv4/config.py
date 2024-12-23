import tensorflow as tf
import cv2
import os
import numpy as np
import sys
import random
from tensorflow import keras
from tensorflow.keras import layers, models
from easydict import EasyDict as edict
from tensorflow.keras.regularizers import l2
from tensorflow.python.framework.convert_to_constants import convert_variables_to_constants_v2
import matplotlib.pyplot as plt
import shutil
import yaml
import colorsys
import glob
import json
import math
np.random.seed(1919)
tf.random.set_seed(1949)

BACKBONE_LIST = ['darknet53', 'csp_darknet53', 'darknet19', 'csp_darknet19', 'resnest50', 'resnest101', 'resnest200', 'resnest269',
                 'resnet50', 'resnet101', 'resnet152', 'resnet50_v2', 'resnet101_v2', 'resnet152_v2', 'resnet18_pytorch', 'resnet34_pytorch',
                 'resnet50_pytorch', 'resnet101_pytorch', 'resnet152_pytorch', 'resnext50_32x4d_pytorch', 'resnext101_32x8d_pytorch',
                 'wide_resnet50_2_pytorch', 'wide_resnet101_2_pytorch', 'mobilenet', 'mobilenet_v2', 'mobilenet_v3_small', 'mobilenet_v3_large',
                 'densenet121', 'densenet169', 'densenet201', 'effnet_b0', 'effnet_b1', 'effnet_b2', 'effnet_b3', 'effnet_b4', 'effnet_b5',
                 'effnet_b6', 'effnet_b7', 'effnet_lite_b0', 'effnet_lite_b1', 'effnet_lite_b2', 'effnet_lite_b3', 'effnet_lite_b4', 'effnet_v2_b0',
                 'effnet_v2_b1', 'effnet_v2_b2', 'effnet_v2_b3', 'effnet_v2_s', 'effnet_v2_m', 'effnet_v2_l', 'vgg16', 'vgg19']
NECK_LIST = ['FPN', 'PANet', 'bifpn']

HEAD_LIST_FOR_OBJECT_DETECTION = ['YOLOv3Head', 'YOLOv5Head', 'R_YOLOv3Head', 'P_YOLOv3Head', 'R_YOLOv4Head', 'P_YOLOv4Head', 'R_YOLOv5Head', 'P_YOLOv5Head']

HEAD_LIST_FOR_ANOMALY_DETECTION = ['PADIM', 'PatchCore', 'RD4AD', 'VAE']

HEAD_LIST_FOR_SEGMENTATION = ['Unet', 'Deeplabv3']

HEAD_LIST_FOR_TEXT_DETECTION = ['CRAFT', '']

HEAD_LIST_FOR_TEXT_RECOGNITATION = ['CRNN', 'PADDLE']

__C                           = edict()
cfg                           = __C
__C.YOLO                      = edict()


__C.YOLO.CLASSES = "/content/drive/MyDrive/Data/YOLOv5/dataset/classes.names"
__C.YOLO.NUM_CLASSES = 80
__C.YOLO.STRIDES = [8, 16, 32]
__C.YOLO.ANCHOR_PER_SCALE = 3
__C.YOLO.IOU_LOSS_THRESH = 0.5
__C.YOLO.ANCHORS = np.array([[12,  16], [19,   36], [40,   28],
                             [36,  75], [76,   55], [72,  146],
                             [142,110], [192, 243], [459, 401]], np.float32)

__C.YOLO.ANCHORS_TINY = np.array([[10, 14], [23, 27],[37, 58],
                                  [81,  82], [135, 169], [344, 319]], np.float32)
__C.YOLO.TRAIN_YOLO_TINY = False


__C.YOLO.TRAIN                     = edict()

__C.YOLO.TRAIN.MODEL_SIZE = (608, 608)
__C.YOLO.TRAIN.EPOCHS = 20
__C.YOLO.TRAIN.ANNOT_PATH = "/content/drive/MyDrive/Data/YOLOv5/dataset/output/train.txt"
__C.YOLO.TRAIN.SAVED_MODEL_FOLDER = "/content/drive/MyDrive/YOLOv4/saved_model"
__C.YOLO.TRAIN.WEIGHTS_PATH = "/content/drive/MyDrive/YOLOv4/weights"
__C.YOLO.TRAIN.PRETRAINED_WEIGHT_PATH = "/content/drive/MyDrive/YOLOv4/base_model/yolov4_tiny/yolov4-tiny.weights"
__C.YOLO.TRAIN.TRAIN_LOG_DIR = '/content/drive/MyDrive/YOLOv4/log'
__C.YOLO.TRAIN.DATA_AUG = True
__C.YOLO.TRAIN.USE_MOSAIC_IMG = False
__C.YOLO.TRAIN.LR_INIT = 1e-2
__C.YOLO.TRAIN.LR_END = 1e-6
__C.YOLO.TRAIN.BATCH_SIZE = 1
__C.YOLO.TRAIN.LEARNING_RATE_LEVELS = 2
__C.YOLO.TRAIN.LEARNING_RATE_STEPS = 2
__C.YOLO.TRAIN.LABEL_SMOOTHING = 0.02
__C.YOLO.TRAIN.CONF_THRESHOLD = 0.4
__C.YOLO.TRAIN.IOU_THRESHOLD = 0.5
__C.YOLO.TRAIN.OPTIMIZERTYPE= 'adam'
__C.YOLO.TRAIN.LOSS_TYPE_FOR_BBOXES = 'ciou'
__C.YOLO.TRAIN.VISUAL_LEARNING_PROCESS=True
__C.YOLO.TRAIN.WARMUP_LEARNING_RATE = 1e-6
__C.YOLO.TRAIN.WARMUP_EPOCHS = 2
__C.YOLO.TRAIN.WARMUP_STEPS = 0
__C.YOLO.TRAIN.SCALE_RANGE = (0.5, 0.5)
__C.YOLO.TRAIN.ADD_IMG_PATH = ""
__C.YOLO.TRAIN.TRANSFER = 'transfer'
__C.YOLO.TRAIN.RESULT_DIR = ""
__C.YOLO.TRAIN.IMG_DIR = ""
__C.MODEL_TYPE = "YOLO"
def get_config_from_json_file(filename):
    if filename is None:
        filename = './config/yolov4.yaml'
    with open(filename, 'r') as f:
        config_json = json.load(f)
        config = edict(config_json)
        get_config_from_edict(config)
def get_config_from_edict(edict):
    __C.YOLO.TRAIN.MODEL_SIZE          = (edict.INPUT_WIDTH,edict.INPUT_HEIGHT) or (640, 640)
    __C.YOLO.TRAIN.LR_INIT = edict.LR_INIT or 1e-2
    __C.YOLO.TRAIN.LR_END = edict.LR_END or 1e-6
    __C.YOLO.TRAIN.LEARNING_RATE_LEVELS = edict.LEARNING_RATE_LEVELS or 2
    __C.YOLO.TRAIN.LEARNING_RATE_STEPS = edict.LEARNING_RATE_STEPS or 2
    __C.YOLO.TRAIN.LABEL_SMOOTHING = edict.LABEL_SMOOTHING or 0.02
    __C.YOLO.TRAIN.WARMUP_LEARNING_RATE = edict.WARMUP_LEARNING_RATE or 1e-6
    __C.YOLO.TRAIN.WARMUP_EPOCHS = edict.WARMUP_EPOCHS or 2
    __C.YOLO.TRAIN.WARMUP_STEPS = edict.WARMUP_STEPS or 0
    #__C.YOLO.TRAIN.SCALE_RANGE = edict.SCALE_RANGE or (0.5, 0.5)
    #__C.YOLO.TRAIN.ADD_IMG_PATH = edict.ADD_IMG_PATH or ""
    #required parameters
    __C.YOLO.CLASSES = edict.Classes or "/content/drive/MyDrive/Data/YOLOv5/dataset/classes.names"
    __C.YOLO.TRAIN_YOLO_TINY = edict.TRAIN_YOLO_TINY or False
    __C.YOLO.TRAIN.EPOCHS = edict.EPOCHS or 20
    __C.YOLO.TRAIN.ANNOT_PATH = edict.AnnotationDir or "/content/drive/MyDrive/Data/YOLOv5/dataset/output/train.txt"
    __C.YOLO.TRAIN.SAVED_MODEL_FOLDER = edict.SavedModelDir or "/content/drive/MyDrive/YOLOv4/saved_model"
    __C.YOLO.TRAIN.WEIGHTS_PATH = edict.WeightDir or "/content/drive/MyDrive/YOLOv4/weights"
    __C.YOLO.TRAIN.PRETRAINED_WEIGHT_DIR = edict.PreTrainModelPath or "/content/drive/MyDrive/YOLOv4/base_model/yolov4_tiny/yolov4-tiny.weights"
    __C.YOLO.TRAIN.TRAIN_LOG_DIR = edict.TrainLogDir or '/content/drive/MyDrive/YOLOv4/log'
    __C.YOLO.TRAIN.DATA_AUG = edict.DATA_AUG or True
    __C.YOLO.TRAIN.USE_MOSAIC_IMG = edict.UseMosaicImage or False
    __C.YOLO.TRAIN.BATCH_SIZE = edict.BATCH_SIZE or 1
    __C.YOLO.TRAIN.OPTIMIZERTYPE = edict.Optimizer or 'adam'
    __C.YOLO.TRAIN.TRANSFER = edict.TrainningType or 'transfer'
    __C.YOLO.TRAIN.VISUAL_LEARNING_PROCESS = edict.VisualizeLearningProcess or True
    __C.YOLO.TRAIN.RESULT_DIR = edict.ResultDir or ""
    __C.YOLO.TRAIN.IMG_DIR = edict.ImageDir or ""
    __C.MODEL_TYPE = edict.ModelType or "YOLO"
    