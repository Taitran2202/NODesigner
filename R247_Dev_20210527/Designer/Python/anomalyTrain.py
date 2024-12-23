import tensorflow as tf
import tensorflow.keras as keras
from tensorflow.keras.models import *
from tensorflow.keras.layers import *
import tensorflow.keras.backend as K
import cv2
import sys
import numpy as np
import glob
import os
os.environ['TF_ENABLE_AUTO_MIXED_PRECISION'] = '1'
IMAGE_ORDERING = 'channels_last'
global n_channels
MERGE_AXIS=-1
gpus = tf.config.experimental.list_physical_devices('GPU')
n_channels=3
colormode = cv2.IMREAD_COLOR
class SegmentModel(tf.Module):
    def __init__(self, model):
        self.model = model
    @tf.function(input_signature=[tf.TensorSpec(shape=(None, None,None ,3), dtype=tf.uint8)])
    def features(self, input):
        result = self.model(input)[1]
        return { "converted_output": result }
if gpus:
    try:
        for gpu in gpus:
            tf.config.experimental.set_memory_growth(gpu, True)
            logical_gpus = tf.config.experimental.list_logical_devices('GPU')
    except RuntimeError as e:
        print(e)
def freeze_session(session, keep_var_names=None, output_names=None, clear_devices=True):
    """
    Freezes the state of a session into a pruned computation graph.

    Creates a new computation graph where variable nodes are replaced by
    constants taking their current value in the session. The new graph will be
    pruned so subgraphs that are not necessary to compute the requested
    outputs are removed.
    @param session The TensorFlow session to be frozen.
    @param keep_var_names A list of variable names that should not be frozen,
                          or None to freeze all the variables in the graph.
    @param output_names Names of the relevant graph outputs.
    @param clear_devices Remove the device directives from the graph for better portability.
    @return The frozen graph definition.
    """
    from tensorflow.compat.v1.graph_util import convert_variables_to_constants
    graph = session.graph
    with graph.as_default():
        freeze_var_names = list(set(v.op.name for v in tf.compat.v1.global_variables()).difference(keep_var_names or []))
        output_names = output_names or []
        output_names += [v.op.name for v in tf.compat.v1.global_variables()]
        # Graph -> GraphDef ProtoBuf
        input_graph_def = graph.as_graph_def()
        if clear_devices:
            for node in input_graph_def.node:
                node.device = ""
        frozen_graph = convert_variables_to_constants(session, input_graph_def,
                                                      output_names, freeze_var_names)
        return frozen_graph
def frozen_keras_graph(model):

  from tensorflow.python.framework.convert_to_constants import convert_variables_to_constants_v2_as_graph

  from tensorflow.lite.python.util import run_graph_optimizations, get_grappler_config



  real_model = tf.function(model).get_concrete_function(tf.TensorSpec(model.inputs[0].shape, model.inputs[0].dtype))

  frozen_func, graph_def = convert_variables_to_constants_v2_as_graph(real_model)



  input_tensors = [

      tensor for tensor in frozen_func.inputs

      if tensor.dtype != tf.resource

  ]

  output_tensors = frozen_func.outputs



  graph_def = run_graph_optimizations(

      graph_def,

      input_tensors,

      output_tensors,

      config=get_grappler_config(["constfold", "function"]),

      graph=frozen_func.graph)

  

  return graph_def
def unet_small(input_height=None, input_width=None,feature_size=10) : 
    kernel = 3
    kernel2 = 5
    filter_size = 64
    dropout_rate=0.8
    pad = 1
    pool_size = 2
    global n_channels
    if IMAGE_ORDERING == 'channels_first':
        img_input = Input(shape=(n_channels,input_height,input_width),name='main_input',dtype='uint8')
    elif IMAGE_ORDERING == 'channels_last':
        img_input = Input(shape=(input_height,input_width , n_channels ),name='main_input',dtype='uint8')
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
    o = keras.layers.Subtract()([o, f2])
    o = (Conv2D(int(filter_size),(kernel2, kernel2), padding='same', data_format=IMAGE_ORDERING))(o)
    o = (BatchNormalization())(o)
    o = (Activation("relu"))( o )
    o = Dropout(dropout_rate)(o)
    o = (UpSampling2D((pool_size, pool_size), data_format=IMAGE_ORDERING))(o)
    #o = (concatenate([o, f1], axis=MERGE_AXIS))
    o = keras.layers.Subtract()([o, f1])
    o = (Conv2D(int(filter_size/2), (kernel, kernel), padding='same', data_format=IMAGE_ORDERING))(o)
    o = (BatchNormalization())(o)
    #o = (Activation("relu"))( o )
    o = Dropout(dropout_rate)(o)
    o = Conv2D(1, (kernel, kernel), padding='same',data_format=IMAGE_ORDERING)(o)
    o = (UpSampling2D((pool_size, pool_size), data_format=IMAGE_ORDERING))(o)
    
    o_shape = Model(img_input , o ).output_shape
    i_shape = Model(img_input , o ).input_shape
    o = (Activation('sigmoid',name='main_output',dtype='float32'))(o)
    convert_output=o*255.0
    convert_output=tf.keras.layers.Lambda(lambda x:tf.cast(x,dtype='uint8'),name='converted_output')(convert_output)
    model = Model( img_input , (o,convert_output) )
    model.output_width = input_width
    model.output_height = input_height
    model.input_height = input_height
    model.input_width = input_width
    model.model_name = "unet_oneclass"
    return model
def _gaussian_kernel(kernel_size, sigma, n_channels, dtype):
    x = tf.range(-kernel_size // 2 + 1, kernel_size // 2 + 1, dtype=dtype)
    g = tf.math.exp(-(tf.pow(x, 2) / (2 * tf.pow(tf.cast(sigma, dtype), 2))))
    g_norm2d = tf.pow(tf.reduce_sum(g), 2)
    g_kernel = tf.tensordot(g, g, axes=0) / g_norm2d
    g_kernel = tf.expand_dims(g_kernel, axis=-1)
    return tf.expand_dims(tf.tile(g_kernel, (1, 1, n_channels)), axis=-1)
def unet_mvtec(input_height=512, input_width=512) : 
    kernel = 3
    kernel2 = 5
    base_filter = 16
    pad = 1
    pool_size = 2
    global n_channels
    print(n_channels)
    if IMAGE_ORDERING == 'channels_first':
        img_input = Input(shape=(3,input_height,input_width),name='main_input')
    elif IMAGE_ORDERING == 'channels_last':
        img_input = Input(shape=(input_height,input_width , n_channels ),name='main_input',dtype='uint8')
    input_normalized = img_input/255
    # Encode-----------------------------------------------------------
    #gaus = tf.keras.layers.GaussianNoise(0.1)(input_normalized)
    x = keras.layers.Conv2D(base_filter, (4, 4), strides=2, activation="relu", padding="same")(
       input_normalized
    )
    #x = (BatchNormalization())(x)
    x = keras.layers.Conv2D(base_filter, (4, 4), strides=2, activation="relu", padding="same")(x)
    #x = (BatchNormalization())(x)
    x = keras.layers.Conv2D(base_filter, (4, 4), strides=2, activation="relu", padding="same")(x)
    #x = (BatchNormalization())(x)
    x = keras.layers.Conv2D(base_filter, (3, 3), strides=1, activation="relu", padding="same")(x)
    #x = (BatchNormalization())(x)
    x = keras.layers.Conv2D(base_filter*2, (4, 4), strides=2, activation="relu", padding="same")(x)
    #x = (BatchNormalization())(x)
    x = keras.layers.Conv2D(base_filter*2, (3, 3), strides=1, activation="relu", padding="same")(x)
    #x = (BatchNormalization())(x)
    x = keras.layers.Conv2D(base_filter*4, (4, 4), strides=2, activation="relu", padding="same")(x)
    #x = (BatchNormalization())(x)
    x = keras.layers.Conv2D(base_filter*2, (3, 3), strides=1, activation="relu", padding="same")(x)
    #x = (BatchNormalization())(x)
    x = keras.layers.Conv2D(base_filter, (3, 3), strides=1, activation="relu", padding="same")(x)
    #x = (BatchNormalization())(x)
    encoded = keras.layers.Conv2D(3, (8, 8), strides=1, padding="same")(x)

    # Decode---------------------------------------------------------------------
    x = keras.layers.Conv2D(base_filter, (3, 3), strides=1, activation="relu", padding="same")(
        encoded
    )
    x = keras.layers.Conv2D(base_filter*2, (3, 3), strides=1, activation="relu", padding="same")(x)
    x = keras.layers.UpSampling2D((2, 2))(x)
    x = keras.layers.Conv2D(base_filter*4, (4, 4), strides=2, activation="relu", padding="same")(
        x
    )
    x = keras.layers.UpSampling2D((2, 2))(x)
    x = keras.layers.Conv2D(base_filter*2, (3, 3), strides=1, activation="relu", padding="same")(x)
    x = keras.layers.UpSampling2D((2, 2))(x)
    x = keras.layers.Conv2D(base_filter*2, (4, 4), strides=2, activation="relu", padding="same")(x)
    x = keras.layers.UpSampling2D((2, 2))(x)
    x = keras.layers.Conv2D(base_filter, (3, 3), strides=1, activation="relu", padding="same")(x)
    x = keras.layers.UpSampling2D((2, 2))(x)
    x = keras.layers.Conv2D(base_filter, (4, 4), strides=2, activation="relu", padding="same")(x)
    x = keras.layers.UpSampling2D((4, 4))(x)
    x = keras.layers.Conv2D(base_filter, (4, 4), strides=2, activation="relu", padding="same")(x)
    x = keras.layers.UpSampling2D((2, 2))(x)
    x = keras.layers.Conv2D(base_filter, (8, 8), activation="relu", padding="same")(x)

    x = keras.layers.UpSampling2D((2, 2))(x)
    decoded = keras.layers.Conv2D(
        3, (8, 8), activation="sigmoid", padding="same"
    )(x)
    #blur = _gaussian_kernel(3, 2, 3, x.dtype)
    #decoded=tf.keras.layers.Lambda(lambda x: tf.nn.depthwise_conv2d(x, blur, [1,1,1,1], 'SAME'),trainable=False)(decoded)
    #calculate input and output differences.
    o = keras.layers.Subtract()([input_normalized,decoded])
    o=tf.keras.layers.Lambda(lambda x: tf.reduce_sum(x*x,axis=-1))(o)
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
    return model
def unet_mobilenet(input_height=512, input_width=512):
    global n_channels
    print(n_channels)
    if IMAGE_ORDERING == 'channels_first':
        img_input = Input(shape=(n_channels,input_height,input_width),name='main_input')
    elif IMAGE_ORDERING == 'channels_last':
        img_input = Input(shape=(input_height,input_width , n_channels ),name='main_input',dtype='uint8')
    input_normalized=tf.keras.layers.Lambda(lambda x:tf.cast(x,dtype='float32'),name='converted_input')(img_input)
    input_normalized = tf.keras.applications.mobilenet_v2.preprocess_input(input_normalized)
    inputs = Input(shape=(input_height, input_width,n_channels ), name="input_image")
    encoder = tf.keras.applications.mobilenet_v2.MobileNetV2(input_tensor=inputs, weights="imagenet", include_top=False, alpha=0.35)
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
        
    x = Conv2D(1, (1, 1), padding="same")(x)
    x = Activation("sigmoid")(x)
    
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
    model.output_width = output_width
    model.output_height = output_height
    model.input_height = input_height
    model.input_width = input_width
    model.model_name = "unet_mobilenet"
    return model
import random
import itertools
def image_segmentation_generator(images_path,seg_path,batch_size,input_height,input_width,output_height,output_width):
    images = glob.glob( os.path.join(images_path,"*.jpg")  ) + glob.glob( os.path.join(images_path,"*.png")  ) +  glob.glob( os.path.join(images_path,"*.bmp"))+ glob.glob( os.path.join(images_path,"*.JPEG"))
    random.shuffle(images)
    zipped = itertools.cycle(images)
    global n_channels
    smallest_div=16
    if(input_height is None):
        while True:
            X = []
            Y = []
            for _ in range( batch_size) :
                im_path= next(zipped) 

                im = cv2.imread(im_path , cv2.IMREAD_IGNORE_ORIENTATION|colormode)
                if((im.shape[0]%smallest_div!=0) | (im.shape[1]%smallest_div!=0)):					
                    im = cv2.resize(im,(im.shape[1]-im.shape[1]%smallest_div,im.shape[0]-im.shape[0]%smallest_div))
                im=im*random.uniform(0.9, 1.1)
                #im = cv2.resize(im,(input_width,input_height))*random.uniform(0.9, 1.1)
                seg_bnme = os.path.basename(im_path).replace(".jpg" , ".png").replace(".jpeg" , ".png").replace(".bmp" , ".png")
                seg_bnme_full = os.path.join(seg_path,seg_bnme)
                
                seg_read = cv2.imread(seg_bnme_full, cv2.IMREAD_IGNORE_ORIENTATION)
                seg_labels = np.zeros((  im.shape[0] , im.shape[1]  , 1 ))
                if (not (seg_read is None)):
                    seg_read = cv2.resize(seg_read,(im.shape[1] , im.shape[0]))
                    seg_labels[: , : , 0 ] = (seg_read > 0 ).astype(np.float32)
                
                #seg_labels = np.reshape(seg_labels, ( output_width*output_height , 1 ))
                X.append(im.reshape(im.shape[0],im.shape[1],n_channels))
                Y.append(seg_labels)
                print(np.array(X).shape)
                print(np.array(Y).shape)
            yield np.array(X) , np.array(Y)
    else:
        while True:
            X = []
            Y = []
            for _ in range( batch_size) :
                im_path= next(zipped) 

                im = cv2.imread(im_path , cv2.IMREAD_IGNORE_ORIENTATION|colormode )
                im = cv2.resize(im,(input_width,input_height))*random.uniform(0.9, 1.1)
                seg_bnme = os.path.basename(im_path).replace(".jpg" , ".png").replace(".jpeg" , ".png").replace(".bmp" , ".png")
                seg_bnme_full = os.path.join(seg_path,seg_bnme)
                
                seg_read = cv2.imread(seg_bnme_full, cv2.IMREAD_IGNORE_ORIENTATION)
                seg_labels = np.zeros((  output_height , output_width  , 1 ))
                if (not (seg_read is None)):
                    seg_read = cv2.resize(seg_read,(output_width,output_height))
                    seg_labels[: , : , 0 ] = (seg_read > 0 ).astype(np.float32)
                #seg_labels = np.reshape(seg_labels, ( output_width*output_height , 1 ))
                X.append(im.reshape(input_height,input_width,n_channels))
                Y.append(seg_labels)
            yield np.array(X) , np.array(Y)
def prepare_train( model  , 
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
    input_height = model.input_height
    input_width = model.input_width
    output_height = model.output_height
    output_width = model.output_width
    opt = tf.keras.optimizers.Adam(learning_rate=0.001)
    #opt=tf.keras.mixed_precision.LossScaleOptimizer(opt)
    if not optimizer_name is None:
        model.compile(loss=['mse',None],
            optimizer= opt ,
            metrics=['accuracy'])
    if not checkpoints_path is None:
        open( checkpoints_path+"_config.json" , "w" ).write( json.dumps( {
            "model_class" : model.model_name ,
            "n_classes" : n_classes ,
            "input_height" : input_height ,
            "input_width" : input_width ,
            "output_height" : output_height ,
            "output_width" : output_width 
        }))
    if ( not (load_weights is None )) and  len( load_weights ) > 0:
        print("Loading weights from " , load_weights )
        
    if auto_resume_checkpoint and ( not checkpoints_path is None ):
        latest_checkpoint = find_latest_checkpoint( checkpoints_path )
        if not latest_checkpoint is None:
            print("Loading the weights from latest checkpoint "  ,latest_checkpoint )
            model.load_weights( latest_checkpoint )
    global train_gen
    train_gen = image_segmentation_generator(train_images,seg_images,  batch_size,   model.input_height ,  model.input_width ,  model.output_height ,  model.output_width)
def SaveModel(model,directory):
    # K.set_learning_phase(0)
    # frozen_graph = freeze_session(tf.compat.v1.keras.backend.get_session(),
    # 						  output_names=['main_output/truediv'])
    # input_names=[model.layers[0].output.name[0:-2]]
    # output_name = [model.layers[len(model.layers)-1].output.name[0:-2]]
    # placeholder_type_enum=tf.uint8.as_datatype_enum
    # output_graph_def = optimize_for_inference_lib.optimize_for_inference(
    # 	frozen_graph,
    # 	['main_input'],
    # 	['output_converted/truediv'],
    # 	placeholder_type_enum,
    # 	False)
    # tf.io.write_graph(output_graph_def, data_path, "tf_model_inference.pb", as_text=False)
    # K.set_learning_phase(1)
    #full_model = tf.function(lambda x: model(x))

    #graph_def = frozen_keras_graph(model)
    #tf.io.write_graph(graph_def, '.', os.path.join(directory,'frozen_graph.pb'),as_text=False)
    tf.saved_model.save(SegmentModel(model), directory)
def CreateModel(image_path,seg_path,modelname='unet',load_weights=None):
    if(modelname=='unet'):
        model = unet_small(input_height=None, input_width=None)
    if(modelname=='unet_mobilenet'):
        model = unet_mobilenet(input_height=None, input_width=None)
    if(modelname=='mvtec'):
        model = unet_mvtec(input_height=None, input_width=None)
    prepare_train(model,train_images=image_path,seg_images=seg_path,load_weights = load_weights)
    return model
def predict_image_file(model,image_path,resize=True):
    im= cv2.imread(image_path,cv2.IMREAD_IGNORE_ORIENTATION|colormode)
    if((im.shape[0]%16!=0) | (im.shape[1]%16!=0)):					
        im = cv2.resize(im,(im.shape[1]-im.shape[1]%16,im.shape[0]-im.shape[0]%16))
    #X = cv2.resize(image , (model.input_width , model.input_height))
    y_result=model.predict(np.expand_dims(im,axis=0))[1][0]
    print(y_result.shape)
    if resize:
        return cv2.resize(y_result,(im.shape[1],im.shape[0]))
    else:
        return y_result   
def TrainModel(model,step=10):
    reduce_lr = tf.keras.callbacks.ReduceLROnPlateau(monitor='main_output_loss', factor=0.5,patience=5, min_lr=0.0001)
    history = model.fit(train_gen  ,steps_per_epoch =step, epochs=1 ,use_multiprocessing=False,class_weight=None,callbacks=[reduce_lr])
    return history.history
def PredictDirectory(model,images_path,outputDir,threshold=128):
    if(not os.path.exists(outputDir)):
        os.mkdir(outputDir)
    images = glob.glob( os.path.join(images_path,"*.jpg")  ) + glob.glob( os.path.join(images_path,"*.png")  ) +  glob.glob( os.path.join(images_path,"*.bmp"))+ glob.glob( os.path.join(images_path,"*.JPEG"))
    for image in images:
        head,tail= os.path.split(image)
        image_predict= predict_image_file(model,image,True)
        ret,thresh1 = cv2.threshold(image_predict,threshold*0.9,255,cv2.THRESH_BINARY)
        cv2.imwrite(os.path.join(outputDir,tail),image_predict);
def FindMinThreshold(model,images_path):
    min=0
    images = glob.glob( os.path.join(images_path,"*.jpg")  ) + glob.glob( os.path.join(images_path,"*.png")  ) +  glob.glob( os.path.join(images_path,"*.bmp"))+ glob.glob( os.path.join(images_path,"*.JPEG"))
    for image in images:
        head,tail= os.path.split(image)
        image_predict= predict_image_file(model,image,False)
        max_pixel_value = image_predict.max()
        if(max_pixel_value>min):
            min=max_pixel_value
    return min
def main(argv):
    step=int(argv[5])
    imagedir = argv[0]
    anotationdir=argv[1]
    savedir = argv[2]
    testdir=argv[3]
    predictdir = argv[4]
    modelname = argv[6]
    precision=argv[7]
    imageordering=argv[8]
    global n_channels
    global colormode
    n_channels=int(argv[9])
    if(n_channels==1):
        colormode=cv2.IMREAD_GRAYSCALE 
    print('Image directory is '+ imagedir)
    print ('Annotation directory is '+ anotationdir)
    print ('Test directory is '+ testdir)
    print ('Save directory is '+ savedir)
    print ('Model name is '+ modelname)
    print("image ordering is:" + imageordering)
    IMAGE_ORDERING = imageordering
    if(precision=='float16'):
        tf.keras.mixed_precision.set_global_policy('mixed_float16')
    if IMAGE_ORDERING == 'channels_first':
        MERGE_AXIS = 1
    elif IMAGE_ORDERING == 'channels_last':
        MERGE_AXIS = -1
    model=CreateModel(imagedir,anotationdir,modelname)
    try:
        model.load_weights(os.path.join(savedir,'variables','variables'))
    except Exception as e:
        print(e)
        model=CreateModel(imagedir,anotationdir,modelname)
    TrainModel(model,step)
    SaveModel(model,savedir)
    thresh=FindMinThreshold(model,imagedir)
    PredictDirectory(model,testdir,predictdir,thresh)
    

if __name__ == "__main__":
    main(sys.argv[1:])