from config import *
from common_modules import *
from loss import compute_loss, compute_r_yolo_loss
import tf2onnx
from optimizer import LrScheduler, Optimizer
from utils import load_yolo_weights, read_class_names


def cspdarknet53(input_data):
    input_data = convolutional(input_data, (3, 3,  3,  32), activate_type="mish")
    input_data = convolutional(input_data, (3, 3, 32,  64), downsample=True, activate_type="mish")

    route = input_data
    route = convolutional(route, (1, 1, 64, 64), activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 64, 64), activate_type="mish")
    for i in range(1):
        input_data = residual_block(input_data,  64,  32, 64, activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 64, 64), activate_type="mish")

    input_data = tf.concat([input_data, route], axis=-1)
    input_data = convolutional(input_data, (1, 1, 128, 64), activate_type="mish")
    input_data = convolutional(input_data, (3, 3, 64, 128), downsample=True, activate_type="mish")
    route = input_data
    route = convolutional(route, (1, 1, 128, 64), activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 128, 64), activate_type="mish")
    for i in range(2):
        input_data = residual_block(input_data, 64,  64, 64, activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 64, 64), activate_type="mish")
    input_data = tf.concat([input_data, route], axis=-1)

    input_data = convolutional(input_data, (1, 1, 128, 128), activate_type="mish")
    input_data = convolutional(input_data, (3, 3, 128, 256), downsample=True, activate_type="mish")
    route = input_data
    route = convolutional(route, (1, 1, 256, 128), activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 256, 128), activate_type="mish")
    for i in range(8):
        input_data = residual_block(input_data, 128, 128, 128, activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 128, 128), activate_type="mish")
    input_data = tf.concat([input_data, route], axis=-1)

    input_data = convolutional(input_data, (1, 1, 256, 256), activate_type="mish")
    route_1 = input_data
    input_data = convolutional(input_data, (3, 3, 256, 512), downsample=True, activate_type="mish")
    route = input_data
    route = convolutional(route, (1, 1, 512, 256), activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 512, 256), activate_type="mish")
    for i in range(8):
        input_data = residual_block(input_data, 256, 256, 256, activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 256, 256), activate_type="mish")
    input_data = tf.concat([input_data, route], axis=-1)

    input_data = convolutional(input_data, (1, 1, 512, 512), activate_type="mish")
    route_2 = input_data
    input_data = convolutional(input_data, (3, 3, 512, 1024), downsample=True, activate_type="mish")
    route = input_data
    route = convolutional(route, (1, 1, 1024, 512), activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 1024, 512), activate_type="mish")
    for i in range(4):
        input_data = residual_block(input_data, 512, 512, 512, activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 512, 512), activate_type="mish")
    input_data = tf.concat([input_data, route], axis=-1)

    input_data = convolutional(input_data, (1, 1, 1024, 1024), activate_type="mish")
    input_data = convolutional(input_data, (1, 1, 1024, 512))
    input_data = convolutional(input_data, (3, 3, 512, 1024))
    input_data = convolutional(input_data, (1, 1, 1024, 512))

    max_pooling_1 = tf.keras.layers.MaxPool2D(pool_size=13, padding='SAME', strides=1)(input_data)
    max_pooling_2 = tf.keras.layers.MaxPool2D(pool_size=9, padding='SAME', strides=1)(input_data)
    max_pooling_3 = tf.keras.layers.MaxPool2D(pool_size=5, padding='SAME', strides=1)(input_data)
    input_data = tf.concat([max_pooling_1, max_pooling_2, max_pooling_3, input_data], axis=-1)

    input_data = convolutional(input_data, (1, 1, 2048, 512))
    input_data = convolutional(input_data, (3, 3, 512, 1024))
    input_data = convolutional(input_data, (1, 1, 1024, 512))

    return route_1, route_2, input_data

def cspdarknet53_tiny(input_data):
    input_data = convolutional(input_data, (3, 3, 3, 32), downsample=True)
    input_data = convolutional(input_data, (3, 3, 32, 64), downsample=True)
    input_data = convolutional(input_data, (3, 3, 64, 64))

    route = input_data
    input_data = route_group(input_data, 2, 1)
    input_data = convolutional(input_data, (3, 3, 32, 32))
    route_1 = input_data
    input_data = convolutional(input_data, (3, 3, 32, 32))
    input_data = tf.concat([input_data, route_1], axis=-1)
    input_data = convolutional(input_data, (1, 1, 32, 64))
    input_data = tf.concat([route, input_data], axis=-1)
    input_data = layers.MaxPool2D(2, 2, 'same')(input_data)

    input_data = convolutional(input_data, (3, 3, 64, 128))
    route = input_data
    input_data = route_group(input_data, 2, 1)
    input_data = convolutional(input_data, (3, 3, 64, 64))
    route_1 = input_data
    input_data = convolutional(input_data, (3, 3, 64, 64))
    input_data = tf.concat([input_data, route_1], axis=-1)
    input_data = convolutional(input_data, (1, 1, 64, 128))
    input_data = tf.concat([route, input_data], axis=-1)
    input_data = layers.MaxPool2D(2, 2, 'same')(input_data)

    input_data = convolutional(input_data, (3, 3, 128, 256))
    route = input_data
    input_data = route_group(input_data, 2, 1)
    input_data = convolutional(input_data, (3, 3, 128, 128))
    route_1 = input_data
    input_data = convolutional(input_data, (3, 3, 128, 128))
    input_data = tf.concat([input_data, route_1], axis=-1)
    input_data = convolutional(input_data, (1, 1, 128, 256))
    route_1 = input_data
    input_data = tf.concat([route, input_data], axis=-1)
    input_data = layers.MaxPool2D(2, 2, 'same')(input_data)

    input_data = convolutional(input_data, (3, 3, 512, 512))

    return route_1, input_data


def prediction(inputs, num_classes):
    outputs = []
    conv_outputs = []
    strides = np.array(cfg.YOLO.STRIDES, np.float32).reshape((-1,1,1))
    anchors = cfg.YOLO.ANCHORS.reshape((cfg.YOLO.ANCHORS.shape[0] // cfg.YOLO.ANCHOR_PER_SCALE, cfg.YOLO.ANCHOR_PER_SCALE, 2)).astype(np.float32) / strides \
            if not cfg.YOLO.TRAIN_YOLO_TINY else cfg.YOLO.ANCHORS_TINY.reshape((cfg.YOLO.ANCHORS_TINY.shape[0] // cfg.YOLO.ANCHOR_PER_SCALE, cfg.YOLO.ANCHOR_PER_SCALE, 2)).astype(np.float32) / strides
    for i in range(len(inputs)):
        conv_shape       = tf.keras.backend.int_shape(inputs[i])
        batch_size       = conv_shape[0]
        output_size      = conv_shape[1]

        conv_output = tf.reshape(inputs[i], (-1, output_size, output_size, 3, 5 + num_classes))
        conv_outputs.append(conv_output)

        grid_xy = tf.meshgrid(tf.range(output_size), tf.range(output_size))
        grid_xy = tf.cast(tf.expand_dims(tf.stack(grid_xy, axis=-1), axis=2),tf.float32) 

        y_norm = tf.sigmoid(conv_output)
        xy, wh, conf, classes = tf.split(y_norm, (2, 2, 1, num_classes), axis=-1)
        xy = (xy * 2.0 - 0.5 + grid_xy) * strides[i]
        wh = (wh * 2) ** 2 * anchors[i] * strides[i]

        output = tf.concat([xy, wh, conf, classes], axis=-1)
        outputs.append(output)
    return conv_outputs, outputs
def prediction_rotation(inputs, num_classes):
    outputs = []
    conv_outputs = []

    strides = np.array(cfg.YOLO.STRIDES, np.float32).reshape((-1,1,1))
    strides = tf.convert_to_tensor(strides, dtype=tf.float32)
    for idx in range(len(inputs)):
        conv_shape       = tf.keras.backend.int_shape(inputs[idx])
        batch_size       = conv_shape[0]
        output_size      = conv_shape[1]

        y = tf.reshape(inputs[idx], (-1, output_size, output_size, 5 + num_classes))
        conv_outputs.append(y)

        grid_xy = tf.meshgrid(tf.range(output_size), tf.range(output_size))
        grid_xy = tf.cast(tf.stack(grid_xy, axis=-1), dtype=tf.float32)

        xy, real, imagin, conf, classes = tf.split(y, (2, 1, 1, 1, num_classes), axis=-1)
        xy = tf.sigmoid(xy)
        
        conf = tf.sigmoid(conf)
        classes = tf.sigmoid(classes)
        
        pred_xy = (xy * 2.0 - 0.5 + grid_xy) * strides[idx]
        pred_real = tf.sigmoid(real)
        pred_imagin = tf.tanh(imagin)


        output = tf.concat([pred_xy, pred_real, pred_imagin, conf, classes], axis=-1)
        outputs.append(output)

    return conv_outputs, outputs


def nms(pred_bboxes, conf_threshold = 0.5, iou_threshold=0.25, max_outputs=50):
    pred_bboxes = [tf.reshape(x, (-1,tf.keras.backend.int_shape(x)[1] * tf.keras.backend.int_shape(x)[2] * tf.keras.backend.int_shape(x)[3], 
                    tf.keras.backend.int_shape(x)[-1])) for x in pred_bboxes]
    pred_bboxes = tf.concat(pred_bboxes, axis=1)
    num_boxes = tf.keras.backend.int_shape(pred_bboxes)[1]
    num_classes = tf.keras.backend.int_shape(pred_bboxes)[-1] - 5
    scores = pred_bboxes[..., 4:5] * pred_bboxes[..., 5:]
    boxes = xywh2xyxy(pred_bboxes[..., :4])
    boxes = tf.reshape(boxes, (-1, num_boxes, 1, 4))
    scores = tf.reshape(scores, (-1, num_boxes, num_classes))
    boxes, scores, classes, valid_detections = tf.image.combined_non_max_suppression(boxes=boxes,
                                                                                    scores=scores,
                                                                                    max_output_size_per_class=50,
                                                                                    max_total_size=max_outputs,
                                                                                    iou_threshold=iou_threshold,
                                                                                    score_threshold=conf_threshold,
                                                                                    clip_boxes=False)
    
    
    y1, x1, y2, x2 = boxes[..., 0:1], boxes[..., 1:2], boxes[..., 2:3], boxes[..., 3:4]
    return tf.concat([x1, y1, x2, y2, tf.expand_dims(scores, axis=-1), tf.expand_dims(classes, axis=-1)], axis=-1), valid_detections

def xywh2xyxy(boxes):
    x1 = boxes[..., 0: 1] - boxes[..., 2: 3] / 2 
    y1 = boxes[..., 1: 2] - boxes[..., 3: 4] / 2
    x2 = boxes[..., 0: 1] + boxes[..., 2: 3] / 2
    y2 = boxes[..., 1: 2] + boxes[..., 3: 4] / 2
    return tf.concat([y1, x1, y2, x2], axis=-1)


# build model
def build_model(input_size, num_classes, training=False, yolo4_type='yolov4'):
    if yolo4_type == 'yolov4':
        input_layer = tf.keras.Input((*input_size, 3))
        route_1, route_2, conv = cspdarknet53(input_layer)

        route = conv
        conv = convolutional(conv, (1, 1, 512, 256))
        conv = upsample(conv)
        route_2 = convolutional(route_2, (1, 1, 512, 256))
        conv = tf.concat([route_2, conv], axis=-1)

        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))

        route_2 = conv
        conv = convolutional(conv, (1, 1, 256, 128))
        conv = upsample(conv)
        route_1 = convolutional(route_1, (1, 1, 256, 128))
        conv = tf.concat([route_1, conv], axis=-1)

        conv = convolutional(conv, (1, 1, 256, 128))
        conv = convolutional(conv, (3, 3, 128, 256))
        conv = convolutional(conv, (1, 1, 256, 128))
        conv = convolutional(conv, (3, 3, 128, 256))
        conv = convolutional(conv, (1, 1, 256, 128))

        route_1 = conv
        conv = convolutional(conv, (3, 3, 128, 256))
        conv_sbbox = convolutional(conv, (1, 1, 256, 3 * (num_classes + 5)), activate=False, bn=False)

        conv = convolutional(route_1, (3, 3, 128, 256), downsample=True)
        conv = tf.concat([conv, route_2], axis=-1)

        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))

        route_2 = conv
        conv = convolutional(conv, (3, 3, 256, 512))
        conv_mbbox = convolutional(conv, (1, 1, 512, 3 * (num_classes + 5)), activate=False, bn=False)

        conv = convolutional(route_2, (3, 3, 256, 512), downsample=True)
        conv = tf.concat([conv, route], axis=-1)

        conv = convolutional(conv, (1, 1, 1024, 512))
        conv = convolutional(conv, (3, 3, 512, 1024))
        conv = convolutional(conv, (1, 1, 1024, 512))
        conv = convolutional(conv, (3, 3, 512, 1024))
        conv = convolutional(conv, (1, 1, 1024, 512))

        conv = convolutional(conv, (3, 3, 512, 1024))
        conv_lbbox = convolutional(conv, (1, 1, 1024, 3 * (num_classes + 5)), activate=False, bn=False)
        if training:
            outputs = prediction([conv_sbbox, conv_mbbox, conv_lbbox], num_classes)
            return tf.keras.Model(input_layer, outputs)
        else:
            outputs = prediction([conv_sbbox, conv_mbbox, conv_lbbox], num_classes)
            outputs = nms(outputs[1])
            return tf.keras.Model(input_layer, outputs)
    elif yolo4_type == 'yolov4_tiny':
        input_layer = tf.keras.Input((*input_size, 3))
        route_1, conv = cspdarknet53_tiny(input_layer)

        conv = convolutional(conv, (1, 1, 512, 256))

        conv_lobj_branch = convolutional(conv, (3, 3, 256, 512))
        conv_lbbox = convolutional(conv_lobj_branch, (1, 1, 512, 3 * (num_classes + 5)), activate=False, bn=False)

        conv = convolutional(conv, (1, 1, 256, 128))
        conv = upsample(conv)
        conv = tf.concat([conv, route_1], axis=-1)

        conv_mobj_branch = convolutional(conv, (3, 3, 128, 256))
        conv_mbbox = convolutional(conv_mobj_branch, (1, 1, 256, 3 * (num_classes + 5)), activate=False, bn=False)
        if training:
            outputs = prediction([conv_mbbox, conv_lbbox], num_classes)
            return tf.keras.Model(input_layer, outputs)
        else:
            outputs = prediction([conv_mbbox, conv_lbbox], num_classes)
            outputs = nms(outputs[1])
            return tf.keras.Model(input_layer, outputs)
def build_model_ryolo(input_size, num_classes, training=False, yolo4_type='yolov4'):
    if yolo4_type == 'yolov4':
        input_layer = tf.keras.Input((*input_size, 3))
        route_1, route_2, conv = cspdarknet53(input_layer)

        route = conv
        conv = convolutional(conv, (1, 1, 512, 256))
        conv = upsample(conv)
        route_2 = convolutional(route_2, (1, 1, 512, 256))
        conv = tf.concat([route_2, conv], axis=-1)

        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))

        route_2 = conv
        conv = convolutional(conv, (1, 1, 256, 128))
        conv = upsample(conv)
        route_1 = convolutional(route_1, (1, 1, 256, 128))
        conv = tf.concat([route_1, conv], axis=-1)

        conv = convolutional(conv, (1, 1, 256, 128))
        conv = convolutional(conv, (3, 3, 128, 256))
        conv = convolutional(conv, (1, 1, 256, 128))
        conv = convolutional(conv, (3, 3, 128, 256))
        conv = convolutional(conv, (1, 1, 256, 128))

        route_1 = conv
        conv = convolutional(conv, (3, 3, 128, 256))
        conv_sbbox = convolutional(conv, (1, 1, 256, (num_classes + 5)), activate=False, bn=False)

        conv = convolutional(route_1, (3, 3, 128, 256), downsample=True)
        conv = tf.concat([conv, route_2], axis=-1)

        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))

        route_2 = conv
        conv = convolutional(conv, (3, 3, 256, 512))
        conv_mbbox = convolutional(conv, (1, 1, 512, (num_classes + 5)), activate=False, bn=False)

        conv = convolutional(route_2, (3, 3, 256, 512), downsample=True)
        conv = tf.concat([conv, route], axis=-1)

        conv = convolutional(conv, (1, 1, 1024, 512))
        conv = convolutional(conv, (3, 3, 512, 1024))
        conv = convolutional(conv, (1, 1, 1024, 512))
        conv = convolutional(conv, (3, 3, 512, 1024))
        conv = convolutional(conv, (1, 1, 1024, 512))

        conv = convolutional(conv, (3, 3, 512, 1024))
        conv_lbbox = convolutional(conv, (1, 1, 1024, (num_classes + 5)), activate=False, bn=False)
        if training:
            outputs = prediction_rotation([conv_sbbox, conv_mbbox, conv_lbbox], num_classes)
            return tf.keras.Model(input_layer, outputs)
        else:
            outputs = prediction_rotation([conv_sbbox, conv_mbbox, conv_lbbox], num_classes)
            return tf.keras.Model(input_layer, outputs[1])
    elif yolo4_type == 'yolov4_tiny':
        input_layer = tf.keras.Input((*input_size, 3))
        route_1, conv = cspdarknet53_tiny(input_layer)

        conv = convolutional(conv, (1, 1, 512, 256))

        conv_lobj_branch = convolutional(conv, (3, 3, 256, 512))
        conv_lbbox = convolutional(conv_lobj_branch, (1, 1, 512, (num_classes + 5)), activate=False, bn=False)

        conv = convolutional(conv, (1, 1, 256, 128))
        conv = upsample(conv)
        conv = tf.concat([conv, route_1], axis=-1)

        conv_mobj_branch = convolutional(conv, (3, 3, 128, 256))
        conv_mbbox = convolutional(conv_mobj_branch, (1, 1, 256, (num_classes + 5)), activate=False, bn=False)
        if training:
            outputs = prediction([conv_mbbox, conv_lbbox], num_classes)
            return tf.keras.Model(input_layer, outputs)
        else:
            outputs = prediction([conv_mbbox, conv_lbbox], num_classes)
            return tf.keras.Model(input_layer, outputs[1])


def build_model_for_darknet_53(input_size, num_classes, training=False, yolo4_type='yolov4'):
    if yolo4_type == 'yolov4':
        input_layer = tf.keras.Input((*input_size, 3))
        route_1, route_2, conv = cspdarknet53(input_layer)

        route = conv
        conv = convolutional(conv, (1, 1, 512, 256))
        conv = upsample(conv)
        route_2 = convolutional(route_2, (1, 1, 512, 256))
        conv = tf.concat([route_2, conv], axis=-1)

        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))

        route_2 = conv
        conv = convolutional(conv, (1, 1, 256, 128))
        conv = upsample(conv)
        route_1 = convolutional(route_1, (1, 1, 256, 128))
        conv = tf.concat([route_1, conv], axis=-1)

        conv = convolutional(conv, (1, 1, 256, 128))
        conv = convolutional(conv, (3, 3, 128, 256))
        conv = convolutional(conv, (1, 1, 256, 128))
        conv = convolutional(conv, (3, 3, 128, 256))
        conv = convolutional(conv, (1, 1, 256, 128))

        route_1 = conv
        conv = convolutional(conv, (3, 3, 128, 256))
        conv_sbbox = convolutional(conv, (1, 1, 256, 3 * (num_classes + 5)), activate=False, bn=False)

        conv = convolutional(route_1, (3, 3, 128, 256), downsample=True)
        conv = tf.concat([conv, route_2], axis=-1)

        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))
        conv = convolutional(conv, (3, 3, 256, 512))
        conv = convolutional(conv, (1, 1, 512, 256))

        route_2 = conv
        conv = convolutional(conv, (3, 3, 256, 512))
        conv_mbbox = convolutional(conv, (1, 1, 512, 3 * (num_classes + 5)), activate=False, bn=False)

        conv = convolutional(route_2, (3, 3, 256, 512), downsample=True)
        conv = tf.concat([conv, route], axis=-1)

        conv = convolutional(conv, (1, 1, 1024, 512))
        conv = convolutional(conv, (3, 3, 512, 1024))
        conv = convolutional(conv, (1, 1, 1024, 512))
        conv = convolutional(conv, (3, 3, 512, 1024))
        conv = convolutional(conv, (1, 1, 1024, 512))

        conv = convolutional(conv, (3, 3, 512, 1024))
        conv_lbbox = convolutional(conv, (1, 1, 1024, 3 * (num_classes + 5)), activate=False, bn=False)
        if training:
            outputs = prediction([conv_sbbox, conv_mbbox, conv_lbbox], num_classes)
            return tf.keras.Model(input_layer, outputs)
        else:
            outputs = prediction([conv_sbbox, conv_mbbox, conv_lbbox], num_classes)
            return tf.keras.Model(input_layer, outputs[1])
    elif yolo4_type == 'yolov4_tiny':
        input_layer = tf.keras.Input((*input_size, 3))
        route_1, conv = cspdarknet53_tiny(input_layer)

        conv = convolutional(conv, (1, 1, 512, 256))

        conv_lobj_branch = convolutional(conv, (3, 3, 256, 512))
        conv_lbbox = convolutional(conv_lobj_branch, (1, 1, 512, 3 * (num_classes + 5)), activate=False, bn=False)

        conv = convolutional(conv, (1, 1, 256, 128))
        conv = upsample(conv)
        conv = tf.concat([conv, route_1], axis=-1)

        conv_mobj_branch = convolutional(conv, (3, 3, 128, 256))
        conv_mbbox = convolutional(conv_mobj_branch, (1, 1, 256, 3 * (num_classes + 5)), activate=False, bn=False)
        if training:
            outputs = prediction([conv_mbbox, conv_lbbox], num_classes)
            return tf.keras.Model(input_layer, outputs)
        else:
            outputs = prediction([conv_mbbox, conv_lbbox], num_classes)
            return tf.keras.Model(input_layer, outputs[1])

class RYOLOTrainer(object):
    def __init__(self, train_log_dir, saved_model_dir, class_file, model_size, anchors_per_scale=3, num_neck=1, multi_gpus=False, optimizerType='adam', 
                 visual_learning_process=False, export_quant_model=False, backbone='csp_darknet53', neck='FPN', head='YOLOv5Head', transfer='transfer', weights_path='',
                 iou_loss_threshold=0.5, label_smoothing=0.02, batch_size=8, epochs=40, class_map=[]):
        self.train_log_dir = train_log_dir
        self.multi_gpus = multi_gpus
        if os.path.exists(train_log_dir):
            shutil.rmtree(train_log_dir)
        self.log_writer = tf.summary.create_file_writer(train_log_dir)
        self.global_step = tf.Variable(0, trainable=False, dtype=tf.int64)
        self.classes_map = read_class_names(class_file_name=class_file) if len(class_map) == 0 else class_map
        self.num_classes = len(self.classes_map) if len(class_map) == 0 else len(class_map)
        self.visual_learning_process = visual_learning_process
        self.saved_model_dir = saved_model_dir
        self.export_quant_model = export_quant_model
        self.input_size = model_size
        self.optimizerType = optimizerType
        self.yolov4_type = 'yolov4_tiny' if cfg.YOLO.TRAIN_YOLO_TINY else 'yolov4'
        self.iou_loss_threshold = iou_loss_threshold
        self.label_smoothing = label_smoothing
        self.transfer = transfer
        self.epochs = epochs
        self.weights_path = weights_path
        self.build_model()

    def build_model(self):
        cfg.YOLO.STRIDES = [8, 16, 32] if not cfg.YOLO.TRAIN_YOLO_TINY else [16, 32]
        cfg.YOLO.NUM_CLASSES = self.num_classes
        if self.multi_gpus:
            self.strategy = tf.distribute.MirroredStrategy(devices=None)
        else:
            self.strategy = tf.distribute.OneDeviceStrategy(device="/gpu:0")
        
        with self.strategy.scope():
            if self.transfer == 'transfer':
                # self.darknet = build_model(self.input_size, 80, False, self.yolov4_type)
                self.darknet = build_model_for_darknet_53(self.input_size, 80, False, self.yolov4_type)
            print(self.input_size)
            print(cfg.YOLO.NUM_CLASSES)
            self.model = build_model_ryolo(self.input_size, cfg.YOLO.NUM_CLASSES, True, self.yolov4_type)
            self.loss_fn = compute_r_yolo_loss
            self.optimizer = Optimizer(self.optimizerType)()        

    def train(self, train_dataset, valid_dataset=None):
        steps_per_epoch = len(train_dataset)
        self.total_steps = int(self.epochs * steps_per_epoch)
        
        cfg.YOLO.TRAIN.WARMUP_STEPS = steps_per_epoch * cfg.YOLO.TRAIN.WARMUP_EPOCHS
        
        with self.strategy.scope():
            self.lr_scheduler = LrScheduler(self.total_steps, scheduler_method='cosine')
            
            if self.transfer == 'scratch':
                print("Train model from scratch")
                print(self.model.summary())
            elif self.transfer == 'resume':
                print("Load weights from latest checkpoint")
                self.model.load_weights(os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5"))
            elif self.transfer == 'transfer':
                print("Transfer learning for model")
                load_yolo_weights(self.darknet, cfg.YOLO.TRAIN.PRETRAINED_WEIGHT_PATH)
                for i, l in enumerate(self.darknet.layers):
                    layer_weights = l.get_weights()
                    if layer_weights != []:
                        try:
                            self.model.layers[i].set_weights(layer_weights)
                        except:
                            print("skipping", self.model.layers[i].name)


        
        prev_epoch_loss = None
        for epoch in range(1, self.epochs + 1):
            print(f'Epoch {epoch}/ {self.epochs}')
            total_loss = 0.0
            # define a progbar
            pb = tf.keras.utils.Progbar(steps_per_epoch, stateful_metrics=['loss'])
            if self.visual_learning_process and epoch % 1 == 0 and epoch !=1:
                self.visual_learning_process_fn()
            for step, data in enumerate(train_dataset):
                image = data[0]
                self.image_input = image
                target = data[2]
                self.gt_boxes = data[1]
                
                loss, lbox, conf_loss, prob_loss = self.dist_train_step(image, target)
                total_loss += loss

                pb.add(1, [('lbox', lbox), ('conf_loss', conf_loss), ('prob_loss', prob_loss), ('total_loss', loss), ('lr', self.optimizer.lr)])

                with self.log_writer.as_default():

                    tf.summary.scalar('lbox', lbox, step=self.global_step)
                    tf.summary.scalar('conf_loss', conf_loss, step=self.global_step)
                    tf.summary.scalar('prob_loss', prob_loss, step=self.global_step)
                    tf.summary.scalar('total_loss', loss, step=self.global_step)
                    tf.summary.scalar('lr', self.optimizer.lr, step=self.global_step)
                self.log_writer.flush()
            
            if epoch % 1 == 0:
                loss_mean = total_loss / steps_per_epoch
                if prev_epoch_loss is None or prev_epoch_loss > loss_mean:
                    prev_epoch_loss = loss_mean
                    print('Saving weights for epoch {} at {}'.format(epoch, os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5")))
                    #self.model.save_weights(os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5"))
                else:
                    pass
        #self.model.save_weights(os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5"))
        self.export_model()
        if self.export_quant_model:
            self.quantization_model()
    
    def check_data_is_valid(self, target):
        boxes = target[..., :8].reshape(-1, 8)
        has_wrong_order = False

        for box in boxes:
            has_wrong_order = True if np.any(box[...,1] > box[...,5]) or np.any(box[...,3] > box[...,5]) \
                               or np.any(box[...,1] > box[...,7]) or np.any(box[...,3] > box[...,7]) or np.any(box[...,0] > box[...,2]) or np.any(box[...,6] > box[...,4]) else False

        if has_wrong_order:
            print('Data is not valid!!!')

    
    def train_step(self, image, target):
        with tf.GradientTape() as tape:
            convs, preds = self.model(image, training=True)
            total_lbox=total_conf_loss=total_prob_loss=0
            for i in range(len(convs)):
                conv, pred = convs[i], preds[i]

                lbox, conf_loss, prob_loss = self.loss_fn(pred, conv, target[i][0], target[i][1])
                total_lbox +=  lbox
                total_conf_loss += conf_loss
                total_prob_loss += prob_loss

            total_loss = total_lbox + total_conf_loss + total_prob_loss
        
        gradients = tape.gradient(total_loss, self.model.trainable_variables)
        self.optimizer.apply_gradients(zip(gradients, self.model.trainable_variables))

        lr = self.lr_scheduler.step()
        self.optimizer.lr.assign(lr)
        self.global_step.assign_add(1)
        return total_loss, total_lbox, total_conf_loss, total_prob_loss

    def dist_train_step(self, image, target):
        with self.strategy.scope():
            loss = self.strategy.run(self.train_step, args=(image, target))
            total_loss_mean = self.strategy.reduce(tf.distribute.ReduceOp.MEAN, loss, axis=None)
            return total_loss_mean
    
    def validate(self, valid_dataset):
        valid_loss = []
        for step, data in enumerate(valid_dataset):
            image = data[0]
            target = data[2:]
            step_valid_loss = self.valid_step(image, target)
            valid_loss.append(step_valid_loss)
        return np.mean(valid_loss)
    
    def valid_step(self, image, target):
        convs, preds = self.model(image, training=True)
        total_giou_loss=total_conf_loss=total_prob_loss=0
        for i in range(len(convs)):
            conv, pred = convs[i], preds[i]
            giou_loss, conf_loss, prob_loss = self.loss_fn(pred, conv, target[i][0], target[i][1])
            total_giou_loss += giou_loss 
            total_conf_loss += conf_loss 
            total_prob_loss += prob_loss
        return total_giou_loss + total_conf_loss + total_prob_loss
    
    def export_model(self):
        print("pb model saved in {}".format(self.saved_model_dir))
        spec = (tf.TensorSpec((None, *cfg.YOLO.TRAIN.MODEL_SIZE, 3), tf.float32, name="input_yolov4"),)
        model_proto, _ = tf2onnx.convert.from_keras(self.model, input_signature=spec, output_path=os.path.join(self.saved_model_dir,'model.onnx'))
        # tf.keras.backend.clear_session()
        #self.model = build_model_ryolo(self.input_size, cfg.YOLO.NUM_CLASSES, False, self.yolov4_type)
        #self.model.load_weights(os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5"))
        #self.model.save(self.saved_model_dir)

    def visual_learning_process_fn(self):
        return
        image_input = self.image_input[0]
        image = image_input * 255.
        image = tf.cast(image, tf.uint8)
        _, pred_bbox = self.model(tf.expand_dims(image_input, 0), training=False)
        pred_bbox = [tf.reshape(x, (tf.shape(x)[0], -1, tf.shape(x)[-1])) for x in pred_bbox]
        pred_bbox = tf.concat(pred_bbox, axis=1)

        bboxes = self.batch_non_max_suppression(pred_bbox, conf_threshold=cfg.YOLO.TRAIN.CONF_THRESHOLD, iou_threshold=cfg.YOLO.TRAIN.IOU_THRESHOLD)
        bboxes = bboxes[0].numpy()
        bboxes = self.resize_back(bboxes, target_sizes=self.input_size, original_shape=self.input_size)

        if bboxes.any():
            img = image
            image = self.draw_box(np.array(img), None, np.array(self.gt_boxes[0]), self.classes_map)
            cv2.imwrite("./original_img_with_epoch.jpg",  cv2.cvtColor(image, cv2.COLOR_RGB2BGR))
            image = self.draw_box(np.array(img), np.array(bboxes), None, self.classes_map)
            cv2.imwrite("./predict_img_with_epoch.jpg",  cv2.cvtColor(image, cv2.COLOR_RGB2BGR))
        else:
            img = image
            cv2.imwrite("./predict_img_with_epoch.jpg",  cv2.cvtColor(np.array(image), cv2.COLOR_RGB2BGR))
            image = self.draw_box(np.array(img), None, np.array(self.gt_boxes[0]), self.classes_map)
            cv2.imwrite("./original_img_with_epoch.jpg",  cv2.cvtColor(image, cv2.COLOR_RGB2BGR))

    
    def batch_non_max_suppression(self, prediction, conf_threshold=0.5, iou_threshold=0.25, classes=None, agnostic=False,
                              labels=()):
        num_classes = tf.shape(prediction)[-1] - 5
        candidates = prediction[..., 4] > conf_threshold
        output = [tf.zeros((0, 6))] * prediction.shape[0]

        for i, pred in enumerate(prediction):
            pred = pred[candidates[i]] 

            if not pred.shape[0]:
                continue

            box = self.xywh2xyxy(pred[:, :4])
            score = pred[:, 4]
            classes = tf.argmax(pred[..., 5:], axis=-1)

            pred_nms = []
            for clss in tf.unique(classes)[0]:
                mask = tf.math.equal(classes, clss)
                box_of_clss = tf.boolean_mask(box, mask)  
                classes_of_clss = tf.boolean_mask(classes, mask)
                score_of_clss = tf.boolean_mask(score, mask)

                select_indices = tf.image.non_max_suppression(box_of_clss, score_of_clss, max_output_size=50,
                                                            iou_threshold=iou_threshold)
                box_of_clss = tf.gather(box_of_clss, select_indices)
                score_of_clss = tf.gather(tf.expand_dims(score_of_clss, -1), select_indices)
                classes_of_clss = tf.cast(tf.gather(tf.expand_dims(classes_of_clss, -1), select_indices), tf.float32)
                pred_of_clss = tf.concat([box_of_clss, score_of_clss, classes_of_clss], axis=-1)
                pred_nms.append(pred_of_clss)

            output[i] = tf.concat(pred_nms, axis=0)
        return output
    
    def draw_box(self, image, label=None, gt_boxes=None, classes_map=None):
        # label, gt_boxes: xyxy
        if label is not None:
            box = label[:, 0:4].copy()
            classes = label[:, -1]    

            if np.max(box) <= 1:
                box[:, [0, 2]] = box[:, [0, 2]] * image.shape[1]
                box[:, [1, 3]] = box[:, [1, 3]] * image.shape[0]
        
        else:
            box = gt_boxes[:, 0:4]
            classes = gt_boxes[:, 4]

        if not isinstance(box, int):
            box = box.astype(np.int16)

        image_h, image_w, _ = image.shape
        num_classes = len(classes_map) if classes_map is not None else len(range(int(np.max(classes)) + 1))
        if label is not None:
            hsv_tuples = [(1.0 * x / num_classes, 1., 1.) for x in range(num_classes)]
            colors = list(map(lambda x: colorsys.hsv_to_rgb(*x), hsv_tuples))
            colors = list(map(lambda x: (int(x[0] * 255), int(x[1] * 255), int(x[2] * 255)), colors))
        else:
            colors = [(21, 243, 29) for i in range(num_classes)]
        bbox_thick = int(0.6 * (image_h + image_w) / 600)   
        font_scale = 0.5

        num_boxes = label.shape[0] if label is not None else gt_boxes.shape[0]
        for i in range(num_boxes):
            x1y1 = tuple(box[i, 0:2])
            x2y2 = tuple(box[i, 2:4])
            if label is not None:
                class_ind = int(classes[i])
                bbox_color = colors[class_ind]
            else:
                class_ind = int(classes[i])
                bbox_color = colors[0]
            image = cv2.rectangle(image, x1y1, x2y2, bbox_color, bbox_thick)

            # show labels
            if classes_map is not None:
                class_ind = classes_map[class_ind]
            else:
                class_ind = str(class_ind)

            if label is not None:
                if label.shape[-1] == 6:
                    score = ': ' + str(round(label[i, -2], 2))
                else:
                    score = ''
            else:
                score = ': 1.0'

            bbox_text = '%s %s' % (class_ind, score)
            t_size = cv2.getTextSize(bbox_text, 0, font_scale, thickness=bbox_thick//2)[0]
            cv2.rectangle(image, x1y1, (x1y1[0] + t_size[0], x1y1[1] - t_size[1] - 3), bbox_color, -1)  # filled
            cv2.putText(image, bbox_text, (x1y1[0], x1y1[1]-2), cv2.FONT_HERSHEY_SIMPLEX, font_scale, (0, 0, 0), bbox_thick//2, lineType=cv2.LINE_AA)

        return image

    def resize_back(self, bboxes, target_sizes, original_shape):
        original_h, original_w = original_shape[:2]
        target_size_h, target_size_w = target_sizes

        resize_ratio = min(target_size_w / original_w, target_size_h / original_h)
        dw = (target_size_w - resize_ratio * original_w) / 2
        dh = (target_size_h - resize_ratio * original_h) / 2
        bboxes[:, [0, 2]] = 1.0 * (bboxes[:, [0, 2]] - dw) / resize_ratio
        bboxes[:, [1, 3]] = 1.0 * (bboxes[:, [1, 3]] - dh) / resize_ratio
        return bboxes

    def xywh2xyxy(self, box):
        x1 = box[..., 0: 1] - box[..., 2: 3] / 2  # top left x
        y1 = box[..., 1: 2] - box[..., 3: 4] / 2  # top left y
        x2 = box[..., 0: 1] + box[..., 2: 3] / 2  # bottom right x
        y2 = box[..., 1: 2] + box[..., 3: 4] / 2  # bottom right y
        output = tf.concat([x1, y1, x2, y2], axis=-1) if isinstance(box, tf.Tensor) else np.concatenate([x1, y1, x2, y2], axis=-1)
        return output

class YOLOTrainer(object):
    def __init__(self, train_log_dir, saved_model_dir, multi_gpus=False, optimizerType = 'adam', 
                 visual_learning_process=False, export_quant_model=False, transfer='transfer'):
        self.train_log_dir = train_log_dir
        self.multi_gpus = multi_gpus
        if os.path.exists(train_log_dir):
            shutil.rmtree(train_log_dir)
        self.log_writer = tf.summary.create_file_writer(train_log_dir)
        self.global_step = tf.Variable(0, trainable=False, dtype=tf.int64)
        self.classes_map = read_class_names(cfg.YOLO.CLASSES)
        self.num_classes = len(self.classes_map)
        self.visual_learning_process = visual_learning_process
        self.export_quant_model = export_quant_model
        self.saved_model_dir = saved_model_dir
        self.input_size = cfg.YOLO.TRAIN.MODEL_SIZE
        self.optimizerType = optimizerType
        self.yolov4_type = 'yolov4_tiny' if cfg.YOLO.TRAIN_YOLO_TINY else 'yolov4'
        self.transfer = transfer
        self.build_model()
        self.current_epoch = 0

    def build_model(self):
        cfg.YOLO.STRIDES = [8, 16, 32] if not cfg.YOLO.TRAIN_YOLO_TINY else [16, 32]
        cfg.YOLO.NUM_CLASSES = self.num_classes
        if self.multi_gpus:
            self.strategy = tf.distribute.MirroredStrategy(devices=None)
        else:
            self.strategy = tf.distribute.OneDeviceStrategy(device="/gpu:0")
        
        with self.strategy.scope():
            if self.transfer == 'transfer':
                self.darknet = build_model_for_darknet_53(self.input_size, 80, False, self.yolov4_type)
                cfg.YOLO.TRAIN.LR_INIT = 1e-3
            self.model = build_model(self.input_size, cfg.YOLO.NUM_CLASSES, True, self.yolov4_type)
            self.loss_fn = compute_loss
            self.optimizer = Optimizer(self.optimizerType)()        

    def train(self, train_dataset, valid_dataset=None):
        steps_per_epoch = len(train_dataset)
        self.total_steps = int(cfg.YOLO.TRAIN.EPOCHS * steps_per_epoch)
        
        cfg.YOLO.TRAIN.WARMUP_STEPS = steps_per_epoch * cfg.YOLO.TRAIN.WARMUP_EPOCHS
        
        with self.strategy.scope():
            self.lr_scheduler = LrScheduler(self.total_steps, scheduler_method='cosine')
            
            if self.transfer == 'scratch':
                print("Train model from scratch")
                print(self.model.summary())
            elif self.transfer == 'resume':
                print("Load weights from latest checkpoint")
                self.model.load_weights(os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5"))
            elif self.transfer == 'transfer':
                print("transfer learning for model")  
                load_yolo_weights(self.darknet, cfg.YOLO.TRAIN.PRETRAINED_WEIGHT_PATH)
                for i, l in enumerate(self.darknet.layers):
                    layer_weights = l.get_weights()
                    if layer_weights != []:
                        try:
                            self.model.layers[i].set_weights(layer_weights)
                        except:
                            print("skipping", self.model.layers[i].name)

        # train_dataset = self.strategy.experimental_distribute_dataset()
        
        prev_epoch_loss = None
        for epoch in range(1, cfg.YOLO.TRAIN.EPOCHS + 1):
            print("=> STEP %d/%d"  %(epoch,cfg.YOLO.TRAIN.EPOCHS))
            #print(f'Epoch {epoch}/ {cfg.YOLO.TRAIN.EPOCHS}')
            self.current_epoch = epoch
            total_loss = 0.0
            # define a progbar
            pb = tf.keras.utils.Progbar(steps_per_epoch, stateful_metrics=['loss'])
            if self.visual_learning_process and epoch % 1 == 0 and epoch != 1:
                self.visual_learning_process_fn()
            for step, data in enumerate(train_dataset):
                image = data[0]
                self.image_input = image
                target = data[2]
                self.gt_boxes = data[1]
                
                loss, lbox, conf_loss, prob_loss = self.dist_train_step(image, target)
                total_loss += loss
                pb.add(1, [('lbox', lbox), ('conf_loss', conf_loss), ('prob_loss', prob_loss), ('total_loss', loss), ('lr', self.optimizer.lr)])
                with self.log_writer.as_default():
                    tf.summary.scalar('lbox', lbox, step=self.global_step)
                    tf.summary.scalar('conf_loss', conf_loss, step=self.global_step)
                    tf.summary.scalar('prob_loss', prob_loss, step=self.global_step)
                    tf.summary.scalar('total_loss', loss, step=self.global_step)
                    tf.summary.scalar('lr', self.optimizer.lr, step=self.global_step)
                self.log_writer.flush()
            
            if epoch % 1 == 0:
                loss_mean = total_loss / steps_per_epoch
                if prev_epoch_loss is None or prev_epoch_loss > loss_mean:
                    prev_epoch_loss = loss_mean
                    print('Saving weights for epoch {} at {}'.format(epoch, os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5")))
                    self.model.save_weights(os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5"))
                else:
                    pass
        import gc
        gc.collect()
        self.export_model()
        if self.export_quant_model:
            self.quantization_model()
    
    def train_step(self, image, target):
        with tf.GradientTape() as tape:
            convs, preds = self.model(image, training=True)
            total_lbox=total_conf_loss=total_prob_loss=0
            for i in range(len(convs)):
                conv, pred = convs[i], preds[i]
                lbox, conf_loss, prob_loss = self.loss_fn(pred, conv, target[i][0], target[i][1])
                total_lbox +=  lbox 
                total_conf_loss += conf_loss 
                total_prob_loss += prob_loss
            total_loss = total_lbox + total_conf_loss + total_prob_loss
        
        gradients = tape.gradient(total_loss, self.model.trainable_variables)
        self.optimizer.apply_gradients(zip(gradients, self.model.trainable_variables))

        lr = self.lr_scheduler.step()
        self.optimizer.lr.assign(lr)
        self.global_step.assign_add(1)
        return total_loss, total_lbox, total_conf_loss, total_prob_loss

    @tf.function
    def dist_train_step(self, image, target):
        with self.strategy.scope():
            loss = self.strategy.run(self.train_step, args=(image, target))
            total_loss_mean = self.strategy.reduce(tf.distribute.ReduceOp.MEAN, loss, axis=None)
            return total_loss_mean
    
    def validate(self, valid_dataset):
        valid_loss = []
        for step, data in enumerate(valid_dataset):
            image = data[0]
            target = data[2:]
            step_valid_loss = self.valid_step(image, target)
            valid_loss.append(step_valid_loss)
        return np.mean(valid_loss)
    
    def valid_step(self, image, target):
        convs, preds = self.model(image, training=True)
        total_lbox=total_conf_loss=total_prob_loss=0
        for i in range(len(convs)):
            conv, pred = convs[i], preds[i]
            lbox, conf_loss, prob_loss = self.loss_fn(pred, conv, target[i][0], target[i][1])
            total_lbox +=  lbox
            total_conf_loss += conf_loss 
            total_prob_loss += prob_loss
        total_loss = total_lbox + total_conf_loss + total_prob_loss
        return total_loss
    
    def export_model(self):
        print("pb model saved in {}".format(self.saved_model_dir))
        #tf.keras.backend.set_learning_phase(0)
        #self.model = build_model(self.input_size, cfg.YOLO.NUM_CLASSES, False, self.yolov4_type)
        #self.model.load_weights(os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5"))
        #self.model.save(self.saved_model_dir)

        spec = (tf.TensorSpec((None, *cfg.YOLO.TRAIN.MODEL_SIZE, 3), tf.float32, name="input_yolov4"),)

        model_proto, _ = tf2onnx.convert.from_keras(self.model, input_signature=spec, output_path=os.path.join(self.saved_model_dir,'model.onnx'))

        ''''
        model_proto, external_tensor_storage = tf2onnx.convert.from_keras(self.model,
                input_signature=spec, opset=None, custom_ops=None,
                custom_op_handlers=None, custom_rewriter=None,
                inputs_as_nchw=None, extra_opset=None,shape_override=None,
                target=None, large_model=False, output_path=os.path.join(self.saved_model_dir,'model.onnx'))
        '''
        

        #frozen_keras_graph(self.saved_model_dir)
        print("Done!!!")


    def visual_learning_process_fn(self):
        image_input = self.image_input[0]
        image = image_input * 255.
        image = tf.cast(image, tf.uint8)
        _, pred_bbox = self.model(tf.expand_dims(image_input, 0), training=False)
        pred_bbox = [tf.reshape(x, (tf.shape(x)[0], -1, tf.shape(x)[-1])) for x in pred_bbox]
        pred_bbox = tf.concat(pred_bbox, axis=1)

        bboxes = self.batch_non_max_suppression(pred_bbox, conf_threshold=cfg.YOLO.TRAIN.CONF_THRESHOLD, iou_threshold=cfg.YOLO.TRAIN.IOU_THRESHOLD)
        bboxes = bboxes[0].numpy()
        bboxes = self.resize_back(bboxes, target_sizes=self.input_size, original_shape=self.input_size)

        if bboxes.any():
            img = image
            image = self.draw_box(np.array(img), None, np.array(self.gt_boxes[0]), self.classes_map)
            cv2.imwrite(cfg.YOLO.TRAIN.RESULT_DIR + "/original_img_with_epoch{0}.jpg".format(self.current_epoch),  cv2.cvtColor(image, cv2.COLOR_RGB2BGR))
            image = self.draw_box(np.array(img), np.array(bboxes), None, self.classes_map)
            cv2.imwrite(cfg.YOLO.TRAIN.RESULT_DIR + "/predict_img_with_epoch{0}.jpg".format(self.current_epoch),  cv2.cvtColor(image, cv2.COLOR_RGB2BGR))
        else:
            img = image
            cv2.imwrite(cfg.YOLO.TRAIN.RESULT_DIR + "/predict_img_with_epoch{0}.jpg".format(self.current_epoch),  cv2.cvtColor(np.array(image), cv2.COLOR_RGB2BGR))
            image = self.draw_box(np.array(img), None, np.array(self.gt_boxes[0]), self.classes_map)
            cv2.imwrite(cfg.YOLO.TRAIN.RESULT_DIR + "/original_img_with_epoch{0}.jpg".format(self.current_epoch),  cv2.cvtColor(image, cv2.COLOR_RGB2BGR))

    
    def batch_non_max_suppression(self, prediction, conf_threshold=0.5, iou_threshold=0.25, classes=None, agnostic=False,
                              labels=()):
        num_classes = tf.shape(prediction)[-1] - 5
        candidates = prediction[..., 4] > conf_threshold
        output = [tf.zeros((0, 6))] * prediction.shape[0]

        for i, pred in enumerate(prediction):
            pred = pred[candidates[i]] 

            if not pred.shape[0]:
                continue

            box = self.xywh2xyxy(pred[:, :4])
            score = pred[:, 4]
            classes = tf.argmax(pred[..., 5:], axis=-1)

            pred_nms = []
            for clss in tf.unique(classes)[0]:
                mask = tf.math.equal(classes, clss)
                box_of_clss = tf.boolean_mask(box, mask)  
                classes_of_clss = tf.boolean_mask(classes, mask)
                score_of_clss = tf.boolean_mask(score, mask)

                select_indices = tf.image.non_max_suppression(box_of_clss, score_of_clss, max_output_size=50,
                                                            iou_threshold=iou_threshold)
                box_of_clss = tf.gather(box_of_clss, select_indices)
                score_of_clss = tf.gather(tf.expand_dims(score_of_clss, -1), select_indices)
                classes_of_clss = tf.cast(tf.gather(tf.expand_dims(classes_of_clss, -1), select_indices), tf.float32)
                pred_of_clss = tf.concat([box_of_clss, score_of_clss, classes_of_clss], axis=-1)
                pred_nms.append(pred_of_clss)

            output[i] = tf.concat(pred_nms, axis=0)
        return output
    
    def draw_box(self, image, label=None, gt_boxes=None, classes_map=None):
        # label, gt_boxes: xyxy
        if label is not None:
            box = label[:, 0:4].copy()
            classes = label[:, -1]    

            if np.max(box) <= 1:
                box[:, [0, 2]] = box[:, [0, 2]] * image.shape[1]
                box[:, [1, 3]] = box[:, [1, 3]] * image.shape[0]
        
        else:
            box = gt_boxes[:, 0:4]
            classes = gt_boxes[:, 4]

        if not isinstance(box, int):
            box = box.astype(np.int16)

        image_h, image_w, _ = image.shape
        num_classes = len(classes_map) if classes_map is not None else len(range(int(np.max(classes)) + 1))
        if label is not None:
            hsv_tuples = [(1.0 * x / num_classes, 1., 1.) for x in range(num_classes)]
            colors = list(map(lambda x: colorsys.hsv_to_rgb(*x), hsv_tuples))
            colors = list(map(lambda x: (int(x[0] * 255), int(x[1] * 255), int(x[2] * 255)), colors))
        else:
            colors = [(21, 243, 29) for i in range(num_classes)]
        bbox_thick = int(0.6 * (image_h + image_w) / 600)   
        font_scale = 0.5

        num_boxes = label.shape[0] if label is not None else gt_boxes.shape[0]
        for i in range(num_boxes):
            x1y1 = tuple(box[i, 0:2])
            x2y2 = tuple(box[i, 2:4])
            if label is not None:
                class_ind = int(classes[i])
                bbox_color = colors[class_ind]
            else:
                class_ind = int(classes[i])
                bbox_color = colors[0]
            image = cv2.rectangle(image, x1y1, x2y2, bbox_color, bbox_thick)

            # show labels
            if classes_map is not None:
                class_ind = classes_map[class_ind]
            else:
                class_ind = str(class_ind)

            if label is not None:
                if label.shape[-1] == 6:
                    score = ': ' + str(round(label[i, -2], 2))
                else:
                    score = ''
            else:
                score = ': 1.0'

            bbox_text = '%s %s' % (class_ind, score)
            t_size = cv2.getTextSize(bbox_text, 0, font_scale, thickness=bbox_thick//2)[0]
            cv2.rectangle(image, x1y1, (x1y1[0] + t_size[0], x1y1[1] - t_size[1] - 3), bbox_color, -1)
            cv2.putText(image, bbox_text, (x1y1[0], x1y1[1]-2), cv2.FONT_HERSHEY_SIMPLEX, font_scale, (0, 0, 0), bbox_thick//2, lineType=cv2.LINE_AA)

        return image

    def resize_back(self, bboxes, target_sizes, original_shape):
        original_h, original_w = original_shape[:2]
        target_size_h, target_size_w = target_sizes

        resize_ratio = min(target_size_w / original_w, target_size_h / original_h)
        dw = (target_size_w - resize_ratio * original_w) / 2
        dh = (target_size_h - resize_ratio * original_h) / 2
        bboxes[:, [0, 2]] = 1.0 * (bboxes[:, [0, 2]] - dw) / resize_ratio
        bboxes[:, [1, 3]] = 1.0 * (bboxes[:, [1, 3]] - dh) / resize_ratio
        return bboxes

    def xywh2xyxy(self, box):
        x1 = box[..., 0: 1] - box[..., 2: 3] / 2
        y1 = box[..., 1: 2] - box[..., 3: 4] / 2
        x2 = box[..., 0: 1] + box[..., 2: 3] / 2
        y2 = box[..., 1: 2] + box[..., 3: 4] / 2
        output = tf.concat([x1, y1, x2, y2], axis=-1) if isinstance(box, tf.Tensor) else np.concatenate([x1, y1, x2, y2], axis=-1)
        return output
