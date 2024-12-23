from config import *

def bbox_iou(boxes1, boxes2):

    boxes1_area = boxes1[..., 2] * boxes1[..., 3]
    boxes2_area = boxes2[..., 2] * boxes2[..., 3]

    boxes1 = tf.concat([boxes1[..., :2] - boxes1[..., 2:] * 0.5,
                        boxes1[..., :2] + boxes1[..., 2:] * 0.5], axis=-1)
    boxes2 = tf.concat([boxes2[..., :2] - boxes2[..., 2:] * 0.5,
                        boxes2[..., :2] + boxes2[..., 2:] * 0.5], axis=-1)

    left_up = tf.maximum(boxes1[..., :2], boxes2[..., :2])
    right_down = tf.minimum(boxes1[..., 2:], boxes2[..., 2:])

    inter_section = tf.maximum(right_down - left_up, 0.0)
    inter_area = inter_section[..., 0] * inter_section[..., 1]
    union_area = boxes1_area + boxes2_area - inter_area + 5e-10

    return 1.0 * inter_area / union_area



def bbox_giou(boxes1, boxes2):

    boxes1 = tf.concat([boxes1[..., :2] - boxes1[..., 2:] * 0.5,
                        boxes1[..., :2] + boxes1[..., 2:] * 0.5], axis=-1)
    boxes2 = tf.concat([boxes2[..., :2] - boxes2[..., 2:] * 0.5,
                        boxes2[..., :2] + boxes2[..., 2:] * 0.5], axis=-1)

    boxes1 = tf.concat([tf.minimum(boxes1[..., :2], boxes1[..., 2:]),
                        tf.maximum(boxes1[..., :2], boxes1[..., 2:])], axis=-1)
    boxes2 = tf.concat([tf.minimum(boxes2[..., :2], boxes2[..., 2:]),
                        tf.maximum(boxes2[..., :2], boxes2[..., 2:])], axis=-1)

    boxes1_area = (boxes1[..., 2] - boxes1[..., 0]) * (boxes1[..., 3] - boxes1[..., 1])
    boxes2_area = (boxes2[..., 2] - boxes2[..., 0]) * (boxes2[..., 3] - boxes2[..., 1])

    left_up = tf.maximum(boxes1[..., :2], boxes2[..., :2])
    right_down = tf.minimum(boxes1[..., 2:], boxes2[..., 2:])

    inter_section = tf.maximum(right_down - left_up, 0.0)
    inter_area = inter_section[..., 0] * inter_section[..., 1]
    union_area = boxes1_area + boxes2_area - inter_area + 1e-10
    iou = inter_area / union_area

    enclose_left_up = tf.minimum(boxes1[..., :2], boxes2[..., :2])
    enclose_right_down = tf.maximum(boxes1[..., 2:], boxes2[..., 2:])
    enclose = tf.maximum(enclose_right_down - enclose_left_up, 0.0)
    enclose_area = enclose[..., 0] * enclose[..., 1] + 1e-10
    giou = iou - 1.0 * (enclose_area - union_area) / enclose_area

    return giou

def bbox_ciou(boxes1, boxes2, epsilon=1e-10):
    b1x1, b1x2 = boxes1[..., 0] - boxes1[..., 2] / 2, boxes1[..., 0] + boxes1[..., 2] / 2
    b1y1, b1y2 = boxes1[..., 1] - boxes1[..., 3] / 2, boxes1[..., 1] + boxes1[..., 3] / 2
    b2x1, b2x2 = boxes2[..., 0] - boxes2[..., 2] / 2, boxes2[..., 0] + boxes2[..., 2] / 2
    b2y1, b2y2 = boxes2[..., 1] - boxes2[..., 3] / 2, boxes2[..., 1] + boxes2[..., 3] / 2

    # intersection area
    inter = tf.maximum(tf.minimum(b1x2, b2x2) - tf.maximum(b1x1, b2x1), 0) * tf.maximum(tf.minimum(b1y2, b2y2) - tf.maximum(b1y1, b2y1), 0)

    # union area
    w1, h1 = b1x2 - b1x1 + epsilon, b1y2 - b1y1 + epsilon
    w2, h2 = b2x2 - b2x1+ epsilon, b2y2 - b2y1 + epsilon
    union = w1 * h1 + w2 * h2 - inter + epsilon

    # iou
    iou = inter / union

    # enclosing box
    cw = tf.maximum(b1x2, b2x2) - tf.minimum(b1x1, b2x1)
    ch = tf.maximum(b1y2, b2y2) - tf.minimum(b1y1, b2y1)

    c2 = cw ** 2 + ch ** 2 + epsilon
    rho2 = ((b2x1 + b2x2) - (b1x1 + b1x2)) ** 2 / 4 + ((b2y1 + b2y2) - (b1y1 + b1y2)) ** 2 / 4

    v = (4 / math.pi ** 2) * tf.pow(tf.atan(w2 / h2) - tf.atan(w1 / h1), 2)
    alpha = v / (1 - iou + v)

    ciou = iou - (rho2 / c2 + v * alpha)
    return ciou

def compute_loss(pred, conv, label, bboxes):
    input_size = cfg.YOLO.TRAIN.MODEL_SIZE[0]
    input_size = tf.cast(input_size, tf.float32)

    conv_raw_conf = conv[:, :, :, :, 4:5]
    conv_raw_prob = conv[:, :, :, :, 5:]

    pred_xywh     = pred[:, :, :, :, 0:4]
    pred_conf     = pred[:, :, :, :, 4:5]

    label_xywh    = label[:, :, :, :, 0:4]
    respond_bbox  = label[:, :, :, :, 4:5]
    label_prob    = label[:, :, :, :, 5:]

    bbox_loss_scale = 2.0 - 1.0 * label_xywh[:, :, :, :, 2:3] * label_xywh[:, :, :, :, 3:4] / (input_size ** 2)

    giou_loss = ciou_loss = 0

    if cfg.YOLO.TRAIN.LOSS_TYPE_FOR_BBOXES == 'giou':
        giou = tf.expand_dims(bbox_giou(pred_xywh, label_xywh), axis=-1)
        giou_loss = respond_bbox * bbox_loss_scale * (1 - giou)
        giou_loss = tf.reduce_mean(tf.reduce_sum(giou_loss, axis=[1,2,3,4]))

    elif cfg.YOLO.TRAIN.LOSS_TYPE_FOR_BBOXES == 'ciou':
        ciou = tf.expand_dims(bbox_ciou(pred_xywh, label_xywh), axis=-1)
        ciou_loss = respond_bbox * bbox_loss_scale * (1 - ciou)
        ciou_loss = tf.reduce_mean(tf.reduce_sum(ciou_loss, axis=[1,2,3,4]))
    

    iou = bbox_iou(pred_xywh[:, :, :, :, np.newaxis, :], bboxes[:, np.newaxis, np.newaxis, np.newaxis, :, :])
    max_iou = tf.expand_dims(tf.reduce_max(iou, axis=-1), axis=-1)  

    respond_bgd = (1.0 - respond_bbox) * tf.cast( max_iou < cfg.YOLO.IOU_LOSS_THRESH, tf.float32)

    conf_focal = tf.pow(respond_bbox - pred_conf, 2)

    conf_loss = conf_focal * (
            respond_bbox * tf.nn.sigmoid_cross_entropy_with_logits(labels=respond_bbox, logits=conv_raw_conf)
            +
            respond_bgd * tf.nn.sigmoid_cross_entropy_with_logits(labels=respond_bbox, logits=conv_raw_conf)
    )

    prob_loss = respond_bbox * tf.nn.sigmoid_cross_entropy_with_logits(labels=label_prob, logits=conv_raw_prob)

    lbox = giou_loss + ciou_loss
    conf_loss = tf.reduce_mean(tf.reduce_sum(conf_loss, axis=[1,2,3,4]))
    prob_loss = tf.reduce_mean(tf.reduce_sum(prob_loss, axis=[1,2,3,4]))

    return lbox, conf_loss, prob_loss
def compute_r_yolo_loss(pred, conv, label, bboxes):

    conv_raw_conf = conv[:, :, :, 4:5]
    conv_raw_prob = conv[:, :, :, 5:]

    pred_xyrm   = pred[:, :, :, 0:4]
    pred_conf     = pred[:, :, :, 4:5]

    label_xyrm  = label[:, :, :, 0:4]
    respond_bbox  = label[:, :, :, 4:5]
    label_prob    = label[:, :, :, 5:]

    constraints_sin_cos = (1 - (pred_xyrm[..., 2] ** 2 + pred_xyrm[..., 3] ** 2)) ** 2
    
    constraints_sin_cos = tf.expand_dims(constraints_sin_cos, axis=-1)

    beta = 0.11

    smoothing_l1_loss = tf.losses.huber(label_xyrm, pred_xyrm, delta=beta)

    smoothing_l1_loss = tf.expand_dims(smoothing_l1_loss, axis=-1)

    lbox = respond_bbox * (smoothing_l1_loss + constraints_sin_cos)

    respond_bgd = (1.0 - respond_bbox)

    conf_focal = tf.pow(respond_bbox - pred_conf, 2)

    conf_loss = conf_focal * (
            respond_bbox * tf.nn.sigmoid_cross_entropy_with_logits(labels=respond_bbox, logits=conv_raw_conf)
            +
            respond_bgd * tf.nn.sigmoid_cross_entropy_with_logits(labels=respond_bbox, logits=conv_raw_conf)
    )

    prob_loss = respond_bbox * tf.nn.sigmoid_cross_entropy_with_logits(labels=label_prob, logits=conv_raw_prob)

    lbox = tf.reduce_mean(tf.reduce_sum(lbox, axis=[1,2,3]))
    conf_loss = tf.reduce_mean(tf.reduce_sum(conf_loss, axis=[1,2,3]))
    prob_loss = tf.reduce_mean(tf.reduce_sum(prob_loss, axis=[1,2,3]))

    return lbox, conf_loss, prob_loss