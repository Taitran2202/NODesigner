"""Load PyTorch Lightning Loggers."""

# Copyright (C) 2020 Intel Corporation
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
# http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions
# and limitations under the License.

import logging
import os
import warnings
from typing import Iterable, List, Union

from omegaconf.dictconfig import DictConfig
from omegaconf.listconfig import ListConfig
from pytorch_lightning.loggers import CSVLogger, LightningLoggerBase

from .tensorboard import AnomalibTensorBoardLogger
from .wandb import AnomalibWandbLogger

__all__ = [
    "AnomalibTensorBoardLogger",
    "AnomalibWandbLogger",
    "configure_logger",
    "get_experiment_logger",
]
AVAILABLE_LOGGERS = ["tensorboard", "wandb", "csv"]


class UnknownLogger(Exception):
    """This is raised when the logger option in `config.yaml` file is set incorrectly."""


def configure_logger(level: Union[int, str] = logging.INFO):
    """Get console logger by name.

    Args:
        level (Union[int, str], optional): Logger Level. Defaults to logging.INFO.

    Returns:
        Logger: The expected logger.
    """

    if isinstance(level, str):
        level = logging.getLevelName(level)

    format_string = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
    logging.basicConfig(format=format_string, level=level)

    # Set Pytorch Lightning logs to have a the consistent formatting with anomalib.
    for handler in logging.getLogger("pytorch_lightning").handlers:
        handler.setFormatter(logging.Formatter(format_string))
        handler.setLevel(level)


def get_experiment_logger(
    config: Union[DictConfig, ListConfig]
) -> Union[LightningLoggerBase, Iterable[LightningLoggerBase], bool]:
    """Return a logger based on the choice of logger in the config file.

    Args:
        config (DictConfig): config.yaml file for the corresponding anomalib model.

    Raises:
        ValueError: for any logger types apart from false and tensorboard

    Returns:
        Union[LightningLoggerBase, Iterable[LightningLoggerBase], bool]: Logger
    """

    # TODO remove when logger is deprecated from project
    if "logger" in config.project.keys():
        warnings.warn(
            "'logger' key will be deprecated from 'project' section of the config file."
            " Please use the logging section in config file.",
            DeprecationWarning,
        )
        if "logging" not in config:
            config.logging = {"logger": config.project.logger, "log_graph": False}
        else:
            config.logging.logger = config.project.logger

    if config.logging.logger in [None, False]:
        return False

    logger_list: List[LightningLoggerBase] = []
    if isinstance(config.logging.logger, str):
        config.logging.logger = [config.logging.logger]

    for logger in config.logging.logger:
        if logger == "tensorboard":
            logger_list.append(
                AnomalibTensorBoardLogger(
                    name="Tensorboard Logs",
                    save_dir=os.path.join(config.project.path, "logs"),
                    log_graph=config.logging.log_graph,
                )
            )
        elif logger == "wandb":
            wandb_logdir = os.path.join(config.project.path, "logs")
            os.makedirs(wandb_logdir, exist_ok=True)
            name = (
                config.model.name
                if "category" not in config.dataset.keys()
                else f"{config.dataset.category} {config.model.name}"
            )
            logger_list.append(
                AnomalibWandbLogger(
                    project=config.dataset.name,
                    name=name,
                    save_dir=wandb_logdir,
                )
            )
        elif logger == "csv":
            logger_list.append(CSVLogger(save_dir=os.path.join(config.project.path, "logs")))
        else:
            raise UnknownLogger(
                f"Unknown logger type: {config.logging.logger}. "
                f"Available loggers are: {AVAILABLE_LOGGERS}.\n"
                f"To enable the logger, set `project.logger` to `true` or use one of available loggers in config.yaml\n"
                f"To disable the logger, set `project.logger` to `false`."
            )

    return logger_list
