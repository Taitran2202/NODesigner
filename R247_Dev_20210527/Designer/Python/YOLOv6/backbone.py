from config import *
from common_modules import *

def EfficientRep(inputs, in_channels=3, channels_list=None, num_repeats=None, block_name='RepVGGBlock', deploy=False):
    assert channels_list is not None
    assert num_repeats is not None

    outputs = []

    block = eval(block_name)
    stem = block(in_channels, channels_list[0], kernel_size=3, stride=2, deploy=deploy)(inputs)

    # ERBlock 2
    x = block(channels_list[0], channels_list[1], kernel_size=3, stride=2, deploy=deploy)(stem)
    x = RepBlock(x, channels_list[1], channels_list[1], num_repeats[1], block_name, deploy=deploy)

    # ERBlock 3
    x = block(channels_list[1], channels_list[2], kernel_size=3, stride=2, deploy=deploy)(x)
    x = RepBlock(x, channels_list[2], channels_list[2], num_repeats[2], block_name, deploy=deploy)
    outputs.append(x)

    # ERBlock 4
    x = block(channels_list[2], channels_list[3], kernel_size=3, stride=2, deploy=deploy)(x)
    x = RepBlock(x, channels_list[3], channels_list[3], num_repeats[3], block_name, deploy=deploy)
    outputs.append(x)

    # ERBlock 5
    x = block(channels_list[3], channels_list[4], kernel_size=3, stride=2, deploy=deploy)(x)
    x = RepBlock(x, channels_list[4], channels_list[4], num_repeats[4], block_name, deploy=deploy)
    x = SimSPPF(channels_list[4], channels_list[4], kernel_size=5)(x)
    outputs.append(x)

    return outputs