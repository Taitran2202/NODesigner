from config import *

def LMExp1(inputs):
    x = layers.Conv2D(48, 7, activation='relu', padding='same', kernel_initializer="random_normal", trainable=False)(inputs)
    return x

def LMExp2(inputs):
    x = layers.Conv2D(48, 7, activation='relu', padding='same', kernel_initializer="random_normal")(inputs)
    return x

def LMExp3(inputs):
    x = layers.Conv2D(48, 11, activation='relu', padding='same', kernel_initializer="random_normal", trainable=False)(inputs)
    return x

def LMExp4(inputs):
    x = layers.Conv2D(48, 11, activation='relu', padding='same', kernel_initializer="random_normal")(inputs)
    return x

def RFSExp1(inputs):
    x = layers.Conv2D(38, 7, activation='relu', padding='same', kernel_initializer="random_normal", trainable=False)(inputs)
    return x

def RFSExp2(inputs):
    x = layers.Conv2D(38, 7, activation='relu', padding='same', kernel_initializer="random_normal")(inputs)
    return x

def RFSExp3(inputs):
    x = layers.Conv2D(38, 7, activation='relu', padding='same', kernel_initializer="random_normal", trainable=False)(inputs)
    return x

def RFSExp4(inputs):
    x = layers.Conv2D(38, 7, activation='relu', padding='same', kernel_initializer="random_normal")(inputs)
    return x

def SExp1(inputs):
    x = layers.Conv2D(13, 7, activation='relu', padding='same', kernel_initializer="random_normal", trainable=False)(inputs)
    return x

def SExp2(inputs):
    x = layers.Conv2D(13, 7, activation='relu', padding='same', kernel_initializer="random_normal")(inputs)
    return x

def SExp3(inputs):
    x = layers.Conv2D(13, 11, activation='relu', padding='same', kernel_initializer="random_normal", trainable=False)(inputs)
    return x

def SExp4(inputs):
    x = layers.Conv2D(13, 11, activation='relu', padding='same', kernel_initializer="random_normal")(inputs)
    return x