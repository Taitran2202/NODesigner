from numba import cuda

def check_gpu():
    cc_cores_per_SM_dict = {
        (2,0) : 32,
        (2,1) : 48,
        (3,0) : 192,
        (3,5) : 192,
        (3,7) : 192,
        (5,0) : 128,
        (5,2) : 128,
        (6,0) : 64,
        (6,1) : 128,
        (7,0) : 64,
        (7,5) : 64,
        (8,0) : 64,
        (8,6) : 128
        }
    device = cuda.get_current_device()
    my_sms = getattr(device, 'MULTIPROCESSOR_COUNT')
    my_cc = device.compute_capability
    cores_per_sm = cc_cores_per_SM_dict.get(my_cc)
    total_cores = cores_per_sm*my_sms
    print("GPU compute capability: " , my_cc)
    print("GPU total number of SMs: " , my_sms)
    print("total cores: " , total_cores)
    return my_cc[0]