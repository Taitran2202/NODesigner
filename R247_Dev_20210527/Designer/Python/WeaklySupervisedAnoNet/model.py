from config import *
from filters import *
from initialize_filters import *
from loss import *

def get_model(model_option, input_shape, img_dim):
    input_image = tf.keras.layers.Input(input_shape, name='input_image')
    print("model shape: "+str(input_shape))
    if model_option == "LMExp1":
        x = tf.keras.layers.Conv2D(48, kernel_size = 7, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same", trainable = False)(input_image)     
    elif model_option == "LMExp2":
        x = tf.keras.layers.Conv2D(48, kernel_size = 7, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same")(input_image)
    elif model_option == "LMExp3":
        x = tf.keras.layers.Conv2D(48, kernel_size = 11, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same", trainable = False)(input_image)
    elif model_option == "LMExp4":
        x = tf.keras.layers.Conv2D(48, kernel_size = 11, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same")(input_image)
    elif model_option == "RFSExp1":
        x = tf.keras.layers.Conv2D(38, kernel_size = 7, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same", trainable = False)(input_image)
    elif model_option == "RFSExp2":
        x = tf.keras.layers.Conv2D(38, kernel_size = 7, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same")(input_image)
    elif model_option == "RFSExp3":
        x = tf.keras.layers.Conv2D(38, kernel_size = 7, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same", trainable = False)(input_image)
    elif model_option == "RFSExp4":
        x = tf.keras.layers.Conv2D(38, kernel_size = 7, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same")(input_image)
    elif model_option == "SExp1":
        x = tf.keras.layers.Conv2D(13, kernel_size = 7, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same", trainable = False)(input_image)
    elif model_option == "SExp2":
        x = tf.keras.layers.Conv2D(13, kernel_size = 7, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same")(input_image)
    elif model_option == "SExp3":
        x = tf.keras.layers.Conv2D(13, kernel_size = 11, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same", trainable = False)(input_image)
    else:
        x = tf.keras.layers.Conv2D(13, kernel_size = 11, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same")(input_image)
        
    # block 1
    x = tf.keras.layers.BatchNormalization()(x)

    # block 2
    x = tf.keras.layers.Conv2D(32, kernel_size = 7, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same")(x)
    x = tf.keras.layers.BatchNormalization()(x)

    # block 3
    x = tf.keras.layers.Conv2D(32, kernel_size = 3, strides = 1, activation = "relu", kernel_initializer = "random_normal", padding = "same")(x)
    bn = tf.keras.layers.BatchNormalization()(x)

    # segmentation 
    seg = tf.keras.layers.Conv2D(1, kernel_size = 1, strides = 1, activation = "sigmoid", kernel_initializer = "random_normal", padding = "same", name = "seg")(bn)

    # classification

    seg_bn = tf.keras.layers.BatchNormalization()(seg)

    seg_GlobalMax = tf.keras.layers.GlobalMaxPooling2D()(seg_bn)
    seg_GlobalMax_Batch = tf.keras.layers.BatchNormalization()(seg_GlobalMax)

    seg_GlobalAver = tf.keras.layers.GlobalAveragePooling2D()(seg_bn)
    seg_GlobalAver_Batch = tf.keras.layers.BatchNormalization()(seg_GlobalAver)

    # Block 1
    x = tf.keras.layers.concatenate([bn, seg_bn])
    x = tf.keras.layers.MaxPooling2D(pool_size = (2, 2), strides = (2, 2))(x)

    # block 2
    x = tf.keras.layers.Conv2D(8, kernel_size = 5, activation = "relu", kernel_initializer = "random_normal", padding = "same")(x)
    x = tf.keras.layers.BatchNormalization()(x)
    x = tf.keras.layers.MaxPooling2D(pool_size = (2, 2), strides = (2, 2))(x)

    # block 3
    x = tf.keras.layers.Conv2D(16, kernel_size = 5, activation = "relu", kernel_initializer = "random_normal", padding = "same")(x)
    x = tf.keras.layers.BatchNormalization()(x)
    x = tf.keras.layers.MaxPooling2D(pool_size = (2, 2), strides = (2, 2))(x)

    # block 4
    class1 = tf.keras.layers.Conv2D(32, kernel_size = 5, activation = "relu", kernel_initializer = "random_normal", padding = "same")(x)
    class1_Batch = tf.keras.layers.BatchNormalization()(class1)

    class1_GlobalMax = tf.keras.layers.GlobalMaxPooling2D()(class1_Batch)
    class1_GlobalMax_Batch = tf.keras.layers.BatchNormalization()(class1_GlobalMax)

    class1_GlobalAver = tf.keras.layers.GlobalAveragePooling2D()(class1_Batch)
    class1_GlobalAver_Batch = tf.keras.layers.BatchNormalization()(class1_GlobalAver)

    poolmerge = tf.keras.layers.concatenate([seg_GlobalMax_Batch, seg_GlobalAver_Batch, class1_GlobalMax_Batch, class1_GlobalAver_Batch])

    slayer = tf.keras.layers.Dense(1, activation = "sigmoid", kernel_initializer = "random_normal", name = "s_layer")(poolmerge)

    model = tf.keras.models.Model(inputs = input_image, outputs = [seg, slayer])

    if "LMExp" in model_option:
        if model_option == "LMExp1" or model_option == "LMExp2":
            F = make_lm_filter(7)
            F = np.expand_dims(F, axis = 2)
            F = F if img_dim == 1 else np.concatenate([F, F, F], axis = 2)
        else:
            F = make_lm_filter(11)
            F = np.expand_dims(F, axis = 2)
            F = F if img_dim == 1 else np.concatenate([F, F, F], axis = 2)
        model.variables[0].assign(F)

    elif "RFS" in model_option:
        if model_option == "RFSExp1" or model_option == "RFSExp2":
            F = make_rfs_filter(7)
            F = np.expand_dims(F, axis = 2)
            F = F if img_dim == 1 else np.concatenate([F, F, F], axis = 2)
        else:
            F = make_rfs_filter(11)
            F = np.expand_dims(F, axis = 2)
            F = F if img_dim == 1 else np.concatenate([F, F, F], axis = 2)
        model.variables[0].assign(F)

    elif "SExp" in model_option:
        if model_option == "SExp1" or model_option == "SExp2":
            F = make_s_filter()
            F = np.expand_dims(F, axis = 2)
            F = F if img_dim == 1 else np.concatenate([F, F, F], axis = 2)
        else:
            F = make_s_filter(13, 11)
            F = np.expand_dims(F, axis = 2)
            F = F if img_dim == 1 else np.concatenate([F, F, F], axis = 2)
        model.variables[0].assign(F)

    return model

class AnoNet(tf.keras.Model):
    def __init__(self, model_option = "LMExp1", input_shape = (None, None, 3), img_dim = 1, train_seg = True, train_class = True, vis = True, total_batch_size = 0):
        super(AnoNet, self).__init__()
        self.model = get_model(model_option, input_shape, img_dim)
        self.train_seg = train_seg
        self.train_class = train_class
        self.vis = vis
        self.num_batch_size = 0
        self.props = dict(boxstyle = 'round', facecolor = 'wheat', alpha = 0.5)
        self.fig = plt.subplots(1, 3, figsize = (18, 10)) if vis else None
        self.img_dim = img_dim
        self.total_batch_size = total_batch_size
        self.current_epoch = 1

    @tf.function
    def train_step(self, data):
        [input_images], [label_seg, label_class] = data
        self.num_batch_size += 1

        if self.train_seg:

            with tf.GradientTape() as tape:
                segs, scores = self(input_images)

                for i in range(8, 29):
                    self.model.layers[i].trainable = False

                loss_seg = tf.math.reduce_mean(dice_loss(label_seg, segs))
                loss_total = loss_seg

                gradients = tape.gradient(loss_total, self.trainable_variables)
                self.optimizer.apply_gradients([
                    (grad, var) 
                    for (grad, var) in zip(gradients, self.trainable_variables) 
                    if grad is not None
                ])

                if self.vis and self.num_batch_size == self.total_batch_size:
                    with tf.experimental.async_scope():
                        self.__vis_data_train__(input_images[0].numpy(), label_seg[0].numpy(), segs[0].numpy(), label_class[0].numpy(), scores[0].numpy())
                        self.num_batch_size = 0

            return {'loss': loss_total}
        
        if self.train_class:

            with tf.GradientTape() as tape:
                segs, scores = self(input_images)

                for i in range(8):
                    self.model.layers[i].trainable = False

                loss_scores = tf.math.reduce_mean(tf.keras.losses.BinaryCrossentropy()(label_class, scores))
                loss_total = loss_scores

                gradients = tape.gradient(loss_total, self.trainable_variables)
                self.optimizer.apply_gradients([
                    (grad, var) 
                    for (grad, var) in zip(gradients, self.trainable_variables) 
                    if grad is not None
                ])

                if self.vis and self.num_batch_size == self.total_batch_size:
                    with tf.experimental.async_scope():
                        self.__vis_data_train__(input_images[0].numpy(), label_seg[0].numpy(), segs[0].numpy(), label_class[0].numpy(), scores[0].numpy())
                        self.num_batch_size = 0
                            
            return {'loss': loss_total}

    def __vis_data_train__(self, image, segs_gt, segs_pred, scores_gt, scores_pred):
       if self.img_dim == 3:
            image = np.array(image)
            image = image * 255.
            image = tf.cast(image, tf.uint8)
            image = np.array(image)

            mask = segs_gt * 255.
            mask = tf.cast(mask, tf.uint8)
            mask = np.array(mask)

            seg = np.array(segs_pred)
            seg = seg * 255.0
            seg = tf.cast(seg, tf.uint8)
            seg = np.array(seg)
            cv2.imwrite(cfg.RESULT_DIR + '/img_with_epoch{0}.jpg'.format(self.current_epoch),  cv2.cvtColor(image, cv2.COLOR_RGB2BGR))

            
            if scores_gt > 0.5: 
                content = 'bad'
            else:
                content = 'good'
            cv2.putText(mask, content, (40,40), cv2.FONT_HERSHEY_SIMPLEX, 1, (255,255,255), 2)
            cv2.imwrite(cfg.RESULT_DIR + '/mask_img_with_epoch{0}.jpg'.format(self.current_epoch), mask)

            if scores_pred >= 0.5: 
                #content = 'bad with scores: %s'%(np.round(scores_pred, 2))
                content = 'bad with score: %s'%(np.round(scores_pred, 2))
            else:
                #content = 'good with scores: %s'%(np.round(scores_pred, 2))
                content = 'good with score: %s'%(np.round(1.0 - scores_pred, 2))
            cv2.putText(seg, content, (40,40), cv2.FONT_HERSHEY_SIMPLEX, 1, (255,255,255), 2)
            cv2.imwrite(cfg.RESULT_DIR + '/predict_img_with_epoch{0}.jpg'.format(self.current_epoch), seg)

            self.current_epoch += 1
       else:
            image = np.array(image)
            image = image * 255.
            image = tf.cast(image, tf.uint8)
            image = np.array(image)

            mask = segs_gt * 255.
            mask = tf.cast(mask, tf.uint8)
            mask = np.array(mask)

            seg = np.array(segs_pred)
            seg = seg * 255.0
            seg = tf.cast(seg, tf.uint8)
            seg = np.array(seg)
            cv2.imwrite(cfg.RESULT_DIR + '/img_with_epoch{0}.jpg'.format(self.current_epoch), image)

            
            if scores_gt > 0.5: 
                content = 'bad'
            else:
                content = 'good'
            cv2.putText(mask, content, (40,40), cv2.FONT_HERSHEY_SIMPLEX, 1, (255,255,255), 2)
            cv2.imwrite(cfg.RESULT_DIR + '/mask_img_with_epoch{0}.jpg'.format(self.current_epoch), mask)

            if scores_pred >= 0.5: 
                content = 'bad with score: %s'%(np.round(scores_pred, 2))
            else:
                content = 'good with score: %s'%(np.round(1.0 - scores_pred, 2))
            cv2.putText(seg, content, (40,40), cv2.FONT_HERSHEY_SIMPLEX, 1, (255,255,255), 2)
            cv2.imwrite(cfg.RESULT_DIR + '/predict_img_with_epoch{0}.jpg'.format(self.current_epoch), seg)

            self.current_epoch += 1
        
    def call(self, inputs):
        if isinstance(inputs, tuple):
            return self.model(inputs[0])
        else:
            return self.model(inputs)