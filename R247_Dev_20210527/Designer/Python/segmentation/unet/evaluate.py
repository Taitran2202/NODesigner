import tensorflow as tf
from tensorflow.keras.models import *
from tensorflow.keras.layers import *
import cv2
import json
import numpy as np
import os

def Evaluate(model,val_data,saveDir):
    num_samples = len(val_data)
    total_good_mask_hist = np.zeros((256,1))
    total_bad_mask_hist = np.zeros((256,1))
    imageResultDir = os.path.join(saveDir,"result")
    if(not os.path.exists(imageResultDir)):
        os.makedirs(imageResultDir)
    for i in range(len(val_data)):
        img,mask = val_data[i]
        imagePath = val_data.images[val_data.indexes[i]]
        imageName = os.path.basename(imagePath)
        model_prediction = model.predict(img)
        predict_mask = model_prediction[1][0]   
        good_mask_hist = cv2.calcHist([predict_mask],[0],(mask[0]==0).astype('uint8'),[256],[0,256])
        bad_mask_hist = cv2.calcHist([predict_mask],[0],(mask[0]!=0).astype('uint8'),[256],[0,256])
        total_good_mask_hist += good_mask_hist/np.sum(good_mask_hist)
        total_bad_mask_hist += bad_mask_hist/np.sum(bad_mask_hist)
        cv2.imwrite(os.path.join(imageResultDir,"{}.png".format(imageName)),predict_mask)
    #save histogram to file with json
    with open(os.path.join(saveDir,'histogram.json'), 'w') as outfile:
        json.dump({"good":np.reshape(total_good_mask_hist,(256)).tolist(), "bad":np.reshape(total_bad_mask_hist,(256)).tolist()}, outfile)