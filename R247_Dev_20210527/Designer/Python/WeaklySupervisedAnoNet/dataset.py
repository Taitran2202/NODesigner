from math import fabs
from config import *
def GetImageData(imagePath,annotationPath,subFolder):
    images = glob.glob(os.path.join(imagePath,subFolder,"*"))
    data = [{'image': path,'annotation':os.path.join(annotationPath,subFolder,os.path.basename(path).replace(".jpg" , ".png").replace(".jpeg" , ".png").replace(".bmp" , ".png"))} for path in images]
    return data 
class Datagenerator(tf.keras.utils.Sequence):
    def __init__(self, path_images = None, path_masks = None, img_dim = 1, batch_size = 16,augmentation=False,  input_size_images = (None, None)):
        super().__init__()
        self.path_images = path_images
        self.path_masks = path_masks
        good_samples = GetImageData(self.path_images,self.path_masks,"good")
        bad_samples = GetImageData(self.path_images,self.path_masks,"bad")
        self.images = [*good_samples,*bad_samples]
        self.n = len(self.images)
        self.img_dim = img_dim
        self.batch_size = batch_size
        self.input_size_images = input_size_images
        self.augmentation = augmentation
        self.on_epoch_end() 
    def on_epoch_end(self):
        self.indexes = np.arange(len(self.images))
        np.random.shuffle(self.indexes)

    def __len__(self):
        return math.ceil(self.n / self.batch_size)

    def __getitem__(self, idx):
        images = []
        segs_gt = []
        class_gt = []

        indexes = self.indexes[idx * self.batch_size : (idx + 1) * self.batch_size]
        x_train = [self.images[i] for i in indexes]

        #x_train = self.images[idx * self.batch_size: (idx + 1)* self.batch_size]

        for data in x_train:
            imagePath = data['image']
            seg_bnme_full = data['annotation']
            image = cv2.imread(imagePath, cv2.IMREAD_UNCHANGED)
            if self.img_dim == 1:
                image = cv2.resize(image, self.input_size_images)
                
            else:
                image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
                image = cv2.resize(image, self.input_size_images)  
            # segmentation
            #seg = cv2.imread(os.sep.join([self.path_masks, imagePath.split("\\")[-1].split(".")[0] + "_label.PNG"]), cv2.IMREAD_UNCHANGED)
            seg = cv2.imread(seg_bnme_full, cv2.IMREAD_UNCHANGED)
            if(seg is not None):
            #seg = cv2.imread(os.sep.join([self.path_masks, imagePath.split("\\")[-1]]), cv2.IMREAD_UNCHANGED)
                seg = cv2.resize(seg, self.input_size_images)
            else:
                seg = np.zeros(self.input_size_images)
            
            
            if(self.augmentation):
                image, seg = self.augment(image, seg)
            seg = seg / 255.0
            seg = np.expand_dims(seg, -1)
            image = image / 255.0
            image = np.expand_dims(image, -1)
            images.append(image)
            segs_gt.append(seg)
            # classification
            #label = imagePath.split("\\")[-2]
            #if label == "NonDefective":
                #class_gt.append([0])
            #else:
                #class_gt.append([1])

            if np.max(seg) == 1.0:
                class_gt.append([1])
            else:
                class_gt.append([0])
            
        images = np.array(images)
        segs_gt = np.array(segs_gt)
        class_gt = np.array(class_gt)

        # create binary
        # class_gt = LabelBinarizer().fit_transform(class_gt)

        return [images], [segs_gt, class_gt]

    def augment(self, image, seg):
        # flip
        if np.random.rand() > 0.5:
            aug = HorizontalFlip(p = 1.0)
            augmented = aug(image = image, mask = seg)
            x = augmented["image"]
            y = augmented["mask"]
            return x,y
        if np.random.rand() > 0.5:
            aug = VerticalFlip(p = 1)
            augmented = aug(image=image, mask = seg)
            x = augmented['image']
            y = augmented['mask']
            return x,y
        # rotate
        if np.random.rand() > 0.5:
            aug = Rotate(limit = cfg.ROTATION_MAX, p = 1.0)
            augmented = aug(image = image, mask = seg)
            x = augmented["image"]
            y = augmented["mask"]
            return x,y
        return image, seg
# def augment_data(path_images, path_masks, folder="bad"):
#     images = sorted(glob.glob(os.path.join(path_images,folder,'*')))
#     mask = []
#     for imagePath in images:
#         x = cv2.imread(imagePath, cv2.IMREAD_UNCHANGED)
#         seg_bnme = os.path.basename(imagePath).replace(".jpg" , ".png").replace(".jpeg" , ".png").replace(".bmp" , ".png")
#         seg_bnme_full = os.path.join(path_masks,seg_bnme)
#         if not os.path.exists(seg_bnme_full):
#             print(seg_bnme_full)
#         y = cv2.imread(seg_bnme_full, cv2.IMREAD_UNCHANGED)
 
#         aug = HorizontalFlip(p = 1.0)
#         augmented = aug(image = x, mask = y)
#         x1 = augmented["image"]
#         y1 = augmented["mask"]

#         aug = VerticalFlip(p = 1)
#         augmented = aug(image=x, mask = y)
#         x2 = augmented['image']
#         y2 = augmented['mask']

#         aug = Rotate(limit = cfg.ROTATION_MAX, p = 1.0)
#         augmented = aug(image = x, mask = y)
#         x3 = augmented["image"]
#         y3 = augmented["mask"]

#         X = [x, x1, x2, x3]
#         Y = [y, y1, y2, y3]
 

#         idx = 0
#         for i, m in zip(X, Y):
#             m = m / 255.0
#             m = (m > 0.5) * 255.0


#             m = np.array(m).astype(np.uint8)
#             if idx != 0:
#                 tmp_image_name = imagePath.split("\\")[-1].split(".")[0] + "_%s.png"%(idx)
#                 tmp_mask_name  = imagePath.split("\\")[-1].split(".")[0] + "_%s.png"%(idx)
#             else:
#                 idx += 1
#                 continue
            
              
#             save_path = os.path.sep.join([path_images, folder])

#             image_path = os.path.sep.join([save_path, tmp_image_name])
#             mask_path  = os.path.sep.join([path_masks, tmp_mask_name])

#             #cv2.imwrite(image_path, i)
#             #cv2.imwrite(mask_path, m)
#             idx += 1
            