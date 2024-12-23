from config import *
from loss import *
from model import *
from dataset import *
from optimizer import *


class YOLOTrainer(object):
    def __init__(self, train_log_dir, saved_model_dir, class_file, model_size, yolo_type='YOLOv6n', multi_gpus=False, optimizerType='adam', 
                 visual_learning_process=False, export_quant_model=False, transfer='transfer', base_weights_dir='', weights_path='',
                 iou_loss_threshold=0.5, label_smoothing=0.02, batch_size=8, epochs=40):
        self.train_log_dir = train_log_dir
        self.multi_gpus = multi_gpus
        if os.path.exists(train_log_dir):
            shutil.rmtree(train_log_dir)
        self.log_writer = tf.summary.create_file_writer(train_log_dir)
        self.global_step = tf.Variable(0, trainable=False, dtype=tf.int64)
        self.classes_map = read_class_names(cfg.YOLO.CLASSES)
        self.num_classes = len(self.classes_map)
        self.visual_learning_process = visual_learning_process
        self.saved_model_dir = saved_model_dir
        self.export_quant_model = export_quant_model
        self.input_size = model_size
        self.optimizerType = optimizerType
        self.yolov_type = yolo_type
        self.iou_loss_threshold = iou_loss_threshold
        self.label_smoothing = label_smoothing
        self.transfer = transfer
        self.epochs = epochs
        self.base_weights_dir = base_weights_dir
        self.weights_path = weights_path
        self.build_model()

    def build_model(self):
        cfg.YOLO.NUM_CLASSES = self.num_classes
        if self.multi_gpus:
            self.strategy = tf.distribute.MirroredStrategy(devices=None)
        else:
            self.strategy = tf.distribute.OneDeviceStrategy(device="/gpu:0")
        
        with self.strategy.scope():
            if self.transfer == 'transfer':
                self.model = build_model(num_classes=self.num_classes, transfer='transfer', base_weights_dir=self.base_weights_dir, 
                                         weight_path=self.weights_path, yolov6_type=self.yolov_type, deploy=False)
            elif self.transfer == 'scratch':
                self.model = build_model(num_classes=self.num_classes, transfer='scratch', base_weights_dir=self.base_weights_dir, 
                                         weight_path=self.weights_path, yolov6_type=self.yolov_type, deploy=False)
            elif self.transfer == 'resume':
                self.model = build_model(num_classes=self.num_classes, transfer='resume', base_weights_dir=self.base_weights_dir, 
                                         weight_path=self.weights_path, yolov6_type=self.yolov_type, deploy=False)
            self.loss_fn = compute_yolo_loss
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
                print(self.model.summary())
            elif self.transfer == 'transfer':
                print("Transfer learning for model")
                print(self.model.summary())


        
        prev_epoch_loss = None
        for epoch in range(1, self.epochs + 1):
            print(f'Epoch {epoch}/ {self.epochs}')
            self.current_epoch = epoch
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
                    self.model.save_weights(os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5"))
                else:
                    pass

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
        self.model.load_weights(os.path.join(cfg.YOLO.TRAIN.WEIGHTS_PATH, "model.h5"))
        self.deploy_model = model_switch(self.model, build_model, num_classes=self.num_classes, transfer=self.transfer, base_weights_dir=self.base_weights_dir, 
                                    weight_path=self.weights_path, yolov6_type=self.yolov_type, save_path=self.saved_model_dir)

        # export ONNX model
        spec = (tf.TensorSpec((None, *cfg.YOLO.TRAIN.MODEL_SIZE, 3), tf.float32, name="input_yolov6"),)
        model_proto, _ = tf2onnx.convert.from_keras(self.deploy_model, input_signature=spec, output_path=os.path.join(self.saved_model_dir,'model.onnx'))
        print('Done!!!')
        # print('Check error outputs of both train model and deploy model')
        # x = tf.random.uniform((1, 640, 640, 3))
        # train_y = self.model(x)[1]
        # deploy_y = self.deploy_model(x)
        # for train_out, deploy_out in zip(train_y, deploy_y):
        #     print(np.mean((train_out - deploy_out) ** 2)) 

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

def train_yolo():
    trainDataset = YOLODataset()
    trainer = YOLOTrainer(train_log_dir=cfg.YOLO.TRAIN.TRAIN_LOG_DIR, saved_model_dir=cfg.YOLO.TRAIN.SAVED_MODEL_DIR, class_file=cfg.YOLO.CLASSES,
                            model_size=cfg.YOLO.TRAIN.MODEL_SIZE, visual_learning_process=cfg.YOLO.TRAIN.VISUAL_LEARNING_PROCESS, 
                            yolo_type = cfg.YOLO.YOLOV6_TYPES, base_weights_dir=cfg.YOLO.TRAIN.PRETRAINED_WEIGHT_PATH_FOR_YOLOV6,
                            weights_path=cfg.YOLO.TRAIN.WEIGHTS_PATH, batch_size=cfg.YOLO.TRAIN.BATCH_SIZE,
                            transfer = cfg.YOLO.TRAIN.TRANSFER, epochs=cfg.YOLO.TRAIN.EPOCHS)

    trainer.train(trainDataset, None)

def build_trainer():
    return train_yolo