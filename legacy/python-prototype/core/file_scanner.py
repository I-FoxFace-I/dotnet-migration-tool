"""
Scanner for C# source files.

Extracts information about namespaces, classes, interfaces, and tests.
"""

import re
from pathlib import Path
from typing import List, Optional, Set

import sys
from pathlib import Path

# Add parent directory to path for imports when running standalone
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from models.file_info import FileInfo, ClassInfo, TestInfo, MemberType, TestFramework
from utils.logging_config import get_logger
from utils.file_utils import read_file_content, find_files

logger = get_logger('core.file_scanner')


class FileScanner:
    """
    Scanner for C# source files.
    
    Extracts:
    - Namespace declarations
    - Class/interface/struct/enum definitions
    - Test methods ([Fact], [Theory], [Test], etc.)
    - Using directives
    
    Usage:
        scanner = FileScanner()
        file_info = scanner.scan_file("path/to/file.cs")
    """
    
    # Regex patterns
    NAMESPACE_PATTERN = re.compile(
        r'^\s*namespace\s+([\w.]+)\s*[;{]',
        re.MULTILINE
    )
    
    # File-scoped namespace (C# 10+): namespace Foo.Bar;
    FILE_SCOPED_NS_PATTERN = re.compile(
        r'^\s*namespace\s+([\w.]+)\s*;',
        re.MULTILINE
    )
    
    USING_PATTERN = re.compile(
        r'^\s*using\s+(?:static\s+)?([\w.]+)\s*;',
        re.MULTILINE
    )
    
    # Class/interface/struct/enum/record pattern
    # Matches: public class Foo : Bar, IFoo
    TYPE_PATTERN = re.compile(
        r'^\s*(?:(?:public|private|protected|internal)\s+)?'
        r'(?:(?:abstract|sealed|static|partial)\s+)*'
        r'(class|interface|struct|enum|record)\s+'
        r'(\w+)'
        r'(?:<[^>]+>)?'  # Generic parameters
        r'(?:\s*:\s*([^{]+))?',  # Inheritance
        re.MULTILINE
    )
    
    # Test method patterns
    XUNIT_FACT_PATTERN = re.compile(r'\[Fact(?:\([^)]*\))?\]')
    XUNIT_THEORY_PATTERN = re.compile(r'\[Theory(?:\([^)]*\))?\]')
    NUNIT_TEST_PATTERN = re.compile(r'\[Test(?:\([^)]*\))?\]')
    NUNIT_TESTCASE_PATTERN = re.compile(r'\[TestCase(?:\([^)]*\))?\]')
    MSTEST_PATTERN = re.compile(r'\[TestMethod(?:\([^)]*\))?\]')
    
    # Method pattern (simplified)
    METHOD_PATTERN = re.compile(
        r'^\s*(?:(?:public|private|protected|internal)\s+)?'
        r'(?:(?:async|virtual|override|static|abstract)\s+)*'
        r'(?:Task\s+|void\s+|[\w<>[\],\s]+\s+)'
        r'(\w+)\s*\(',
        re.MULTILINE
    )
    
    # Trait pattern for xUnit
    TRAIT_PATTERN = re.compile(r'\[Trait\s*\(\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\)\]')
    
    def __init__(self):
        """Initialize the file scanner."""
        pass
    
    def scan_file(self, file_path: str | Path) -> FileInfo:
        """
        Scan a C# file and extract information.
        
        Args:
            file_path: Path to the .cs file
            
        Returns:
            FileInfo object with extracted information
        """
        file_path = Path(file_path)
        
        content = read_file_content(file_path)
        if content is None:
            return FileInfo(path=file_path)
        
        # Count lines
        line_count = content.count('\n') + 1
        
        # Parse namespace
        namespace = self._extract_namespace(content)
        
        # Parse using directives
        usings = self._extract_usings(content)
        
        # Parse classes
        classes = self._extract_classes(content, namespace)
        
        return FileInfo(
            path=file_path,
            namespace=namespace,
            classes=classes,
            using_directives=usings,
            line_count=line_count
        )
    
    def scan_directory(
        self, 
        directory: str | Path, 
        recursive: bool = True
    ) -> List[FileInfo]:
        """
        Scan all C# files in a directory.
        
        Args:
            directory: Directory path
            recursive: Whether to scan recursively
            
        Returns:
            List of FileInfo objects
        """
        directory = Path(directory)
        files = []
        
        for file_path in find_files(directory, "*.cs", recursive):
            try:
                file_info = self.scan_file(file_path)
                files.append(file_info)
            except Exception as e:
                logger.warning(f"Failed to scan {file_path}: {e}")
        
        logger.info(f"Scanned {len(files)} files in {directory}")
        return files
    
    def _extract_namespace(self, content: str) -> Optional[str]:
        """
        Extract the namespace from file content.
        
        Args:
            content: File content
            
        Returns:
            Namespace string or None
        """
        # Try file-scoped namespace first (C# 10+)
        match = self.FILE_SCOPED_NS_PATTERN.search(content)
        if match:
            return match.group(1)
        
        # Try block-scoped namespace
        match = self.NAMESPACE_PATTERN.search(content)
        if match:
            return match.group(1)
        
        return None
    
    def _extract_usings(self, content: str) -> Set[str]:
        """
        Extract using directives from file content.
        
        Args:
            content: File content
            
        Returns:
            Set of namespace strings
        """
        usings = set()
        for match in self.USING_PATTERN.finditer(content):
            usings.add(match.group(1))
        return usings
    
    def _extract_classes(self, content: str, namespace: Optional[str]) -> List[ClassInfo]:
        """
        Extract class/interface definitions from file content.
        
        Args:
            content: File content
            namespace: File namespace
            
        Returns:
            List of ClassInfo objects
        """
        classes = []
        lines = content.split('\n')
        
        for match in self.TYPE_PATTERN.finditer(content):
            member_type_str, name, inheritance = match.groups()
            
            # Determine member type
            member_type = MemberType(member_type_str.lower())
            
            # Parse inheritance
            base_class = None
            interfaces = []
            if inheritance:
                parts = [p.strip() for p in inheritance.split(',')]
                for part in parts:
                    # Remove generic parameters for simple comparison
                    simple_name = re.sub(r'<[^>]+>', '', part).strip()
                    if simple_name.startswith('I') and simple_name[1:2].isupper():
                        interfaces.append(part)
                    elif base_class is None and member_type == MemberType.CLASS:
                        base_class = part
            
            # Find line number
            line_number = content[:match.start()].count('\n') + 1
            
            # Check modifiers
            match_text = match.group(0)
            is_public = 'public' in match_text or 'public' not in match_text  # Default is internal
            is_abstract = 'abstract' in match_text
            is_static = 'static' in match_text
            is_partial = 'partial' in match_text
            is_sealed = 'sealed' in match_text
            
            # Extract tests for this class
            tests = self._extract_tests_for_class(content, name, line_number)
            
            class_info = ClassInfo(
                name=name,
                namespace=namespace,
                member_type=member_type,
                base_class=base_class,
                interfaces=interfaces,
                is_public='public' in match_text,
                is_abstract=is_abstract,
                is_static=is_static,
                is_partial=is_partial,
                is_sealed=is_sealed,
                line_number=line_number,
                tests=tests
            )
            
            classes.append(class_info)
        
        return classes
    
    def _extract_tests_for_class(
        self, 
        content: str, 
        class_name: str, 
        class_line: int
    ) -> List[TestInfo]:
        """
        Extract test methods for a class.
        
        Args:
            content: File content
            class_name: Name of the class
            class_line: Line number where class starts
            
        Returns:
            List of TestInfo objects
        """
        tests = []
        lines = content.split('\n')
        
        # Find the class body (simplified - looks for methods after class declaration)
        in_class = False
        brace_count = 0
        current_attributes = []
        
        for i, line in enumerate(lines):
            line_num = i + 1
            
            # Start tracking after class line
            if line_num == class_line:
                in_class = True
                continue
            
            if not in_class:
                continue
            
            # Track braces to know when we exit the class
            brace_count += line.count('{') - line.count('}')
            if brace_count < 0:
                break
            
            # Collect test attributes
            if self.XUNIT_FACT_PATTERN.search(line):
                current_attributes.append(('xunit', 'fact'))
            if self.XUNIT_THEORY_PATTERN.search(line):
                current_attributes.append(('xunit', 'theory'))
            if self.NUNIT_TEST_PATTERN.search(line):
                current_attributes.append(('nunit', 'test'))
            if self.NUNIT_TESTCASE_PATTERN.search(line):
                current_attributes.append(('nunit', 'testcase'))
            if self.MSTEST_PATTERN.search(line):
                current_attributes.append(('mstest', 'test'))
            
            # Collect traits
            traits = []
            for trait_match in self.TRAIT_PATTERN.finditer(line):
                traits.append(f"{trait_match.group(1)}:{trait_match.group(2)}")
            
            # Check for method definition
            method_match = self.METHOD_PATTERN.search(line)
            if method_match and current_attributes:
                method_name = method_match.group(1)
                
                # Determine framework and type
                framework = TestFramework.UNKNOWN
                is_theory = False
                
                for fw, attr_type in current_attributes:
                    if fw == 'xunit':
                        framework = TestFramework.XUNIT
                        is_theory = attr_type == 'theory'
                    elif fw == 'nunit':
                        framework = TestFramework.NUNIT
                        is_theory = attr_type == 'testcase'
                    elif fw == 'mstest':
                        framework = TestFramework.MSTEST
                
                test_info = TestInfo(
                    name=method_name,
                    class_name=class_name,
                    framework=framework,
                    is_theory=is_theory,
                    traits=traits,
                    line_number=line_num
                )
                tests.append(test_info)
                
                # Reset for next method
                current_attributes = []
        
        return tests


def scan_file(file_path: str | Path) -> FileInfo:
    """
    Convenience function to scan a single file.
    
    Args:
        file_path: Path to the .cs file
        
    Returns:
        FileInfo object
    """
    scanner = FileScanner()
    return scanner.scan_file(file_path)


def scan_directory(directory: str | Path, recursive: bool = True) -> List[FileInfo]:
    """
    Convenience function to scan a directory.
    
    Args:
        directory: Directory path
        recursive: Whether to scan recursively
        
    Returns:
        List of FileInfo objects
    """
    scanner = FileScanner()
    return scanner.scan_directory(directory, recursive)
