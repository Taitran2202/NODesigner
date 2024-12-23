from config import *


def load_yolo_weights(model, weights_file):
    range1 = 110 if not cfg.YOLO.TRAIN_YOLO_TINY else 21
    range2 = [93, 101, 109] if not cfg.YOLO.TRAIN_YOLO_TINY else [17, 20]
    with open(weights_file, 'rb') as wf:
        major, minor, revision, seen, _ = np.fromfile(wf, dtype=np.int32, count=5)
        
        j = 0
        for i in range(range1):
            if i > 0:
                conv_layer_name = 'conv2d_%d' %i
            else:
                conv_layer_name = 'conv2d'
            
            if j > 0:
                bn_layer_name = 'batch_normalization_%d' %j
            else:
                bn_layer_name = 'batch_normalization'
            
            conv_layer = model.get_layer(conv_layer_name)
            filters = conv_layer.filters
            k_size = conv_layer.kernel_size[0]
            in_dim = conv_layer.input_shape[-1]

            if i not in range2:
                bn_weights = np.fromfile(wf, dtype=np.float32, count=4 * filters)
                bn_weights = bn_weights.reshape((4, filters))[[1, 0, 2, 3]]
                bn_layer = model.get_layer(bn_layer_name)
                j += 1
            else:
                conv_bias = np.fromfile(wf, dtype=np.float32, count=filters)
                

            conv_shape = (filters, in_dim, k_size, k_size)
            conv_weights = np.fromfile(wf, dtype=np.float32, count=np.product(conv_shape))
            conv_weights = conv_weights.reshape(conv_shape).transpose([2, 3, 1, 0])

            if i not in range2:
                conv_layer.set_weights([tf.convert_to_tensor(conv_weights)])
                bn_layer.set_weights(tf.convert_to_tensor(bn_weights))
            else:
                conv_layer.set_weights([tf.convert_to_tensor(conv_weights), tf.convert_to_tensor(conv_bias)])

def read_class_names(yolo_classes):
    names = {}
    for ID, data in enumerate(yolo_classes):
        names[ID] = data.Name
    return names

def get_key(val, my_dict):
    for key, value in my_dict.items():
         if val == value:
             return key
 
    return -1
def load_annotation_bb_json(image_dir,annotation_dir):
    label_dict = {}
    boxes = []
    image_names = []
    imagesPath =glob.glob( os.path.join(image_dir,"*.jpg")) + \
                glob.glob( os.path.join(image_dir,"*.png")) +  \
                glob.glob( os.path.join(image_dir,"*.bmp"))+ \
                glob.glob( os.path.join(image_dir,"*.JPEG"))
    for i in range(len(imagesPath)):
        img_name = os.path.basename(imagesPath[i])
        annotation_path= os.path.join(annotation_dir, img_name+'.txt')
        box = []
        if os.path.exists(annotation_path):
            with open(annotation_path) as json_file:
                image_names.append(img_name)
                data = json.load(json_file)
                for p in data:
                    x=int(p['x']+p['w']/2)
                    y=int(p['y']+p['h']/2)
                    w=int(p['w'])
                    h=int(p['h'])
                    label_name = p['annotation']
                    if not label_name in list(label_dict.keys()):
                        values = list(label_dict.values())
                        if len(values) == 0:
                            label_dict[label_name] = 0
                        else:
                            label_dict[label_name] = values[-1] + 1
                box.append([x, y, w, h, 0, label_dict[label_name]])
            boxes.append(np.array(box, dtype=np.float32))
    return label_dict, boxes, image_names, None, None

def xcycwha2xyxyxyxy(batches, with_label=True):
    boxes_list = []
    if with_label:
        for bboxes in batches:
            bboxes = np.array(bboxes)
            x1, y1, x2, y2, x3, y3, x4, y4 = -bboxes[:, 2:3]/2, bboxes[:, 3:4]/2, bboxes[:, 2:3]/2, bboxes[:, 3:4]/2, \
                                              bboxes[:, 2:3]/2, -bboxes[:, 3:4]/2, -bboxes[:, 2:3]/2, -bboxes[:, 3:4]/2
            xyxyxyxy = np.concatenate((x1, y1, x2, y2, x3, y3, x4, y4), axis=-1).reshape(-1, 4, 2)
            R = np.zeros((xyxyxyxy.shape[0], 2, 2), dtype=np.float32)
            cos_theta = np.cos(bboxes[:, 4])
            sin_theta = np.sin(bboxes[:, 4])
            R[:, 0, 0], R[:, 1, 1] = cos_theta, cos_theta
            R[:, 0, 1], R[:, 1, 0] = sin_theta, -sin_theta

            xyxyxyxy = np.matmul(xyxyxyxy, R).reshape(-1, 8) + np.array(bboxes[:, [0, 1, 0, 1, 0, 1, 0, 1]])
            cls = bboxes[:, -1]
            new_boxes = np.concatenate([xyxyxyxy, cls[:, np.newaxis]], axis=-1)
            boxes_list.append(new_boxes)
    else:
        for bboxes in batches:
            bboxes = np.array(bboxes)
            x1, y1, x2, y2, x3, y3, x4, y4 = -bboxes[:, 2:3]/2, bboxes[:, 3:4]/2, bboxes[:, 2:3]/2, bboxes[:, 3:4]/2, \
                                              bboxes[:, 2:3]/2, -bboxes[:, 3:4]/2, -bboxes[:, 2:3]/2, -bboxes[:, 3:4]/2
            xyxyxyxy = np.concatenate((x1, y1, x2, y2, x3, y3, x4, y4), axis=-1).reshape(-1, 4, 2)
            R = np.zeros((xyxyxyxy.shape[0], 2, 2), dtype=np.float32)
            cos_theta = np.cos(bboxes[:, 4])
            sin_theta = np.sin(bboxes[:, 4])
            R[:, 0, 0], R[:, 1, 1] = cos_theta, cos_theta
            R[:, 0, 1], R[:, 1, 0] = sin_theta, -sin_theta

            xyxyxyxy = np.matmul(xyxyxyxy, R).reshape(-1, 8) + np.array(bboxes[:, [0, 1, 0, 1, 0, 1, 0, 1]])
            new_boxes = xyxyxyxy
            boxes_list.append(new_boxes)

    return np.array(boxes_list)
def order_corners(boxes):
    """
        Return sorted corners for loss.py::class Polygon_ComputeLoss::build_targets
        Sorted corners have the following restrictions: 
                                y3, y4 >= y1, y2; x1 <= x2; x4 <= x3
    """
    if not isinstance(boxes, np.ndarray):
        boxes = np.array(boxes, np.float32)
    #boxes = torch.from_numpy(boxes)
    if boxes.shape[0] == 0:
        return np.empty(0, 8)
    boxes = np.reshape(boxes,(-1, 4, 2))
    x = boxes[..., 0]
    y = boxes[..., 1]
    y_indices = np.argsort(y) # sort y
    y_sorted = np.sort(y)
    idx = np.arange(0, y.shape[0], dtype="int32")
    complete_idx = idx[:, None].repeat(4).reshape(-1,4)
    x_sorted = x[complete_idx, y_indices]
    x_sorted[:, :2] = np.sort(x_sorted[:, :2])
    x_bottom_indices = np.argsort(x_sorted[:, 0])
    x_sorted[:, 2:4] = np.sort(x_sorted[:, 2:4])[::-1]
    x_top_indices = np.argsort(x_sorted[:, 2])[::-1]
    y_sorted[idx, :2] = y_sorted[idx, :2][complete_idx[:, :2], x_bottom_indices]
    y_sorted[idx, 2:4] = y_sorted[idx, 2:4][complete_idx[:, 2:4], x_top_indices]
    
    # prevent the ambiguous case when the diagonal of the quadrilateral is parallel to the x-axis
    special = (y_sorted[:, 1] == y_sorted[:, 2]) & (x_sorted[:, 1] > x_sorted[:, 2])
    if idx[special].shape[0] != 0:
        x_sorted_1 = x_sorted[idx[special], 1].clone()
        x_sorted[idx[special], 1] = x_sorted[idx[special], 2]
        x_sorted[idx[special], 2] = x_sorted_1
    return np.stack((x_sorted, y_sorted), axis=2).reshape(-1, 8)
def xyxyxyxy2xywhrm(batches, with_label=True):
    boxes_list = []
    if with_label:
        for bboxes in batches:
            w = np.maximum(np.sqrt((bboxes[:, 2] - bboxes[:, 0]) ** 2 + (bboxes[:, 3] - bboxes[:, 1]) ** 2), 
                           np.sqrt((bboxes[:, 6] - bboxes[:, 4]) ** 2 + (bboxes[:, 7] - bboxes[:, 5]) ** 2))
            h = np.maximum(np.sqrt((bboxes[:, 4] - bboxes[:, 2]) ** 2 + (bboxes[:, 5] - bboxes[:, 3]) ** 2), 
                           np.sqrt((bboxes[:, 6] - bboxes[:, 0]) ** 2 + (bboxes[:, 7] - bboxes[:, 1]) ** 2))
            xc, yc = bboxes[:, 0:8:2].mean(axis=-1), bboxes[:, 1:8:2].mean(axis=-1)
            theta_tan = (bboxes[:, 3]-bboxes[:, 1])/(bboxes[:, 2]-bboxes[:, 0])
            theta = np.arctan(theta_tan)
            while np.any(theta > (np.pi / 2)):
                theta = np.where(theta > (np.pi / 2), theta - np.pi, theta)
            while np.any(theta < (-np.pi / 2)):
                theta = np.where(theta > (np.pi / 2), theta + np.pi, theta)
            re = np.cos(theta)
            im = np.sin(theta)
            cls = bboxes[:, -1]
            new_boxes = np.concatenate([xc[..., np.newaxis], yc[..., np.newaxis], w[..., np.newaxis], h[..., np.newaxis], 
                                        re[..., np.newaxis], im[..., np.newaxis], cls[..., np.newaxis]], axis=-1)
            boxes_list.append(new_boxes)
    else:
        for bboxes in batches:
            w = np.maximum(np.sqrt((bboxes[:, 2] - bboxes[:, 0]) ** 2 + (bboxes[:, 3] - bboxes[:, 1]) ** 2), 
                           np.sqrt((bboxes[:, 6] - bboxes[:, 4]) ** 2 + (bboxes[:, 7] - bboxes[:, 5]) ** 2))
            h = np.maximum(np.sqrt((bboxes[:, 4] - bboxes[:, 2]) ** 2 + (bboxes[:, 5] - bboxes[:, 3]) ** 2), 
                           np.sqrt((bboxes[:, 6] - bboxes[:, 0]) ** 2 + (bboxes[:, 7] - bboxes[:, 1]) ** 2))
            xc, yc = bboxes[:, 0:8:2].mean(axis=-1), bboxes[:, 1:8:2].mean(axis=-1)
            theta_tan = (bboxes[:, 3]-bboxes[:, 1])/(bboxes[:, 2]-bboxes[:, 0])
            theta = np.arctan(theta_tan)

            while np.any(theta > (np.pi / 2)):
                theta = np.where(theta > (np.pi / 2), theta - np.pi, theta)
            while np.any(theta < (-np.pi / 2)):
                theta = np.where(theta > (np.pi / 2), theta + np.pi, theta)
            re = np.cos(theta)
            im = np.sin(theta)
            new_boxes = np.concatenate([xc[..., np.newaxis], yc[..., np.newaxis], w[..., np.newaxis], h[..., np.newaxis], 
                                        re[..., np.newaxis], im[..., np.newaxis]], axis=-1)
            boxes_list.append(new_boxes)
    
    return np.array(boxes_list, dtype=np.float32)