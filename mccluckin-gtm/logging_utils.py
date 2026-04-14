from __future__ import annotations

import logging
from pathlib import Path

from config import Settings


def configure_logging(settings: Settings) -> None:
    root = logging.getLogger()
    if getattr(root, "_mccluckin_configured", False):
        return

    Path(settings.log_file_path).parent.mkdir(parents=True, exist_ok=True)
    formatter = logging.Formatter("%(asctime)s %(levelname)s %(name)s %(message)s")
    console = logging.StreamHandler()
    console.setFormatter(formatter)
    file_handler = logging.FileHandler(settings.log_file_path, encoding="utf-8")
    file_handler.setFormatter(formatter)

    root.setLevel(logging.INFO)
    root.addHandler(console)
    root.addHandler(file_handler)
    root._mccluckin_configured = True  # type: ignore[attr-defined]

