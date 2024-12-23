from config import *
from utils import *
from pathlib import Path

class Dataset(object):
    def __init__(self):
        super(Dataset, self).__init__()
        self.annot_path = cfg.YOLO.TRAIN.ANNOT_PATH
        self.input_size = cfg.YOLO.TRAIN.MODEL_SIZE
        self.img_dir = cfg.YOLO.TRAIN.IMG_DIR
        self.batch_size = cfg.YOLO.TRAIN.BATCH_SIZE
        self.strides = np.array(cfg.YOLO.STRIDES)
        self.classes = self.read_class_names(cfg.YOLO.CLASSES)
        print('classes:', self.classes)
        self.num_classes = len(self.classes)
        self.anchors = (cfg.YOLO.ANCHORS.reshape(len(cfg.YOLO.STRIDES), cfg.YOLO.ANCHOR_PER_SCALE, 2).T / np.array(cfg.YOLO.STRIDES)).T \
                        if not cfg.YOLO.TRAIN_YOLO_TINY else (cfg.YOLO.ANCHORS_TINY.reshape(len(cfg.YOLO.STRIDES), cfg.YOLO.ANCHOR_PER_SCALE, 2).T / np.array(cfg.YOLO.STRIDES)).T 

        self.anchor_per_scale = cfg.YOLO.ANCHOR_PER_SCALE
        self.data_aug = cfg.YOLO.TRAIN.DATA_AUG
        self.use_mosaic_img = cfg.YOLO.TRAIN.USE_MOSAIC_IMG
        self.annotations = self.load_annotations()
        self.num_samples = len(self.annotations)
        self.num_batchs = int(np.ceil(self.num_samples / self.batch_size))
        self.idx = range(self.num_samples)
        self.images_path = []
        self.labels_ori = []
        self.scale_range = cfg.YOLO.TRAIN.SCALE_RANGE
        self.max_bbox_per_scale = 150
        self.batch_count = 0

        for i in self.idx:
            image_path, bboxes = self.parse_annotation(self.annotations[i])
            self.images_path.append(image_path)
            self.labels_ori.append(bboxes)
    
    def image_preprocess(self, image, target_size, keep_ratio=True, gt_boxes=None):
        ih, iw = target_size
        h, w, _ = image.shape   
        scale = min(iw/w, ih/h)
        nw, nh = int(scale * w), int(scale * h)
        image_resized = cv2.resize(image, (nw, nh))

        if keep_ratio:
            image_padded = np.full(shape=[ih, iw, 3], fill_value=128.0)
            dw, dh = (iw - nw), (ih - nh)
            image_padded[dh:nh+dh, dw:nw+dw, :] = image_resized
            if gt_boxes is None:
                return image_padded
            else:
                gt_boxes[:, [0, 2]] = gt_boxes[:, [0, 2]] * scale + dw
                gt_boxes[:, [1, 3]] = gt_boxes[:, [1, 3]] * scale + dh
                return image_padded, gt_boxes
        else:
            if gt_boxes is None:
                return image_resized
            else:
                return image_resized, gt_boxes
    
    def read_class_names(self, yolo_classes):
        names = {}
        for ID, data in enumerate(yolo_classes):
            names[data.Name] = ID
        return names

    def load_annotations(self):
        annotations=[]
        imagesPath =glob.glob( os.path.join(self.img_dir,"*.jpg")) + \
                    glob.glob( os.path.join(self.img_dir,"*.png")) +  \
                    glob.glob( os.path.join(self.img_dir,"*.bmp"))+ \
                    glob.glob( os.path.join(self.img_dir,"*.JPEG"))
        for i in range(len(imagesPath)):
            img_name = os.path.basename(imagesPath[i])
            annotation_path= os.path.join(self.annot_path, img_name+'.txt')
            bboxes = []
            if os.path.exists(annotation_path):
                with open(annotation_path) as json_file:
                    data = json.load(json_file)
                    for p in data:
                        x=int(p['x'])
                        y=int(p['y'])
                        w=int(p['w'])
                        h=int(p['h'])
                        label = p['annotation']
                        #annotations.append([imagesPath[i], x, y, x+w, y+h, get_key(label,self.classes)])
                        if(label in self.classes):
                            [bboxes.append(value) for value in [x, y, x+w, y+h, self.classes[label]]]                      
                annotations.append([imagesPath[i],*bboxes])
        # upsample for using batch_size to improve learning speed and accurate
        while len(annotations) <= self.batch_size * 2:
            annotations += annotations.copy()
        np.random.shuffle(annotations)
        return annotations
    
    #def load_annotations(self):
        #with open(self.annot_path, 'r') as f:
            #txt = f.readlines()
            #annotations = [line.strip() for line in txt if len(line.strip().split()[1:]) != 0]
        #np.random.shuffle(annotations)
        #return annotations

    def parse_annotation(self, annotation):
        image_path = annotation[0]
        if not os.path.exists(image_path):
            raise KeyError("%s does not exist ... " %image_path)
        #bboxes = np.array([list(map(int, annotation[1:]))])
        bboxes = np.array([list(map(int, annotation[1:][(i-1)*5:i*5])) for i in range(1, int(len(annotation[1:]) / 5) + 1)])
        _bboxes =  []
        for bbox in bboxes:
            if bbox[2] - bbox[0] !=0 and bbox[3] - bbox[1] !=0:
                _bboxes.append(bbox)
        bboxes = np.array(_bboxes)
        if len(bboxes) == 0:
            annotation = self.annotations[int(random.random() * self.num_samples)]
            image_path, bboxes = self.parse_annotation(annotation)
            return image_path, bboxes
        return image_path, bboxes
    
    #def parse_annotation(self, annotation):
        #line = annotation.split()
        #image_path = cfg.YOLO.TRAIN.ADD_IMG_PATH + line[0]
        #if not os.path.exists(image_path):
            #raise KeyError("%s does not exist ... " %image_path)
        #bboxes = np.array([[int(float(value)) for value in box.split(',')] for box in line[1:]])     
        #_bboxes =  []
        #for bbox in bboxes:
            #if bbox[2] - bbox[0] !=0 and bbox[3] - bbox[1] !=0:
                #_bboxes.append(bbox)
        #bboxes = np.array(_bboxes)
        #if len(bboxes) == 0:
            #annotation = self.annotations[int(random.random() * self.num_samples)]
            #image_path, bboxes = self.parse_annotation(annotation)
            #return image_path, bboxes
        #return image_path, bboxes
    
    def random_horizontal_flip(self, image, bboxes):
        if random.random() < 0.5:
            _, w, _ = image.shape
            image = image[:, ::-1, :]
            bboxes[:, [0,2]] = w - bboxes[:, [2,0]]
        return image, bboxes

    def random_crop(self, image, bboxes):
        if random.random() < 0.5:
            h, w, _ = image.shape
            max_bbox = np.concatenate([np.min(bboxes[:, 0:2], axis=0), np.max(bboxes[:, 2:4], axis=0)], axis=-1)

            max_l_trans = max_bbox[0]
            max_u_trans = max_bbox[1]
            max_r_trans = w - max_bbox[2]
            max_d_trans = h - max_bbox[3]

            crop_xmin = max(0, int(max_bbox[0] - random.uniform(0, max_l_trans)))
            crop_ymin = max(0, int(max_bbox[1] - random.uniform(0, max_u_trans)))
            crop_xmax = max(w, int(max_bbox[2] + random.uniform(0, max_r_trans)))
            crop_ymax = max(h, int(max_bbox[3] + random.uniform(0, max_d_trans)))

            image = image[crop_ymin : crop_ymax, crop_xmin : crop_xmax]

            bboxes[:, [0, 2]] = bboxes[:, [0, 2]] - crop_xmin
            bboxes[:, [1, 3]] = bboxes[:, [1, 3]] - crop_ymin

        return image, bboxes
    
    def random_translate(self, image, bboxes):
        if random.random() < 0.5:
            h, w, _ = image.shape
            max_bbox = np.concatenate([np.min(bboxes[:, 0:2], axis=0), np.max(bboxes[:, 2:4], axis=0)], axis=-1)

            max_l_trans = max_bbox[0]
            max_u_trans = max_bbox[1]
            max_r_trans = w - max_bbox[2]
            max_d_trans = h - max_bbox[3]

            tx = random.uniform(-(max_l_trans - 1), (max_r_trans - 1))
            ty = random.uniform(-(max_u_trans - 1), (max_d_trans - 1))

            M = np.array([[1, 0, tx], [0, 1, ty]])
            image = cv2.warpAffine(image, M, (w, h))

            bboxes[:, [0, 2]] = bboxes[:, [0, 2]] + tx
            bboxes[:, [1, 3]] = bboxes[:, [1, 3]] + ty

        return image, bboxes
    
    def __iter__(self):
        return self
    
    def __len__(self):
        return self.num_batchs
    
    def __next__(self):
        with tf.device('/cpu:0'):
            self.train_input_size = self.input_size
            self.train_output_sizes = [self.train_input_size // stride for stride in self.strides]

            batch_image = np.zeros((self.batch_size, self.train_input_size[0], self.train_input_size[1], 3), dtype=np.float32)
            batch_gt_bboxes = []
            batch_label_bboxes = []
            batch_bboxes = []
        
            for i in range(len(self.train_output_sizes)):
                batch_label_bboxes.append(np.zeros((self.batch_size, self.train_output_sizes[i][0], self.train_output_sizes[i][1],
                                              self.anchor_per_scale, 5 + self.num_classes), dtype=np.float32))
                batch_bboxes.append(np.zeros((self.batch_size, self.max_bbox_per_scale, 4), dtype=np.float32))

            num = 0
            if self.batch_count < self.num_batchs:
                while num < self.batch_size:
                    index = self.batch_count * self.batch_size + num
                    if index >= self.num_samples: index -= self.num_samples
                
                    annotation = self.annotations[index]
                    image_path, bboxes = self.parse_annotation(annotation)

                    if self.data_aug:
                        if self.use_mosaic_img:
                            image, bboxes = self.load_mosaic_image(self.images_path, self.labels_ori, index, self.input_size, self.scale_range)
                            image, bboxes = self.image_preprocess(np.copy(image), [*self.train_input_size], True, np.copy(bboxes))
                            image = image / 255.
                        else:
                            image = cv2.imread(image_path)
                            image, bboxes = self.random_horizontal_flip(np.copy(image), np.copy(bboxes))
                            image, bboxes = self.random_crop(np.copy(image), np.copy(bboxes))
                            image, bboxes = self.random_translate(np.copy(image), np.copy(bboxes))
                            image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
                            image, bboxes = self.image_preprocess(np.copy(image), [*self.train_input_size], True, np.copy(bboxes))
                            image = image / 255.
                    else:
                        image = cv2.imread(image_path)
                        image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
                        image, bboxes = self.image_preprocess(np.copy(image), [*self.train_input_size], True, np.copy(bboxes))
                        image = image / 255.

                    label, bboxes_xywh = self.preprocess_true_boxes(bboxes)
                    batch_image[num, :, :, :] = image
                    batch_gt_bboxes.append(bboxes)
                    for i  in range(len(label)):
                        batch_label_bboxes[i][num, :, :, :, :] = label[i]
                        batch_bboxes[i][num, :, :] = bboxes_xywh[i]
                    num += 1
            
                self.batch_count += 1
                batch_target = []
                for i in range(len(batch_label_bboxes)):
                    batch_target.append((batch_label_bboxes[i], batch_bboxes[i]))
            
                return batch_image, batch_gt_bboxes, batch_target
            else:
                self.batch_count = 0
                np.random.shuffle(self.annotations)
                raise StopIteration 

    def load_mosaic_image(self, all_img_list, all_annos, index, model_size, scale_range, filter_scale=0.0):
        max_index = len(all_annos) - 1
        idxs = [index] + [random.randint(0, max_index) for _ in range(3)]
        output_img = np.zeros([model_size[0], model_size[1], 3], dtype=np.uint8)
        scale_x = scale_range[0] + random.random() * (scale_range[1] - scale_range[0])
        scale_y = scale_range[0] + random.random() * (scale_range[1] - scale_range[0])
        divid_point_x = int(scale_x * model_size[1])
        divid_point_y = int(scale_y * model_size[0])

        new_anno = []
        for i, idx in enumerate(idxs):
            path = all_img_list[idx]
            img_annos = all_annos[idx]
            img = cv2.imread(path)
            img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
            img_height, img_width, _ = img.shape
            if i == 0:
                img = cv2.resize(img, (divid_point_x, divid_point_y))
                output_img[:divid_point_y, :divid_point_x, :] = img
                for bbox in img_annos:
                    xmin = bbox[0] * scale_x / img_width
                    ymin = bbox[1] * scale_y / img_height
                    xmax = bbox[2] * scale_x / img_width
                    ymax = bbox[3] * scale_y / img_height
                    new_anno.append([xmin, ymin, xmax, ymax, bbox[4]])
            elif i == 1:
                img = cv2.resize(img, (model_size[1] - divid_point_x, divid_point_y))
                output_img[:divid_point_y, divid_point_x:model_size[1], :] = img
                for bbox in img_annos:
                    xmin = scale_x + bbox[0] * (1 - scale_x) / img_width
                    ymin = bbox[1] * scale_y / img_height
                    xmax = scale_x + bbox[2] * (1 - scale_x) / img_width
                    ymax = bbox[3] * scale_y / img_height
                    new_anno.append([xmin, ymin, xmax, ymax, bbox[4]])
            elif i == 2:
                img = cv2.resize(img, (divid_point_x, model_size[0] - divid_point_y))
                output_img[divid_point_y:model_size[0], :divid_point_x, :] = img
                for bbox in img_annos:
                    xmin = bbox[0] * scale_x / img_width
                    ymin = scale_y + bbox[1] * (1 - scale_y) / img_height
                    xmax = bbox[2] * scale_x / img_width
                    ymax = scale_y + bbox[3] * (1 - scale_y) / img_height
                    new_anno.append([xmin, ymin, xmax, ymax, bbox[4]])
            else:
                img = cv2.resize(img, (model_size[1] - divid_point_x, model_size[0] - divid_point_y))
                output_img[divid_point_y:model_size[0], divid_point_x:model_size[1], :] = img
                for bbox in img_annos:
                    xmin = scale_x + bbox[0] * (1 - scale_x) / img_width
                    ymin = scale_y + bbox[1] * (1 - scale_y) / img_height
                    xmax = scale_x + bbox[2] * (1 - scale_x) / img_width
                    ymax = scale_y + bbox[3] * (1 - scale_y) / img_height
                    new_anno.append([xmin, ymin, xmax, ymax, bbox[4]])

        if 0 < filter_scale:
            new_anno = [anno for anno in new_anno if filter_scale < (anno[2] - anno[0]) and filter_scale < (anno[3] - anno[1])]
        new_anno = np.array(new_anno)
        new_anno[:, [0, 2]] = new_anno[:, [0, 2]] * model_size[1]
        new_anno[:, [1, 3]] = new_anno[:, [1, 3]] * model_size[1]
        return output_img, new_anno
        

        
    def bbox_iou(self, boxes1, boxes2):

        boxes1 = np.array(boxes1)
        boxes2 = np.array(boxes2)

        boxes1_area = boxes1[..., 2] * boxes2[..., 3]
        boxes2_area = boxes2[..., 2] * boxes2[..., 3]

        boxes1 = np.concatenate([boxes1[..., :2] - boxes1[..., 2:] * 0.5,
                                boxes1[..., :2] + boxes1[..., 2:] * 0.5], axis=-1)
        boxes2 = np.concatenate([boxes2[..., :2] - boxes2[..., 2:] * 0.5,
                                boxes2[..., :2] + boxes2[..., 2:] * 0.5], axis=-1)
        
        left_up = np.maximum(boxes1[..., :2], boxes2[..., :2])
        right_down = np.minimum(boxes1[..., 2:], boxes2[..., 2:])

        inter_section = np.maximum(right_down - left_up, 0.0)
        inter_area = inter_section[..., 0] * inter_section[..., 1]
        union_area = boxes1_area + boxes2_area - inter_area + 1e-9

        return inter_area / union_area
    
    def preprocess_true_boxes(self, bboxes):
        label = [np.zeros((self.train_output_sizes[i][0], self.train_output_sizes[i][1], self.anchor_per_scale, 5 + self.num_classes)) 
                    for i in range(len(cfg.YOLO.STRIDES))]

        bboxes_xywh = [np.zeros((self.max_bbox_per_scale, 4)) for _ in range(len(cfg.YOLO.STRIDES))]
        bbox_count = np.zeros((len(cfg.YOLO.STRIDES),))


        for bbox in bboxes:
            bbox_coor = bbox[:4]
            bbox_class_ind = int(bbox[4])

            onehot = np.zeros(self.num_classes, dtype=np.float32)

            onehot[bbox_class_ind] = 1.0
            uniform_distribution = np.full(self.num_classes, 1.0 / self.num_classes)
            deta = 0.01
            smooth_onehot = onehot * (1 - deta) + deta * uniform_distribution

            bbox_xywh = np.concatenate([(bbox_coor[2:] + bbox_coor[:2]) * 0.5, bbox_coor[2:] - bbox_coor[:2]], axis=-1)
            bbox_xywh_scaled = 1.0 * bbox_xywh[np.newaxis, :] / self.strides[:, np.newaxis]

            iou = []
            exist_positive = False
            for i in range(len(cfg.YOLO.STRIDES)):
                anchors_xywh = np.zeros((self.anchor_per_scale, 4))
                anchors_xywh[:, 0:2] = np.floor(bbox_xywh_scaled[i, 0:2]).astype(np.int32) + 0.5
                anchors_xywh[:, 2:4] = self.anchors[i]

                iou_scale = self.bbox_iou(bbox_xywh_scaled[i][np.newaxis, :], anchors_xywh)
                iou.append(iou_scale)
                iou_mask = iou_scale > 0.3

                if np.any(iou_mask):
                    xind, yind = np.floor(bbox_xywh_scaled[i, 0:2]).astype(np.int32)

                    label[i][yind, xind, iou_mask, :] = 0
                    label[i][yind, xind, iou_mask, 0:4] = bbox_xywh
                    label[i][yind, xind, iou_mask, 4:5] = 1.0
                    label[i][yind, xind, iou_mask, 5:] = smooth_onehot
                    
                    bbox_ind = int(bbox_count[i] % self.max_bbox_per_scale)
                    bboxes_xywh[i][bbox_ind, :4] = bbox_xywh
                    bbox_count[i] += 1

                    exist_positive = True
            if not exist_positive:
                best_anchor_ind = np.argmax(np.array(iou).reshape(-1), axis=-1)
                best_detect = int(best_anchor_ind / self.anchor_per_scale)
                best_anchor = int(best_anchor_ind % self.anchor_per_scale)
                xind, yind = np.floor(bbox_xywh_scaled[best_detect, 0:2]).astype(np.int32)

                label[best_detect][yind, xind, best_anchor, :] = 0
                label[best_detect][yind, xind, best_anchor, 0:4] = bbox_xywh
                label[best_detect][yind, xind, best_anchor, 4:5] = 1.0
                label[best_detect][yind, xind, best_anchor, 5:] = smooth_onehot

                bbox_ind = int(bbox_count[best_detect] % self.max_bbox_per_scale)
                bboxes_xywh[best_detect][bbox_ind, :4] = bbox_xywh
                bbox_count[best_detect] += 1

        return label, bboxes_xywh

class PolygoDataset(object):
    def __init__(self):
        self.annot_paths = [str(f) for f in Path(cfg.YOLO.TRAIN.ANNOT_PATH).glob("*")]


        # label_dict, image_names, boxes, width, height = load_annotation_json(self.annot_paths)

        #label_dict, boxes, image_names, width, height = load_annotation_xml(self.annot_paths)
        label_dict, boxes, image_names, width, height = load_annotation_bb_json(cfg.YOLO.TRAIN.IMG_DIR,cfg.YOLO.TRAIN.ANNOT_PATH)
        boxes = xcycwha2xyxyxyxy(boxes, with_label=True)
        self.input_size = cfg.YOLO.TRAIN.MODEL_SIZE
        self.classes = list(label_dict.keys())
        self.num_classes = len(self.classes)
        self.strides = np.array(cfg.YOLO.STRIDES)
        self.data_aug = cfg.YOLO.TRAIN.DATA_AUG
        self.use_mosaic_img = cfg.YOLO.TRAIN.USE_MOSAIC_IMG
        self.labels_ori = boxes
        self.image_paths = [os.path.join(cfg.YOLO.TRAIN.IMG_DIR,image_name) for image_name in image_names]
        self.num_samples = len(boxes)
        self.batch_size = cfg.YOLO.TRAIN.BATCH_SIZE
        self.num_batchs = int(np.ceil(self.num_samples / self.batch_size))
        self.idx = range(self.num_samples)
        self.scale_range = cfg.YOLO.TRAIN.SCALE_RANGE
        self.max_bbox_per_scale = 150
        self.batch_count = 0


    def image_preprocess(self, image, target_size, keep_ratio=True, gt_boxes=None):
        ih, iw = target_size
        h, w, _ = image.shape   
        scale = min(iw/w, ih/h)
        nw, nh = int(scale * w), int(scale * h)
        image_resized = cv2.resize(image, (nw, nh))

        if keep_ratio:
            image_padded = np.full(shape=[ih, iw, 3], fill_value=0.0)
            dw, dh = (iw - nw), (ih - nh)
            image_padded[dh:nh+dh, dw:nw+dw, :] = image_resized
            if gt_boxes is None:
                return image_padded
            else:
                gt_boxes[:, [0, 2, 4, 6]] = gt_boxes[:, [0, 2, 4, 6]] * scale + dw
                gt_boxes[:, [1, 3, 5, 7]] = gt_boxes[:, [1, 3, 5, 7]] * scale + dh
                return image_padded, gt_boxes
        else:
            if gt_boxes is None:
                return image_resized
            else:
                return image_resized, gt_boxes

    def polygon_random_perspective(self, image, bboxes, degrees=5, translate=.1, scale=.1, shear=5, perspective=0.0, border=(0, 0)):
        height = image.shape[0] + border[0] * 2
        width = image.shape[1] + border[1] * 2

        # Center
        C = np.eye(3)
        C[0, 2] = -image.shape[1] / 2  # x translation (pixels)
        C[1, 2] = -image.shape[0] / 2  # y translation (pixels)

        # Perspective
        P = np.eye(3)
        P[2, 0] = random.uniform(-perspective, perspective)  # x perspective (about y)
        P[2, 1] = random.uniform(-perspective, perspective)  # y perspective (about x)

        # Rotation and Scale
        R = np.eye(3)
        a = random.uniform(-degrees, degrees)
        # a += random.choice([-180, -90, 0, 90])  # add 90deg rotations to small rotations
        s = random.uniform(1 - scale, 1 + scale)
        # s = 2 ** random.uniform(-scale, scale)
        R[:2] = cv2.getRotationMatrix2D(angle=a, center=(0, 0), scale=s)

        # Shear
        S = np.eye(3)
        S[0, 1] = math.tan(random.uniform(-shear, shear) * math.pi / 180)  # x shear (deg)
        S[1, 0] = math.tan(random.uniform(-shear, shear) * math.pi / 180)  # y shear (deg)

        # Translation
        T = np.eye(3)
        T[0, 2] = random.uniform(0.5 - translate, 0.5 + translate) * width  # x translation (pixels)
        T[1, 2] = random.uniform(0.5 - translate, 0.5 + translate) * height  # y translation (pixels)

        # Combined rotation matrix
        M = T @ S @ R @ P @ C  # order of operations (right to left) is IMPORTANT
        image_transformed = False

        # Transform label coordinates
        n = len(bboxes)
        if n:
            new = np.zeros((n, 8))
            xy = np.ones((n * 4, 3))
            xy[:, :2] = bboxes[:, 1:].reshape(n * 4, 2)
            xy = xy @ M.T  # transform
            new = (xy[:, :2] / xy[:, 2:3] if perspective else xy[:, :2]).reshape(n, 8)

            top = max(new[:, 1::2].max().item()-height, 0)
            bottom = abs(min(new[:, 1::2].min().item(), 0))
            left = abs(min(new[:, 0::2].min().item(), 0))
            right = max(new[:, 0::2].max().item()-width, 0)

            R2 = np.eye(3)
            r = min(height/(height+top+bottom), width/(width+left+right))
            R2[:2] = cv2.getRotationMatrix2D(angle=0., center=(0, 0), scale=r)
            M2 = T @ S @ R @ R2 @ P @ C
            if (border[0] != 0) or (border[1] != 0) or (M2 != np.eye(3)).any():
                if perspective:
                    image = cv2.warpPerspective(image, M2, dsize=(width, height), borderValue=(114, 114, 114))
                else:
                    image = cv2.warpAffine(image, M2[:2], dsize=(width, height), borderValue=(114, 114, 114))
                
                image_transformed = True
                new = np.zeros((n, 8))
                xy = np.ones((n * 4, 3))
                xy[:, :2] = bboxes[:, 1:].reshape(n * 4, 2)
                xy = xy @ M2.T
                new = (xy[:, :2] / xy[:, 2:3] if perspective else xy[:, :2]).reshape(n, 8)

            cx, cy = new[:, 0::2].mean(-1), new[:, 1::2].mean(-1)
            new[(cx>width)|(cx<-0.)|(cy>height)|(cy<-0.)] = 0.

            i = self.polygon_box_candidates(box1=bboxes[:, 1:].T * s, box2=new.T, area_thr=0.08)
            bboxes = bboxes[i]
            bboxes[:, 1:] = new[i]

        if not image_transformed:
            M = T @ S @ R @ P @ C
            if (border[0] != 0) or (border[1] != 0) or (M != np.eye(3)).any():
                if perspective:
                    image = cv2.warpPerspective(image, M, dsize=(width, height), borderValue=(114, 114, 114))
                else:
                    image = cv2.warpAffine(image, M[:2], dsize=(width, height), borderValue=(114, 114, 114))
                image_transformed = True
        
        return image, bboxes

    def polygon_box_candidates(self, box1, box2, wh_thr=2, ar_thr=10, area_thr=0.1, eps=1e-16):
        w1, h1 = box1[0::2].max(axis=0)-box1[0::2].min(axis=0), box1[1::2].max(axis=0)-box1[1::2].min(axis=0)
        w2, h2 = box2[0::2].max(axis=0)-box2[0::2].min(axis=0), box2[1::2].max(axis=0)-box2[1::2].min(axis=0)
        ar = np.maximum(w2 / (h2 + eps), h2 / (w2 + eps))
        return (w2 > wh_thr) & (h2 > wh_thr) & (w2 * h2 / (w1 * h1 + eps) > area_thr) & (ar < ar_thr)

    def random_horizontal_flip(self, image, bboxes):
        if random.random() < 0.5:
            _, w, _ = image.shape
            image = image[:, ::-1, :]
            bboxes[:, [0,2,4,6]] = w - bboxes[:, [6,4,2,0]]
        return image, bboxes
    

    def random_crop(self, image, bboxes):
        if random.random() < 0.5:
            h, w, _ = image.shape
            max_bbox = np.concatenate([np.min(bboxes[:, 0:2], axis=0), np.max(bboxes[:, 4:6], axis=0)], axis=-1)

            max_l_trans = max_bbox[0]
            max_u_trans = max_bbox[1]
            max_r_trans = w - max_bbox[2]
            max_d_trans = h - max_bbox[3]

            crop_xmin = max(0, int(max_bbox[0] - random.uniform(0, max_l_trans)))
            crop_ymin = max(0, int(max_bbox[1] - random.uniform(0, max_u_trans)))
            crop_xmax = max(w, int(max_bbox[2] + random.uniform(0, max_r_trans)))
            crop_ymax = max(h, int(max_bbox[3] + random.uniform(0, max_d_trans)))

            image = image[crop_ymin : crop_ymax, crop_xmin : crop_xmax]

            bboxes[:, [0, 2, 4, 6]] = bboxes[:, [0, 2, 4, 6]] - crop_xmin
            bboxes[:, [1, 3, 5, 7]] = bboxes[:, [1, 3, 5, 7]] - crop_ymin

        return image, bboxes

    def random_translate(self, image, bboxes):
        if random.random() < 0.5:
            h, w, _ = image.shape
            max_bbox = np.concatenate([np.min(bboxes[:, 0:2], axis=0), np.max(bboxes[:, 4:6], axis=0)], axis=-1)

            max_l_trans = max_bbox[0]
            max_u_trans = max_bbox[1]
            max_r_trans = w - max_bbox[2]
            max_d_trans = h - max_bbox[3]

            tx = random.uniform(-(max_l_trans - 1), (max_r_trans - 1))
            ty = random.uniform(-(max_u_trans - 1), (max_d_trans - 1))

            M = np.array([[1, 0, tx], [0, 1, ty]])
            image = cv2.warpAffine(image, M, (w, h))

            bboxes[:, [0, 2, 4, 6]] = bboxes[:, [0, 2, 4, 6]] + tx
            bboxes[:, [1, 3, 5, 7]] = bboxes[:, [1, 3, 5, 7]] + ty

        return image, bboxes
    

    def load_mosaic_image(self, all_img_list, all_annos, index, model_size, scale_range, filter_scale=0.0):
        max_index = len(all_annos) - 1
        idxs = [index] + [random.randint(0, max_index) for _ in range(3)]
        output_img = np.zeros([model_size[0], model_size[1], 3], dtype=np.uint8)
        scale_x = scale_range[0] + random.random() * (scale_range[1] - scale_range[0])
        scale_y = scale_range[0] + random.random() * (scale_range[1] - scale_range[0])
        divid_point_x = int(scale_x * model_size[1])
        divid_point_y = int(scale_y * model_size[0])

        new_anno = []
        for i, idx in enumerate(idxs):
            path = all_img_list[idx]
            img_annos = all_annos[idx]
            img = cv2.imread(path)
            img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
            img_height, img_width, _ = img.shape
            if i == 0:
                img = cv2.resize(img, (divid_point_x, divid_point_y))
                output_img[:divid_point_y, :divid_point_x, :] = img
                for bbox in img_annos:
                    x1 = bbox[0] * scale_x / img_width
                    y1 = bbox[1] * scale_y / img_height
                    x2 = bbox[2] * scale_x / img_width
                    y2 = bbox[3] * scale_y / img_height
                    x3 = bbox[4] * scale_x / img_width
                    y3 = bbox[5] * scale_y / img_height
                    x4 = bbox[6] * scale_x / img_width
                    y4 = bbox[7] * scale_y / img_height
                    new_anno.append([x1, y1, x2, y2, x3, y3, x4, y4, bbox[8]])
            elif i == 1:
                img = cv2.resize(img, (model_size[1] - divid_point_x, divid_point_y))
                output_img[:divid_point_y, divid_point_x:model_size[1], :] = img
                for bbox in img_annos:
                    x1 = scale_x + bbox[0] * (1 - scale_x) / img_width
                    y1 = bbox[1] * scale_y / img_height
                    x2 = scale_x + bbox[2] * (1 - scale_x) / img_width
                    y2 = bbox[3] * scale_y / img_height
                    x3 = scale_x + bbox[4] * (1 - scale_x) / img_width
                    y3 = bbox[5] * scale_y / img_height
                    x4 = scale_x + bbox[6] * (1 - scale_x) / img_width
                    y4 = bbox[7] * scale_y / img_height
                    new_anno.append([x1, y1, x2, y2, x3, y3, x4, y4, bbox[8]])
            elif i == 2:
                img = cv2.resize(img, (divid_point_x, model_size[0] - divid_point_y))
                output_img[divid_point_y:model_size[0], :divid_point_x, :] = img
                for bbox in img_annos:
                    x1 = bbox[0] * scale_x / img_width
                    y1 = scale_y + bbox[1] * (1 - scale_y) / img_height
                    x2 = bbox[2] * scale_x / img_width
                    y2 = scale_y + bbox[3] * (1 - scale_y) / img_height
                    x3 = bbox[4] * scale_x / img_width
                    y3 = scale_y + bbox[5] * (1 - scale_y) / img_height
                    x4 = bbox[6] * scale_x / img_width
                    y4 = scale_y + bbox[7] * (1 - scale_y) / img_height
                    new_anno.append([x1, y1, x2, y2, x3, y3, x4, y4, bbox[8]])
            else:
                img = cv2.resize(img, (model_size[1] - divid_point_x, model_size[0] - divid_point_y))
                output_img[divid_point_y:model_size[0], divid_point_x:model_size[1], :] = img
                for bbox in img_annos:
                    x1 = scale_x + bbox[0] * (1 - scale_x) / img_width
                    y1 = scale_y + bbox[1] * (1 - scale_y) / img_height
                    x2 = scale_x + bbox[2] * (1 - scale_x) / img_width
                    y2 = scale_y + bbox[3] * (1 - scale_y) / img_height
                    x3 = scale_x + bbox[4] * (1 - scale_x) / img_width
                    y3 = scale_y + bbox[5] * (1 - scale_y) / img_height
                    x4 = scale_x + bbox[6] * (1 - scale_x) / img_width
                    y4 = scale_y + bbox[7] * (1 - scale_y) / img_height
                    new_anno.append([x1, y1, x2, y2, x3, y3, x4, y4, bbox[8]])

        if 0 < filter_scale:
            new_anno = [anno for anno in new_anno if filter_scale < (anno[2] - anno[0]) and filter_scale < (anno[3] - anno[1])]
        new_anno = np.array(new_anno)
        new_anno[:, [0, 2, 4, 6]] = new_anno[:, [0, 2, 4, 6]] * model_size[1]
        new_anno[:, [1, 3, 5, 7]] = new_anno[:, [1, 3, 5, 7]] * model_size[1]
        return output_img, new_anno

    def __iter__(self):
        return self

    def __len__(self):
        return self.num_batchs

    def __next__(self):
        with tf.device('/cpu:0'):
            self.train_input_size = self.input_size
            self.train_output_sizes = [self.train_input_size // stride for stride in self.strides]

            batch_image = np.zeros((self.batch_size, self.train_input_size[0], self.train_input_size[1], 3), dtype=np.float32)
            batch_gt_bboxes = []
            batch_label_bboxes = []
            batch_bboxes = []
            for i in range(len(self.train_output_sizes)):
                batch_label_bboxes.append(np.zeros((self.batch_size, self.train_output_sizes[i][0], self.train_output_sizes[i][1], 5 + self.num_classes), dtype=np.float32))
                batch_bboxes.append(np.zeros((self.batch_size, self.max_bbox_per_scale, 4), dtype=np.float32))
            
            num = 0
            if self.batch_count < self.num_batchs:
                while num < self.batch_size:
                    index = self.batch_count * self.batch_size + num
                    if index >= self.num_samples: index -= self.num_samples

                    image_path = self.image_paths[index]
                    bboxes = np.array(self.labels_ori[index])
                    if self.data_aug:
                        if self.use_mosaic_img:
                            image, bboxes = self.load_mosaic_image(self.image_paths, self.labels_ori, index, self.input_size, self.scale_range)
                            image, bboxes = self.image_preprocess(np.copy(image), [*self.train_input_size], True, np.copy(bboxes))
                            image = image / 255.
                        else:
                            image = cv2.imread(image_path)
                            image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
                            bboxes = np.concatenate([bboxes[..., -1:], bboxes[..., 0:-1]], axis=-1)
                            if random.random() > 0.5:
                                image, bboxes = self.polygon_random_perspective(image, bboxes)
                            bboxes = np.concatenate([bboxes[..., 1:], bboxes[..., 0:1]], axis=-1)
                            image, bboxes = self.image_preprocess(np.copy(image), [*self.train_input_size], True, np.copy(bboxes))
                            image = image / 255.
                    else:
                        image = cv2.imread(image_path)
                        if image.ndim < 3:
                            image = cv2.merge([image, image, image])
                        image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
                        image, bboxes = self.image_preprocess(np.copy(image), [*self.train_input_size], True, np.copy(bboxes))
                        image = image / 255.
                    
                    label, bboxes_xyrm = self.preprocess_true_boxes(bboxes)

                    batch_image[num, :, :, :] = image
                    batch_gt_bboxes.append(bboxes)
                    for i  in range(len(label)):
                        batch_label_bboxes[i][num, :, :, :] = label[i]
                        batch_bboxes[i][num, :, :] = bboxes_xyrm[i]
                    num += 1
                
                self.batch_count += 1
                batch_target = []
                for i in range(len(batch_label_bboxes)):
                    batch_target.append((batch_label_bboxes[i], batch_bboxes[i]))
                
                return batch_image, batch_gt_bboxes, batch_target
            else:
                self.batch_count = 0
                raise StopIteration

    def bbox_iou(self, boxes1, boxes2):

        boxes1 = np.array(boxes1)
        boxes2 = np.array(boxes2)

        boxes1_area = boxes1[..., 2] * boxes2[..., 3]
        boxes2_area = boxes2[..., 2] * boxes2[..., 3]

        boxes1 = np.concatenate([boxes1[..., :2] - boxes1[..., 2:4] * 0.5,
                                boxes1[..., :2] + boxes1[..., 2:4] * 0.5], axis=-1)
        boxes2 = np.concatenate([boxes2[..., :2] - boxes2[..., 2:4] * 0.5,
                                boxes2[..., :2] + boxes2[..., 2:4] * 0.5], axis=-1)
        
        left_up = np.maximum(boxes1[..., :2], boxes2[..., :2])
        right_down = np.minimum(boxes1[..., 2:4], boxes2[..., 2:4])

        inter_section = np.maximum(right_down - left_up, 0.0)
        inter_area = inter_section[..., 0] * inter_section[..., 1]
        union_area = boxes1_area + boxes2_area - inter_area + 1e-9

        return inter_area / union_area

    def preprocess_true_boxes(self, bboxes):
        label = [np.zeros((self.train_output_sizes[i][0], self.train_output_sizes[i][1], 5 + self.num_classes)) 
                    for i in range(len(cfg.YOLO.STRIDES))]

        bboxes_xyrm = [np.zeros((self.max_bbox_per_scale, 4)) for _ in range(len(cfg.YOLO.STRIDES))]

        bbox_count = np.zeros((len(cfg.YOLO.STRIDES),))

        bbox_xyxyxyxy = bboxes

        bbox_xyxyxyxy = order_corners(bbox_xyxyxyxy[..., :-1])

        class_ind = bboxes[..., -1:]

        bboxes = xyxyxyxy2xywhrm([bbox_xyxyxyxy], with_label=False)[0]

        bboxes = np.concatenate([bboxes, class_ind], axis=-1)

        for idx, bbox in enumerate(bboxes):
            bbox_coor = bbox[:2]
            bbox_real = bbox[4]
            bbox_imagin = bbox[5]
            bbox_class_ind = int(bbox[6])

            onehot = np.zeros(self.num_classes, dtype=np.float32) 

            onehot[bbox_class_ind] = 1.0
            uniform_distribution = np.full(self.num_classes, 1.0 / self.num_classes)
            deta = 0.01
            smooth_onehot = onehot * (1 - deta) + deta * uniform_distribution

            for i in range(len(cfg.YOLO.STRIDES)):
                xind, yind = np.floor(bbox_coor / cfg.YOLO.STRIDES[i]).astype(np.int32)
                xind = np.clip(xind, 0, self.train_output_sizes[i][0] - 1)     
                yind = np.clip(yind, 0, self.train_output_sizes[i][1] - 1)

                label[i][yind, xind, :] = 0
                label[i][yind, xind, 0:2] = bbox_coor
                label[i][yind, xind, 2:3] = bbox_real
                label[i][yind, xind, 3:4] = bbox_imagin
                label[i][yind, xind, 4:5] = 1.0
                label[i][yind, xind, 5:] = smooth_onehot

                bbox_ind = int(bbox_count[i] % self.max_bbox_per_scale)
                bboxes_xyrm[i][bbox_ind, 0:2] = bbox_coor
                bboxes_xyrm[i][bbox_ind, 2:3] = bbox_real
                bboxes_xyrm[i][bbox_ind, 3:4] = bbox_imagin
                bbox_count[i] += 1

        return label, bboxes_xyrm