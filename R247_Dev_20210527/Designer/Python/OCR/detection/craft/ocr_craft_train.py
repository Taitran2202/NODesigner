import tensorflow as tf
#tf.config.optimizer.set_jit(True)
import tensorflow.keras as keras
from tensorflow.keras.models import *
from tensorflow.keras.layers import *
import tensorflow.keras.backend as K
import cv2
import sys
import numpy as np
import glob
import os
import json
import random
import time
from tensorflow.python.framework.convert_to_constants import convert_variables_to_constants_v2
os.environ['TF_ENABLE_AUTO_MIXED_PRECISION'] = '1'
IMAGE_ORDERING = 'channels_last'
global n_channels
MERGE_AXIS=-1
n_channels=3
colormode = cv2.IMREAD_COLOR
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
#mixed_precision.set_policy(policy) // unexptected behavior with some GPU
IMAGE_ORDERING = 'channels_last'
if IMAGE_ORDERING == 'channels_first':
	MERGE_AXIS = 1
elif IMAGE_ORDERING == 'channels_last':
	MERGE_AXIS = -1
def findNearestChar(source,character_points):
    result=None
    min_x =999999
    center_1 = np.mean(source, axis=0)
    for char in character_points:
        center_2  =np.mean(char, axis=0)
        distance =center_2[0]-center_1[0]
        height_diff = abs(center_2[1] - center_1[1])
        if(height_diff<20):
            if ((distance<min_x) & (distance>0)):
                min_x = distance
                result = char
    return result
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
def gen_affinity(character_points):
    affinity_boxes=[]
    for bbox_1 in character_points:
        bbox_2=findNearestChar(bbox_1,character_points)
        if(bbox_2 is not None):
            center_1, center_2 = np.mean(bbox_1, axis=0), np.mean(bbox_2, axis=0)
            tl = np.mean([bbox_1[0], bbox_1[1], center_1], axis=0)
            bl = np.mean([bbox_1[2], bbox_1[3], center_1], axis=0)
            tr = np.mean([bbox_2[0], bbox_2[1], center_2], axis=0)
            br = np.mean([bbox_2[2], bbox_2[3], center_2], axis=0)
            affinity_boxes.append([tl,tr,br,bl])
    return np.asarray(affinity_boxes)
def get_gaussian_heatmap(size=512, distanceRatio=3.34):
    v = np.abs(np.linspace(-size / 2, size / 2, num=size))
    x, y = np.meshgrid(v, v)
    g = np.sqrt(x**2 + y**2)
    g *= distanceRatio / (size / 2)
    g = np.exp(-(1 / 2) * (g**2))    
    return g.clip(0, 1)
class UNETOCR:
    def __init__(self,input_width,input_height):
        self.input_width=input_width
        self.input_height=input_height
        #Custom loss fucntion
    def Restore(self,directory):
        self.model.load_weights(os.path.join(directory,'variables','variables'))
    def weighted_bce(self,y_true, y_pred):
        weights = (y_true*50) + 1.       
        bce = tf.keras.losses.binary_crossentropy(y_true, y_pred)
        weighted_bce = tf.math.reduce_mean(tf.math.multiply(weights[:,:,0],bce[0]))
        weighted_bce1 = tf.math.reduce_mean(tf.math.multiply(weights[:,:,1],bce[1]))
        return weighted_bce*0.5+weighted_bce1*0.5
    def buildModel(self):
        input_height=self.input_height
        input_width=self.input_width
        global n_channels
        print(n_channels)
        if IMAGE_ORDERING == 'channels_first':
            img_input = Input(shape=(n_channels,input_height,input_width),name='main_input')
        elif IMAGE_ORDERING == 'channels_last':
            img_input = Input(shape=(input_height,input_width , n_channels ),name='main_input',dtype='uint8')
        input_normalized=tf.keras.layers.Lambda(lambda x:tf.cast(x,dtype='float32'),name='converted_input')(img_input)
        input_normalized = tf.keras.applications.mobilenet_v2.preprocess_input(input_normalized)
        inputs = Input(shape=(input_height, input_width,n_channels ), name="input_image")
        encoder = tf.keras.applications.mobilenet_v2.MobileNetV2(input_shape=(input_height,input_width , n_channels),input_tensor=inputs,  weights="imagenet", include_top=False, alpha=0.35)
        encoder.trainable=False
        skip_connection_names = ["input_image", "block_1_expand_relu", "block_3_expand_relu", "block_6_expand_relu"]
        encoder_output = encoder.get_layer("block_13_expand_relu").output  
        f = [16, 32, 48, 64]
        x = encoder_output
        for i in range(1, len(skip_connection_names)+1, 1):
            x_skip = encoder.get_layer(skip_connection_names[-i]).output
            x = UpSampling2D((2, 2))(x)
            x = Concatenate()([x, x_skip])
            
            x = Conv2D(f[-i], (3, 3), padding="same")(x)
            x = BatchNormalization()(x)
            x = Activation("relu")(x)
            
            x = Conv2D(f[-i], (3, 3), padding="same")(x)
            x = BatchNormalization()(x)
            x = Activation("relu")(x)
            
        x = Conv2D(2, (1, 1), padding="same")(x)
        x = Reshape(( input_height*input_width,-1))(x)
        x = Activation("sigmoid",name="main_output")(x)
        
        base_model = Model(encoder.inputs, x,name='base_model')
        o_shape = base_model.output_shape
        i_shape = base_model.input_shape
        if IMAGE_ORDERING == 'channels_last':
            output_height = o_shape[1]
            output_width = o_shape[2]
            input_height = i_shape[1]
            input_width = i_shape[2]
        float_output=base_model(input_normalized)
        convert_output=float_output*255.0
        convert_output=tf.keras.layers.Lambda(lambda x:tf.cast(x,dtype='uint8'),name='converted_output')(convert_output)
        model = Model(img_input,(float_output,convert_output),name='unet_mobilenet')
        model.summary()
        model.output_width = input_width
        model.output_height = input_height
        model.input_height = input_height
        model.input_width = input_width
        model.model_name = "unet_mobilenet"
        self.model=model
        return model
    def buildModel_2(self):
        kernel = 3
        kernel2 = 3
        filter_size = 64
        dropout_rate=0.2
        pad = 1
        pool_size = 2
        if IMAGE_ORDERING == 'channels_first':
            img_input = Input(shape=(3,self.input_height,self.input_width),name='main_input')
        elif IMAGE_ORDERING == 'channels_last':
            img_input = Input(shape=(self.input_height,self.input_width , 3 ),name='main_input',dtype='uint8')
        x = img_input/255
        #encoder layer
        x = (Conv2D(filter_size, (kernel, kernel) , data_format=IMAGE_ORDERING , padding='same'))( x )
        x = (BatchNormalization())( x )
        x = (Activation("relu"))( x )
        f1 = (MaxPooling2D((pool_size, pool_size) , data_format=IMAGE_ORDERING  ))( x )
        x = (Conv2D(int(filter_size/2), (kernel2, kernel2) , data_format=IMAGE_ORDERING , padding='same'))( f1 )
        x = (BatchNormalization())( x )
        x = (Activation("relu"))( x )
        f2 = (MaxPooling2D((pool_size, pool_size) , data_format=IMAGE_ORDERING  ))( x )
        x = (Conv2D(int(filter_size/2), (kernel, kernel) , data_format=IMAGE_ORDERING , padding='same'))( f2 )
        x = (BatchNormalization())( x )
        x = (Activation("relu"))( x )
        f3 = (MaxPooling2D((pool_size, pool_size) , data_format=IMAGE_ORDERING  ))( x )
        
        #decode layer
        o = (Conv2D(int(filter_size/2), (kernel, kernel), padding='same', data_format=IMAGE_ORDERING))(f3)
        o = (BatchNormalization())(o)
        o = (Activation("relu"))( o )
        o = Dropout(dropout_rate)(o)
        o = (UpSampling2D((pool_size, pool_size), data_format=IMAGE_ORDERING))(o)
        #o = (concatenate([o, f2], axis=MERGE_AXIS))
        o = keras.layers.Concatenate()([o, f2])
        o = (Conv2D(int(filter_size),(kernel2, kernel2), padding='same', data_format=IMAGE_ORDERING))(o)
        o = (BatchNormalization())(o)
        o = (Activation("relu"))( o )
        o = Dropout(dropout_rate)(o)
        o = (UpSampling2D((pool_size, pool_size), data_format=IMAGE_ORDERING))(o)
        #o = (concatenate([o, f1], axis=MERGE_AXIS))
        o = keras.layers.Concatenate()([o, f1])
        o = (Conv2D(int(filter_size/2), (kernel, kernel), padding='same', data_format=IMAGE_ORDERING))(o)
        o = (BatchNormalization())(o)
        #o = (Activation("relu"))( o )
        o = Dropout(dropout_rate)(o)
        o = Conv2D(2, (kernel, kernel), padding='same',data_format=IMAGE_ORDERING)(o)
        o = (UpSampling2D((pool_size, pool_size), data_format=IMAGE_ORDERING))(o)
        
        o_shape = Model(img_input , o ).output_shape
        i_shape = Model(img_input , o ).input_shape
        if IMAGE_ORDERING == 'channels_last':
            output_height = o_shape[1]
            output_width = o_shape[2]
            input_height = i_shape[1]
            input_width = i_shape[2]
            o = Reshape(( output_height*output_width,-1))(o)
        o = (Activation('sigmoid',name='main_output',dtype='float32'))(o)
        convert_output=o*255.0
        convert_output=tf.keras.layers.Lambda(lambda x:tf.cast(x,dtype='uint8'),name='converted_output')(convert_output)
        model = Model( img_input , (o,convert_output) )
        model.output_width = output_width
        model.output_height = output_height
        model.input_height = input_height
        model.input_width = input_width
        model.model_name = "unet_oneclass"
        self.model=model
    def image_segmentation_generator(self,images_path,seg_path,batch_size,input_height,input_width,output_height,output_width):
        images = glob.glob( os.path.join(images_path,"*.jpg")  ) + glob.glob( os.path.join(images_path,"*.png")  ) +  glob.glob( os.path.join(images_path,"*.bmp"))+ glob.glob( os.path.join(images_path,"*.JPEG"))
        random.shuffle(images)
        zipped = itertools.cycle(images)
        heatmap = get_gaussian_heatmap(100).reshape(100,100)
        src = np.array([[0, 0], [heatmap.shape[1], 0], [heatmap.shape[1], heatmap.shape[0]],
                    [0, heatmap.shape[0]]]).astype('float32')
        while True:
            
            X = []
            Y = []
            
            for _ in range( batch_size) :
                im_path= next(zipped) 
                
                im = cv2.imread(im_path , 1 )
                scalex = input_width/im.shape[1]
                scaley =input_height/im.shape[0]
                im = cv2.resize(im,(input_width,input_height))*random.uniform(0.8, 1.2)
                angle = random.uniform(-20,20)
                M = cv2.getRotationMatrix2D((input_width/2,input_height/2),angle,1)
                im = cv2.warpAffine(im,M,(input_width,input_height))
                seg_bnme = os.path.basename(im_path)
                seg_bnme_full = os.path.join(seg_path,seg_bnme+'.txt')
                box = np.zeros((  output_height , output_width   ),dtype=np.float32)
                center = np.zeros((  output_height , output_width   ),dtype=np.float32)
                character_points=[]
                with open(seg_bnme_full) as json_file:
                    data = json.load(json_file)
                    for p in data:
                        x=p['x']*scalex
                        y=p['y']*scaley
                        w=p['w']*scalex
                        h=p['h']*scaley
                        character_point = np.array([[x,y],[x+w,y],[x+w,y+h],[x,y+h]]).astype(np.float32)
                        character_points.append(character_point)
                        MA = cv2.getPerspectiveTransform(src=src,dst=character_point)
                        #create gauusian map center char
                        box+=cv2.warpPerspective(heatmap, MA, dsize=(box.shape[1],box.shape[0])).astype('float32')
                        #segment center point
                        
                affinity_boxes = gen_affinity(character_points)
                for affinity_box in affinity_boxes:
                    MA = cv2.getPerspectiveTransform(src=src,dst=affinity_box)
                    center+=cv2.warpPerspective(heatmap, MA, dsize=(center.shape[1],center.shape[0])).astype('float32')
                box_rotated = cv2.warpAffine(box,M,(output_width,output_height))
                affinity_rotated = cv2.warpAffine(center,M,(output_width,output_height))               
                seg_labels = np.reshape(cv2.merge([box_rotated,affinity_rotated]), ( output_width*output_height , 2 ))
                X.append(im)
                Y.append(seg_labels)
            yield np.array(X) , np.array(Y)
    def prepare_train( self , 
		train_images , seg_images,
		input_height=None , 
		input_width=None , 
		verify_dataset=True,
		checkpoints_path=None , 
		epochs = 5,
		batch_size = 2,
		validate=False , 
		val_images=None , 
		val_annotations=None ,
		val_batch_size=1 , 
		auto_resume_checkpoint=False ,
		load_weights=None ,
		steps_per_epoch=512,
		optimizer_name='adadelta',
		classweight=None):
        input_height = self.model.input_height
        input_width = self.model.input_width
        output_height = self.model.output_height
        output_width = self.model.output_width
        opt = tf.keras.optimizers.Adam(learning_rate=0.01)
        if not optimizer_name is None:
            self.model.compile(loss=[self.weighted_bce,None],
			    optimizer= opt ,
			    metrics=['accuracy'])
        if not checkpoints_path is None:
            open( checkpoints_path+"_config.json" , "w" ).write( json.dumps( {
			    "model_class" : self.model.model_name ,
			    "n_classes" : n_classes ,
			    "input_height" : input_height ,
			    "input_width" : input_width ,
			    "output_height" : output_height ,
			    "output_width" : output_width 
		    }))
        if ( not (load_weights is None )) and  len( load_weights ) > 0:
            print("Loading weights from " , load_weights )
            self.model.load_weights(load_weights)
        if auto_resume_checkpoint and ( not checkpoints_path is None ):
            latest_checkpoint = find_latest_checkpoint( checkpoints_path )
            if not latest_checkpoint is None:
                print("Loading the weights from latest checkpoint "  ,latest_checkpoint )
                self.model.load_weights( latest_checkpoint )      
        self.train_gen = self.image_segmentation_generator(train_images,seg_images,  batch_size,   self.model.input_height ,  self.model.input_width ,  self.model.output_height ,  self.model.output_width)
    def TrainModel(self,step=10):
        reduce_lr = tf.keras.callbacks.ReduceLROnPlateau(monitor='main_output_loss', factor=0.5,patience=5, min_lr=0.0001)
        history = self.model.fit(self.train_gen  ,steps_per_epoch =step, epochs=1 ,use_multiprocessing=False,callbacks=[reduce_lr])
    def representative_dataset(self):
        for data in tf.data.Dataset.from_generator((self.train_gen)).batch(1).take(10):
            yield [tf.dtypes.cast(data, tf.float32)]    
    def SaveModel(self,directory):
        # converter = tf.lite.TFLiteConverter.from_keras_model(self.model)
        # converter.optimizations = [tf.lite.Optimize.DEFAULT]      
        # tflite_quant_model = converter.convert()
        # with open(os.path.join(directory,'tfmodel.tflite'), 'wb') as f:
        #    f.write(tflite_quant_model)
        
        tf.keras.models.save_model(self.model, directory)
    def PredictDirectory(self,images_path,outputDir,threshold=128):
        if(not os.path.exists(outputDir)):
            os.mkdir(outputDir)
        images = glob.glob( os.path.join(images_path,"*.jpg")  ) + glob.glob( os.path.join(images_path,"*.png")  ) +  glob.glob( os.path.join(images_path,"*.bmp"))+ glob.glob( os.path.join(images_path,"*.JPEG"))
        for image in images:
            head,tail= os.path.split(image)
            image_box,image_center= self.predict_image_file(image,True)            
            cv2.imwrite(os.path.join(outputDir,'box'+tail),image_box)
            cv2.imwrite(os.path.join(outputDir,'center'+tail),image_center)
    def predict_image_file(self,image_path,resize=True):
        image= cv2.imread(image_path,1)
        X = cv2.resize(image , (self.model.input_width , self.model.input_height))
        timestart=time.time()
        y_result=self.model.predict(np.expand_dims(X,axis=0))
        print(time.time()-timestart)
        y_box = y_result[1][0][:,0]
        y_center = y_result[1][0][:,1]
        if resize:
            return cv2.resize(y_box.reshape((self.model.output_height,self.model.output_width)),(image.shape[1],image.shape[0])),cv2.resize(y_center.reshape((self.model.output_height,self.model.output_width)),(image.shape[1],image.shape[0]))
        else:
            return y_box.reshape((self.model.output_height,self.model.output_width)),y_center.reshape((self.model.output_height,self.model.output_width))     

def main2(argv):
   step=int(argv[5])
   imagedir = argv[0]
   anotationdir=argv[1]
   savedir = argv[2]
   testdir=argv[3]
   predictdir = argv[4]
   
   print('Image directory is '+ imagedir)
   print ('Annotation directory is '+ anotationdir)
   print ('Test directory is '+ testdir)
   print ('Save directory is '+ savedir)
   print ('Model name is OCR UNET')
   model=UNETOCR(512,512)
   print ('build model')
   model.buildModel()
   #model.Restore(savedir)
   print ('prepare train')
   model.prepare_train(train_images=imagedir,seg_images=anotationdir)
   print ('train model')
   model.TrainModel(step)
   #thresh=model.FindMinThreshold(model,imagedir)
   model.PredictDirectory(testdir,predictdir,128)
   model.SaveModel(savedir)
   frozen_keras_graph(savedir)
if __name__ == "__main__":
   main2(sys.argv[1:])