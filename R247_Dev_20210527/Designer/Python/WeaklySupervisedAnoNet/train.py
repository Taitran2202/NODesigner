from config import *
from dataset import *
from loss import *
from optimizer import *
from filters import *
from initialize_filters import *
from model import *
from tensorflow.python.framework.convert_to_constants import convert_variables_to_constants_v2
import tf2onnx

def frozen_keras_graph(model_dir):
    frozen_path = os.path.join(model_dir, 'frozen_graph.pb')
    model = tf.keras.models.load_model(model_dir, compile=False)
    infer = model.signatures['serving_default']
    full_model = tf.function(infer).get_concrete_function(
        tf.TensorSpec(infer.inputs[0].shape, infer.inputs[0].dtype))
    # Get frozen ConcreteFunction
    frozen_func = convert_variables_to_constants_v2(full_model)
    tf.io.write_graph(frozen_func.graph, '.', frozen_path,as_text=False)
    
    frozen_func.graph.as_graph_def()

    layers = [op.name for op in frozen_func.graph.get_operations()]
    print("-" * 50)
    print("Frozen model layers: ")
    for layer in layers:
        print(layer)

    print("-" * 50)
    print("Frozen model inputs: ")
    print(frozen_func.inputs)
    print("Frozen model outputs: ")
    print(frozen_func.outputs)
    print('Successfully export frozen graph in ',str(model_dir))


def lr_decay(epoch):
    return cfg.LR_INIT * np.power(cfg.LR_DECAY_RATE, epoch // cfg.LR_DECAY_STEPS)

def train():
    if not os.path.exists(cfg.CHECKPOINT_PATH):
        os.mkdir(cfg.CHECKPOINT_PATH)
    
    if not os.path.exists(os.path.sep.join([cfg.CHECKPOINT_PATH, "segmentation"])):
        os.mkdir(os.path.sep.join([cfg.CHECKPOINT_PATH, "segmentation"]))
    
    if not os.path.exists(os.path.sep.join([cfg.CHECKPOINT_PATH, "classification"])):
        os.mkdir(os.path.sep.join([cfg.CHECKPOINT_PATH, "classification"]))
    
    if not os.path.exists(os.path.sep.join([cfg.CHECKPOINT_PATH, "model"])):
        os.mkdir(os.path.sep.join([cfg.CHECKPOINT_PATH, "model"]))

    if not os.path.exists(cfg.SAVED_MODEL_DIR):
        os.mkdir(cfg.SAVED_MODEL_DIR)

    savemodel_path = os.path.sep.join([cfg.SAVED_MODEL_DIR, cfg.MODEL_OPTIONS])
    if not os.path.exists(savemodel_path):
        os.mkdir(savemodel_path)
    
    manager = Datagenerator(cfg.IMAGE_DIR, cfg.MASKS_DIR, cfg.CHANNELS, cfg.BATCH_SIZE,cfg.AUGMENT and cfg.TRAIN_SEG, tuple((cfg.IMG_SIZE[1], cfg.IMG_SIZE[0])))

    steps_per_epoch = len(manager)

    anonet = AnoNet(model_option = cfg.MODEL_OPTIONS, input_shape = (*cfg.IMG_SIZE, cfg.CHANNELS), img_dim = cfg.CHANNELS, train_seg = cfg.TRAIN_SEG, train_class = not cfg.TRAIN_SEG,  vis = cfg.VISUAL_LEARNING_PROCESS, total_batch_size = steps_per_epoch)

    if cfg.TRAIN_SEG:
        checkpoint_path = os.path.sep.join([os.path.sep.join([cfg.CHECKPOINT_PATH, "segmentation"]), "model_anonet_%s.ckpt"%(cfg.MODEL_OPTIONS)])
        checkpoint_dir = os.path.dirname(checkpoint_path)
        latest = tf.train.latest_checkpoint(checkpoint_dir)

        if latest and cfg.TRANSFER == "resume":
            anonet.load_weights(latest)
    else:
        checkpoint_path = os.path.sep.join([os.path.sep.join([cfg.CHECKPOINT_PATH, "classification"]), "model_anonet_%s.ckpt"%(cfg.MODEL_OPTIONS)])
        checkpoint_dir_cls = os.path.dirname(checkpoint_path)
        latest_cls = tf.train.latest_checkpoint(checkpoint_dir_cls)
        if latest_cls and cfg.TRANSFER == "resume":
            anonet.load_weights(latest_cls)
        else:
            checkpoint_dir = os.path.dirname(os.path.sep.join([os.path.sep.join([cfg.CHECKPOINT_PATH, "segmentation"]), "model_anonet_%s.ckpt"%(cfg.MODEL_OPTIONS)]))
            latest = tf.train.latest_checkpoint(checkpoint_dir)

            if latest:
                anonet.load_weights(latest)

    opt = tf.keras.optimizers.Adam(cfg.LR_INIT)

    anonet.compile(optimizer = opt)

    lr_scheduler = tf.keras.callbacks.LearningRateScheduler(lr_decay)
    modelckpt = tf.keras.callbacks.ModelCheckpoint(filepath = checkpoint_path, mode = 'min', monitor = 'loss', save_best_only = True, save_weights_only = True, verbose = 1)

    epochs = cfg.EPOCHS_FOR_SEG if cfg.TRAIN_SEG else cfg.EPOCHS_FOR_CLS
    anonet.fit(manager,
                steps_per_epoch = steps_per_epoch,
                epochs = epochs,
                callbacks = [lr_scheduler, modelckpt],
                )

    # Save the model
    #if not cfg.TRAIN_SEG:
    #anonet.save(savemodel_path)
    #frozen_keras_graph(savemodel_path)
    model_proto, external_tensor_storage = tf2onnx.convert.from_keras(anonet.model,
                input_signature=None, opset=None, custom_ops=None,
                custom_op_handlers=None, custom_rewriter=None,
                inputs_as_nchw=['input_image'], extra_opset=None,shape_override=None,
                target=None, large_model=False, output_path=os.path.join(savemodel_path,'model.onnx'))


def main(argv):
    get_config_from_json_file(argv[0])
    # if cfg.AUGMENT and cfg.TRAIN_SEG:
    #     augment_data(cfg.IMAGE_DIR, cfg.MASKS_DIR, "bad")
    #     augment_data(cfg.IMAGE_DIR, cfg.MASKS_DIR, "good")

    device = cuda.get_current_device()
    device.reset()
    tf.keras.backend.clear_session()
    train()

if __name__ == '__main__':
    main(sys.argv[1:])