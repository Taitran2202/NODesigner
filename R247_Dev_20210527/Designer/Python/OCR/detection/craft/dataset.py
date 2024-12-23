import cv2
import tensorflow as tf
import glob
import os
import numpy as np
import albumentations as A
import json
from utils import *

class CRAFTDataset(tf.keras.utils.Sequence):
    def __init__(self, imageDir, annotationDir, imageSize=(None,None),batch_size=1,shuffle=True, subsampling=1, augment=True, smallest_div=16,augmentationSetting=None):
        super(CRAFTDataset, self)
        self.shuffle = shuffle
        self.batch_size= batch_size
        self.smallest_div = smallest_div
        self.imageDir = imageDir
        self.maskDir = annotationDir
        self.imageSize = imageSize
        self.augment = augment
        self.transform = A.Compose(
            self.LoadAugmentationSetting(augmentationSetting),
        )
        self.subsampling= subsampling
        self.imageList = []
        self.annotationList = []
        self.images = glob.glob( os.path.join(imageDir,"*.jpg")  ) + glob.glob( os.path.join(imageDir,"*.png")  ) +  glob.glob( os.path.join(imageDir,"*.bmp"))+ glob.glob( os.path.join(imageDir,"*.JPEG"))
        self.annotations = [os.path.join(annotationDir,os.path.basename(im_path)+'.txt') for im_path in self.images]
        self.annotationsData = [
                        self.readAnnotationData(os.path.join(annotationDir,os.path.basename(im_path)+'.txt')) 
                        for im_path in self.images]
        self.num_samples = len(self.images)
        self.batch_size = min(self.batch_size, self.num_samples)
        self.indexes = np.arange(self.num_samples)
        self.heatmap = get_gaussian_heatmap(100).reshape(100,100)
        self.heatmapSize = np.array([[0, 0], [self.heatmap.shape[1], 0], [self.heatmap.shape[1], self.heatmap.shape[0]],
                    [0, self.heatmap.shape[0]]]).astype('float32')
        print("Number of samples: ", self.num_samples)
        #self.loadMetaData(self.annotations)
    def LoadAugmentationSetting(self,setting):
        transforms = []
        if(setting is not None):
            if(setting.Brightness):
                transforms.append(A.RandomBrightnessContrast(p=setting.BrightnessRange))
            if(setting.HorizontalFlip):
                transforms.append(A.HorizontalFlip(p=0.5))
            if(setting.VerticalFlip):
                transforms.append(A.VerticalFlip(p=0.5))
            if(setting.Rotation):
                transforms.append(A.Rotate(p=setting.RotationRange))
        return transforms
        
    def loadMetaData(self, masks):
        self.metaData = []
        for mask in masks:
            meta_file = mask.replace(".png" , ".txt")
            if (os.path.exists(meta_file)):
                with open(meta_file) as json_file:
                    data = json.load(json_file)
                    self.metaData.append(data)
            else:
                self.metaData.append(None)


    def __len__(self):
        return int(np.floor(self.num_samples / self.batch_size))
    def __getitem__(self, index):
        indexes = self.indexes[index * self.batch_size:(index + 1) * self.batch_size]
        return self.__data_generation(indexes)
    def readAnnotationData(self, annotationPath):
        scalex=1
        scaley=1
        character_points=[]
        character_groups=[]
        with open(annotationPath) as json_file:
            dataread = json.load(json_file)
            data = dataread['characters']
            for p in data:
                x=p['x']*scalex
                y=p['y']*scaley
                w=p['w']*scalex
                h=p['h']*scaley
                character_point = np.array([[x,y],[x+w,y],[x+w,y+h],[x,y+h]]).astype(np.float32)
                character_points.append(character_point)
            groups = dataread['groups']
            for g in groups:
                boxes = g['box']
                groupId = g['groupId']  #not to be use yet
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
        return {'points':character_points,'aff_boxes':affinity_boxes}
    
    def __data_generation(self, indexes):
        X = []
        y = []
        for idx in indexes:
            image = cv2.imread(self.images[idx])
            width = image.shape[1]
            height = image.shape[0]
            annotationData = self.annotationsData[idx]
            character_points= annotationData['points']
            affinity_boxes= annotationData['aff_boxes']
            box = np.zeros((height,width),dtype=np.float32)
            center = np.zeros((height,width),dtype=np.float32)
            for point in character_points:
                MA = cv2.getPerspectiveTransform(src=self.heatmapSize,dst=point)
                #create gauusian map center char
                box+=cv2.warpPerspective(self.heatmap, MA, dsize=(box.shape[1],box.shape[0])).astype('float32')
            for affinity_box in affinity_boxes:
                MA = cv2.getPerspectiveTransform(src=self.heatmapSize,dst=affinity_box)
                center+=cv2.warpPerspective(self.heatmap, MA, dsize=(center.shape[1],center.shape[0])).astype('float32')
            targetsize = (image.shape[1],image.shape[0])
            if (self.subsampling>1):
                targetsize = (int(targetsize[0]/self.subsampling), int(targetsize[1]/self.subsampling))
            if((targetsize[0]%self.smallest_div!=0) | (targetsize[1]%self.smallest_div!=0)):
                targetsize = ((targetsize[0]-targetsize[0]%self.smallest_div),(targetsize[1]-targetsize[1]%self.smallest_div))						
            if ((targetsize[0]!=image.shape[1]) | (targetsize[1]!=image.shape[0])):
                image = cv2.resize(image, (targetsize[0],targetsize[1]))
                box = cv2.resize(box, (targetsize[0],targetsize[1]))
                center = cv2.resize(center, (targetsize[0],targetsize[1]))
            
            if (self.augment):
                image,masks = self.augmentation(image,[box,center])
                mask_labels = np.reshape(cv2.merge([masks[0],masks[1]]),(-1,2))
            else:
                mask_labels = np.reshape(cv2.merge([box,center]),(-1,2))
            X.append(image)
            y.append(mask_labels)
        return np.array(X), np.array(y)

    def augmentation(self, image, seg_labels):
        transformed  = self.transform(image=image,masks = seg_labels)
        return transformed['image'], transformed['masks']
    def on_epoch_end(self):
        if self.shuffle:
            np.random.shuffle(self.indexes)
