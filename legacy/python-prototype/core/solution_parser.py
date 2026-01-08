"""
Parser for .NET Solution (.sln) files.

.sln file format reference:
https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file
"""

import re
from pathlib import Path
from typing import List, Optional, Tuple

import sys
from pathlib import Path

# Add parent directory to path for imports when running standalone
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from models.solution import Solution, Project
from utils.logging_config import get_logger

logger = get_logger('core.solution_parser')


class SolutionParser:
    """
    Parser for .NET Solution files (.sln).
    
    Usage:
        parser = SolutionParser()
        solution = parser.parse("path/to/solution.sln")
    """
    
    # Regex patterns for .sln file parsing
    # Project line format: Project("{GUID}") = "Name", "Path", "{ProjectGUID}"
    PROJECT_PATTERN = re.compile(
        r'Project\("\{([A-F0-9-]+)\}"\)\s*=\s*"([^"]+)"\s*,\s*"([^"]+)"\s*,\s*"\{([A-F0-9-]+)\}"',
        re.IGNORECASE
    )
    
    # Solution folder GUID
    SOLUTION_FOLDER_GUID = "2150E333-8FDC-42A3-9474-1A3956D46DE8"
    
    # Known project type GUIDs
    PROJECT_TYPE_GUIDS = {
        "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC": "csharp",      # C#
        "F184B08F-C81C-45F6-A57F-5ABD9991F28F": "vb",          # VB.NET
        "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942": "cpp",         # C++
        "9A19103F-16F7-4668-BE54-9A1E7A4F7556": "csharp_sdk",  # C# SDK-style
        "2150E333-8FDC-42A3-9474-1A3956D46DE8": "folder",      # Solution Folder
    }
    
    def __init__(self):
        """Initialize the solution parser."""
        self._solution_dir: Optional[Path] = None
    
    def parse(self, sln_path: str | Path) -> Solution:
        """
        Parse a .sln file and return a Solution object.
        
        Args:
            sln_path: Path to the .sln file
            
        Returns:
            Solution object with parsed projects
            
        Raises:
            FileNotFoundError: If the .sln file doesn't exist
            ValueError: If the file is not a valid .sln file
        """
        sln_path = Path(sln_path)
        
        if not sln_path.exists():
            raise FileNotFoundError(f"Solution file not found: {sln_path}")
        
        if sln_path.suffix.lower() != '.sln':
            raise ValueError(f"Not a solution file: {sln_path}")
        
        self._solution_dir = sln_path.parent
        
        logger.info(f"Parsing solution: {sln_path}")
        
        content = sln_path.read_text(encoding='utf-8-sig')  # Handle BOM
        
        # Parse projects
        projects = self._parse_projects(content)
        
        solution = Solution(
            name=sln_path.stem,
            path=sln_path,
            projects=projects
        )
        
        logger.info(f"Parsed {len(projects)} projects from {sln_path.name}")
        
        return solution
    
    def _parse_projects(self, content: str) -> List[Project]:
        """
        Parse project entries from solution content.
        
        Args:
            content: Solution file content
            
        Returns:
            List of Project objects
        """
        projects = []
        
        for match in self.PROJECT_PATTERN.finditer(content):
            type_guid, name, relative_path, project_guid = match.groups()
            
            # Skip solution folders
            if type_guid.upper() == self.SOLUTION_FOLDER_GUID:
                logger.debug(f"Skipping solution folder: {name}")
                continue
            
            # Resolve project path
            project_path = self._resolve_project_path(relative_path)
            
            if project_path is None:
                logger.warning(f"Could not resolve path for project: {name} ({relative_path})")
                continue
            
            project = Project(
                name=name,
                path=project_path,
                guid=project_guid
            )
            
            projects.append(project)
            logger.debug(f"Found project: {name} at {project_path}")
        
        return projects
    
    def _resolve_project_path(self, relative_path: str) -> Optional[Path]:
        """
        Resolve a project path relative to the solution directory.
        
        Args:
            relative_path: Relative path from .sln file
            
        Returns:
            Absolute Path or None if not found
        """
        if self._solution_dir is None:
            return None
        
        # Windows paths in .sln use backslashes, normalize them
        normalized_path = relative_path.replace('\\', '/')
        
        full_path = self._solution_dir / normalized_path
        
        # Check if file exists
        if full_path.exists():
            return full_path.resolve()
        
        # Try with .csproj extension if not specified
        if not full_path.suffix:
            csproj_path = full_path.with_suffix('.csproj')
            if csproj_path.exists():
                return csproj_path.resolve()
        
        return None
    
    def get_project_paths(self, sln_path: str | Path) -> List[Tuple[str, Path]]:
        """
        Get a list of (name, path) tuples for all projects in a solution.
        
        This is a lightweight method that doesn't fully parse projects.
        
        Args:
            sln_path: Path to the .sln file
            
        Returns:
            List of (project_name, project_path) tuples
        """
        solution = self.parse(sln_path)
        return [(p.name, p.path) for p in solution.projects]


def parse_solution(sln_path: str | Path) -> Solution:
    """
    Convenience function to parse a solution file.
    
    Args:
        sln_path: Path to the .sln file
        
    Returns:
        Solution object
    """
    parser = SolutionParser()
    return parser.parse(sln_path)
