import tensorflow as tf
from tensorflow import keras
import tensorflow
from tensorflow.keras.layers import *
import numpy as np
import cv2
import os
from sklearn.decomposition import PCA
from tensorflow.python.keras.backend import dtype
from tensorflow.python.keras.optimizer_v1 import Adam
from tensorflow.keras.applications.vgg19 import preprocess_input
tf.config.run_functions_eagerly(True)
import cv2

class FeatCAE(tf.keras.Model):
    def __init__(self,extractor,latent_dim=50, is_bn=True):
        super(FeatCAE, self).__init__()
        self.extractor = extractor
        in_channels = extractor.outchannels
        self.conv1= Conv2D(filters=(in_channels + 2 * latent_dim) // 2,kernel_size=1,padding='same')
        
        self.conv2= Conv2D(filters=2 * latent_dim,kernel_size=1,padding='same')
        self.encoder= Conv2D(filters=latent_dim,kernel_size=1,padding='same')
        
        self.conv3= Conv2D(filters=2 * latent_dim,kernel_size=1,padding='same')
        self.conv4= Conv2D(filters= (in_channels + 2 * latent_dim) // 2,kernel_size=1,padding='same')
        self.decoder= Conv2D(filters= in_channels,kernel_size=1,padding='same')
        #self.difference= tensorflow.keras.layers.Subtract()
        self.main_output = tensorflow.keras.layers.Lambda(lambda x: tf.reduce_mean(tf.math.squared_difference(x[0],x[1]),axis=-1))
        self.batchnorm1= BatchNormalization()
        self.batchnorm2= BatchNormalization()
        self.batchnorm3= BatchNormalization()
        self.batchnorm4= BatchNormalization()
        self.relu1= ReLU()
        self.relu2= ReLU()
        self.relu3= ReLU()
        self.relu4= ReLU()
    def compile(
        self,
        optimizer,
        metrics
    ):
        """ Configure the distiller.

        Args:
            optimizer: Keras optimizer for the student weights
            metrics: Keras metrics for evaluation
            student_loss_fn: Loss function of difference between student
                predictions and ground-truth
            distillation_loss_fn: Loss function of difference between soft
                student predictions and soft teacher predictions
            alpha: weight to student_loss_fn and 1-alpha to distillation_loss_fn
            temperature: Temperature for softening probability distributions.
                Larger temperature gives softer distributions.
        """
        super(FeatCAE, self).compile(optimizer=optimizer, metrics=metrics)
    def call(self, inputs):
        x = self.conv1(inputs)
        x = self.batchnorm1(x)
        x = self.relu1(x)
        x = self.conv2(x)
        x = self.batchnorm2(x)
        x = self.relu2(x)
        x = self.encoder(x)
        x = self.conv3(x)
        x = self.batchnorm3(x)
        x = self.relu3(x)
        x = self.conv4(x)
        x = self.batchnorm4(x)
        x = self.relu4(x)
        x = self.decoder(x)
        #x = self.difference([x,inputs])
        x = self.main_output([x,inputs])
        return x
    def l2loss(self,y_pred,y_input):
        return tf.reduce_mean(tf.math.squared_difference(y_pred,y_input))
    def l2loss_single(self,y_pred):
        return tf.reduce_mean(y_pred)
    def train_step(self, data):
        
        x = data[0]
        features=self.extractor.ExtractFeature(x)
        # Forward pass of teacher
        
        with tf.GradientTape() as tape:
            prediction = self(features, training=True)
            #loss =  self.l2loss(prediction,features)
            loss =  self.l2loss_single(prediction)
            loss += sum(self.losses)
        trainable_vars = self.trainable_variables
        gradients = tape.gradient(loss, trainable_vars)

        # Update weights
        self.optimizer.apply_gradients(zip(gradients, trainable_vars))
        results = {m.name: m.result() for m in self.metrics}
        results.update(
            {"loss": loss}
        )
        return results
    def estimate_thred_with_fpr(self, traindata, expect_fpr=0.05):
        """
        Use training set to estimate the threshold.
        """
        print("Estimating threshold ...")
        threshold = 0
        scores_list = []
        i=0
        for item in traindata:
            i=i+1
            if i>=traindata.n:
                break
            image = preprocess_input(item[0].copy())
            features=self.extractor.ExtractFeature(image)
            output = self(features)
            diff = output[0].numpy()
            scores_list.append(diff)
        scores = np.concatenate(scores_list, axis=0)

        # find the optimal threshold
        max_step = 100
        min_th = scores.min()
        max_th = scores.max()
        delta = (max_th - min_th) / max_step
        for step in range(max_step):
            threshold = max_th - step * delta
            # segmentation
            binary_score_maps = np.zeros_like(scores)
            binary_score_maps[scores <= threshold] = 0
            binary_score_maps[scores > threshold] = 1

            # estimate the optimal threshold base on user defined min_area
            fpr = binary_score_maps.sum() / binary_score_maps.size
            print(
                "threshold {}: find fpr {} / user defined fpr {}".format(threshold, fpr, expect_fpr))
            if fpr >= expect_fpr:  # find the optimal threshold
                print("find optimal threshold:", threshold)
                print("Done.\n")
                break
        return threshold, min_th, max_th

class Extractor:
    def __init__(self,layers=['block1_conv2','block2_conv2','block3_conv2'],inputsize=(224,224)):
        super(Extractor, self).__init__()
        self.inputsize= inputsize
        vgg=tf.keras.applications.vgg19.VGG19(
            include_top=False, weights='imagenet', input_tensor=None,
            input_shape=(inputsize[0],inputsize[1],3))
        # vgg.summary()
        self.feat_layers= layers
        self.extractor = tf.keras.Model(inputs=vgg.inputs,
                        outputs=[layer.output for layer in (vgg.get_layer(layername) for layername in layers)])
        self.outchannels = 0
        for output in self.extractor.outputs:
            self.outchannels +=output.shape[-1]
        pass
    def ExtractFeature(self,input):
        features_map = self.extractor(input)
        out_features=[]
        for feature in features_map:
            resized_features=tf.image.resize(
                feature, (self.inputsize[0],self.inputsize[1]), method= tf.image.ResizeMethod.BILINEAR, preserve_aspect_ratio=False,
                antialias=False, name=None)
            resized_features = tf.keras.layers.AveragePooling2D(
                pool_size=(4, 4), strides=(4,4), padding='valid', data_format=None)(resized_features)
            out_features.append(resized_features)
        return tf.concat(out_features,axis=-1)
    def estimate_latent_dim(self,traindata):
        print("Estimating latent code dimension ...")
        pca = PCA(n_components=0.9)
        feature_subset = []
        i=0
        for item in traindata:
            i=i+1
            image = preprocess_input(item[0].copy())
            features=self.ExtractFeature(image)
            feature_subset.append(features)
            if i == 1:
                break
        feature_subset = tf.concat(feature_subset,axis=-1)
        feature_subset = tf.transpose(feature_subset,perm=[0,3,1,2])
        feature_subset = tf.reshape(feature_subset, [feature_subset.shape[0],feature_subset.shape[1],-1])
        feature_subset = tf.transpose(feature_subset,perm=[0,2,1])
        feature_subset = tf.unstack(feature_subset)
        
        feature_subset =feature_subset[0].numpy()
        pca.fit(feature_subset)
        latent_dim,_ = pca.components_.shape
        print("cd =",latent_dim)
        return latent_dim

def main(argv):
    imagedir = argv[0]
    modeldir = argv[1]
    step=int(argv[2])
    checkpoint_path = os.path.join(modeldir,'checkpoint')
    cnn_layers=["block1_conv1","block1_conv2","block2_conv1","block2_conv2","block3_conv1","block3_conv2","block3_conv3",
                "block3_conv4"]
    inputsize = (512,512)
    extractor = Extractor(layers= cnn_layers,inputsize=inputsize)
    latent_dim = None
    cae = FeatCAE(extractor)
    cae.build((None,inputsize[0]//2,inputsize[1]//2,extractor.outchannels))
    cae.compile(tf.keras.optimizers.SGD(learning_rate=0.1),metrics=['loss'])
    reduce_lr = tf.keras.callbacks.ReduceLROnPlateau(monitor='loss', factor=0.2,patience=5, min_lr=0.0001,verbose=1)
    model_checkpoint_callback=tf.keras.callbacks.ModelCheckpoint(
            checkpoint_path,
            monitor="loss",
            verbose=0,
            save_best_only=False,
            save_weights_only=True,
            mode="auto",
            save_freq="epoch",
            options=None,
        )
    train_datagen = tf.keras.preprocessing.image.ImageDataGenerator(
        preprocessing_function=preprocess_input,
        # zoom_range=0.2,
        horizontal_flip=True,
        vertical_flip=True,
        )
    traindata= train_datagen.flow_from_directory(
        imagedir,
        class_mode="binary",
        color_mode="rgb",
        batch_size=1,
        target_size=inputsize,
        )
    cae.fit(traindata,epochs = step,callbacks=[reduce_lr,model_checkpoint_callback],batch_size=1)
    #tf.keras.models.save_model(extractor.extractor,os.path.join(modeldir,'extractor')
    tf.keras.models.save_model(cae,os.path.join(modeldir))
import sys
if __name__ == "__main__":
    main(sys.argv[1:])
    #python anomaly_detection_main.py "C:\Users\TAN VU\AppData\Roaming\R247\Jobs\anomaly\designer 0\c2e26a66-eb2b-46b9-a2f4-b9c117c045dc" "C:\Users\TAN VU\AppData\Roaming\R247\Jobs\anomaly\designer 0\c2e26a66-eb2b-46b9-a2f4-b9c117c045dc\data" 3