import glob
import numpy as np
import random
from cv2 import resize
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

from tensorflow.keras.preprocessing.image import ImageDataGenerator
from tensorflow.keras.preprocessing.image import img_to_array
from tensorflow.keras.optimizers import SGD
from tensorflow.keras.optimizers import Adam
from tensorflow.keras.utils import to_categorical

from model import *


#CHARS = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"

def train_model(epoch, weights_file_name, anno_path, num_classes, model_dir, transfer="transfer"):
    EPOCHS_1 = epoch
    INIT_LR_1 = 1e-1
    BS = 16

    EPOCHS_2 = epoch * 2
    INIT_LR_2 = 1e-4
    data = []
    labels = []

    with open(anno_path, 'r') as f:
        txt = f.readlines()
        for j in range(len(txt)):
            subString = txt[j].split(' ')
            img_path = subString[0] + ' ' + subString[1]
            img = cv2.imread(img_path)
            img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
        
            for i in range(len(subString) - 2):
                x1 = int(subString[i+2].split(',')[0])
                y1 = int(subString[i+2].split(',')[1])
                x2 = int(subString[i+2].split(',')[2])
                y2 = int(subString[i+2].split(',')[3])
                label = int(subString[i+2].split(',')[4])
                image = img[y1:y2, x1:x2]
                image_resized = cv2.resize(image, (32, 32))
                image = img_to_array(image_resized)
                data.append(image)
                labels.append(label)

    data = np.array(data, dtype = "float") / 255.0
    labels = np.array(labels)
    le = LabelEncoder().fit(labels)
    labels = to_categorical(le.transform(labels), num_classes)

    aug = ImageDataGenerator(rotation_range = 10, zoom_range = 0.05, width_shift_range = 0.1, height_shift_range = 0.1, 
        shear_range = 0.15, horizontal_flip = False, fill_mode = "nearest")
    if transfer=='transfer':
        print("[INFO] compile model...")
        base_model  = OcrResnet.build(32, 32, 3, 62, (3, 3, 3), (64, 64, 128, 256), reg = 0.0005)
        checkpoint_filepath = weights_file_name
        base_model.load_weights(checkpoint_filepath)
        base_model.trainable = False;
        print("create new model")
        x = base_model.layers[-2].output
        predictions = Dense(num_classes, activation = "softmax", kernel_regularizer = tf.keras.regularizers.L2(0.0005), name = "class_output")(x)
        model = Model(inputs = base_model.input, outputs = predictions)
        opt = SGD(learning_rate = INIT_LR_1, decay = INIT_LR_1 / EPOCHS_1)
        model.compile(loss = "categorical_crossentropy", optimizer = opt, metrics = ["accuracy"])
        model.fit(aug.flow(data, labels, batch_size = BS), steps_per_epoch = len(data) // BS, epochs = EPOCHS_1, verbose = 1)
        print("fine tuning")
        model.trainable = True;
        opt = SGD(learning_rate = INIT_LR_2, decay = INIT_LR_2 / EPOCHS_2)
        model.compile(loss = "categorical_crossentropy", optimizer = opt, metrics = ["accuracy"])
        model.fit(aug.flow(data, labels, batch_size = BS), steps_per_epoch = len(data) // BS, epochs = EPOCHS_2, verbose = 1)
        model.save(model_dir)

def load_class_names(file_name):
    with open(file_name, 'r') as f:
        class_names = f.read().splitlines()
    return class_names

def main(argv):
    anno_path = argv[0]
    saved_model_dir = argv[1]
    weights_file_name = argv[2]
    epochs = int(argv[3])
    transfer = argv[4]
    num_classes = len(load_class_names(argv[5]))
    train_model(epochs, weights_file_name, anno_path, num_classes, saved_model_dir, transfer)

if __name__ == "__main__":
    main(sys.argv[1:])