"""
File utility functions for Migration Tool.
"""

import shutil
from pathlib import Path
from typing import List, Optional, Generator
import os

import sys
from pathlib import Path

# Add parent directory to path for imports when running standalone
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from utils.logging_config import get_logger

logger = get_logger('utils.file_utils')


def ensure_directory(path: Path) -> Path:
    """
    Ensure a directory exists, creating it if necessary.
    
    Args:
        path: Directory path
        
    Returns:
        The path (for chaining)
    """
    path.mkdir(parents=True, exist_ok=True)
    return path


def copy_file(source: Path, target: Path, overwrite: bool = False) -> bool:
    """
    Copy a file from source to target.
    
    Args:
        source: Source file path
        target: Target file path
        overwrite: Whether to overwrite existing files
        
    Returns:
        True if successful, False otherwise
    """
    if not source.exists():
        logger.error(f"Source file does not exist: {source}")
        return False
    
    if target.exists() and not overwrite:
        logger.warning(f"Target file already exists: {target}")
        return False
    
    try:
        ensure_directory(target.parent)
        shutil.copy2(source, target)
        logger.debug(f"Copied: {source} -> {target}")
        return True
    except Exception as e:
        logger.error(f"Failed to copy {source} to {target}: {e}")
        return False


def move_file(source: Path, target: Path, overwrite: bool = False) -> bool:
    """
    Move a file from source to target.
    
    Args:
        source: Source file path
        target: Target file path
        overwrite: Whether to overwrite existing files
        
    Returns:
        True if successful, False otherwise
    """
    if not source.exists():
        logger.error(f"Source file does not exist: {source}")
        return False
    
    if target.exists() and not overwrite:
        logger.warning(f"Target file already exists: {target}")
        return False
    
    try:
        ensure_directory(target.parent)
        shutil.move(str(source), str(target))
        logger.debug(f"Moved: {source} -> {target}")
        return True
    except Exception as e:
        logger.error(f"Failed to move {source} to {target}: {e}")
        return False


def find_files(
    directory: Path,
    pattern: str = "*.cs",
    recursive: bool = True,
    exclude_dirs: Optional[List[str]] = None
) -> Generator[Path, None, None]:
    """
    Find files matching a pattern in a directory.
    
    Args:
        directory: Directory to search
        pattern: Glob pattern (default: *.cs)
        recursive: Whether to search recursively
        exclude_dirs: Directory names to exclude (e.g., ['bin', 'obj'])
        
    Yields:
        Matching file paths
    """
    if exclude_dirs is None:
        exclude_dirs = ['bin', 'obj', '.git', 'node_modules', '__pycache__']
    
    if not directory.exists():
        logger.warning(f"Directory does not exist: {directory}")
        return
    
    glob_method = directory.rglob if recursive else directory.glob
    
    for path in glob_method(pattern):
        # Check if any parent directory is in exclude list
        if any(excluded in path.parts for excluded in exclude_dirs):
            continue
        yield path


def read_file_content(path: Path, encoding: str = 'utf-8') -> Optional[str]:
    """
    Read file content safely.
    
    Args:
        path: File path
        encoding: File encoding
        
    Returns:
        File content or None if error
    """
    try:
        return path.read_text(encoding=encoding)
    except UnicodeDecodeError:
        # Try with different encoding
        try:
            return path.read_text(encoding='utf-8-sig')
        except Exception as e:
            logger.error(f"Failed to read {path}: {e}")
            return None
    except Exception as e:
        logger.error(f"Failed to read {path}: {e}")
        return None


def write_file_content(path: Path, content: str, encoding: str = 'utf-8') -> bool:
    """
    Write content to a file safely.
    
    Args:
        path: File path
        content: Content to write
        encoding: File encoding
        
    Returns:
        True if successful, False otherwise
    """
    try:
        ensure_directory(path.parent)
        path.write_text(content, encoding=encoding)
        logger.debug(f"Wrote: {path}")
        return True
    except Exception as e:
        logger.error(f"Failed to write {path}: {e}")
        return False


def get_relative_path(path: Path, base: Path) -> Path:
    """
    Get relative path from base directory.
    
    Args:
        path: Full path
        base: Base directory
        
    Returns:
        Relative path
    """
    try:
        return path.relative_to(base)
    except ValueError:
        # Path is not relative to base
        return path
