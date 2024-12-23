import tensorflow as tf
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from IPython.display import display
import cv2
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras import backend as K
from easydict import EasyDict as edict
from tensorflow.python.framework.convert_to_constants import convert_variables_to_constants_v2
import os
import sys
import shutil
import random
import math
from config import *
from model import *
from utils import *
from dataset import *
from loss import *
from common_modules import *
from load_weight import *
from easydict import EasyDict
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

def train_model(weights_file_name, weights_dir, saved_model_dir, transfer='transfer'):
  trainset = Dataset('train')
  logdir = "./data/log"
  steps_per_epoch = len(trainset)
  global_steps = tf.Variable(1, trainable=False, dtype=tf.int64)
  warmup_steps = cfg.TRAIN.WARMUP_EPOCHS * steps_per_epoch
  total_steps = cfg.TRAIN.EPOCHS * steps_per_epoch
  class_names = [data.Name for data in cfg.YOLO.CLASSES]
  n_classes = len(class_names)
  _weights_file_name = weights_file_name
  data_format = K.image_data_format()

  if os.path.exists(logdir): 
    shutil.rmtree(logdir)
  writer = tf.summary.create_file_writer(logdir)
  MODEL_SIZE=[cfg.TRAIN.INPUT_HEIGHT,cfg.TRAIN.INPUT_WIDTH]

  if transfer == 'scratch':
    print("Train model from scratch")
    model = get_yolo_model(n_classes, MODEL_SIZE, cfg.YOLO.ANCHORS, False)
  elif transfer == "transfer":
    print("Transfer learning")
    old_model = get_yolo_model(80, MODEL_SIZE, cfg.YOLO.ANCHORS, False)
    # load weigths for old_model
    load_weights(_weights_file_name, old_model)
    x = old_model.layers[-2].output
    outputs = YoloHead(n_classes, MODEL_SIZE, cfg.YOLO.ANCHORS, data_format)(x)
    model = keras.Model(inputs = old_model.layers[0].input, outputs=outputs)
    model.layers[-2].training=False
    model.layers[-2].trainable=False
    
    print("Created new model successfully")
  elif transfer == "resume":
    print("Continue model training")
    model = get_yolo_model(n_classes, MODEL_SIZE, cfg.YOLO.ANCHORS, False)
    model.load_weights(weights_dir)
    model.layers[-2].training=True
    model.layers[-2].trainable=True
    
  # define optimizer
  optimizer = tf.keras.optimizers.Adam()

  def train_step(image_data, target):
    with tf.GradientTape() as tape:
      _, convs, preds = model(image_data, training=True)
      giou_loss=conf_loss=prob_loss=0

      # optimizing process
      for i in range(3):
        conv, pred = convs[i], preds[i]
        loss_items = compute_loss(pred, conv, *target[i], i)
        giou_loss += loss_items[0]
        conf_loss += loss_items[1]
        prob_loss += loss_items[2]

      total_loss = giou_loss + conf_loss + prob_loss

      gradients = tape.gradient(total_loss, model.trainable_variables)
      optimizer.apply_gradients(zip(gradients, model.trainable_variables))
      print("=> STEP %d/%d   lr: %.6f   giou_loss: %4.2f   conf_loss: %4.2f   "
                "prob_loss: %4.2f   total_loss: %.2f" %(global_steps,total_steps+cfg.TRAIN.FINETUNE_EPOCHS, optimizer.lr.numpy(),
                                                        giou_loss, conf_loss,
                                                        prob_loss, total_loss))
      sys.stdout.flush()
      # update learning rate
      global_steps.assign_add(1)
      if global_steps < warmup_steps:
          lr = global_steps / warmup_steps *cfg.TRAIN.LR_INIT
      else:
          lr = cfg.TRAIN.LR_END + 0.5 * (cfg.TRAIN.LR_INIT - cfg.TRAIN.LR_END) * (
              (1 + tf.cos((global_steps - warmup_steps) / (total_steps - warmup_steps) * np.pi))
          )
      optimizer.lr.assign(lr.numpy())

      # writing summary data
      with writer.as_default():
          tf.summary.scalar("lr", optimizer.lr, step=global_steps)
          tf.summary.scalar("loss/total_loss", total_loss, step=global_steps)
          tf.summary.scalar("loss/giou_loss", giou_loss, step=global_steps)
          tf.summary.scalar("loss/conf_loss", conf_loss, step=global_steps)
          tf.summary.scalar("loss/prob_loss", prob_loss, step=global_steps)
      writer.flush()


  print("Start training model")
  print(len(trainset))
  for echo in range(cfg.TRAIN.EPOCHS):
    for image_data, target in trainset:
      train_step(image_data, target)
  if transfer == "transfer":
    print("fine tuning")
    model.layers[-2].trainable = True
    model.layers[-2].training = True
    cfg.TRAIN.LR_INIT = optimizer.learning_rate.numpy()/5
    cfg.TRAIN.EPOCHS = cfg.TRAIN.FINETUNE_EPOCHS
    # define optimizer
    optimizer = tf.keras.optimizers.Adam(1e-6)
    for echo in range(cfg.TRAIN.EPOCHS):
      for image_data, target in trainset:
        train_step(image_data, target)
    model.save_weights(weights_dir)
  model.save(saved_model_dir)
  frozen_keras_graph(saved_model_dir)

def LoadConfiguration(dir):
  with open(dir, 'r') as f:
    config = json.load(f)
  return config 
def main(argv):
  config_json = LoadConfiguration(argv[0])
  config = EasyDict(config_json)
  get_config_from_edict(config)
  weights_dir = config.WeightDir
  saved_model_dir = config.SavedModelDir
  weights_file_name = config.PreTrainModelPath
  transfer = config.TrainningType
  if transfer == "transfer":
    cfg.TRAIN.LR_INIT = 1e-2
  train_model(weights_file_name, weights_dir, saved_model_dir, transfer)
if __name__ == "__main__":
  main(sys.argv[1:])