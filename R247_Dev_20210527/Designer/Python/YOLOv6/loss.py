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


def bbox_iou_loss(boxes1, boxes2, iou_type='siou', epsilon=1e-10):
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

    if iou_type == 'giou':
        c_area = cw * ch + epsilon
        iou = iou - (c_area - union) / c_area
        return iou
    elif iou_type in ['diou', 'ciou']:
        c2 = cw ** 2 + ch ** 2 + epsilon
        rho2 = ((b2x1 + b2x2 - b1x1 - b1x2) ** 2 + (b2y1 + b2y2 - b1y1 - b1y2) ** 2) / 4
        if iou_type == 'diou':
            iou = iou - rho2 / c2
            return iou
        else:
            v = (4 / math.pi ** 2) * tf.pow(tf.atan(w2 / h2) - torch.atan(w1 / h1), 2)
            alpha = v / (v - iou + (1 + epsilon))
            iou = iou - (rho2 / c2 + v * alpha)
            return iou
    else:
        s_cw = (b2x1 + b2x2 - b1x1 - b1x2) * 0.5
        s_ch = (b2y1 + b2y2 - b1y1 - b1y2) * 0.5
        sigma = tf.pow(s_cw ** 2 + s_ch ** 2, 0.5)
        sin_alpha_1 = tf.abs(s_cw) / sigma
        sin_alpha_2 = tf.abs(s_ch) / sigma
        threshold = pow(2, 0.5) / 2
        sin_alpha = tf.where(sin_alpha_1 > threshold, sin_alpha_2, sin_alpha_1)
        angle_cost = tf.cos(tf.asin(sin_alpha) * 2 - math.pi / 2)
        rho_x = (s_cw / cw) ** 2
        rho_y = (s_ch / ch) ** 2
        gamma = angle_cost - 2
        distance_cost = 2 - tf.exp(gamma * rho_x) - tf.exp(gamma * rho_y)
        omiga_w = tf.abs(w1 - w2) / tf.maximum(w1, w2)
        omiga_h = tf.abs(h1 - h2) / tf.maximum(h1, h2)
        shape_cost = tf.pow(1 - tf.exp(-1 * omiga_w), 4) + tf.pow(1 - tf.exp(-1 * omiga_h), 4)
        iou = iou - 0.5 * (distance_cost + shape_cost)
        return iou


def compute_yolo_loss(pred, conv, label, bboxes):
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

    iou_loss = tf.expand_dims(bbox_iou_loss(pred_xywh, label_xywh, iou_type=cfg.YOLO.TRAIN.LOSS_TYPE_FOR_BBOXES), axis=-1)
    iou_loss =  respond_bbox * bbox_loss_scale * (1 - iou_loss)
    

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

    lbox = tf.reduce_mean(tf.reduce_sum(iou_loss, axis=[1,2,3,4]))
    conf_loss = tf.reduce_mean(tf.reduce_sum(conf_loss, axis=[1,2,3,4]))
    prob_loss = tf.reduce_mean(tf.reduce_sum(prob_loss, axis=[1,2,3,4]))

    return lbox, conf_loss, prob_loss