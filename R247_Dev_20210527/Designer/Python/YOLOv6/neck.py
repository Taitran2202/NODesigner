from config import *
from common_modules import *

def RepPANNeck(inputs, channels_list=None, num_repeats=None, block_name='RepVGGBlock', deploy=False):
    assert channels_list is not None
    assert num_repeats is not None

    x2, x1, x0 = inputs

    fpn_out0 = SimConv(channels_list[4], channels_list[5], 1, 1)(x0)
    upsample_feat0 = Transpose(channels_list[5], channels_list[5])(fpn_out0)
    f_concat_layer0 = tf.concat([upsample_feat0, x1], axis=-1)
    f_out0 = RepBlock(f_concat_layer0, channels_list[3] + channels_list[5], channels_list[5], num_repeats[5], block_name, deploy=deploy)

    fpn_out1 =SimConv(channels_list[5], channels_list[6], 1, 1)(f_out0)
    upsample_feat1 = Transpose(channels_list[6], channels_list[6])(fpn_out1)
    f_concat_layer1 = tf.concat([upsample_feat1, x2], axis=-1)
    pan_out2 = RepBlock(f_concat_layer1, channels_list[2] + channels_list[6], channels_list[6], num_repeats[6], block_name, deploy=deploy)

    down_feat1 = SimConv(channels_list[6], channels_list[7], 3, 2)(pan_out2)
    p_concat_layer1 = tf.concat([down_feat1, fpn_out1], axis=-1)
    pan_out1 = RepBlock(p_concat_layer1, channels_list[6] + channels_list[7], channels_list[8], num_repeats[7], block_name, deploy=deploy)

    down_feat0 = SimConv(channels_list[8], channels_list[9], 3, 2)(pan_out1)
    p_concat_layer2 = tf.concat([down_feat0, fpn_out0], axis=-1)
    pan_out0 = RepBlock(p_concat_layer2, channels_list[5] + channels_list[9], channels_list[10], num_repeats[8], block_name, deploy=deploy)

    outputs = [pan_out2, pan_out1, pan_out0]
    return outputs