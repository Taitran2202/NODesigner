import cv2
import os
import matplotlib.pyplot as plt
from pathlib import Path
import numpy as np
import math
import argparse
from easydict import EasyDict as edict
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
import glob
import shutil
from sklearn.preprocessing import LabelBinarizer
import warnings
from albumentations import HorizontalFlip, VerticalFlip, Rotate
from tqdm import tqdm
import json
import sys
from numba import cuda 



np.random.seed(1919)
tf.random.set_seed(1949)

initializer = tf.random_normal_initializer(stddev=0.01)
warnings.filterwarnings('ignore')
os.environ['TF_FORCE_GPU_ALLOW_GROWTH'] = 'true'
tf.config.run_functions_eagerly(True)

__C                           = edict()
cfg                           = __C

__C.IMG_SIZE                  = (512, 512)
__C.CHANNELS                  = 1
__C.BATCH_SIZE                = 16
__C.EPOCHS_FOR_SEG			  = 10
__C.EPOCHS_FOR_CLS			  = 5
__C.IMAGE_DIR                 = ""
__C.MASKS_DIR                 = ""
__C.CHECKPOINT_PATH           = ""
__C.SAVED_MODEL_DIR           = ""
__C.RESULT_DIR				  = ""
__C.OPTIMIZER_TYPE            = "adam"
__C.MODEL_OPTIONS             = "SExp1"
__C.LR_DECAY_RATE		      = 0.94
__C.LR_DECAY_STEPS            = 65
__C.LR_INIT                   = 1e-4
__C.VISUAL_LEARNING_PROCESS   = True
__C.TRANSFER                  = "scratch"
__C.TRAIN_SEG				  = True
__C.ROTATION_MAX			  = 45
__C.AUGMENT					  = True



def get_config_from_json_file(filename):
    with open(filename, 'r') as f:
        config_json = json.load(f)
        config = edict(config_json)
        get_config_from_edict(config)

def get_config_from_edict(edict):
    __C.IMG_SIZE = (edict.ImageHeight, edict.ImageWidth)
    __C.CHANNELS = edict.Channels
    __C.BATCH_SIZE = edict.BatchSize
    __C.IMAGE_DIR = edict.ImagesDir
    __C.MASKS_DIR = edict.MasksDir
    __C.CHECKPOINT_PATH = edict.CheckpointDir
    __C.SAVED_MODEL_DIR = edict.SavedModelDir
    __C.RESULT_DIR = edict.ResultDir
    __C.AUGMENT = edict.Augmentation
    __C.TRAIN_SEG = edict.TrainSeg
    __C.VISUAL_LEARNING_PROCESS = edict.Monitor
    __C.EPOCHS_FOR_SEG = edict.EpochsForSeg
    __C.EPOCHS_FOR_CLS = edict.EpochsForCls
    __C.MODEL_OPTIONS = edict.ModelOpitions
