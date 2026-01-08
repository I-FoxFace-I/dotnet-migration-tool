"""
Logging configuration for Migration Tool.

Provides file and console logging with rotation.
"""

import logging
import os
from pathlib import Path
from datetime import datetime
from logging.handlers import RotatingFileHandler

# Log directory
LOG_DIR = Path(__file__).parent.parent / "logs"
LOG_DIR.mkdir(exist_ok=True)

# Log file path with timestamp
LOG_FILE = LOG_DIR / f"migration_tool_{datetime.now().strftime('%Y%m%d')}.log"

# Format
LOG_FORMAT = "%(asctime)s - %(name)s - %(levelname)s - %(message)s"
DATE_FORMAT = "%Y-%m-%d %H:%M:%S"


def setup_logging(level: int = logging.DEBUG) -> logging.Logger:
    """
    Setup logging with both file and console handlers.
    
    Args:
        level: Logging level (default: DEBUG)
        
    Returns:
        Root logger instance
    """
    # Get root logger for migration_tool
    logger = logging.getLogger("migration_tool")
    logger.setLevel(level)
    
    # Remove existing handlers to avoid duplicates
    logger.handlers.clear()
    
    # File handler with rotation (10MB max, keep 5 backups)
    file_handler = RotatingFileHandler(
        LOG_FILE,
        maxBytes=10 * 1024 * 1024,  # 10MB
        backupCount=5,
        encoding="utf-8"
    )
    file_handler.setLevel(logging.DEBUG)
    file_handler.setFormatter(logging.Formatter(LOG_FORMAT, DATE_FORMAT))
    logger.addHandler(file_handler)
    
    # Console handler (INFO and above)
    console_handler = logging.StreamHandler()
    console_handler.setLevel(logging.INFO)
    console_handler.setFormatter(logging.Formatter(LOG_FORMAT, DATE_FORMAT))
    logger.addHandler(console_handler)
    
    logger.info(f"Logging initialized. Log file: {LOG_FILE}")
    
    return logger


def get_logger(name: str) -> logging.Logger:
    """
    Get a logger instance for a specific module.
    
    Args:
        name: Logger name (e.g., 'core.migration_engine')
        
    Returns:
        Logger instance
    """
    return logging.getLogger(f"migration_tool.{name}")


# Initialize logging on module import
_root_logger = setup_logging()


def get_log_file_path() -> Path:
    """Get the current log file path."""
    return LOG_FILE


def get_recent_logs(lines: int = 100) -> str:
    """
    Get recent log entries.
    
    Args:
        lines: Number of lines to return
        
    Returns:
        Recent log content as string
    """
    if not LOG_FILE.exists():
        return "No log file found."
    
    with open(LOG_FILE, "r", encoding="utf-8") as f:
        all_lines = f.readlines()
        return "".join(all_lines[-lines:])


def clear_old_logs(days: int = 7):
    """
    Clear log files older than specified days.
    
    Args:
        days: Number of days to keep logs
    """
    import time
    
    cutoff = time.time() - (days * 24 * 60 * 60)
    
    for log_file in LOG_DIR.glob("migration_tool_*.log*"):
        if log_file.stat().st_mtime < cutoff:
            log_file.unlink()
            _root_logger.info(f"Deleted old log file: {log_file}")
