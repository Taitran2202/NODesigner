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
IMAGE_DIMS = (32,32)
CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
def load_images_from_folder(images_path, anotationDir):
    imagesPath = glob.glob( os.path.join(images_path,"*.jpg")  ) + glob.glob( os.path.join(images_path,"*.png")  ) +  glob.glob( os.path.join(images_path,"*.bmp"))+ glob.glob( os.path.join(images_path,"*.JPEG"))
    seg_path = anotationDir
    images = []
    labels = []
    IMAGE_DIMS = (32,32)
    for i in range(0, len(imagesPath)):
        origin_image = np.asarray(Image.open(imagesPath[i]))
        seg_bnme = os.path.basename(imagesPath[i])
        seg_bnme_full = os.path.join(seg_path,seg_bnme + '.txt')
        with open(seg_bnme_full) as json_file:
                dataread = json.load(json_file)
                data= dataread['characters']
                for p in data:
                    x=int(p['x'])
                    y=int(p['y'])
                    w=int(p['w'])
                    h=int(p['h'])
                    label = CHARS.index(p['annotation'])
                    image = origin_image[y:y+h,x:x+w]
                    image = cv2.resize(image, (IMAGE_DIMS[1],IMAGE_DIMS[0]))                       
                    labels.append(label)
                    images.append(image)
    return images, labels
def train_model(epoch, weights_file_name, img_path,anno_path, num_classes, model_dir, transfer="transfer"):
    EPOCHS_1 = epoch
    INIT_LR_1 = 1e-1
    BS = 8

    EPOCHS_2 = epoch * 2
    INIT_LR_2 = 1e-4
    data,labels = load_images_from_folder(img_path, anno_path)
    data = np.array(data)
    labels = np.array(labels)
    labels = to_categorical(labels, num_classes)
    print(labels)
    aug = ImageDataGenerator(rotation_range = 10, zoom_range = 0.05, width_shift_range = 0.1, height_shift_range = 0.1, 
        shear_range = 0.15, horizontal_flip = False, fill_mode = "nearest")
    if transfer=='transfer':
        print("[INFO] compile model...")
        main_input = Input(shape = (32, 32, 3), name = "main_input",dtype='uint8')
        converted_input = tf.keras.layers.experimental.preprocessing.Rescaling(1/255.0)(main_input)
        base_model  = OcrResnet.build(32, 32, 3, 62, (3, 3, 3), (64, 64, 128, 256), reg = 0.0005)
        checkpoint_filepath = weights_file_name
        base_model.load_weights(checkpoint_filepath)
        base_model.trainable = False;
        print("create new model")
        x = base_model.layers[-2].output
        predictions = Dense(num_classes, activation = "softmax", kernel_regularizer = tf.keras.regularizers.L2(0.0005), name = "class_output")(x)
        
        model =Model(main_input, Model(inputs = base_model.input, outputs = predictions)(converted_input))
        opt = SGD(learning_rate = INIT_LR_1, decay = INIT_LR_1 / EPOCHS_1)
        model.compile(loss = "categorical_crossentropy", optimizer = opt, metrics = ["accuracy"])
        model.fit(aug.flow(data, labels, batch_size = BS), steps_per_epoch = len(data) // BS, epochs = EPOCHS_1, verbose = 1)
        print("fine tuning")
        model.trainable = True;
        opt = SGD(learning_rate = INIT_LR_2, decay = INIT_LR_2 / EPOCHS_2)
        model.compile(loss = "categorical_crossentropy", optimizer = opt, metrics = ["accuracy"])
        model.fit(aug.flow(data, labels, batch_size = BS), steps_per_epoch = len(data) // BS, epochs = EPOCHS_2, verbose = 1)
        for image in data:
            image = image.reshape((1,32,32,3))
            print( CHARS[np.argmax(model.predict(image)[0])])
        tf.saved_model.save(model, model_dir)
        frozen_keras_graph(model_dir)
def load_class_names(file_name):
    with open(file_name, 'r') as f:
        class_names = f.read().splitlines()
    return class_names

def main(argv):
    img_path = argv[0]
    anno_path = argv[1]
    saved_model_dir = argv[2]
    weights_file_name = argv[3]
    epochs = int(argv[4])
    transfer = argv[5]
    num_classes = len(CHARS)
    print(img_path)
    print(anno_path)
    print(saved_model_dir)
    train_model(epochs, weights_file_name, img_path,anno_path, num_classes, saved_model_dir, transfer)

if __name__ == "__main__":
    main(sys.argv[1:])