import tensorflow as tf
import os
from tensorflow.keras.layers import *
from tensorflow.keras.models import Model
IMAGE_ORDERING = 'channels_last'
class CRAFTVGG16:
    def __init__(self,input_width,input_height):
        self.input_width=input_width
        self.input_height=input_height
        self.pretrainPath = 'weights/vgg16'
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
        not_train_layers = self.pretrain_model.layers[:-20]
        for layer in not_train_layers:
            layer.trainable = False
        last_conv2d = pretrain_model(img_input)
        upsampling = tf.keras.layers.UpSampling2D(size=(2, 2), interpolation='bilinear',name='upsampling')(last_conv2d)
        # charater and affinity map
        float_output = Reshape(( -1,2))(upsampling)  
        float_output=float_output
        model = Model(img_input,float_output,name='unet_vgg16')
        self.model=model
        return model