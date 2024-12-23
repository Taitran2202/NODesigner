from builder_trainer import *
from config import *
from utils import *
from common_modules import *
from backbone import *
from neck import *
from head import *
from model import *
from loss import *
from optimizer import *
from dataset import *

def main(argv):
    get_config_from_json_file(argv[0])
    trainer = build_trainer()
    trainer()

if __name__ == '__main__':
    main(sys.argv[1:])