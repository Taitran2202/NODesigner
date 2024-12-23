from config import *

class Optimizer(object):
    def __init__(self, optimizer_method='adam'):
        self.optimizer_method = optimizer_method
    
    def __call__(self):
        if self.optimizer_method == 'adam':
            return tf.keras.optimizers.Adam()
        elif self.optimizer_method == 'rmsprop':
            return tf.keras.optimizers.RMSprop()
        elif self.optimizer_method == 'sgd':
            return tf.keras.optimizers.SGD()
        else:
            raise ValueError('Unsupported optimizer {}'.format(self.optimizer_method))

class LrScheduler(object):
    def __init__(self, total_steps, scheduler_method='cosine'):
        if scheduler_method == 'step':
            self.scheduler = Step(total_steps)
        elif scheduler_method == 'cosine':
            self.scheduler = Cosine(total_steps)
        self.step_count = 0
        self.total_steps = total_steps
    
    def step(self):
        self.step_count += 1
        lr = self.scheduler(self.step_count)
        return lr
    
    def plot(self):
        lr = []
        for i in range(self.total_steps):
            lr.append(self.step())
        
        plt.plot(range(self.total_steps), lr)
        plt.show()

class Step(tf.keras.optimizers.schedules.LearningRateSchedule):
    def __init__(self, total_steps):
        super(Step, self).__init__()
        self.total_steps = total_steps
    
    def __call__(self, global_step):
        warmup_lr = cfg.YOLO.TRAIN.WARMUP_LEARNING_RATE
        warmup_steps = cfg.YOLO.TRAIN.WARMUP_STEPS
        init_lr = cfg.YOLO.TRAIN.LR_INIT
        lr_levels = cfg.YOLO.TRAIN.LEARNING_RATE_LEVELS
        lr_steps = cfg.YOLO.TRAIN.LEARNING_RATE_STEPS

        assert warmup_steps < self.total_steps, "warmup {}, total {}".format(warmup_steps, self.total_steps)

        linear_warmup = warmup_lr + tf.cast(global_step, tf.float32) / warmup_steps * (init_lr - warmup_lr)
        learning_rate = tf.where(global_step < warmup_steps, linear_warmup, init_lr)

        for next_learning_rate, start_step in zip(lr_levels, lr_steps):
            learning_rate = tf.where(global_step >= start_step, next_learning_rate, learning_rate)
        
        return learning_rate

class Cosine(tf.keras.optimizers.schedules.LearningRateSchedule):
    def __init__(self, total_steps):
        super(Cosine, self).__init__()
        self.total_steps = total_steps
    
    def __call__(self, global_step):
        init_lr = cfg.YOLO.TRAIN.LR_INIT
        warmup_lr = cfg.YOLO.TRAIN.WARMUP_LEARNING_RATE
        warmup_steps = cfg.YOLO.TRAIN.WARMUP_STEPS
        assert warmup_steps < self.total_steps, "warmup {}, total {}".format(warmup_steps, self.total_steps)

        linear_warmup = warmup_lr + tf.cast(global_step, tf.float32) / warmup_steps * (init_lr - warmup_lr)
        cosine_learning_rate = init_lr * (tf.cos(np.pi * (global_step - warmup_steps) / (self.total_steps - warmup_steps)) + 1.0) / 2.0
        learning_rate = tf.where(global_step < warmup_steps, linear_warmup, cosine_learning_rate)
        
        return learning_rate