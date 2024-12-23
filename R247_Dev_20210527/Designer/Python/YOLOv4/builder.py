from config import *
from model import *
from dataset import *

def TrainYOLO():
    trainer = YOLOTrainer(cfg.YOLO.TRAIN.TRAIN_LOG_DIR, cfg.YOLO.TRAIN.SAVED_MODEL_FOLDER, 
                    optimizerType=cfg.YOLO.TRAIN.OPTIMIZERTYPE, visual_learning_process=cfg.YOLO.TRAIN.VISUAL_LEARNING_PROCESS,
                    transfer=cfg.YOLO.TRAIN.TRANSFER)
    trainDataset = Dataset()
    trainer.train(trainDataset, None)
def TrainRYOLO():   
    cfg.YOLO.STRIDES =[8, 16, 32]
    cfg.YOLO.ANCHORS = np.array([[12,  16], [19,   36], [40,   28],
                                [36,  75], [76,   55], [72,  146],
                                [142,110], [192, 243], [459, 401]], np.float32)

    cfg.YOLO.ANGLES = [-np.pi * 60 / 180, -np.pi * 30 / 180, 0, np.pi * 30 / 180, np.pi * 60 / 180, np.pi * 90 / 180]
    cfg.YOLO.ANCHOR_PER_SCALE = 3
    cfg.YOLO.NUM_NECK = 1
    cfg.YOLO.IOU_LOSS_THRESH = 0.5
    cfg.YOLO.TRAIN.LOSS_TYPE_FOR_BBOXES = 'ciou'
    cfg.YOLO.TRAIN.MODEL_SIZE = (256, 256)
    cfg.YOLO.TRAIN.EPOCHS = 100
    cfg.YOLO.TRAIN.TRANSFER = 'transfer'
    cfg.YOLO.TRAIN.BATCH_SIZE = 1
    cfg.YOLO.TRAIN.LOSS_TYPE_FOR_BBOXES = 'ciou'
    cfg.YOLO.TRAIN.LR_INIT = 1e-3
    cfg.YOLO.TRAIN.VISUAL_LEARNING_PROCESS = False
    cfg.YOLO.TRAIN_YOLO_TINY = False
    cfg.YOLO.NUM_NECK = 1
    cfg.YOLO.TRAIN.DATA_AUG = False
    cfg.YOLO.TRAIN.USE_MOSAIC_IMG = False
    # cfg.YOLO.TRAIN.ANNOT_PATH = "/content/drive/MyDrive/NO_dataset/Rotated_Obj_detection/annotations"
    trainDataset = PolygoDataset()
    trainer = RYOLOTrainer(train_log_dir=cfg.YOLO.TRAIN.TRAIN_LOG_DIR, saved_model_dir=cfg.YOLO.TRAIN.SAVED_MODEL_FOLDER, class_file=cfg.YOLO.CLASSES,
                        model_size=cfg.YOLO.TRAIN.MODEL_SIZE, num_neck=cfg.YOLO.NUM_NECK, visual_learning_process=cfg.YOLO.TRAIN.VISUAL_LEARNING_PROCESS,
                        backbone=BACKBONE_LIST[1], neck=NECK_LIST[0], head=HEAD_LIST_FOR_OBJECT_DETECTION[3], weights_path=cfg.YOLO.TRAIN.WEIGHTS_PATH,
                        batch_size=cfg.YOLO.TRAIN.BATCH_SIZE, epochs=cfg.YOLO.TRAIN.EPOCHS, class_map=trainDataset.classes)
    trainer.train(trainDataset, None)

def BuildTrainner():
    if(cfg.MODEL_TYPE == "YOLO"):
        return TrainYOLO
    elif(cfg.MODEL_TYPE == "RYOLO"):
        return TrainRYOLO
