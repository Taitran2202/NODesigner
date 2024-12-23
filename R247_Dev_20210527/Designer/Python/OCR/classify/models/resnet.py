import tensorflow as tf
import os
from tensorflow.keras.layers import *
from tensorflow.keras.models import Model
IMAGE_ORDERING = 'channels_last'
CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
class OCRResnet():
    def __init__(self,input_width,input_height):
        self.input_width=input_width
        self.input_height=input_height
        self.pretrainPath = 'weights/resnet/ocr_resnet50.h5'
        self.num_classes = len(CHARS)
        #Custom loss fucntion
    def build(self):
        main_input = Input(shape = (32, 32, 3), name = "main_input",dtype='float32')
        input_preprocess = main_input/255.0
        pretrain_model  = Resnet.build(self.input_width, self.input_height, 3, 62, (3, 3, 3), (64, 64, 128, 256), reg = 0.0005)
        pretrain_model.load_weights(self.pretrainPath)
        #pretrain_model.trainable = False
        for layer in pretrain_model.layers[:-2]:
            layer.trainable = False
        pretrain_model.layers[-1]._name = 'class_output'
        self.model =Model(main_input, pretrain_model(input_preprocess), name = "OCRResnet")
        return self.model
    def GetLabels(self):
        return list(CHARS)
class OCRResnetBase():
    def __init__(self,input_width,input_height):
        self.input_width=input_width
        self.input_height=input_height
        self.pretrainPath = 'weights/resnet/ocr_resnet50.h5'
        self.num_classes = len(CHARS)
        #Custom loss fucntion
    def build(self):
        main_input = Input(shape = (32, 32, 3), name = "main_input",dtype='float32')
        input_preprocess = main_input/255.0
        pretrain_model  = Resnet.build(self.input_width, self.input_height, 3, 62, (3, 3, 3), (64, 64, 128, 256), reg = 0.0005)
        pretrain_model.load_weights(self.pretrainPath)
        pretrain_model.trainable = False
        self.model =Model(main_input, pretrain_model(input_preprocess), name = "OCRResnet")
        return self.model
    def GetLabels(self):
        return list(CHARS)
class Resnet:
    @staticmethod
    def residual_module(data, K, stride, chanDim, red = False, reg = 0.001, bnEps = 2e-5, bnMom = 0.9):
        shortcut = data

        bn1 = BatchNormalization(axis = chanDim, epsilon = bnEps, momentum = bnMom)(data)
        act1 = Activation("relu")(bn1)
        conv1 = Conv2D(int(K * 0.25), (1, 1), use_bias = False, kernel_regularizer = tf.keras.regularizers.L2(reg))(act1)
        
        bn2 = BatchNormalization(axis = chanDim, epsilon = bnEps, momentum = bnMom)(conv1)
        act2 = Activation("relu")(bn2)
        conv2 = Conv2D(int(K * 0.25), (3, 3), strides = stride, padding = "same", use_bias = False, kernel_regularizer = tf.keras.regularizers.L2(reg))(act2)
        
        bn3 = BatchNormalization(axis = chanDim, epsilon = bnEps, momentum = bnMom)(conv2)
        act3 = Activation("relu")(bn3)
        conv3 = Conv2D(K, (1, 1), use_bias = False, kernel_regularizer = tf.keras.regularizers.L2(reg))(act3)
        
        if red:
            shortcut = Conv2D(K, (1, 1), strides = stride, use_bias = False, kernel_regularizer = tf.keras.regularizers.L2(reg))(act1)
        
        x = Add()([conv3, shortcut])

        return x

    @staticmethod
    def build(width, height, depth, classes, stages, filters, reg = 0.0001, bnEps = 2e-5, bnMom = 0.9):
        inputShape = (height, width, depth)
        chanDim = -1

        if IMAGE_ORDERING == "channels_first":
            inputShape = (depth, height, width)
            chanDim = 1

        inputs = Input(shape = inputShape, name = "ocr_input")
        x = BatchNormalization(axis = chanDim, epsilon = bnEps, momentum = bnMom)(inputs)
        x = Conv2D(filters[0], (3, 3), use_bias = False, padding = "same", kernel_regularizer = tf.keras.regularizers.L2(reg))(x)
        
        for i in range(0, len(stages)):
            
            stride = (1, 1) if i == 0 else (2, 2)
            x = Resnet.residual_module(x, filters[i + 1], stride, chanDim, red = True, bnEps = bnEps, bnMom = bnMom)
            
            for j in range(0, stages[i] - 1):
                x = Resnet.residual_module(x, filters[i + 1], (1, 1), chanDim, bnEps = bnEps, bnMom = bnMom)
        
        x = BatchNormalization(axis = chanDim, epsilon = bnEps, momentum = bnMom)(x)
        x =  Activation("relu")(x)
        x = AveragePooling2D((8, 8))(x)

        x = Flatten()(x)
        x = Dense(classes, activation = "softmax", kernel_regularizer = tf.keras.regularizers.L2(reg), name = "class_output")(x)

        model = Model(inputs, x, name = "resnet")

        return model