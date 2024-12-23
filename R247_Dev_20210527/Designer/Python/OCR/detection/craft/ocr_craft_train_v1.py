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
import itertools
import time
from tensorflow.python.framework.convert_to_constants import convert_variables_to_constants_v2
os.environ['TF_ENABLE_AUTO_MIXED_PRECISION'] = '1'
IMAGE_ORDERING = 'channels_last'
global colormode
colormode = cv2.IMREAD_COLOR
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
def gen_affinity_groups(character_groups):
    affinity_boxes=[]
    for characters in character_groups:
        for i in range(len(characters)-1):
            character_1 = characters[i]
            character_2 = characters[i+1]
            center_1, center_2 = np.mean(character_1, axis=0), np.mean(character_2, axis=0)
            tl = np.mean([character_1[0], character_1[1], center_1], axis=0)
            bl = np.mean([character_1[2], character_1[3], center_1], axis=0)
            tr = np.mean([character_2[0], character_2[1], center_2], axis=0)
            br = np.mean([character_2[2], character_2[3], center_2], axis=0)
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
    def MSE_OHEM_Loss(self,y_true, y_pred):
        loss_every_sample = []
        batch_size = y_true.shape[0]
        for i in range(batch_size):
            output_img = tf.reshape(y_pred[i], [-1])
            target_img = tf.reshape(y_true[i], [-1])
            positive_mask = tf.cast(tf.greater(target_img, 0), dtype = tf.float32)
            sample_loss = tf.math.square(tf.math.subtract(output_img, target_img))
            
            num_all = output_img.get_shape().as_list()[0]
            num_positive = tf.cast(tf.math.reduce_sum(positive_mask), dtype = tf.int32)
            
            positive_loss = tf.math.multiply(sample_loss, positive_mask)
            positive_loss_m = tf.math.reduce_sum(positive_loss)/tf.cast(num_positive, dtype = tf.float32)
            nagative_loss = tf.math.multiply(sample_loss, (1 - positive_mask))
            # nagative_loss_m = tf.math.reduce_sum(nagative_loss)/(num_all - num_positive)

            k = num_positive * 3     
            k = tf.cond((k + num_positive) > num_all, lambda: tf.cast((num_all - num_positive), dtype = tf.int32), lambda: k)
            k = tf.cond(k > 0, lambda: k, lambda: k + 1)   
            nagative_loss_topk, _ = tf.math.top_k(nagative_loss, k)
            res = tf.cond(k < 10, lambda: tf.math.reduce_mean(sample_loss),
                                lambda: positive_loss_m + tf.math.reduce_sum(nagative_loss_topk)/tf.cast(k, dtype=tf.float32))
            loss_every_sample.append(res)

        return tf.math.reduce_mean(tf.convert_to_tensor(loss_every_sample))
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
        float_input=tf.keras.layers.Lambda(lambda x:tf.cast(x,dtype='float32'),name='converted_input')(img_input)
        pretrain_path = 'Designer/Python/OCR/model_craft_mobilenet_unet'
        pretrain_model = tf.keras.models.load_model(pretrain_path)
        pretrain_model.trainable = False
        self.pretrain_model = pretrain_model
        pretrain_model.summary()
        self.train_layers = ['conv2d','conv2d_1','conv2d_2','conv2d_3','conv2d_4','conv2d_5','conv2d_6']
        train_layers = self.train_layers[3:7]
        for layer in pretrain_model.layers:
            if layer.name in train_layers:
                layer.trainable = True
                print(layer.name,'trainable')
            else:
                layer.trainable = False
        # upsample due to output size is half of input size in pretrain model
        #last_conv2d = tf.keras.layers.Conv2D(2, kernel_size = 3, activation = "relu", padding = 'same')(pretrain_model(float_input))
        last_conv2d = pretrain_model(float_input)
        upsampling = tf.keras.layers.UpSampling2D(size=(2, 2), interpolation='bilinear',name='upsampling')(last_conv2d)

        # charater and affinity map
        float_output = Reshape(( -1,2))(upsampling)  
        convert_output=float_output*255.0
        convert_output=tf.keras.layers.Lambda(lambda x:tf.cast(x,dtype='uint8'),name='converted_output')(convert_output)
        model = Model(img_input,(float_output,convert_output),name='unet_mobilenet')
        
        model.model_name = "unet_mobilenet"
        if (input_width is not None):
            model.output_width = input_width//2
            model.output_height = input_height//2
            
        else:
            model.output_width = input_width
            model.output_height = input_height
        model.input_height = input_height
        model.input_width = input_width
        self.model=model
        return model
    def image_segmentation_generator_1(self,images_path,seg_path,batch_size,input_height,input_width,output_height,output_width):
        images = glob.glob( os.path.join(images_path,"*.jpg")  ) + glob.glob( os.path.join(images_path,"*.png")  ) +  glob.glob( os.path.join(images_path,"*.bmp"))+ glob.glob( os.path.join(images_path,"*.JPEG"))
        random.shuffle(images)
        zipped = itertools.cycle(images)
        
        global n_channels
        smallest_div=16
        heatmap = get_gaussian_heatmap(100).reshape(100,100)
        src = np.array([[0, 0], [heatmap.shape[1], 0], [heatmap.shape[1], heatmap.shape[0]],
                    [0, heatmap.shape[0]]]).astype('float32')
        if(input_height is None):
            while True:
                X = []
                Y = []
                for _ in range( batch_size) :
                    im_path= next(zipped) 

                    im = cv2.imread(im_path , cv2.IMREAD_IGNORE_ORIENTATION|colormode)
                    #resize image
                    #if((im.shape[0]%smallest_div!=0) | (im.shape[1]%smallest_div!=0)):					
                    #    im = cv2.resize(im,(im.shape[1]-im.shape[1]%smallest_div,im.shape[0]-im.shape[0]%smallest_div))
                    #pad image instead of resize
                                          	
                    #im = cv2.copyMakeBorder(im,(im.shape[1]-im.shape[1]%smallest_div,im.shape[0]-im.shape[0]%smallest_div))
                    input_width = im.shape[1]
                    input_height = im.shape[0]
                    im=im*random.uniform(0.9, 1.1)
                    scalex = 1
                    scaley = 1
                    angle = random.uniform(-20,20)
                    M = cv2.getRotationMatrix2D((input_width/2,input_height/2),angle,1)
                    im = cv2.warpAffine(im,M,(input_width,input_height))
                    seg_bnme = os.path.basename(im_path)
                    seg_bnme_full = os.path.join(seg_path,seg_bnme+'.txt')
                    box = np.zeros((  input_height , input_width   ),dtype=np.float32)
                    center = np.zeros((  input_height , input_width   ),dtype=np.float32)
                    character_points=[]
                    character_groups=[]
                    with open(seg_bnme_full) as json_file:
                        dataread = json.load(json_file)
                        data = dataread['characters']
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
                        groups = dataread['groups']
                        for g in groups:
                            boxes = g['box']
                            groupId = g['groupId']
                            groupChacracters=[]
                            for b in boxes:                         
                                x=b['x']*scalex
                                y=b['y']*scaley
                                w=b['w']*scalex
                                h=b['h']*scaley
                                character_point=np.array([[x,y],[x+w,y],[x+w,y+h],[x,y+h]]).astype(np.float32)
                                groupChacracters.append(character_point)
                            character_groups.append(groupChacracters)
                            
                    affinity_boxes = gen_affinity_groups(character_groups)
                    for affinity_box in affinity_boxes:
                        MA = cv2.getPerspectiveTransform(src=src,dst=affinity_box)
                        center+=cv2.warpPerspective(heatmap, MA, dsize=(center.shape[1],center.shape[0])).astype('float32')
                    box_rotated = cv2.warpAffine(box,M,(input_width,input_height))
                    affinity_rotated = cv2.warpAffine(center,M,(input_width,input_height))
                    pad_h=0
                    pad_w=0
                    if((im.shape[0]%smallest_div!=0) | (im.shape[1]%smallest_div!=0)):
                        pad_h = smallest_div - im.shape[0]%smallest_div
                        pad_w = smallest_div - im.shape[1]%smallest_div
                        im_padded = cv2.copyMakeBorder(im,0,pad_h,0,pad_w,cv2.BORDER_CONSTANT,value=[0,0,0])
                    else:
                        im_padded = im
                    if(pad_h!=0 or pad_w!=0):
                        box_rotated=cv2.copyMakeBorder(box_rotated,0,pad_h,0,pad_w,cv2.BORDER_CONSTANT,value=[0,0,0])
                        affinity_rotated=cv2.copyMakeBorder(affinity_rotated,0,pad_h,0,pad_w,cv2.BORDER_CONSTANT,value=[0,0,0])
                    seg_labels = np.reshape(cv2.merge([box_rotated,affinity_rotated]), ( box_rotated.shape[0]* box_rotated.shape[1] , 2 ))
                    X.append(im_padded)
                    Y.append(seg_labels)
                yield np.array(X) , np.array(Y)
        else:
            raise Exception('input_height and input_width must be None') 

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
                    dataread = json.load(json_file)
                    data = dataread['characters']
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
		batch_size = 1,
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
            self.model.compile(loss=[self.MSE_OHEM_Loss,None],
			    optimizer= opt ,
			    metrics=['accuracy'],run_eagerly=True)
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
        self.batch_size = batch_size 
        images = glob.glob( os.path.join(train_images,"*.jpg")  ) + glob.glob( os.path.join(train_images,"*.png")  ) +  glob.glob( os.path.join(train_images,"*.bmp"))+ glob.glob( os.path.join(train_images,"*.JPEG"))
        self.num_samples = len(images)
        self.train_gen = self.image_segmentation_generator_1(train_images,seg_images,  batch_size,   self.model.input_height ,  self.model.input_width ,  self.model.output_height ,  self.model.output_width)
    def TrainModel(self,step=10):
        reduce_lr = tf.keras.callbacks.ReduceLROnPlateau(monitor='reshape_accuracy', factor=0.5,patience=2, min_lr=0.000001)

        history = self.model.fit(self.train_gen  ,steps_per_epoch = 50, epochs=step ,use_multiprocessing=False,callbacks=[reduce_lr])
        #train_layers = self.train_layers
        #for layer in self.pretrain_model.layers:
        #    if layer.name in train_layers:
        #        layer.trainable = True
        #        print(layer.name,'trainable')
        #    else:
        #        layer.trainable = False
        #opt = tf.keras.optimizers.Adam(learning_rate=0.0001)
        #self.model.compile(loss=[self.MSE_OHEM_Loss,None],
		#	    optimizer= opt ,
		#	    metrics=['accuracy'],run_eagerly=True)
        #history = self.model.fit(self.train_gen  ,steps_per_epoch =step//10, epochs=1 ,use_multiprocessing=False)
    def representative_dataset(self):
        for data in tf.data.Dataset.from_generator((self.train_gen)).batch(1).take(10):
            yield [tf.dtypes.cast(data, tf.float32)]    
    def SaveModel(self,directory):        
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
        im= cv2.imread(image_path,cv2.IMREAD_IGNORE_ORIENTATION|colormode)
        if(self.model.input_width is  None):
            if((im.shape[0]%16!=0) | (im.shape[1]%16!=0)):					
                im = cv2.resize(im,(im.shape[1]-im.shape[1]%16,im.shape[0]-im.shape[0]%16))
            X = im
            timestart=time.time()
            y_result=self.model.predict(np.expand_dims(X,axis=0))
            print(time.time()-timestart)
            y_box = y_result[1][0][:,0]
            y_center = y_result[1][0][:,1]
            return y_box.reshape((im.shape[0],im.shape[1])),y_center.reshape((im.shape[0],im.shape[1]))     

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
   model=UNETOCR(None,None)
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