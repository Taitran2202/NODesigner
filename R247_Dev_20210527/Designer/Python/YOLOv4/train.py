from builder import BuildTrainner
from config import *
from utils import *
from common_modules import *
from model import *
from loss import *
from optimizer import *
from dataset import *
import tf2onnx



def main(argv):
    get_config_from_json_file(argv[0])
    # cfg.YOLO.CLASSES = argv[0]
    # cfg.YOLO.TRAIN_YOLO_TINY = False if argv[1] == "yolov4" else True
    # cfg.YOLO.TRAIN.EPOCHS = int(argv[2])
    # cfg.YOLO.TRAIN.ANNOT_PATH = argv[3]
    # cfg.YOLO.TRAIN.SAVED_MODEL_FOLDER = argv[4]
    # cfg.YOLO.TRAIN.WEIGHTS_PATH = argv[5]
    # cfg.YOLO.TRAIN.PRETRAINED_WEIGHT_PATH = argv[6]
    # cfg.YOLO.TRAIN.TRAIN_LOG_DIR = argv[7]
    # cfg.YOLO.TRAIN.DATA_AUG = True if argv[8] == "True" else False
    # cfg.YOLO.TRAIN.USE_MOSAIC_IMG = True if argv[9] == "True" else False
    # cfg.YOLO.TRAIN.BATCH_SIZE = int(argv[10])
    # cfg.YOLO.TRAIN.OPTIMIZERTYPE = argv[11]
    # cfg.YOLO.TRAIN.TRANSFER = argv[12]
    # cfg.YOLO.TRAIN.VISUAL_LEARNING_PROCESS = True if argv[13] == "True" else False
    # cfg.YOLO.TRAIN.RESULT_DIR = argv[14]
    # cfg.YOLO.TRAIN.IMG_DIR = argv[15]
    if(cfg.YOLO.TRAIN_YOLO_TINY):
        cfg.YOLO.TRAIN.PRETRAINED_WEIGHT_PATH= os.path.join(cfg.YOLO.TRAIN.PRETRAINED_WEIGHT_DIR, 'yolov4-tiny.weights')
    else:
        cfg.YOLO.TRAIN.PRETRAINED_WEIGHT_PATH= os.path.join(cfg.YOLO.TRAIN.PRETRAINED_WEIGHT_DIR, 'yolov4.weights')
    trainer = BuildTrainner()
    trainer()

if __name__ == '__main__':
    main(sys.argv[1:])
    