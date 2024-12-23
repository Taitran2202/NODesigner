from models.medium import ClassifierOCRMedium
from models.resnet import OCRResnet, OCRResnetBase
from models.small import ClassifierOCRSmall
import string

def getModel(name,input_width=32,input_height=32):
    if name=='small':
        return ClassifierOCRSmall(input_width=input_width,input_height=input_height)
    if name=='medium':
        return ClassifierOCRMedium(input_width=input_width,input_height=input_height)
    if name=='resnet':
        return OCRResnet(input_width=input_width,input_height=input_height)
    if name=='resnetbase':
        return OCRResnetBase(input_width=input_width,input_height=input_height)

class OCRMODEL:
    def __init__(self):
        pass
    def GetLabels(self):
        return list((string.digits+string.ascii_lowercase).upper())
    