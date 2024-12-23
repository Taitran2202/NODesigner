from config import *

def SiLU(x): 
    x = tf.convert_to_tensor(x)
    return x * tf.sigmoid(x)

def Hard_swish(x):
    x = tf.convert_to_tensor(x)
    return x * tf.nn.relu6(x + 3) / 6

def Mish(x):
    x = tf.convert_to_tensor(x)
    return x * tf.math.tanh(tf.math.softplus(x))

def Swish(x):
    x = tf.convert_to_tensor(x)
    return tf.nn.swish(x)

def LeakyReLU(x):
    x = tf.convert_to_tensor(x)
    return tf.nn.leaky_relu(x, alpha=0.1)


def conv_bn(in_channels, out_channels, kernel_size, stride, padding, groups=1, idx=0, layer_name=None):
    result = tf.keras.Sequential()
    padding = 'same' if padding == 1 else 'valid'
    if kernel_size == 3 and stride == 2 and padding == 'same':
        result.add(tf.keras.layers.ZeroPadding2D(1))
        result.add(tf.keras.layers.Conv2D(out_channels, kernel_size, stride, 'valid', groups=groups, use_bias=False, name='conv_%s_%s'%(idx, layer_name)))
    else:
        result.add(tf.keras.layers.Conv2D(out_channels, kernel_size, stride, padding, groups=groups, use_bias=False, name='conv_%s_%s'%(idx, layer_name)))
    result.add(tf.keras.layers.BatchNormalization(name='bn_%s_%s'%(idx, layer_name)))
    return result


class RepVGGBlock(tf.keras.layers.Layer):
    def __init__(self, in_channels, out_channels, kernel_size=3, stride=1, padding=1, dilation=1, groups=1, deploy=False, use_se=False):
        super(RepVGGBlock, self).__init__()
        self.deploy = deploy
        self.groups = groups
        self.in_channels = in_channels
        assert kernel_size == 3
        assert padding == 1

        padding_11 = padding - kernel_size // 2
        self.nonlinearity = tf.keras.layers.ReLU()

        self.idx = 0 if len(self.name.split('_')) == 3 else int(self.name.split('_')[-1])

        if deploy:
            self.rbr_reparam = tf.keras.Sequential(
                [
                  tf.keras.layers.ZeroPadding2D(padding=1),
                  tf.keras.layers.Conv2D(filters=out_channels, kernel_size=kernel_size, strides=stride, padding='valid',
                                         dilation_rate=dilation, groups=groups, use_bias=True)
                ]
            )
        else:
            self.rbr_identity = tf.keras.layers.BatchNormalization() if in_channels == out_channels and stride == 1 else None
            self.rbr_dense = conv_bn(in_channels, out_channels, kernel_size, stride, padding, groups, self.idx, 'rbr_dense')
            self.rbr_1x1 = conv_bn(in_channels, out_channels, 1, stride, padding_11, groups, self.idx, 'rbr_1x1')
            print('RepVGG Block, identity = ', self.rbr_identity)


    def call(self, x):
        if hasattr(self, 'rbr_reparam'):
            return self.nonlinearity(self.rbr_reparam(x))
        
        if self.rbr_identity is None:
            id_out = 0
        else:
            id_out = self.rbr_identity(x)
        
        return self.nonlinearity(self.rbr_dense(x) + self.rbr_1x1(x) + id_out)


    def get_equivalent_kernel_bias(self):
        kernel3x3, bias3x3 = self._fuse_bn_tensor(self.rbr_dense)
        kernel1x1, bias1x1 = self._fuse_bn_tensor(self.rbr_1x1)
        kernelid, biasid = self._fuse_bn_tensor(self.rbr_identity)
        return kernel3x3 + self._pad_1x1_to_3x3_tensor(kernel1x1) + kernelid, bias3x3 + bias1x1 + biasid
    
    def _pad_1x1_to_3x3_tensor(self, kernel1x1):
        if kernel1x1 is None:
            return 0
        return tf.pad(kernel1x1, [[1, 1], [1, 1], [0, 0], [0, 0]])
    
    def _fuse_bn_tensor(self, branch):
        if branch is None:
            return 0, 0
        
        if isinstance(branch, tf.keras.Sequential):
            kernel = branch.get_layer('conv_%s_%s'%(self.idx, 'rbr_dense')).weights[0] if branch is self.rbr_dense else \
                     branch.get_layer('conv_%s_%s'%(self.idx, 'rbr_1x1')).weights[0]
            running_mean = branch.get_layer('bn_%s_%s'%(self.idx, 'rbr_dense')).moving_mean if branch is self.rbr_dense else \
                           branch.get_layer('bn_%s_%s'%(self.idx, 'rbr_1x1')).moving_mean
            running_var = branch.get_layer('bn_%s_%s'%(self.idx, 'rbr_dense')).moving_variance if branch is self.rbr_dense else \
                          branch.get_layer('bn_%s_%s'%(self.idx, 'rbr_1x1')).moving_variance
            gamma = branch.get_layer('bn_%s_%s'%(self.idx, 'rbr_dense')).gamma if branch is self.rbr_dense else \
                    branch.get_layer('bn_%s_%s'%(self.idx, 'rbr_1x1')).gamma
            beta = branch.get_layer('bn_%s_%s'%(self.idx, 'rbr_dense')).beta if branch is self.rbr_dense else \
                   branch.get_layer('bn_%s_%s'%(self.idx, 'rbr_1x1')).beta
            eps = branch.get_layer('bn_%s_%s'%(self.idx, 'rbr_dense')).epsilon if branch is self.rbr_dense else \
                  branch.get_layer('bn_%s_%s'%(self.idx, 'rbr_1x1')).epsilon
        else:
            assert isinstance(branch, tf.keras.layers.BatchNormalization)
            if not hasattr(self, 'id_tensor'):
                input_dim = self.in_channels // self.groups
                kernel_value = np.zeros(
                    (3, 3, self.in_channels, input_dim), dtype=np.float32
                )
                for i in range(self.in_channels):
                    kernel_value[1, 1, i % input_dim, i] = 1
                self.id_tensor = tf.convert_to_tensor(kernel_value, tf.float32)
            kernel = self.id_tensor
            running_mean = branch.moving_mean
            running_var = branch.moving_variance
            gamma = branch.gamma
            beta = branch.beta
            eps = branch.epsilon
        std = tf.sqrt(running_var + eps)
        t = gamma / std
        return kernel * t, beta - running_mean * gamma / std
    
    def repvgg_convert(self):
        kernel, bias = self.get_equivalent_kernel_bias()
        return kernel, bias

def RepBlock(x, in_channels, out_channels, n=1, block='RepVGGBlock', deploy=False):
    block = eval(block)
    x = block(in_channels, out_channels, deploy=deploy)(x)
    if n > 1:
        for _ in range(n - 1):
            x = block(out_channels, out_channels, deploy=deploy)(x)
    return x


class SimConv(tf.keras.layers.Layer):
    def __init__(self, in_channels, out_channels, kernel_size, stride, groups=1, bias=False):
        super(SimConv, self).__init__()
        padding = 'same' if kernel_size // 2 == 1 else 'valid'
        self.conv = tf.keras.layers.Conv2D(out_channels, kernel_size, stride, padding, groups=groups, use_bias=bias)
        self.bn = tf.keras.layers.BatchNormalization()
        self.act = tf.keras.layers.ReLU()
    
    def call(self, x):
        return self.act(self.bn(self.conv(x)))
    
    def call_fuse(self, x):
        return self.act(self.conv)

class SimSPPF(tf.keras.layers.Layer):
    def __init__(self, in_channels, out_channels, kernel_size=5):
        super(SimSPPF, self).__init__()
        c_ = in_channels // 2
        self.cv1 = SimConv(in_channels, c_, 1, 1)
        self.cv2 = SimConv(c_ * 4, out_channels, 1, 1)
        padding = 'same' if kernel_size // 2 >= 1 else 'valid'
        self.m = tf.keras.layers.MaxPool2D(pool_size=kernel_size, strides=1, padding=padding)
    
    def call(self, x):
        x = self.cv1(x)
        with warnings.catch_warnings():
            warnings.simplefilter('ignore')
            y1 = self.m(x)
            y2 = self.m(y1)
            return self.cv2(tf.concat([x, y1, y2, self.m(y2)], axis=-1))


class Transpose(tf.keras.layers.Layer):
    def __init__(self, in_channles, out_channels, kernel_size=2, stride=2):
        super(Transpose, self).__init__()
        self.upsample_transpose = tf.keras.layers.Conv2DTranspose(out_channels, kernel_size, stride, use_bias=True)

    def call(self, x):
        return self.upsample_transpose(x)


class Conv(tf.keras.layers.Layer):
    def __init__(self, in_channels, out_channels, kernel_size, stride, groups=1, bias=False, **kwargs):
        super(Conv, self).__init__(**kwargs)
        padding = 'same' if kernel_size // 2 >= 1 else 'valid'
        self.conv = tf.keras.layers.Conv2D(out_channels, kernel_size, stride, padding, groups=groups, use_bias=bias)
        self.bn = tf.keras.layers.BatchNormalization()
        self.act = SiLU

    def call(self, x):
        return self.act(self.bn(self.conv(x)))

    def call_fuse(self, x):
        return self.act(self.conv(x))