"""
Data models for .NET Solution and Project structures.
"""

from dataclasses import dataclass, field
from pathlib import Path
from typing import List, Optional, Dict
from enum import Enum


class ProjectType(Enum):
    """Types of .NET projects."""
    CLASS_LIBRARY = "classlib"
    CONSOLE = "console"
    WPF = "wpf"
    TEST = "test"
    UNKNOWN = "unknown"


@dataclass
class ProjectReference:
    """Reference to another project."""
    name: str
    path: Path
    
    def __hash__(self):
        return hash(self.path)
    
    def __eq__(self, other):
        if not isinstance(other, ProjectReference):
            return False
        return self.path == other.path


@dataclass
class PackageReference:
    """Reference to a NuGet package."""
    name: str
    version: Optional[str] = None
    
    def __hash__(self):
        return hash(self.name)
    
    def __eq__(self, other):
        if not isinstance(other, PackageReference):
            return False
        return self.name == other.name


@dataclass
class Project:
    """Represents a .NET project (.csproj)."""
    name: str
    path: Path
    guid: Optional[str] = None
    target_framework: Optional[str] = None
    project_type: ProjectType = ProjectType.UNKNOWN
    
    # References
    project_references: List[ProjectReference] = field(default_factory=list)
    package_references: List[PackageReference] = field(default_factory=list)
    
    # Files (populated by FileScanner)
    source_files: List[Path] = field(default_factory=list)
    
    # Metadata
    root_namespace: Optional[str] = None
    output_type: Optional[str] = None
    
    @property
    def directory(self) -> Path:
        """Get the project directory."""
        return self.path.parent
    
    @property
    def is_test_project(self) -> bool:
        """Check if this is a test project."""
        if self.project_type == ProjectType.TEST:
            return True
        # Check by package references
        test_packages = {'xunit', 'nunit', 'mstest', 'Microsoft.NET.Test.Sdk'}
        return any(pkg.name.lower() in [t.lower() for t in test_packages] 
                   for pkg in self.package_references)
    
    @property
    def file_count(self) -> int:
        """Get number of source files."""
        return len(self.source_files)
    
    def __hash__(self):
        return hash(self.path)
    
    def __eq__(self, other):
        if not isinstance(other, Project):
            return False
        return self.path == other.path


@dataclass
class Solution:
    """Represents a .NET Solution (.sln)."""
    name: str
    path: Path
    projects: List[Project] = field(default_factory=list)
    
    # Solution folders (virtual folders in .sln)
    folders: Dict[str, List[str]] = field(default_factory=dict)
    
    @property
    def directory(self) -> Path:
        """Get the solution directory."""
        return self.path.parent
    
    @property
    def project_count(self) -> int:
        """Get number of projects."""
        return len(self.projects)
    
    @property
    def test_projects(self) -> List[Project]:
        """Get all test projects."""
        return [p for p in self.projects if p.is_test_project]
    
    @property
    def source_projects(self) -> List[Project]:
        """Get all non-test projects."""
        return [p for p in self.projects if not p.is_test_project]
    
    def get_project_by_name(self, name: str) -> Optional[Project]:
        """Find a project by name."""
        for project in self.projects:
            if project.name == name:
                return project
        return None
    
    def get_project_by_path(self, path: Path) -> Optional[Project]:
        """Find a project by path."""
        for project in self.projects:
            if project.path == path or project.path.resolve() == path.resolve():
                return project
        return None
    
    def __repr__(self):
        return f"Solution(name='{self.name}', projects={self.project_count})"
