"""
Parser for .NET Project (.csproj) files.

.csproj files are XML files with MSBuild format.
"""

import xml.etree.ElementTree as ET
from pathlib import Path
from typing import List, Optional

import sys
from pathlib import Path

# Add parent directory to path for imports when running standalone
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from models.solution import Project, ProjectReference, PackageReference, ProjectType
from utils.logging_config import get_logger

logger = get_logger('core.project_parser')


class ProjectParser:
    """
    Parser for .NET Project files (.csproj).
    
    Usage:
        parser = ProjectParser()
        project = parser.parse("path/to/project.csproj")
    """
    
    # MSBuild XML namespace
    MSBUILD_NS = "{http://schemas.microsoft.com/developer/msbuild/2003}"
    
    # Test package names (lowercase)
    TEST_PACKAGES = {
        'xunit', 'xunit.runner.visualstudio',
        'nunit', 'nunit3testadapter',
        'mstest.testframework', 'mstest.testadapter',
        'microsoft.net.test.sdk'
    }
    
    def __init__(self):
        """Initialize the project parser."""
        self._project_dir: Optional[Path] = None
    
    def parse(self, csproj_path: str | Path) -> Project:
        """
        Parse a .csproj file and return a Project object.
        
        Args:
            csproj_path: Path to the .csproj file
            
        Returns:
            Project object with parsed information
            
        Raises:
            FileNotFoundError: If the .csproj file doesn't exist
            ValueError: If the file is not a valid .csproj file
        """
        csproj_path = Path(csproj_path)
        
        if not csproj_path.exists():
            raise FileNotFoundError(f"Project file not found: {csproj_path}")
        
        if csproj_path.suffix.lower() != '.csproj':
            raise ValueError(f"Not a project file: {csproj_path}")
        
        self._project_dir = csproj_path.parent
        
        logger.debug(f"Parsing project: {csproj_path}")
        
        try:
            tree = ET.parse(csproj_path)
            root = tree.getroot()
        except ET.ParseError as e:
            raise ValueError(f"Invalid XML in {csproj_path}: {e}")
        
        # Detect if SDK-style or legacy format
        is_sdk_style = root.get('Sdk') is not None or self._has_sdk_import(root)
        
        # Parse properties
        target_framework = self._get_property(root, 'TargetFramework') or self._get_property(root, 'TargetFrameworks')
        root_namespace = self._get_property(root, 'RootNamespace')
        output_type = self._get_property(root, 'OutputType')
        
        # Parse references
        project_references = self._parse_project_references(root)
        package_references = self._parse_package_references(root)
        
        # Determine project type
        project_type = self._determine_project_type(output_type, package_references)
        
        project = Project(
            name=csproj_path.stem,
            path=csproj_path,
            target_framework=target_framework,
            root_namespace=root_namespace or csproj_path.stem,
            output_type=output_type,
            project_type=project_type,
            project_references=project_references,
            package_references=package_references
        )
        
        logger.debug(f"Parsed project: {project.name} ({project.project_type.value})")
        
        return project
    
    def _has_sdk_import(self, root: ET.Element) -> bool:
        """Check if project uses SDK import."""
        for elem in root.iter():
            if elem.tag.endswith('Import'):
                sdk = elem.get('Sdk')
                if sdk:
                    return True
        return False
    
    def _get_property(self, root: ET.Element, name: str) -> Optional[str]:
        """
        Get a property value from the project file.
        
        Args:
            root: XML root element
            name: Property name
            
        Returns:
            Property value or None
        """
        # Try SDK-style (no namespace)
        for prop_group in root.iter('PropertyGroup'):
            elem = prop_group.find(name)
            if elem is not None and elem.text:
                return elem.text.strip()
        
        # Try legacy style (with namespace)
        for prop_group in root.iter(f'{self.MSBUILD_NS}PropertyGroup'):
            elem = prop_group.find(f'{self.MSBUILD_NS}{name}')
            if elem is not None and elem.text:
                return elem.text.strip()
        
        return None
    
    def _parse_project_references(self, root: ET.Element) -> List[ProjectReference]:
        """
        Parse ProjectReference elements.
        
        Args:
            root: XML root element
            
        Returns:
            List of ProjectReference objects
        """
        references = []
        
        # Find all ProjectReference elements (SDK-style and legacy)
        for tag in ['ProjectReference', f'{self.MSBUILD_NS}ProjectReference']:
            for elem in root.iter(tag):
                include = elem.get('Include')
                if include:
                    ref_path = self._resolve_reference_path(include)
                    if ref_path:
                        references.append(ProjectReference(
                            name=ref_path.stem,
                            path=ref_path
                        ))
        
        return references
    
    def _parse_package_references(self, root: ET.Element) -> List[PackageReference]:
        """
        Parse PackageReference elements.
        
        Args:
            root: XML root element
            
        Returns:
            List of PackageReference objects
        """
        references = []
        
        # Find all PackageReference elements
        for tag in ['PackageReference', f'{self.MSBUILD_NS}PackageReference']:
            for elem in root.iter(tag):
                name = elem.get('Include')
                version = elem.get('Version')
                
                # Version might be in child element
                if version is None:
                    version_elem = elem.find('Version') or elem.find(f'{self.MSBUILD_NS}Version')
                    if version_elem is not None:
                        version = version_elem.text
                
                if name:
                    references.append(PackageReference(
                        name=name,
                        version=version
                    ))
        
        return references
    
    def _resolve_reference_path(self, relative_path: str) -> Optional[Path]:
        """
        Resolve a reference path relative to the project directory.
        
        Args:
            relative_path: Relative path from .csproj file
            
        Returns:
            Absolute Path or None if not found
        """
        if self._project_dir is None:
            return None
        
        # Normalize path separators
        normalized_path = relative_path.replace('\\', '/')
        
        full_path = (self._project_dir / normalized_path).resolve()
        
        return full_path if full_path.exists() else None
    
    def _determine_project_type(
        self, 
        output_type: Optional[str], 
        packages: List[PackageReference]
    ) -> ProjectType:
        """
        Determine the project type based on properties and packages.
        
        Args:
            output_type: OutputType property value
            packages: List of package references
            
        Returns:
            ProjectType enum value
        """
        # Check for test packages
        package_names = {p.name.lower() for p in packages}
        if package_names & self.TEST_PACKAGES:
            return ProjectType.TEST
        
        # Check output type
        if output_type:
            output_lower = output_type.lower()
            if output_lower == 'exe':
                return ProjectType.CONSOLE
            elif output_lower == 'winexe':
                return ProjectType.WPF
            elif output_lower == 'library':
                return ProjectType.CLASS_LIBRARY
        
        return ProjectType.UNKNOWN
    
    def enrich_project(self, project: Project) -> Project:
        """
        Enrich an existing Project object with parsed data.
        
        Args:
            project: Project object with path set
            
        Returns:
            Enriched Project object
        """
        if not project.path.exists():
            logger.warning(f"Project file not found: {project.path}")
            return project
        
        parsed = self.parse(project.path)
        
        # Copy parsed data to existing project
        project.target_framework = parsed.target_framework
        project.root_namespace = parsed.root_namespace
        project.output_type = parsed.output_type
        project.project_type = parsed.project_type
        project.project_references = parsed.project_references
        project.package_references = parsed.package_references
        
        return project


def parse_project(csproj_path: str | Path) -> Project:
    """
    Convenience function to parse a project file.
    
    Args:
        csproj_path: Path to the .csproj file
        
    Returns:
        Project object
    """
    parser = ProjectParser()
    return parser.parse(csproj_path)
