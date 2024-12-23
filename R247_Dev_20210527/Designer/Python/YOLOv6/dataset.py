from config import *
from utils import *
from pathlib import Path

class YOLODataset(object):
    def __init__(self):
        super(YOLODataset, self).__init__()
        self.annot_path = cfg.YOLO.TRAIN.ANNOT_PATH
        self.input_size = cfg.YOLO.TRAIN.MODEL_SIZE
        self.img_dir = cfg.YOLO.TRAIN.IMG_DIR
        self.batch_size = cfg.YOLO.TRAIN.BATCH_SIZE
        self.strides = np.array(cfg.YOLO.STRIDES)
        self.classes = self.read_class_names(cfg.YOLO.CLASSES)
        self.num_classes = len(self.classes)
        self.anchors = (cfg.YOLO.ANCHORS.reshape(len(cfg.YOLO.STRIDES), cfg.YOLO.ANCHOR_PER_SCALE, 2).T / np.array(cfg.YOLO.STRIDES)).T  
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
            image_padded = np.full(shape=[ih, iw, 3], fill_value=0.0)
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
    
    def read_class_names(self, class_file_name):
        names = {}
        for ID, data in enumerate(class_file_name):
            names[data.Name] = ID
        return names
    
    def load_annotations(self):
        annotations=[]
        imagesPath = glob.glob( os.path.join(self.img_dir,"*.jpg")) + \
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
                        if(label in self.classes):
                            [bboxes.append(value) for value in [x, y, x+w, y+h, self.classes[label]]]                      
                annotations.append([imagesPath[i],*bboxes])
        # upsample for using batch_size to improve learning speed and accurate
        while len(annotations) <= self.batch_size * 2:
            annotations += annotations.copy()
        np.random.shuffle(annotations)
        return annotations
    
    def parse_annotation(self, annotation):
        image_path = annotation[0]
        if not os.path.exists(image_path):
            raise KeyError("%s does not exist ... " %image_path)
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