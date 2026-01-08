"""
Core business logic for Migration Tool.

Modules:
- solution_parser: Parse .sln files
- project_parser: Parse .csproj files  
- file_scanner: Scan C# files for classes, tests, etc.
- namespace_fixer: Fix namespaces after migration
- migration_engine: Orchestrate migration operations
- git_manager: Git operations (branch, commit, etc.)
"""

import sys
from pathlib import Path

# Add parent directory to path for imports
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from core.solution_parser import SolutionParser
from core.project_parser import ProjectParser
from core.file_scanner import FileScanner

__all__ = [
    'SolutionParser',
    'ProjectParser', 
    'FileScanner',
]
