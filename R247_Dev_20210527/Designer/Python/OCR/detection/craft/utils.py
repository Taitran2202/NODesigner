import numpy as np
from easydict import EasyDict as edict
import json
import tensorflow as tf

def get_config_from_json_file(filename):
    if filename is None:
        filename = './config/UNET.yaml'
    with open(filename, 'r') as f:
        config_json = json.load(f)
        config = edict(config_json)
        return config
def weighted_bce(y_true, y_pred):
    weights = (y_true*50) + 1.       
    bce = tf.keras.losses.binary_crossentropy(y_true, y_pred)
    weighted_bce = tf.math.reduce_mean(tf.math.multiply(weights[:,:,0],bce[0]))
    weighted_bce1 = tf.math.reduce_mean(tf.math.multiply(weights[:,:,1],bce[1]))
    return weighted_bce*0.5+weighted_bce1*0.5
def MSE_OHEM_Loss(y_true, y_pred):
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