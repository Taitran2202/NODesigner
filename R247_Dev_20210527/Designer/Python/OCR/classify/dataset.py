import cv2
import tensorflow as tf
import glob
import os
import numpy as np
import albumentations as A
import json
from utils import *

class CharDataset(tf.keras.utils.Sequence):
    def __init__(self, imageDir, annotationDir,charList, imageSize=(32,32),batch_size=1,shuffle=True, augment=True, augmentationSetting=None):
        super(CharDataset, self)
        self.shuffle = shuffle
        self.batch_size= batch_size
        self.imageDir = imageDir
        self.maskDir = annotationDir
        self.imageSize = imageSize
        self.augment = augment
        self.charList = charList
        self.num_classes = len(charList)
        self.transform = A.Compose(
            self.LoadAugmentationSetting(augmentationSetting),
        )
        self.images,self.labels = self.PrepareData(imageDir, annotationDir)
        self.num_samples = len(self.images)
        self.batch_size = min(self.batch_size, self.num_samples)
        self.indexes = np.arange(self.num_samples)
        print("Number of samples: ", self.num_samples)
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
    def __len__(self):
        return int(np.floor(self.num_samples / self.batch_size))
    def __getitem__(self, index):
        indexes = self.indexes[index * self.batch_size:(index + 1) * self.batch_size]
        return self.__data_generation(indexes)
    def PrepareData(self, imageDir, annotationDir):
        images = []
        labels = []
        imagesPath = glob.glob( os.path.join(imageDir,"*.jpg")  ) + glob.glob( os.path.join(imageDir,"*.png")  ) +  glob.glob( os.path.join(imageDir,"*.bmp"))+ glob.glob( os.path.join(imageDir,"*.JPEG"))
        annotationsPath = [os.path.join(annotationDir,os.path.basename(im_path)+'.txt') for im_path in imagesPath]
        for i in range(len(imagesPath)):            
            if(os.path.exists(annotationsPath[i])):
                imageOrigin = cv2.imread(imagesPath[i])
                with open(annotationsPath[i]) as json_file:
                    dataread = json.load(json_file)
                    data = dataread['characters']
                    for p in data:
                        x=int(p['x'])
                        y=int(p['y'])
                        w=int(p['w'])
                        h=int(p['h'])
                        label = p['annotation']
                        image = imageOrigin[y:y+h,x:x+w]
                        image = cv2.resize(image, self.imageSize)
                        image = image.astype(np.float32)
                        images.append(image)
                        label_array = np.zeros(self.num_classes)
                        try:
                            label_index = self.charList.index(label)
                            label_array[label_index] = 1
                        except:
                            label_index = self.num_classes
                        labels.append(label_array)
        return images, labels
    def __data_generation(self, indexes):
        X = []
        y = []
        for idx in indexes:
            if(self.augment):
                image = self.augmentation(self.images[idx])
            else:
                image = self.images[idx]
            X.append(image)
            y.append(self.labels[idx])
        return np.array(X), np.array(y)

    def augmentation(self, image):
        transformed  = self.transform(image=image)
        return transformed['image']
    def on_epoch_end(self):
        if self.shuffle:
            np.random.shuffle(self.indexes)
