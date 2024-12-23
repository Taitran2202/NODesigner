import cv2
import sys
import numpy as np
import glob
import os
import json
import random
import itertools
import time
import string
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.layers import BatchNormalization
from tensorflow.keras.layers import Conv2D
from tensorflow.keras.layers import AveragePooling2D
from tensorflow.keras.layers import MaxPooling2D
from tensorflow.keras.layers import ZeroPadding2D
from tensorflow.keras.layers import Activation
from tensorflow.keras.layers import Dense
from tensorflow.keras.layers import Flatten
from tensorflow.keras.layers import Input
from tensorflow.keras.models import Model
from tensorflow.keras.models import load_model
from tensorflow.keras.layers import add
from tensorflow.keras.regularizers import l2
import tensorflow.keras.backend as K
from tensorflow.keras.optimizers import SGD
from tensorflow.keras.preprocessing.image import ImageDataGenerator
# from sklearn.model_selection import train_test_split
from tensorflow.keras.preprocessing.image import img_to_array
from tensorflow.python.keras.backend import dtype
# from sklearn.preprocessing import LabelBinarizer
# from sklearn.preprocessing import LabelBinarizer
from tensorflow.python.keras.backend_config import set_floatx
# from tensorflow.python.keras.optimizers import Adam
from tensorflow.python.framework.convert_to_constants import convert_variables_to_constants_v2
#tf.config.optimizer.set_jit(True)
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
mixed_precision.set_policy(policy)
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
    def buildBaseModel(self, classes):
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
        outputs = layers.Dense(classes, activation='softmax', name='output_layer')(x)
        # return the constructed network architecture
        self.base_model = keras.Model(inputs, outputs, name='minivggnet')
    def Restore(self,directory):
        try:
            self.model.load_weights(os.path.join(directory,'variables','variables'))
        except:
            pass
    def buildModel(self):
        # load a pre-train base model
        model_high = False
        if self.base_model is None:
            if(model_high):
                self.base_model = load_model('./Designer/Python/OCR/base_model_high')
                self.base_model.trainable = False
                self.base_model.get_layer('dense').trainable=True
                #self.base_model.get_layer('conv2d').trainable=True
                self.base_model.get_layer('output_layer').trainable=True
                # self.base_model.get_layer('conv2d_19').trainable=True
                assert self.base_model.layers[0].name == 'input_layer'
                assert self.base_model.layers[-1].name == 'output_layer'
                x = layers.Dense(512, activation='sigmoid')( self.base_model.get_layer('flatten').output)
            else:
                self.base_model = load_model('./Designer/Python/OCR/base model')
                self.base_model.trainable = False
                self.base_model.get_layer('dense_4').trainable=True
                self.base_model.get_layer('output_layer').trainable=True
                assert self.base_model.layers[0].name == 'input_layer'
                assert self.base_model.layers[-1].name == 'output_layer'
                x = layers.Dense(1400, activation='sigmoid')( self.base_model.get_layer('flatten_4').output)
                
                #self.buildBaseModel(36)
        #for layer in self.base_model.layers:
        #    if not layer.name == "output_layer":
        #        layer.trainable = False
        
        outputs = layers.Dense(36, activation='softmax', name='output_layer', dtype=tf.float32)(x)
        # outputs = layers.Dense(self.input_classes, activation='softmax', name='new_output_layer')(self.base_model.layers[-2].output)
        # new_model = keras.Model(self.base_model.inputs, outputs, name='retrainminivggnet')
        new_model = keras.Model(self.base_model.inputs, outputs, name='retrainminivggnet')
        self.model = new_model

    def load_images_from_folder(self,images_path, anotationDir):
        imagesPath = glob.glob( os.path.join(images_path,"*.jpg")  ) + glob.glob( os.path.join(images_path,"*.png")  ) +  glob.glob( os.path.join(images_path,"*.bmp"))+ glob.glob( os.path.join(images_path,"*.JPEG"))
        seg_path = anotationDir
        images = []
        labels = []
        IMAGE_DIMS = (32,32)
        for i in range(0, len(imagesPath)):
            origin_image = cv2.imread(imagesPath[i])
            seg_bnme = os.path.basename(imagesPath[i])
            seg_bnme_full = os.path.join(seg_path,seg_bnme + '.txt')
            with open(seg_bnme_full) as json_file:
                    dataread = json.load(json_file)
                    data= dataread['characters']
                    for p in data:
                        x=int(p['x'])
                        y=int(p['y'])
                        w=int(p['w'])
                        h=int(p['h'])
                        label = p['annotation']
                        image = origin_image[y:y+h,x:x+w]
                        image = cv2.resize(image, (IMAGE_DIMS[1],IMAGE_DIMS[0]))                       
                        images.append(255-image)
                        labels.append(label)
        return images, labels

    def prepare_train(self, images_path, anotationDir):
        images, labels = self.load_images_from_folder(images_path,anotationDir)
        new_data = np.array(images, dtype="float")
        new_labels = np.array(labels)
        self.baseLabels = (string.digits+string.ascii_lowercase).upper()
        new_lbs = []
        for i in range(0, len(new_labels)):
            idx = self.baseLabels.find(new_labels[i].upper())
            new_lbs.append(idx)
        new_lbs = np.array(new_lbs)
        return new_data, new_lbs
    def load_az_dataset(self,datasetPath):
        # initialize the list of data and labels
        self.baseLabels = (string.ascii_lowercase).upper()
        data = []
        labels = []
        # loop over the rows of the A-Z handwritten digit dataset
        for row in open(datasetPath):
            # parse the label and image from the row
            row = row.split(",")
            label = int(row[0])
            image = np.array([int(x) for x in row[1:]], dtype="uint8")
            # images are represented as single channel (grayscale) images
            # that are 28x28=784 pixels -- we need to take this flattened
            # 784-d list of numbers and repshape them into a 28x28 matrix
            image = image.reshape((28, 28,1))
            image=np.concatenate((image,)*3, axis=-1)
            # update the list of data and labels
            data.append(image)
            labels.append(label)
        # convert the data and labels to NumPy arrays
        data = np.array(data, dtype="float32")
        labels = np.array(labels, dtype="int")
        # return a 2-tuple of the A-Z data and labels
        return data, labels
    def TrainModel(self,images_path, anotationdir,step):
        aug = ImageDataGenerator(
            #brightness_range=[0.9,1.1],
            rotation_range=10,
            zoom_range=0.15,
            width_shift_range=0.15,
            height_shift_range=0.15,
            shear_range=0.15,
            horizontal_flip=False,
            fill_mode="nearest")

        

        folder = images_path
        retrain=False
        if(retrain):
            new_data,new_lbs = self.load_az_dataset('D:/images/char/A_Z Handwritten Data.csv')
        else:
            new_data, new_lbs = self.prepare_train(folder, anotationdir)
            from sklearn.utils import class_weight
            # class_weights = class_weight.compute_class_weight('balanced',
            #                                          np.arange(0,36),
            #                                          new_lbs)
            sample_weights= class_weight.compute_sample_weight(class_weight='balanced', y=new_lbs)
            class_weights_dict = dict.fromkeys(range(0,36))
            class_weights= np.ones((36))
            for idx in new_lbs:
                class_weights[idx]=class_weights[idx]+1
            for key in class_weights_dict:
                class_weights_dict[key]=1/(class_weights[key])
            # self.trained_labels = new_labels
        # new_data = np.array(new_data, dtype="float")
        # new_labels = np.array(new_labels)

        # # binarize the labels
        # lb = LabelBinarizer()
        # new_lbs = lb.fit_transform(new_labels)
        # retrain an existing model with new dataset
        # first train
        #for layer in self.base_model.layers:
        #   if layer.__class__.__name__ == "Dropout":
        #       layer.rate = 0.1
        #for layer in self.base_model.layers:
        #   if not layer.name == "output_layer":
        #       layer.trainable = False
        EPOCHS = step
        INIT_LR = 1e-2
        BS = 1
        print("[INFO] training network...")
        Hori=None
        count=0
        for data in aug.flow(new_data, new_lbs, batch_size=BS):
            image = data[0][0].astype(np.uint8)
            cv2.putText(image,self.baseLabels[data[1][0]],(0,32),cv2.FONT_HERSHEY_SIMPLEX,1,(255,0,0))
            if(count==20):
                break
            if(Hori is not None):
                Hori = np.concatenate((Hori, image), axis=1)
            else:
                Hori=image
            count=count+1
        #cv2.imshow('images',Hori)
        #cv2.waitKey(-1)        
        #self.base_model.trainable = True
        #opt = SGD(lr=INIT_LR, decay=INIT_LR / EPOCHS, momentum=0.9, nesterov=True)
        learning_rate_reduction = tf.keras.callbacks.ReduceLROnPlateau(monitor='loss', 
                                            patience=2, 
                                            verbose=1, 
                                            factor=0.5, 
                                            min_lr=0.00001)
        my_callbacks = [
            tf.keras.callbacks.EarlyStopping(monitor="loss", patience=100),learning_rate_reduction
        ]                                 
        #self.base_model.trainable = True # allow all layers of base_model can be updated   
        #opt=tf.keras.optimizers.Adam(INIT_LR)
        opt = SGD(lr=INIT_LR, decay=INIT_LR / EPOCHS, momentum=0.9, nesterov=True)
        self.model.compile(loss="sparse_categorical_crossentropy", optimizer=opt, metrics=["accuracy"])
        # H = self.model.fit(
        #     aug.flow(new_data, new_lbs, batch_size=BS),
        #     steps_per_epoch=len(new_data) // BS,
        #     epochs=EPOCHS,
        #     verbose=2,
        #     callbacks=my_callbacks)


       # second train
        EPOCHS = step
        STEPS_PER_EPOCH=10
        INIT_LR = 1e-5    # Low learning rate
        BS = 20
        
        #sample_weights = np.repeat(sample_weights,STEPS_PER_EPOCH/len(new_lbs))
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
        tf.keras.models.save_model(self.model, self.directory)

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
        image= cv2.imread(image_path,1)
        X = cv2.resize(image , (32 , 32))
        timestart=time.time()
        y_result=self.model.predict(np.expand_dims(X,axis=0))[0]
        print(time.time()-timestart)
        idx = np.argmax(y_result)
        return self.baseLabels[idx]

def main(argv):
#    step=int(argv[5])
   imagedir = argv[0]
#    anotationdir=argv[1]
#    savedir = argv[2]
#    testdir=argv[3]
#    predictdir = argv[4]
#    modelname = argv[6]
   print('Image directory is '+ imagedir)
#    print ('Annotation directory is '+ anotationdir)
#    print ('Test directory is '+ testdir)
#    print ('Save directory is '+ savedir)
#    print ('Model name is '+ modelname)
#    model=CreateModel(imagedir,anotationdir,modelname)
#    TrainModel(model,step)
#    thresh=FindMinThreshold(model,imagedir)
#    PredictDirectory(model,testdir,predictdir,thresh)
#    SaveModel(model,savedir)

def main2(argv):
    imagedir = argv[0]
    anotationdir=argv[1]
    savedir = argv[2]
    testdir=argv[3]
    predictdir = argv[4]
    step=int(argv[5])
    #modelname = argv[6]

    print('Image directory is '+ imagedir)
    print ('Annotation directory is '+ anotationdir)
    print ('Test directory is '+ testdir)
    print ('Save directory is '+ savedir)
    print ('Model name is minivggnet')
    model=RetrainModel(32,32,3,11)
    print ('build a new model')
    model.buildModel()
    #model.Restore(savedir)
    print ('prepare train and train the model')
    model.TrainModel(imagedir,anotationdir,step)
    model.PredictDirectory(imagedir,anotationdir)
    model.SaveModel(savedir)
    frozen_keras_graph(savedir)

if __name__ == "__main__":
   main2(sys.argv[1:])