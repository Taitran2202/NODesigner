import tensorflow as tf
import os
from tensorflow.keras.layers import *
from tensorflow.keras.models import Model
IMAGE_ORDERING = 'channels_last'
class ClassifierOCRSmall(Model):
    def __init__(self,input_width,input_height,num_hidden_units=1400,num_classes=36):
        self.input_width=input_width
        self.input_height=input_height
        self.pretrainPath = 'weights/small'
        self.num_hidden_units = num_hidden_units
        self.num_classes = num_classes
        #Custom loss fucntion
    def build(self):
        input_height=self.input_height
        input_width=self.input_width
        n_channels=3
        if IMAGE_ORDERING == 'channels_first':
            img_input = Input(shape=(n_channels,input_height,input_width),name='main_input',dtype='float32')
        elif IMAGE_ORDERING == 'channels_last':
            img_input = Input(shape=(input_height,input_width , n_channels ),name='main_input',dtype='float32')
        pretrain_model = tf.keras.models.load_model(self.pretrainPath)
        pretrain_model.trainable = False
        pretrain_model.get_layer('dense_4').trainable=True
        pretrain_model.get_layer('output_layer').trainable=True
        x = Dense(self.num_hidden_units, activation='sigmoid')( self.pretrain_model.get_layer('flatten_4').output)
        outputs = Dense(self.num_classes, activation='softmax', name='class_output', dtype=tf.float32)(x)
        dummy_model = Model(self.pretrain_model.inputs, outputs, name='dummy_model')
        self.model = Model(inputs=img_input, outputs=dummy_model(img_input), name='OCRClassifySmall')
        return self.model