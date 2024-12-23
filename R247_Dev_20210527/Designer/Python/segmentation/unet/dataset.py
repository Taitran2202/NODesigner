import cv2
import tensorflow as tf
import glob
import os
import numpy as np
import albumentations as A
import json

from yaml import load
class SegmentationDataset(tf.keras.utils.Sequence):
    def __init__(self, imageDir, maskDir, imageSize=(None,None),batch_size=1,shuffle=True, subsampling=1, augment=True, smallest_div=16,augmentationSetting=None):
        super(SegmentationDataset, self)
        self.shuffle = shuffle
        self.batch_size= batch_size
        self.smallest_div = smallest_div
        self.imageDir = imageDir
        self.maskDir = maskDir
        self.imageSize = imageSize
        self.augment = augment
        self.transform = A.Compose(
            self.LoadAugmentationSetting(augmentationSetting),
        )
        self.subsampling= subsampling
        self.imageList = []
        self.maskList = []
        self.images = glob.glob( os.path.join(imageDir,"*.jpg")  ) + glob.glob( os.path.join(imageDir,"*.png")  ) +  glob.glob( os.path.join(imageDir,"*.bmp"))+ glob.glob( os.path.join(imageDir,"*.JPEG"))
        self.masks = [os.path.join(maskDir,os.path.basename(im_path).replace(".jpg" , ".png").replace(".jpeg" , ".png").replace(".bmp" , ".png")) for im_path in self.images]
        self.num_samples = len(self.images)
        self.batch_size = min(self.batch_size, self.num_samples)
        self.indexes = np.arange(self.num_samples)
        print("Number of samples: ", self.num_samples)
        self.loadMetaData(self.masks)
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
    def __data_generation(self, indexes):
        X = []
        y = []
        for idx in indexes:
            metadata= self.metaData[idx]
            image = cv2.imread(self.images[idx])
            mask = cv2.imread(self.masks[idx],0)
            if (metadata is not None):
                if(metadata["ROI"] is not None):
                    image = image[int(metadata["ROI"]["row1"]):int(metadata["ROI"]["row2"]), int(metadata["ROI"]["col1"]):int(metadata["ROI"]["col2"])]
                    mask = mask[int(metadata["ROI"]["row1"]):int(metadata["ROI"]["row2"]), int(metadata["ROI"]["col1"]):int(metadata["ROI"]["col2"])]
            targetsize = (image.shape[1],image.shape[0])     
            if (self.subsampling>1):
                targetsize = (int(targetsize[0]/self.subsampling), int(targetsize[1]/self.subsampling))
                #image = cv2.resize(image, (int(image.shape[1]/self.subsampling),int(image.shape[0]/self.subsampling)))
            if((targetsize[0]%self.smallest_div!=0) | (targetsize[1]%self.smallest_div!=0)):
                targetsize = ((targetsize[0]-targetsize[0]%self.smallest_div),(targetsize[1]-targetsize[1]%self.smallest_div))						
            if (targetsize[0]!=image.shape[1] | targetsize[1]!=image.shape[0]):
                image = cv2.resize(image, (targetsize[0],targetsize[1]))            
            seg_labels = np.zeros((  image.shape[0] , image.shape[1]  , 1 ))
            if (not (mask is None)):
                mask = cv2.resize(mask,(image.shape[1] , image.shape[0]))
                seg_labels[: , : , 0 ] = (mask > 0 ).astype(np.float32)
            if (self.augment):
                image, seg_labels = self.augmentation(image, seg_labels)
            X.append(image)
            y.append(seg_labels)
        return np.array(X), np.array(y)

    def augmentation(self, image, seg_labels):
        transformed  = self.transform(image=image,mask = seg_labels)
        return transformed['image'], transformed['mask']
    def on_epoch_end(self):
        if self.shuffle:
            np.random.shuffle(self.indexes)
