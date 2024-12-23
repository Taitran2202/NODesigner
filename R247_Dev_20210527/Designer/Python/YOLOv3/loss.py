import tensorflow as tf
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from IPython.display import display
import cv2
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras import backend as K
from easydict import EasyDict as edict
import os
import sys
import shutil
import random
import math
from config import *
from model import *
from utils import *
from dataset import *
from common_modules import *


def bbox_iou(boxes1, boxes2):

    boxes1_area = boxes1[..., 2] * boxes1[..., 3]
    boxes2_area = boxes2[..., 2] * boxes2[..., 3]

    boxes1 = tf.concat([boxes1[..., :2] - boxes1[..., 2:] * 0.5,
                        boxes1[..., :2] + boxes1[..., 2:] * 0.5], axis=-1)
    boxes2 = tf.concat([boxes2[..., :2] - boxes2[..., 2:] * 0.5,
                        boxes2[..., :2] + boxes2[..., 2:] * 0.5], axis=-1)

    left_up = tf.maximum(boxes1[..., :2], boxes2[..., :2])
    right_down = tf.minimum(boxes1[..., 2:], boxes2[..., 2:])

    inter_section = tf.maximum(right_down - left_up, 0.0)
    inter_area = inter_section[..., 0] * inter_section[..., 1]
    union_area = boxes1_area + boxes2_area - inter_area + 5e-10

    return 1.0 * inter_area / union_area

def bbox_giou(boxes1, boxes2):

    boxes1 = tf.concat([boxes1[..., :2] - boxes1[..., 2:] * 0.5,
                        boxes1[..., :2] + boxes1[..., 2:] * 0.5], axis=-1)
    boxes2 = tf.concat([boxes2[..., :2] - boxes2[..., 2:] * 0.5,
                        boxes2[..., :2] + boxes2[..., 2:] * 0.5], axis=-1)

    boxes1 = tf.concat([tf.minimum(boxes1[..., :2], boxes1[..., 2:]),
                        tf.maximum(boxes1[..., :2], boxes1[..., 2:])], axis=-1)
    boxes2 = tf.concat([tf.minimum(boxes2[..., :2], boxes2[..., 2:]),
                        tf.maximum(boxes2[..., :2], boxes2[..., 2:])], axis=-1)

    boxes1_area = (boxes1[..., 2] - boxes1[..., 0]) * (boxes1[..., 3] - boxes1[..., 1])
    boxes2_area = (boxes2[..., 2] - boxes2[..., 0]) * (boxes2[..., 3] - boxes2[..., 1])

    left_up = tf.maximum(boxes1[..., :2], boxes2[..., :2])
    right_down = tf.minimum(boxes1[..., 2:], boxes2[..., 2:])

    inter_section = tf.maximum(right_down - left_up, 0.0)
    inter_area = inter_section[..., 0] * inter_section[..., 1]
    union_area = boxes1_area + boxes2_area - inter_area + 1e-10
    iou = inter_area / union_area

    enclose_left_up = tf.minimum(boxes1[..., :2], boxes2[..., :2])
    enclose_right_down = tf.maximum(boxes1[..., 2:], boxes2[..., 2:])
    enclose = tf.maximum(enclose_right_down - enclose_left_up, 0.0)
    enclose_area = enclose[..., 0] * enclose[..., 1] + 1e-10
    giou = iou - 1.0 * (enclose_area - union_area) / enclose_area

    return giou


def compute_loss(pred, conv, label, bboxes, i=0):
  input_size = cfg.TRAIN.INPUT_HEIGHT
  conv_raw_conf = conv[:, :, :, :, 4:5]
  conv_raw_prob = conv[:, :, :, :, 5:]

  pred_xywh, pred_conf, _ = tf.split(pred, [4, 1, -1], axis=-1) # xywh, conf, prob

  
  shape = pred_xywh.shape
  pred_xywh = tf.reshape(pred_xywh, [-1, int(math.sqrt(shape[1] // 3)), int(math.sqrt(shape[1] // 3)), 3, shape[-1]])

  shape = pred_conf.shape
  pred_conf = tf.reshape(pred_conf, [-1, int(math.sqrt(shape[1] // 3)), int(math.sqrt(shape[1] // 3)), 3, shape[-1]])

  label_xywh = label[:, :, :, :, 0:4]
  respond_bbox = label[:, :, :, :, 4:5]
  label_prob = label[:, :, :, :, 5:]

  giou = tf.expand_dims(bbox_giou(pred_xywh, label_xywh), axis=-1)
  input_size = tf.cast(input_size, tf.float32)
  bbox_loss_scale = 2.0 - 1.0 * label_xywh[:, :, :, :, 2:3] * label_xywh[:, :, :, :, 3:4] / (input_size ** 2)
  giou_loss = respond_bbox * bbox_loss_scale * (1- giou)

  iou = bbox_iou(pred_xywh[:, :, :, :, np.newaxis, :], bboxes[:, np.newaxis, np.newaxis, np.newaxis, :, :])
  max_iou = tf.expand_dims(tf.reduce_max(iou, axis=-1), axis=-1)

  respond_bgd = (1.0 - respond_bbox) * tf.cast( max_iou < cfg.IOU_LOSS_THRESH, tf.float32)
  conf_focal = tf.pow(respond_bbox - pred_conf, 2)
  conf_loss = conf_focal * (
            respond_bbox * tf.nn.sigmoid_cross_entropy_with_logits(labels=respond_bbox, logits=conv_raw_conf)
            +
            respond_bgd * tf.nn.sigmoid_cross_entropy_with_logits(labels=respond_bbox, logits=conv_raw_conf)
    )
  prob_loss = respond_bbox * tf.nn.sigmoid_cross_entropy_with_logits(labels=label_prob, logits=conv_raw_prob)

  giou_loss = tf.reduce_mean(tf.reduce_sum(giou_loss, axis=[1,2,3,4]))
  conf_loss = tf.reduce_mean(tf.reduce_sum(conf_loss, axis=[1,2,3,4]))
  prob_loss = tf.reduce_mean(tf.reduce_sum(prob_loss, axis=[1,2,3,4]))

  return giou_loss, conf_loss, prob_loss