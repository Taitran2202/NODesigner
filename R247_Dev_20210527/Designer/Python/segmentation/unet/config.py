from omegaconf import DictConfig, ListConfig, OmegaConf
from pathlib import Path
def get_config(model_name):
    config_path = Path(f"configs/{model_name}.yaml")
    if not config_path.exists():
        raise FileNotFoundError(f"Config file {config_path} not found")
    config = OmegaConf.load(config_path)
    return config
