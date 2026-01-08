"""
Data models for Migration Tool.

Models:
- Solution: Represents a .sln file
- Project: Represents a .csproj project
- FileInfo: Information about a C# file
- ClassInfo: Information about a class/interface
- MigrationPlan: Plan for file migration
- MigrationStep: Single step in migration
"""

import sys
from pathlib import Path

# Add parent directory to path for imports
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from models.solution import Solution, Project, ProjectReference
from models.file_info import FileInfo, ClassInfo, TestInfo

__all__ = [
    'Solution',
    'Project',
    'ProjectReference',
    'FileInfo',
    'ClassInfo',
    'TestInfo',
]
