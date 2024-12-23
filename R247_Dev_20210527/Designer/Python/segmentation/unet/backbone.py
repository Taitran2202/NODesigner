import tensorflow as tf
import numpy as np
from tensorflow.keras.layers import *
from tensorflow.keras.applications import *
def getBackbone(input_shape,input_tensor,backbone_name):
    if backbone_name == "resnet50":
        return ResNet50(input_shape=input_shape,input_tensor= tf.keras.applications.resnet50.preprocess_input(input_tensor),include_top=False)
    elif backbone_name == "mobilenet":
        return MobileNet(input_shape=input_shape,input_tensor= tf.keras.applications.mobilenet.preprocess_input(input_tensor), include_top=False)
    elif backbone_name == "mobilenetv2":
        return MobileNetV2(input_shape=input_shape,input_tensor= tf.keras.applications.mobilenet_v2.preprocess_input(input_tensor), include_top=False)
    elif backbone_name == "inceptionv3":
        return InceptionV3(input_shape=input_shape, input_tensor= tf.keras.applications.inception_v3.preprocess_input(input_tensor),include_top=False)
    elif backbone_name == "xception":
        return Xception(input_shape=input_shape, input_tensor= tf.keras.applications.xception.preprocess_input(input_tensor), include_top=False)
    elif backbone_name == "nasnet":
        return NASNetLarge(input_shape=input_shape,  input_tensor= tf.keras.applications.nasnet.preprocess_input(input_tensor),include_top=False)
    elif backbone_name == "vgg16":
        return VGG16(input_shape=input_shape,  input_tensor= tf.keras.applications.vgg16.preprocess_input(input_tensor),include_top=False)
    elif backbone_name == "vgg19":
        return VGG19(input_shape=input_shape,  input_tensor= tf.keras.applications.vgg19.preprocess_input(input_tensor),include_top=False)
    elif backbone_name == "resnet101":
        return ResNet101(input_shape=input_shape, input_tensor= tf.keras.applications.resnet.preprocess_input(input_tensor), include_top=False)
    elif backbone_name == "resnet152":
        return ResNet152(input_shape=input_shape,  input_tensor= tf.keras.applications.resnet.preprocess_input(input_tensor),include_top=False)
    elif backbone_name == "inceptionresnetv2":
        return InceptionResNetV2(input_shape=input_shape, include_top=False)
    elif backbone_name == "mobilenetv3large":
        return MobileNetV3Large(input_shape=input_shape,input_tensor= tf.keras.applications.mobilenet_v3.preprocess_input(input_tensor), include_top=False)
    elif backbone_name == "mobilenetv3small":
        return MobileNetV3Small(input_shape=input_shape,input_tensor= tf.keras.applications.mobilenet_v3.preprocess_input(input_tensor), include_top=False)
    elif backbone_name == "densenet121":
        return DenseNet121(input_shape=input_shape, include_top=False)
    elif backbone_name == "densenet169":
        return DenseNet169(input_shape=input_shape, include_top=False)
    elif backbone_name == "densenet201":
        return DenseNet201(input_shape=input_shape, include_top=False)
    elif backbone_name == "nasnetlarge":
        return NASNetLarge(input_shape=input_shape, include_top=False)
    elif backbone_name == "nasnetmobile":
        return NASNetMobile(input_shape=input_shape, include_top=False)
    elif backbone_name == "efficientnetb0":
        return EfficientNetB0(input_shape=input_shape, include_top=False)
    elif backbone_name == "efficientnetb1":
        return EfficientNetB1(input_shape=input_shape, include_top=False)
    elif backbone_name == "efficientnetb2":
        return EfficientNetB2(input_shape=input_shape, include_top=False)
    elif backbone_name == "efficientnetb3":
        return EfficientNetB3(input_shape=input_shape, include_top=False)
    elif backbone_name == "efficientnetb4":
        return EfficientNetB4(input_shape=input_shape, include_top=False)
    elif backbone_name == "efficientnetb5":
        return EfficientNetB5(input_shape=input_shape, include_top=False)
    elif backbone_name == "efficientnetb6":
        return EfficientNetB6(input_shape=input_shape, include_top=False)
    elif backbone_name == "efficientnetb7":
        return EfficientNetB7(input_shape=input_shape, include_top=False)