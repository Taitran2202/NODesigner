import tensorflow as tf
import numpy as np
from tensorflow.keras.layers import *
from tensorflow.keras.applications import *
from config import get_config
from backbone import getBackbone
class UNET:
    def __init__(self, input_shape=(512,512,3), num_classes=1,backboneName='mobilenetv3large',filters=32,  kernel_size=3, padding='same', activation='relu', dropout=0.2):
        super(UNET, self).__init__()
        self.backboneName = backboneName
        self.input_shape = input_shape
        self.num_classes = num_classes
        self.filters = filters
        self.kernel_size = kernel_size
        self.padding = padding
        self.activation = activation
        self.dropout = dropout
    def build(self):
        input = Input(shape=self.input_shape,name='main_input',dtype='uint8')
        input_normalized=tf.keras.layers.Lambda(lambda x:tf.cast(x,dtype='float32'),name='converted_input')(input)
        encoder = getBackbone(self.input_shape,input_normalized, self.backboneName)
        encoder.trainable=False
        model_config = get_config(self.backboneName)
        skip_connection_names = model_config.skip_connection_names
        skip_connections = [encoder.get_layer(name).output for name in skip_connection_names]
        f = model_config.f
        x = encoder.get_layer(model_config.encoder_output_name).output
        for i in range(1, len(skip_connection_names)+1, 1):
            x_skip = skip_connections[-i]
            x = UpSampling2D((2, 2))(x)
            x = Concatenate()([x, x_skip])            
            x = Conv2D(f[-i], (3, 3), padding="same")(x)
            x = BatchNormalization()(x)
            x = Activation("relu")(x)
            
            x = Conv2D(f[-i], (3, 3), padding="same")(x)
            x = BatchNormalization()(x)
            x = Activation("relu")(x)
        x = Conv2D(1, (1, 1), padding="same")(x)
        x = Activation("sigmoid")(x)
        base_model = tf.keras.Model(encoder.inputs, x,name='base_model')
        float_output=base_model(input_normalized)
        convert_output=float_output*255.0
        convert_output=tf.keras.layers.Lambda(lambda x:tf.cast(x,dtype='uint8'),name='converted_output')(convert_output)
        model = tf.keras.Model(input,(float_output,convert_output),name='unet')
        return model
