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

__C                           = edict()

cfg                           = __C

__C.BATCH_NORM_DECAY = 0.9

__C.BATCH_NORM_EPSILON = 1e-05

__C.LEAKY_RELU = 0.1

__C.IOU_LOSS_THRESH = 0.5

__C.MAX_OUTPUT_SIZE = 20

__C.IOU_THRESHOLD = 0.5

__C.CONFIDENCE_THRESHOLD = 0.5



# YOLO options
__C.YOLO                      = edict()

# Set the class name
__C.YOLO.CLASSES              = "/content/drive/MyDrive/Data/YOLOv5/dataset/classes.names"
__C.YOLO.ANCHORS              = [(10, 13), (16, 30), (33, 23),
                                    (30, 61), (62, 45), (59, 119),
                                    (116, 90), (156, 198), (373, 326)]
__C.YOLO.STRIDES              = [8, 16, 32]
__C.YOLO.ANCHOR_PER_SCALE     = 3

# Train options
__C.TRAIN                     = edict()

__C.TRAIN.ANNOT_PATH          = "/content/drive/MyDrive/Data/YOLOv5/dataset/output/train.txt"
__C.TRAIN.IMG_PATH            = "/content/drive/MyDrive/Data/YOLOv5/dataset/images/train"
__C.TRAIN.BATCH_SIZE          = 1
# __C.TRAIN.INPUT_SIZE            = [320, 352, 384, 416, 448, 480, 512, 544, 576, 608]
__C.TRAIN.INPUT_HEIGHT        = 416
__C.TRAIN.INPUT_WIDTH         = 416
__C.TRAIN.DATA_AUG            = True
__C.TRAIN.LR_INIT             = 3e-4
__C.TRAIN.LR_END              = 1e-6
__C.TRAIN.WARMUP_EPOCHS       = 2
__C.TRAIN.EPOCHS              = 40


# TEST options
__C.TEST                      = edict()

__C.TEST.ANNOT_PATH           = "./data/dataset/test_mask_annotations.txt"
__C.TEST.BATCH_SIZE           = 1
__C.TEST.INPUT_SIZE           = 416
__C.TEST.DATA_AUG             = False
__C.TEST.DECTECTED_IMAGE_PATH = "./data/eval_detection/"
__C.TEST.SCORE_THRESHOLD      = 0.3
__C.TEST.IOU_THRESHOLD        = 0.5

def get_config_from_edict(edict):
    # general config
    __C.BATCH_NORM_DECAY          = edict.BATCH_NORM_DECAY or 0.9
    __C.BATCH_NORM_EPSILON        = edict.BATCH_NORM_EPSILON or 1e-05
    __C.LEAKY_RELU                = edict.LEAKY_RELU or 0.1
    __C.IOU_LOSS_THRESH           = edict.IOU_LOSS_THRESH or 0.5
    __C.MAX_OUTPUT_SIZE           = edict.MAX_OUTPUT_SIZE or 20
    __C.IOU_THRESHOLD             = edict.IOU_THRESHOLD or 0.5
    __C.CONFIDENCE_THRESHOLD      = edict.CONFIDENCE_THRESHOLD or 0.5
    # yolo config
    __C.YOLO.CLASSES              = edict.Classes or "/content/drive/MyDrive/Data/YOLOv5/dataset/classes.names"
    __C.YOLO.STRIDES              = edict.STRIDES or [8, 16, 32]
    __C.YOLO.ANCHOR_PER_SCALE     = edict.ANCHOR_PER_SCALE or 3
    __C.YOLO.IOU_LOSS_THRESH      = edict.IOU_LOSS_THRESH or 0.5
    # train config
    __C.TRAIN.ANNOT_PATH          = edict.AnnotationDir or "/content/drive/MyDrive/Data/YOLOv5/dataset/output/train.txt"
    __C.TRAIN.IMG_PATH            = edict.ImageDir or "/content/drive/MyDrive/Data/YOLOv5/dataset/images/train"
    __C.TRAIN.BATCH_SIZE          = edict.BATCH_SIZE or 1
    __C.TRAIN.INPUT_HEIGHT        = edict.INPUT_HEIGHT or 416
    __C.TRAIN.INPUT_WIDTH         = edict.INPUT_WIDTH or 416
    __C.TRAIN.DATA_AUG            = edict.DATA_AUG or True
    __C.TRAIN.LR_INIT             = edict.LR_INIT or 3e-4
    __C.TRAIN.LR_END              = edict.LR_END or 1e-6
    __C.TRAIN.WARMUP_EPOCHS       = edict.WARMUP_EPOCHS or 2
    __C.TRAIN.EPOCHS              = edict.EPOCHS or 40
    __C.TRAIN.FINETUNE_EPOCHS     = edict.FINETUNE_EPOCHS or 40
