import cv2
import sys
import numpy as np
import glob
import os
import json
import time
import ast
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.models import load_model
import tensorflow.keras.backend as K
from tensorflow.keras.optimizers import SGD
from tensorflow.keras.preprocessing.image import ImageDataGenerator
from tensorflow.python.framework.convert_to_constants import convert_variables_to_constants_v2
from PIL import Image
import tf2onnx
from easydict import EasyDict as edict

os.environ['TF_ENABLE_AUTO_MIXED_PRECISION'] = '1'
gpus = tf.config.experimental.list_physical_devices('GPU')
if gpus:
	try:
		for gpu in gpus:
			tf.config.experimental.set_memory_growth(gpu, True)
			logical_gpus = tf.config.experimental.list_logical_devices('GPU')
	except RuntimeError as e:
		print(e)
from tensorflow.keras.mixed_precision import experimental as mixed_precision
policy = mixed_precision.Policy('mixed_float16')
#mixed_precision.set_policy(policy)
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
class RetrainModel:
    def __init__(self,input_width,input_height,input_depth,input_classes):
        self.input_width=input_width
        self.input_height=input_height
        self.input_depth=input_depth
        self.input_classes=input_classes
        self.is_new_model = True
        self.base_model=None
    def CustomAugmentation(self,aug,images,labels,batch_size):
        generator = aug.flow(images,labels,batch_size)
        for data in generator:           
            yield data[0],data[1]
    def buildBaseModel(self):
        # initialize the model along with the input shape to be
        # "channels last" and the channels dimension itself
        inputShape = (self.input_height, self.input_width, self.input_depth)
        chanDim = -1
        if K.image_data_format() == "channels_first":
            inputShape = (self.input_depth, self.input_height, self.input_width)
            chanDim = 1
        inputs = keras.Input(shape=inputShape, name='input_layer')
        x = inputs/255.0
        x = layers.Conv2D(32, (3,3), padding='same', activation='relu')(inputs)
        x = layers.BatchNormalization(chanDim)(x)
        x = layers.Conv2D(32, (3,3), padding='same', activation='relu')(x)
        x = layers.MaxPooling2D(pool_size=(2,2), strides=(2,2))(x)
        x = layers.Dropout(0.25)(x)
        x = layers.Conv2D(64, (3,3), padding='same', activation='relu')(x)
        x = layers.BatchNormalization(chanDim)(x)
        x = layers.Conv2D(64, (3,3), padding='same', activation='relu')(x)
        x = layers.BatchNormalization(chanDim)(x)
        x = layers.MaxPool2D(pool_size=(2,2), strides=(2,2))(x)
        x = layers.Dropout(0.25)(x)
        x = layers.Flatten()(x)
        x = layers.Dense(512, activation='sigmoid')(x)
        x = layers.Dropout(0.25)(x)
        outputs = layers.Dense(len(self.input_classes), activation='softmax', name='output_layer')(x)
        # return the constructed network architecture
        self.base_model = keras.Model(inputs, outputs, name='minivggnet')
    def Restore(self,directory):
        try:
            self.model.load_weights(os.path.join(directory,'variables','variables'))
        except:
            pass
    def buildModel(self,modelname='efficientnet'):
        if self.base_model is None:
            if(modelname=='high'):
                self.base_model = load_model('./webcam/ocr/transferLearningModel/base_model_high')
                self.base_model.trainable = False
                self.base_model.get_layer('dense').trainable=True
                #self.base_model.get_layer('conv2d').trainable=True
                self.base_model.get_layer('output_layer').trainable=True
                # self.base_model.get_layer('conv2d_19').trainable=True
                assert self.base_model.layers[0].name == 'input_layer'
                assert self.base_model.layers[-1].name == 'output_layer'
                x = layers.Dense(512, activation='sigmoid')( self.base_model.get_layer('flatten').output)
            elif modelname=='efficientnet':
                # self.base_model = load_model('./webcam/classifier/saved_model')
                self.base_model = tf.keras.applications.EfficientNetB0()
                inputlayer= keras.Input(self.base_model.inputs[0].shape[1:],name='main_input',dtype='uint8')
                preprocessinglayer = tf.keras.applications.efficientnet.preprocess_input
                self.base_model.trainable = False
                self.base_model.get_layer('top_conv').trainable = True
                self.base_model.get_layer('top_bn').trainable = True
                x = self.base_model.get_layer('avg_pool').output
                x = layers.Dense(512, activation='relu')(x)
                x = layers.Dropout(0.3)(x)
            elif modelname=='mobilenetv2':
                from tensorflow.keras.applications.mobilenet_v2 import MobileNetV2
                preprocessinglayer = tf.keras.applications.mobilenet_v2.preprocess_input
                self.base_model = MobileNetV2()  
                self.base_model.trainable=False
                self.base_model.get_layer('Conv_1').trainable=True
                self.base_model.get_layer('Conv_1_bn').trainable=True                                
                inputlayer= keras.Input(self.base_model.inputs[0].shape[1:],name='main_input') 
                x = self.base_model.get_layer('global_average_pooling2d').output
                #x = layers.Dropout(0.6)(x)
                x = layers.Dense(1024,activation='relu')(x)
                #x = layers.Dropout(0.6)(x)
            elif modelname=='mobilenetv3':
                from tensorflow.keras.applications import MobileNetV3Large
                preprocessinglayer = tf.keras.applications.mobilenet_v3.preprocess_input
                self.base_model = MobileNetV3Large(include_top=True)  
                self.base_model.trainable=False
                self.base_model.get_layer('Conv_2').trainable=True                            
                inputlayer= keras.Input(self.base_model.inputs[0].shape[1:],name='main_input') 
                x = self.base_model.get_layer('global_average_pooling2d').output
                x = layers.Dropout(0.6)(x)
                x = layers.Dense(512,activation='relu')(x)
                x = layers.Dropout(0.6)(x)
            else:
                self.base_model = load_model('./webcam/ocr/transferLearningModel/base model')
                self.base_model.trainable = False
                self.base_model.get_layer('dense_4').trainable=True
                self.base_model.get_layer('output_layer').trainable=True
                assert self.base_model.layers[0].name == 'input_layer'
                assert self.base_model.layers[-1].name == 'output_layer'
                x = layers.Dense(1400, activation='sigmoid')( self.base_model.get_layer('flatten_4').output)
        outputs = layers.Dense(len(self.input_classes), activation='softmax', name='output_layer',dtype='float32')(x)
        self.model = keras.Model(inputlayer,keras.Model(self.base_model.inputs, outputs, name='EfficientNetB0')(preprocessinglayer(inputlayer)))

    def load_images_from_folder(self,images_path, anotationDir):
        imagesPath =glob.glob( os.path.join(images_path,"*.jpg")) + \
                    glob.glob( os.path.join(images_path,"*.png")) +  \
                    glob.glob( os.path.join(images_path,"*.bmp"))+ \
                    glob.glob( os.path.join(images_path,"*.JPEG"))
        seg_path = anotationDir
        images = []
        labels = []
        IMAGE_DIMS = (224,224)
        for i in range(0, len(imagesPath)):
            origin_image = np.asarray(Image.open(imagesPath[i]))
            print("hihi")
            print(len(origin_image.shape))
            if (len(origin_image.shape)<3):
                origin_image = cv2.cvtColor(origin_image, cv2.COLOR_GRAY2BGR)

            seg_bnme = os.path.basename(imagesPath[i])
            seg_bnme_full = os.path.join(seg_path,seg_bnme + '.txt')
            with open(seg_bnme_full) as json_file:
                    data = json.load(json_file)
                    for p in data:
                        x=int(p['x'])
                        y=int(p['y'])
                        w=int(p['w'])
                        h=int(p['h'])
                        label = p['annotation']
                        image = origin_image[y:y+h,x:x+w]
                        image = cv2.resize(image, (IMAGE_DIMS[1],IMAGE_DIMS[0]))                       
                        images.append(image)
                        labels.append(label)
        return images, labels

    def prepare_train(self, images_path, anotationDir):
        images, labels = self.load_images_from_folder(images_path,anotationDir)
        new_data = np.array(images, dtype="float")
        new_labels = np.array(labels)
        self.baseLabels = self.input_classes
        new_lbs = []
        for i in range(0, len(new_labels)):
            if new_labels[i] in self.baseLabels:
                idx = self.baseLabels.index(new_labels[i])
            else:
                idx=-1
            new_lbs.append(idx)
        new_lbs = np.array(new_lbs)
        new_data = new_data[new_lbs>-1]
        new_lbs = new_lbs[new_lbs>-1]
        return new_data, new_lbs
    
    def TrainModel(self,images_path, anotationdir,step, augmentation):
        #aug = ImageDataGenerator(
            ##brightness_range=[0.9,1.1],
            #rotation_range=90,
            #zoom_range=0.25,
            #width_shift_range=0.25,
            #height_shift_range=0.25,
            #shear_range=0.25,
            #horizontal_flip=False,
            #fill_mode="nearest")
        datagen_args = dict(rotation_range=90,
                width_shift_range=0.25,
                height_shift_range=0.25,
                horizontal_flip=False,
                vertical_flip=False,
                ##brightness_range=[1.0,1.0],
                zoom_range = 0.25,
                shear_range=0.25,
                fill_mode="nearest")
        listAug  = json.loads(augmentation) 
        for item in listAug: 
            datagen_args[item['Name']] = item['Value']
            #if item['Name']== 'rotation_range':
             #   datagen_args['rotation_range'] = item['Value']
            #elif item['Name']== 'width_shift_range':
             #   datagen_args['width_shift_range'] = item['Value']
            #elif item['Name']== 'height_shift_range':
             #   datagen_args['height_shift_range'] = item['Value']
            #elif item['Name']=='horizontal_flip':
             #   datagen_args['horizontal_flip'] = item['Value']           
            #elif item['Name']== 'vertical_flip':
             #   datagen_args['vertical_flip'] = item['Value']            
            #elif item['Name']== 'brightness_range':
             #   if (item['Value']>1):
              #      datagen_args['brightness_range'] = [1, item['Value']]
               # else:
                #    datagen_args['brightness_range'] = [item['Value'], 1]
            #elif item['Name']== 'zoom_range':
             #   datagen_args['zoom_range'] = item['Value']
            #elif item['Name']== 'shear_range':
             #   datagen_args['shear_range'] = item['Value']
 
        aug  = ImageDataGenerator(**datagen_args)
        folder = images_path
        new_data, new_lbs = self.prepare_train(folder, anotationdir)
        EPOCHS = step
        INIT_LR = 1e-2
        BS = 2
        print("[INFO] training network...")
        #count=0
        #for data in aug.flow(new_data, new_lbs, batch_size=BS):
        #    image = data[0][0].astype(np.uint8)
        #    cv2.putText(image,self.baseLabels[data[1][0]],(0,32),cv2.FONT_HERSHEY_SIMPLEX,1,(255,0,0))
        #    if(count==20):
        #        break
        #    count=count+1
        learning_rate_reduction = tf.keras.callbacks.ReduceLROnPlateau(monitor='loss', 
                                            patience=2, 
                                            verbose=1, 
                                            factor=0.5, 
                                            min_lr=0.00001)
        my_callbacks = [tf.keras.callbacks.EarlyStopping(monitor="loss", patience=100),learning_rate_reduction]                                 
        opt = SGD(lr=INIT_LR, decay=INIT_LR / EPOCHS, momentum=0.9, nesterov=True)
        self.model.compile(loss="sparse_categorical_crossentropy", optimizer=opt, metrics=["accuracy"])
        print(self.model.summary())
       # second train
        EPOCHS = step
        STEPS_PER_EPOCH= max(len(new_lbs),(50//len(new_lbs))*len(new_lbs))
        INIT_LR = 1e-5    # Low learning rate
        BS = 2

        H = self.model.fit(
            #aug.flow(new_data, new_lbs, batch_size=BS),
            self.CustomAugmentation(aug,new_data, new_lbs,BS),
            steps_per_epoch=STEPS_PER_EPOCH,
            epochs=EPOCHS,
            verbose=2,callbacks=my_callbacks,validation_data=(new_data, new_lbs))
        return H

    def SaveModel(self,directory = "./webcam/ocr/transferLearningModel"):
        self.directory = directory
        # converter = tf.lite.TFLiteConverter.from_keras_model(self.model)
        # converter.optimizations = [tf.lite.Optimize.DEFAULT]      
        # tflite_quant_model = converter.convert()
        # with open(os.path.join(directory,'tfmodel.tflite'), 'wb') as f:
        #    f.write(tflite_quant_model)
        tf.saved_model.save(self.model, self.directory)

    def PredictDirectory(self,images_path, anotationDir, batch_size=11):        
        images, labels = self.load_images_from_folder(images_path, anotationDir)
        images = [np.expand_dims(image, axis=0) for image in images]
        images = np.vstack(images)
        a = time.time()
        predictions = self.model.predict(images, batch_size=batch_size)
        print(time.time()- a)
        idx = predictions.argmax(axis=1)
        results = []
        for i in range(0, len(idx)):
            results.append(self.baseLabels[idx[i]])
        print('label: '+str(labels))
        print('predict: '+str(results))

    def predict_image_file(self,image_path):
        image= cv2.imread(image_path,cv2.IMREAD_IGNORE_ORIENTATION|cv2.IMREAD_COLOR)
        X = cv2.resize(image , (32 , 32))
        timestart=time.time()
        y_result=self.model.predict(np.expand_dims(X,axis=0))[0]
        print(time.time()-timestart)
        idx = np.argmax(y_result)
        return self.baseLabels[idx]
def get_config_from_json_file(filename):
    with open(filename, 'r') as f:
        config_json = json.load(f)
        config = edict(config_json)
        return config
def main(argv):
    config = get_config_from_json_file(argv[0])
    step=int(config.Epoch)
    imagedir = config.ImageDir
    anotationdir=config.AnnotationDir
    savedir = config.ModelDir
    classes = config.ClassList
    augmentation = config.Augmentation
    classes = ast.literal_eval(classes)
    print('Augmentation is '+ augmentation)
    print('Image directory is '+ imagedir)
    print ('Annotation directory is '+ anotationdir)
    print ('Save directory is '+ savedir)
    print ('class list is'+ str(classes))
    print ('Model name is minivggnet')
    model=RetrainModel(224,224,3,classes)
    print ('build a new model')
    model.buildModel()
    #model.Restore(savedir)
    print ('prepare train and train the model')
    model.TrainModel(imagedir,anotationdir,step, augmentation)
    model.PredictDirectory(imagedir,anotationdir)
    model.SaveModel(savedir)
    model_proto, external_tensor_storage = tf2onnx.convert.from_keras(model.model,
                input_signature=None, opset=None, custom_ops=None,
                custom_op_handlers=None, custom_rewriter=None,
                inputs_as_nchw=['main_input'], extra_opset=None,shape_override=None,
                target=None, large_model=False, output_path=os.path.join(savedir,'model.onnx'))
    frozen_keras_graph(savedir)
if __name__ == "__main__":
   main(sys.argv[1:])