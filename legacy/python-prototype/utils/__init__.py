"""
Utility functions for Migration Tool.

Utilities:
- logging_config: Logging setup
- file_utils: File operations
"""

import sys
from pathlib import Path

# Add parent directory to path for imports
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from utils.logging_config import setup_logging, get_logger

__all__ = [
    'setup_logging',
    'get_logger',
]
