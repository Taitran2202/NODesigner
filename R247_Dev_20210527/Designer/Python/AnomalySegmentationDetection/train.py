import tensorflow as tf 
import sys
from tensorflow import keras 
from tensorflow.keras.preprocessing.image import ImageDataGenerator
from tensorflow.keras.optimizers import Adam
from tensorflow.keras.applications.vgg16 import VGG16
from tensorflow.keras.applications.vgg19 import VGG19
from tensorflow.keras.applications.mobilenet_v2 import MobileNetV2
from tensorflow.keras import Input, Model, Sequential
from tensorflow.keras import layers
from sklearn.model_selection import train_test_split
from sklearn.decomposition import PCA
from skimage import io
from skimage.transform import resize
import numpy as np
import cv2
import matplotlib.pyplot as plt
import os
import random
from tqdm import tqdm
from random import randrange
import tensorflow_probability as tfp
import json
import config as configfile
from dataset import *
from model import *
from check_gpu import *
import tf2onnx
from tensorflow.python.framework.convert_to_constants import convert_variables_to_constants_v2
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
def train(weights_file_path, model_dir, ckpt_path, epochs, dataset_type='Sequence', fast_train=False):
  # reduce training time option
  if fast_train:
    policy = tf.keras.mixed_precision.Policy('mixed_float16')
    tf.keras.mixed_precision.set_global_policy(policy)


  input_model_path = weights_file_path
  output_model_path = model_dir
  model_checkpoint_path = ckpt_path

  # create train_datagen
  if dataset_type == 'tf.data':
    train_datagen = Datagen(configfile.PATH, configfile.INPUT_SIZE, configfile.BATCH_SIZE)._get_dataset()
    validation_datagen = Datagen(configfile.PATH, configfile.INPUT_SIZE, configfile.BATCH_SIZE, "Validation")._get_dataset()
  else:
    train_datagen = CustomDataGen(configfile.PATH, configfile.BATCH_SIZE, configfile.INPUT_SIZE, True, seed,mask=configfile.mask)
    validation_datagen = CustomDataGen(configfile.PATH, configfile.BATCH_SIZE, configfile.INPUT_SIZE, True, seed, "Validation",configfile.mask)

  # print("transfer learning")
  as_model = AnomalySegmentator(LEAKYRELU_ALPHA=configfile.LEAKYRELU_ALPHA)
  optimizer = tf.keras.optimizers.Adam(1e-4)
  as_model.compile(loss=as_model.reconstruction_loss(), optimizer=optimizer)
  as_model.build_model(weights_file_path, (*configfile.INPUT_SIZE, 3))
  as_model.compute_pca(train_datagen, dataset_type)
  plateau = tf.keras.callbacks.ReduceLROnPlateau(
      monitor='loss', factor=0.5, patience=5, verbose=1
  )
  es = tf.keras.callbacks.EarlyStopping(monitor='loss', mode='min', patience=15, verbose=1)
  save_best = tf.keras.callbacks.ModelCheckpoint(model_checkpoint_path, monitor='loss',save_best_only=True,
                                                 mode='min', save_weights_only=True)
  if(configfile.EarlyStopping):
    train_callbacks = [es, save_best, plateau]
  else:
    train_callbacks = [save_best, plateau]
  history = as_model.fit(train_datagen,
        epochs=epochs,
        validation_data=validation_datagen,
        shuffle=True,
        callbacks=train_callbacks) 
  as_model.load_weights(model_checkpoint_path)
  as_model.compute_threshold(train_datagen, dataset_type)
  thresholdForDataset = as_model.threshold.numpy()
  with open(os.path.join(model_dir,'threshold.txt'), 'w') as f:
    f.write(str(thresholdForDataset.astype('float32')))
  if not fast_train:
    model_proto, external_tensor_storage = tf2onnx.convert.from_keras(as_model,
                input_signature=None, opset=None, custom_ops=None,
                custom_op_handlers=None, custom_rewriter=None,
                inputs_as_nchw=['input_1'], extra_opset=None,shape_override=None,
                target=None, large_model=False, output_path=os.path.join(output_model_path,'model.onnx'))
    #as_model.save(output_model_path)
    #frozen_keras_graph(output_model_path)
  else:
    # create new model with input has dtype is float32
    inputs = keras.Input(shape=(*configfile.INPUT_SIZE, 3), name='input_1', dtype='float32')
    outputs = as_model(inputs)
    final_model = Model(inputs, outputs)
    model_proto, external_tensor_storage = tf2onnx.convert.from_keras(final_model,
                input_signature=None, opset=None, custom_ops=None,
                custom_op_handlers=None, custom_rewriter=None,
                inputs_as_nchw=['input_1'], extra_opset=None,shape_override=None,
                target=None, large_model=False, output_path=os.path.join(output_model_path,'model.onnx'))
    #final_model.save(output_model_path)
    #frozen_keras_graph(output_model_path)

  # reset options for reducing training time
  if fast_train:
    policy = tf.keras.mixed_precision.Policy('float32')
    tf.keras.mixed_precision.set_global_policy(policy)
def LoadConfiguration(dir):
  with open(dir, 'r') as f:
    config = json.load(f)
  return config
def main(argv):
  config = LoadConfiguration(argv[0])
  weights_dir = config['WeightDir']
  saved_model_dir = config['SaveModelDir']
  weights_file_name = config['PreTrainModelPath']
  epochs = config['Epoch']
  dataset_type = config['DatasetType']
  
  configfile.PATH=config['PATH']
  configfile.BATCH_SIZE=config['BATCH_SIZE']
  configfile.INPUT_SIZE=(config['INPUT_HEIGHT'],config['INPUT_WIDTH'])
  configfile.LEAKYRELU_ALPHA = config['LEAKYRELU_ALPHA']
  configfile.EarlyStopping = config['EarlyStopping']
  configfile.UseMask = config['UseMask']
  if(configfile.UseMask):
    configfile.mask = config['Mask']
  else:
    configfile.mask=None
  
  gpu_info = check_gpu()
  fast_train = False
  if(config['Precision']=='float16'):
    if gpu_info >= 7:
      fast_train = True
  train(weights_file_name,saved_model_dir, weights_dir, epochs, dataset_type, fast_train)

if __name__ == "__main__":
  main(sys.argv[1:])