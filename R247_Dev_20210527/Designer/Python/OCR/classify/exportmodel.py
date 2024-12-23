import os
from tensorflow.keras.optimizers import SGD
from tensorflow.keras.optimizers import Adam
from model import getModel
from dataset import CharDataset
import cv2
from utils import *
import tensorflow as tf
import tf2onnx
import sys
import omegaconf

ocrmodel = getModel('resnetbase')
model = ocrmodel.build()
model_proto, external_tensor_storage = tf2onnx.convert.from_keras(model,
                input_signature=None, opset=14, custom_ops=None,
                custom_op_handlers=None, custom_rewriter=None,
                inputs_as_nchw=['main_input'], extra_opset=None,shape_override=None,
                target=None, large_model=False, output_path=os.path.join('weights/resnet','model.onnx'))
# save model meta data
metaData = {"CharList": ''.join(ocrmodel.GetLabels())}
yamlString = omegaconf.OmegaConf.to_yaml(metaData)
with open(os.path.join('weights/resnet','meta.yaml'), 'w') as f:
    f.write(yamlString)