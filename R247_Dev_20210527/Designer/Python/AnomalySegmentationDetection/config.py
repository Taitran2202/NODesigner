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
seed = random.randint(0, 2**32)