from models.mobilenetv2 import CRAFTMobileNetV2
from models.vgg16 import CRAFTVGG16

def getModel(name,input_width=None,input_height=None):
    if name=='mobilenetv2':
        return CRAFTMobileNetV2(input_width=input_width,input_height=input_height)
    if name=='vgg16':
        return CRAFTVGG16(input_width=input_width,input_height=input_height)
