from config import *
from common_modules import *
from backbone import *
from neck import *
from head import *

def prediction(inputs, num_classes):
    outputs = []
    conv_outputs = []
    strides = np.array(cfg.YOLO.STRIDES, np.float32).reshape((-1,1,1))
    anchors = cfg.YOLO.ANCHORS.reshape((cfg.YOLO.ANCHORS.shape[0] // cfg.YOLO.ANCHOR_PER_SCALE, cfg.YOLO.ANCHOR_PER_SCALE, 2)).astype(np.float32) / strides
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


def nms(pred_bboxes, conf_threshold=0.5, iou_threshold=0.25, max_outputs=50):
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


def make_divisible(x, divisor):
    return math.ceil(x / divisor) * divisor


def build_model(num_classes=80, transfer='transfer', base_weights_dir="", weight_path="", yolov6_type='YOLOv6n', deploy=False):
    print('model type is: ',yolov6_type)
    depth_mul = cfg.YOLO.YOLOV6_SCALE[yolov6_type]['depth_multiple']
    width_mul = cfg.YOLO.YOLOV6_SCALE[yolov6_type]['width_multiple']
    num_repeat_backbone = cfg.YOLO.YOLOV6_BACKBONE_NUM_REPEATS
    num_repeat_neck = cfg.YOLO.YOLOV6_NECK_NUM_REPEATS
    num_repeat = [(max(round(i * depth_mul), 1) if i > 1 else i) for i in (num_repeat_backbone + num_repeat_neck)]

    channels_list_backbone = cfg.YOLO.YOLOV6_BACKBONE_OUTPUTS_CHANNELS
    channels_list_neck = cfg.YOLO.YOLOV6_NECK_OUTPUTS_CHANNELS
    channels_list = [make_divisible(i * width_mul, 8) for i in (channels_list_backbone + channels_list_neck)]

    inputs = tf.keras.layers.Input(shape=(*cfg.YOLO.TRAIN.MODEL_SIZE, 3))
    backbone_ouputs = EfficientRep(inputs, 3, channels_list, num_repeat, block_name='RepVGGBlock', deploy=deploy)
    neck_ouputs = RepPANNeck(backbone_ouputs, channels_list, num_repeat, block_name='RepVGGBlock', deploy=deploy)
    outputs = build_efficdehead(neck_ouputs, channels_list, 3, num_classes) if transfer == 'scratch' or transfer == 'resume' else \
              build_efficdehead(neck_ouputs, channels_list, 3, 80) if deploy == False and transfer == 'transfer' else \
              build_efficdehead(neck_ouputs, channels_list, 3, num_classes)
    if deploy:
        outputs = prediction(outputs, num_classes)
        outputs = nms(outputs[1])
        return tf.keras.Model(inputs, outputs)
    else:
        outputs = prediction(outputs, num_classes) if transfer == 'scratch' or transfer == 'resume' else \
                  prediction(outputs, 80)
        model = tf.keras.Model(inputs, outputs)
        if transfer == 'scratch':
            return model
        elif transfer == 'transfer':
            model.load_weights(os.path.join(base_weights_dir, 'model.h5'))
            cls_conv0 = model.get_layer('cls_conv0').ouput
            reg_conv0 = model.get_layer('reg_conv0').ouput
            cls_conv1 = model.get_layer('cls_conv1').ouput
            reg_conv1 = model.get_layer('reg_conv1').ouput
            cls_conv2 = model.get_layer('cls_conv2').ouput
            reg_conv2 = model.get_layer('reg_conv2').ouput
            new_outputs = build_new_heads([cls_conv0, reg_conv0, cls_conv1, reg_conv1, cls_conv2, reg_conv2], 3, num_classes)
            new_outputs = prediction(new_outputs, num_classes)
            return tf.keras.Model(inputs, new_outputs)
        elif transfer == 'resume':
            model.load_weights(os.path.join(weight_path, 'model.h5'))
            return model
        else:
            raise NotImplementedError('This ' + transfer + 'has been supported yet')


def model_switch(model, build_func, num_classes=80, transfer='transfer', base_weights_dir="", weight_path="", yolov6_type='YOLOv6n', save_path=None):
    deploy_model = build_func(num_classes=num_classes, transfer=transfer, 
                              base_weights_dir=base_weights_dir, weight_path=weight_path, 
                              yolov6_type=yolov6_type, deploy=True)
    
    for layer, deploy_layer in zip(model.layers, deploy_model.layers):
        if hasattr(layer, "repvgg_convert"):
            kernel, bias = layer.repvgg_convert()
            deploy_layer.rbr_reparam.set_weights([kernel, bias])
        elif isinstance(layer, SimSPPF):
            weights = layer.get_weights()
            deploy_layer.set_weights(weights)
        elif isinstance(layer, SimConv):
            weights = layer.get_weights()
            deploy_layer.set_weights(weights)
        elif isinstance(layer, Transpose):
            weights = layer.get_weights()
            deploy_layer.set_weights(weights)
        elif isinstance(layer, Conv):
            weights = layer.get_weights()
            deploy_layer.set_weights(weights)
        elif isinstance(layer, tf.keras.layers.Conv2D):
            weights = layer.get_weights()
            deploy_layer.set_weights(weights)
    if save_path is not None:
        deploy_model.save(save_path)
    
    return deploy_model