from config import *
from common_modules import *

def build_efficdehead(inputs, channels_list, num_anchors, num_classes):
    x0, x1, x2 = inputs

    stem0 = Conv(channels_list[6], channels_list[6], kernel_size=1, stride=1)(x0)
    cls_conv0 = Conv(channels_list[6], channels_list[6], kernel_size=3, stride=1, name='cls_conv0')(stem0)
    reg_conv0 = Conv(channels_list[6], channels_list[6], kernel_size=3, stride=1, name='reg_conv0')(stem0)

    cls_pred0 = tf.keras.layers.Conv2D(num_anchors * num_classes, kernel_size=1)(cls_conv0)
    reg_pred0 = tf.keras.layers.Conv2D(num_anchors * 4, kernel_size=1)(reg_conv0)
    obj_pred0 = tf.keras.layers.Conv2D(num_anchors * 1, kernel_size=1)(reg_conv0)

    stem1 = Conv(channels_list[6], channels_list[6], kernel_size=1, stride=1)(x1)
    cls_conv1 = Conv(channels_list[6], channels_list[6], kernel_size=3, stride=1, name='cls_conv1')(stem1)
    reg_conv1 = Conv(channels_list[6], channels_list[6], kernel_size=3, stride=1, name='reg_conv1')(stem1)

    cls_pred1 = tf.keras.layers.Conv2D(num_anchors * num_classes, kernel_size=1)(cls_conv1)
    reg_pred1 = tf.keras.layers.Conv2D(num_anchors * 4, kernel_size=1)(reg_conv1)
    obj_pred1 = tf.keras.layers.Conv2D(num_anchors * 1, kernel_size=1)(reg_conv1)

    stem2 = Conv(channels_list[6], channels_list[6], kernel_size=1, stride=1)(x2)
    cls_conv2 = Conv(channels_list[6], channels_list[6], kernel_size=3, stride=1, name='cls_conv2')(stem2)
    reg_conv2 = Conv(channels_list[6], channels_list[6], kernel_size=3, stride=1, name='reg_conv2')(stem2)

    cls_pred2 = tf.keras.layers.Conv2D(num_anchors * num_classes, kernel_size=1)(cls_conv2)
    reg_pred2 = tf.keras.layers.Conv2D(num_anchors * 4, kernel_size=1)(reg_conv2)
    obj_pred2 = tf.keras.layers.Conv2D(num_anchors * 1, kernel_size=1)(reg_conv2)
    
    output0 = tf.concat([reg_pred0, obj_pred0, cls_pred0], axis=-1)
    output1 = tf.concat([reg_pred1, obj_pred1, cls_pred1], axis=-1)
    output2 = tf.concat([reg_pred2, obj_pred2, cls_pred2], axis=-1)
    return [output0, output1, output2]

def build_new_heads(inputs, num_anchors, num_classes):
    cls_conv0, reg_conv0, cls_conv1, reg_conv1, cls_conv2, reg_conv2 = inputs

    cls_pred0 = tf.keras.layers.Conv2D(num_anchors * num_classes, kernel_size=1)(cls_conv0)
    reg_pred0 = tf.keras.layers.Conv2D(num_anchors * 4, kernel_size=1)(reg_conv0)
    obj_pred0 = tf.keras.layers.Conv2D(num_anchors * 1, kernel_size=1)(reg_conv0)

    cls_pred1 = tf.keras.layers.Conv2D(num_anchors * num_classes, kernel_size=1)(cls_conv1)
    reg_pred1 = tf.keras.layers.Conv2D(num_anchors * 4, kernel_size=1)(reg_conv1)
    obj_pred1 = tf.keras.layers.Conv2D(num_anchors * 1, kernel_size=1)(reg_conv1)

    cls_pred2 = tf.keras.layers.Conv2D(num_anchors * num_classes, kernel_size=1)(cls_conv2)
    reg_pred2 = tf.keras.layers.Conv2D(num_anchors * 4, kernel_size=1)(reg_conv2)
    obj_pred2 = tf.keras.layers.Conv2D(num_anchors * 1, kernel_size=1)(reg_conv2)

    output0 = tf.concat([reg_pred0, obj_pred0, cls_pred0], axis=-1)
    output1 = tf.concat([reg_pred1, obj_pred1, cls_pred1], axis=-1)
    output2 = tf.concat([reg_pred2, obj_pred2, cls_pred2], axis=-1)
    return [output0, output1, output2]