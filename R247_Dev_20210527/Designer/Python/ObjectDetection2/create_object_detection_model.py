import os
import sys
import random
import math
import re
import time
import numpy as np
import cv2
import matplotlib
import matplotlib.pyplot as plt
from numpy.core.fromnumeric import var
from six import iteritems
import skimage.draw
import warnings
import json
import datetime
import tensorflow as tf
from tensorflow.python.keras import backend as K
from tensorflow import keras

#os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3' 
#os.environ['TF_GPU_ALLOCATOR=cuda_malloc_async']
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
#policy = mixed_precision.Policy('mixed_float16')
#mixed_precision.set_policy(policy)

from config import Config
import utils
import model as modellib
#import visualize

from model import log


class UserDataConfig(Config):
    """Configuration for training on the toy shapes dataset.
    Derives from the base Config class and overrides values specific
    to the toy shapes dataset.
    """
    # Give the configuration a recognizable name
    NAME = "userData"

    # Train on 1 GPU and 8 images per GPU. We can put multiple images on each
    # GPU because the images are small. Batch size is 8 (GPUs * images/GPU).
    GPU_COUNT = 1
    IMAGES_PER_GPU = 1

    # Number of classes (including background)
    NUM_CLASSES = 2

    # Use small images for faster training. Set the limits of the small side
    # the large side, and that determines the image shape.
    IMAGE_MIN_DIM = 512
    IMAGE_MAX_DIM = 512

    # Use smaller anchors because our image and objects are small
    RPN_ANCHOR_SCALES = (8, 16, 32, 64,128)  # anchor side in pixels

    # Reduce training ROIs per image because the images are small and have
    # few objects. Aim to allow ROI sampling to pick 33% positive ROIs.
    TRAIN_ROIS_PER_IMAGE = 32

    # Use a small epoch since the data is simple
    #STEPS_PER_EPOCH = step

    # use small validation steps since the epoch is small
    VALIDATION_STEPS = 2
    

def get_ax(rows=1, cols=1, size=8):
    """Return a Matplotlib Axes array to be used in
    all visualizations in the notebook. Provide a
    central point to control graph sizes.
    
    Change the default size attribute to control the size
    of rendered images
    """
    _, ax = plt.subplots(rows, cols, figsize=(size*cols, size*rows))
    return ax

class UserDataset(utils.Dataset):
    def load_userDataset(self, dataset_dir,classes):
        """Load a subset of the Balloon dataset.
        dataset_dir: Root directory of the dataset.
        subset: Subset to load: train or val
        """
        for i in range(len(classes)):          
            self.add_class('userDataset',i+1,classes[i])
        
        dataset_train_annotations_dir = os.path.join(dataset_dir, "annotations")        
        for file in os.listdir(dataset_train_annotations_dir):
            # Check whether file is in text format or not
            if file.endswith(".txt"):
                file_path = f"{dataset_train_annotations_dir}\{file}"
                annotations = dict() 
                with open(file_path,) as f:
                    # load_mask() needs the image size to convert polygons to masks.
                    image_path = os.path.join(dataset_dir,"images", file.replace(".txt",""))
                    if (os.path.exists(image_path)):
                        image = skimage.io.imread(image_path)
                        height, width = image.shape[:2]
                        data = json.load(f)
                        annotations['filename'] = file.replace(".txt","")
                        polygons = []
                        className=[]
                        for item in data:
                            polygons.append({
                                "name" : "rect",
                                "x" :  item["x"],
                                "y": item["y"],
                                "width" :  item["w"],
                                "height" :  item["h"],
                            })
                            className.append({
                                "class_name":item["annotation"]
                            })
                   

                        self.add_image(
                            "userDataset",
                            image_id=file.replace(".txt", ""),  # use file name as a unique image id
                            path=image_path,
                            width=width, height=height,
                            polygons=polygons, className = className)

    
    def load_mask(self, image_id):
        """Generate instance masks for an image.
        Returns:
        masks: A bool array of shape [height, width, instance count] with
            one mask per instance.
        class_ids: a 1D array of class IDs of the instance masks.
        """
        # If not a balloon dataset image, delegate to parent class.
        image_info = self.image_info[image_id]
        className = image_info['className']
        # count = len(class_name)
        if image_info["source"] != "userDataset":
            return super(self.__class__, self).load_mask(image_id)

        # Convert polygons to a bitmap mask of shape
        # [height, width, instance_count]
        info = self.image_info[image_id]
        mask = np.zeros([info["height"], info["width"], len(info["polygons"])],  
                        dtype=np.uint8)
        
        for i, p in enumerate(info["polygons"]):
            # Get indexes of pixels inside the polygon and set them to 1
            if p['name'] == 'rect':
                all_points_y = [p['y'],p['y'],p['y']+p['height'],p['y']+p['height']]
                all_points_x = [p['x'],p['x'] + p['width'],p['x'] + p['width'],p['x']]
                rr, cc = skimage.draw.polygon(all_points_y, all_points_x)
            else:
                rr, cc = skimage.draw.polygon(p['all_points_y'], p['all_points_x'])
            mask[rr, cc, i] = 1
            # Map class names to class IDs.

        class_ids = np.array([self.class_names.index(s['class_name']) for s in className]) 
        # Return mask, and array of class IDs of each instance. Since we have
        # one class ID only, we return an array of 1s
        return mask.astype(np.bool), class_ids.astype(np.int32)

    def image_reference(self, image_id):
        """Return the path of the image."""
        info = self.image_info[image_id]
        if info["source"] == "balloon":
            return info["path"]
        else:
            super(self.__class__, self).image_reference(image_id)        


def main(argv):
    step=int(argv[3])
    dataset_dir = argv[0]
    pre_train_model = argv[2]
    MODEL_DIR = argv[1]
    import ast
    classes = argv[4]
    classes = ast.literal_eval(classes)
    #classes = np.array(["rect2","circle2"])
    print('dataset directory is '+ dataset_dir)
    print('model dir is '+ MODEL_DIR)
    print('pre_train_model is' + pre_train_model)
    print ('class list is'+ str(classes))
    numClasses = len(classes) + 1 
    print("num_classes is: " + str(numClasses))
    class trainconfig(UserDataConfig):
        NUM_CLASSES = numClasses
    train_config  = trainconfig()
    
    #Prepare train dataset
    dataset_train = UserDataset()
    dataset_train.load_userDataset(dataset_dir,classes)
    dataset_train.prepare()
 
    # Create model in training mode
    model = modellib.MaskRCNN(mode="training", config=train_config, model_dir=MODEL_DIR)
    #model.load_weights(pre_train_model, by_name=True, exclude=["mrcnn_class_logits", "mrcnn_bbox_fc", "mrcnn_bbox", "mrcnn_mask"])
    model.load_weights(pre_train_model, by_name=True)
    model.train(dataset_train, dataset_train, learning_rate= train_config.LEARNING_RATE / 10, epochs=2, layers="all", step=step)

    #ReCreate model after train
    class outConfig(UserDataConfig):        
        GPU_COUNT = 1              
        IMAGES_PER_GPU = 1    
        NUM_CLASSES = numClasses
    
    out_config = outConfig()
    model = modellib.MaskRCNN(mode="inference", config=out_config,model_dir=MODEL_DIR) 
    model_path = os.path.join(MODEL_DIR,"userdata", "mask_rcnn_userdata.h5")
    model.load_weights(model_path, by_name=True)
    print(model.keras_model.inputs)  
    print(model.keras_model.outputs)  
    tf.compat.v1.disable_eager_execution()
    model_keras = model.keras_model
    K.set_learning_phase(0)

    # Create output layer with customized names
    num_output = 7
    pred_node_names = ["detections", "mrcnn_class", "mrcnn_bbox", "mrcnn_mask",
                    "rois_1", "rpn_class", "rpn_bbox"]
    pred_node_names = ["output_" + name for name in pred_node_names]
    pred = [tf.identity(model_keras.outputs[i], name=pred_node_names[i])
            for i in range(num_output)]
    sess = K.get_session()
    # Get the object detection graph
    od_graph_def =  tf.compat.v1.graph_util.convert_variables_to_constants(sess, sess.graph.as_graph_def(),
                                                            pred_node_names)
    model_dirpath = MODEL_DIR

    filename = 'mrcnn.pb'
    pb_filepath = os.path.join(model_dirpath, filename)
    frozen_graph_path = pb_filepath
    with tf.io.gfile.GFile(frozen_graph_path, 'wb') as f:
        f.write(od_graph_def.SerializeToString())
    print('Saved frozen graph {} ...'.format(os.path.basename(pb_filepath)))

if __name__ == "__main__":
    main(sys.argv[1:])


