import tensorflow as tf
from tensorflow.keras.models import *
from tensorflow.keras.layers import *
import cv2
import sys
import json
from easydict import EasyDict as edict
import tf2onnx
import sys
import os
from evaluate import Evaluate
physical_devices = tf.config.list_physical_devices('GPU')
try:
  tf.config.experimental.set_memory_growth(physical_devices[0], True)
except:
  # Invalid device or cannot modify virtual devices once initialized.
  pass
dir = os.path.dirname(os.path.abspath(__file__))
os.chdir(dir)
from dataset import SegmentationDataset
from model import UNET
def get_config_from_json_file(filename):
    if filename is None:
        filename = './config/UNET.yaml'
    with open(filename, 'r') as f:
        config_json = json.load(f)
        config = edict(config_json)
        return config
class SegmentModel(tf.Module):
    def __init__(self, model):
        self.model = model
    @tf.function(input_signature=[tf.TensorSpec(shape=(None, None,None ,3), dtype=tf.uint8)])
    def features(self, input):
        result = self.model(input)[1]
        return { "converted_output": result }
def main(argv):
    config = get_config_from_json_file(argv[0])
    step=int(config.EPOCHS)
    imagedir = config.ImageDir
    anotationdir=config.AnnotationDir
    savedir = config.SavedModelDir
    backboneName = config.ModelName
    precision=config.Precision
    imageordering=config.ImageOrdering
    augmentationSetting  = edict(config.AugmentationSetting)
    if(precision=="float16"):        
        policy = tf.keras.mixed_precision.Policy('mixed_float16')
        tf.keras.mixed_precision.set_global_policy(policy)    
    global n_channels
    global colormode
    n_channels=int(config.NumChannels)
    if(n_channels==1):
        colormode=cv2.IMREAD_GRAYSCALE    
    subsampling = config.Subsampling
    trainData = SegmentationDataset(imagedir, anotationdir, imageSize=(None,None), batch_size=1, shuffle=True, 
                        subsampling=subsampling,augment=config.Augmentation,augmentationSetting=augmentationSetting)
    model = UNET(input_shape=(None,None,3),backboneName=backboneName).build()
    opt = tf.keras.optimizers.Adam(learning_rate=config.StartLearningRate)
    model.compile(loss=['mse',None],
            optimizer= opt ,
            metrics=['accuracy'])
    print(f'Image directory : {imagedir}')
    print(f'Annotation directory : {anotationdir}')
    print(f'Save directory : {savedir}')
    print(f'Backbone : {backboneName}')
    print(f"image ordering : {imageordering}")
    print(f"Subsampling : {subsampling}")
    print(f"Precision : {precision}")
    reduce_lr = tf.keras.callbacks.ReduceLROnPlateau(monitor='base_model_loss', factor=0.5,patience=5, min_lr=config.EndLearningRate)
    earlystoping = tf.keras.callbacks.EarlyStopping(
                            monitor="base_model_loss",
                            min_delta=0,
                            patience=5,
                            verbose=0,
                            mode="auto",
                            baseline=None,
                            restore_best_weights=False,
                        )
    history = model.fit(trainData  ,steps_per_epoch =len(trainData), epochs=step ,use_multiprocessing=False,class_weight=None,callbacks=[reduce_lr,earlystoping])
    Evaluate(model,SegmentationDataset(imagedir, anotationdir, imageSize=(None,None), batch_size=1, shuffle=True,augment=False, subsampling=subsampling),savedir)
    model_proto, external_tensor_storage = tf2onnx.convert.from_keras(model,
                input_signature=None, opset=13, custom_ops=None,
                custom_op_handlers=None, custom_rewriter=None,
                inputs_as_nchw=['main_input'], extra_opset=None,shape_override=None,
                target=None, large_model=False, output_path=os.path.join(savedir,'model.onnx'))
if __name__ == "__main__":
    main(sys.argv[1:])