import os
dir = os.path.dirname(os.path.abspath(__file__))
os.chdir(dir)
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

def main(argv):
    config = get_config_from_json_file(argv[0])
    step=int(config.Epochs)
    imagedir = config.ImageDir
    anotationdir=config.AnnotationDir
    savedir = config.SavedModelDir
    modelName = config.ModelName
    precision=config.Precision
    earlystoping = config.EarlyStopping
    imageordering=config.ImageOrdering
    augmentationSetting  = edict(config.AugmentationSetting)
    INIT_LR = float(config.StartLearningRate)
    if(precision=="float16"):        
        policy = tf.keras.mixed_precision.Policy('mixed_float16')
        tf.keras.mixed_precision.set_global_policy(policy)    
    global n_channels
    global colormode
    n_channels=int(config.NumChannels)
    if(n_channels==1):
        colormode=cv2.IMREAD_GRAYSCALE    
    subsampling = config.Subsampling
    print(f'Image directory : {imagedir}')
    print(f'Annotation directory : {anotationdir}')
    print(f'Save directory : {savedir}')
    print(f'Model Name : {modelName}')
    print(f"image ordering : {imageordering}")
    print(f"Subsampling : {subsampling}")
    print(f"Precision : {precision}")
    print(f"EarlyStop : {earlystoping}")
    print(f"StartLearningRate : {INIT_LR}")

    ocrmodel = getModel(modelName)
    trainData = CharDataset(imagedir, anotationdir,ocrmodel.GetLabels(),imageSize=(32,32), batch_size=8, shuffle=True, 
                        augment=config.Augmentation,augmentationSetting=augmentationSetting)
    model = ocrmodel.build()
    opt = SGD(learning_rate = INIT_LR, decay = INIT_LR / step)
    model.compile(loss = "categorical_crossentropy", optimizer = opt, metrics = ["accuracy"])
    
    reduce_lr = tf.keras.callbacks.ReduceLROnPlateau(monitor='class_output_accuracy', factor=0.5,patience=5, min_lr=1e-7)
    earlystoping_callback = tf.keras.callbacks.EarlyStopping(
                            monitor="class_output_accuracy",
                            min_delta=0,
                            patience=10,
                            verbose=0,
                            mode="auto",
                            baseline=None,
                            restore_best_weights=False)
    if(earlystoping):
        history = model.fit(trainData  ,steps_per_epoch =len(trainData), epochs=step ,use_multiprocessing=False,class_weight=None,callbacks=[reduce_lr,earlystoping_callback])
    else:
        history = model.fit(trainData  ,steps_per_epoch =len(trainData), epochs=step ,use_multiprocessing=False,class_weight=None,callbacks=[reduce_lr])
    #finetuning
    model.trainable = True
    for layer in model.layers:
        layer.trainable = True
    opt = SGD(learning_rate = INIT_LR/5, decay = INIT_LR /(5*step//2))
    model.compile(loss = "categorical_crossentropy", optimizer = opt, metrics = ["accuracy"])
    history = model.fit(trainData ,steps_per_epoch =len(trainData), epochs=step//2 ,use_multiprocessing=False,class_weight=None,callbacks=[reduce_lr])
    #Evaluate(model,SegmentationDataset(imagedir, anotationdir, imageSize=(None,None), batch_size=1, shuffle=True,augment=False, subsampling=subsampling),savedir)
    model_proto, external_tensor_storage = tf2onnx.convert.from_keras(model,
                input_signature=None, opset=13, custom_ops=None,
                custom_op_handlers=None, custom_rewriter=None,
                inputs_as_nchw=['main_input'], extra_opset=None,shape_override=None,
                target=None, large_model=False, output_path=os.path.join(savedir,'model.onnx'))
    # save model meta data
    metaData = {"CharList": ''.join(ocrmodel.GetLabels())}
    yamlString = omegaconf.OmegaConf.to_yaml(metaData)
    with open(os.path.join(savedir,'meta.yaml'), 'w') as f:
        f.write(yamlString)
if __name__ == "__main__":
    main(sys.argv[1:])