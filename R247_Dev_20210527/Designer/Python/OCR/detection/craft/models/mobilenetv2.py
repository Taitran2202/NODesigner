import tensorflow as tf
import os
from tensorflow.keras.layers import *
from tensorflow.keras.models import Model
IMAGE_ORDERING = 'channels_last'
class CRAFTMobileNetV2:
    def __init__(self,input_width,input_height):
        self.input_width=input_width
        self.input_height=input_height
        self.pretrainPath = 'weights/mobilenetv2'
        #Custom loss fucntion
    def Restore(self,directory):
        self.model.load_weights(os.path.join(directory,'variables','variables'))
    def build(self):
        input_height=self.input_height
        input_width=self.input_width
        n_channels=3
        if IMAGE_ORDERING == 'channels_first':
            img_input = Input(shape=(n_channels,input_height,input_width),name='main_input',dtype='float32')
        elif IMAGE_ORDERING == 'channels_last':
            img_input = Input(shape=(input_height,input_width , n_channels ),name='main_input',dtype='float32')
        pretrain_model = tf.keras.models.load_model(self.pretrainPath)
        #pretrain_model.trainable = False
        self.pretrain_model = pretrain_model
        #pretrain_model.summary()
        self.train_layers = ['conv2d','conv2d_1','conv2d_2','conv2d_3','conv2d_4','conv2d_5','conv2d_6']
        not_train_layers = self.pretrain_model.layers[:-20]
        for layer in not_train_layers:
            layer.trainable = False
        # for layer in pretrain_model.layers:
        #     if layer.name in train_layers:
        #         layer.trainable = True
        #     else:
        #         layer.trainable = False
        # upsample due to output size is half of input size in pretrain model
        #last_conv2d = tf.keras.layers.Conv2D(2, kernel_size = 3, activation = "relu", padding = 'same')(pretrain_model(float_input))
        last_conv2d = pretrain_model(img_input)
        upsampling = tf.keras.layers.UpSampling2D(size=(2, 2), interpolation='bilinear',name='upsampling')(last_conv2d)
        # charater and affinity map
        float_output = Reshape(( -1,2))(upsampling)  
        float_output=float_output
        model = Model(img_input,float_output,name='unet_mobilenetv2')
        model.model_name = "unet_mobilenet"
        model.output_width = input_width
        model.output_height = input_height
        model.input_height = input_height
        model.input_width = input_width
        self.model=model
        return model