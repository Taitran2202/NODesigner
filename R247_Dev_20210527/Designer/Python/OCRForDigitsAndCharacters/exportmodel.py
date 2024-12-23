import glob
import numpy as np
import random
from cv2 import resize
import tensorflow as tf
from matplotlib.image import imread
from matplotlib.pyplot import imshow
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import LabelEncoder
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report
from imutils import paths
import cv2
import os
import sys
import matplotlib.pyplot as plt
from tensorflow.python.framework.convert_to_constants import convert_variables_to_constants_v2
from tensorflow.keras.preprocessing.image import ImageDataGenerator
from tensorflow.keras.preprocessing.image import img_to_array
from tensorflow.keras.optimizers import SGD
from tensorflow.keras.optimizers import Adam
from tensorflow.keras.utils import to_categorical
from PIL import Image
from model import *
import json
IMAGE_DIMS = (32,32)
def frozen_keras_graph(model_dir):
    frozen_path = os.path.join(model_dir,'frozen_graph.pb')
    model = tf.keras.models.load_model(model_dir, compile=False)
    infer = model.signatures['serving_default']
    full_model = tf.function(infer).get_concrete_function(
        tf.TensorSpec(infer.inputs[0].shape, infer.inputs[0].dtype))
    # Get frozen ConcreteFunction
    frozen_func = convert_variables_to_constants_v2(full_model)
    tf.io.write_graph(frozen_func.graph, '.', frozen_path,as_text=False)
    print('Successfully export frozen graph in ',str(model_dir))
CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
main_input = Input(shape = (32, 32, 3), name = "main_input",dtype='uint8')
converted_input = tf.keras.layers.experimental.preprocessing.Rescaling(1/255.0)(main_input)
base_model  = OcrResnet.build(32, 32, 3, 62, (3, 3, 3), (64, 64, 128, 256), reg = 0.0005)
checkpoint_filepath = 'weights/ocr_resnet50.h5'
base_model.load_weights(checkpoint_filepath)
model =Model(main_input, base_model(converted_input))
tf.saved_model.save(model, "uint8model")
frozen_keras_graph("uint8model")
