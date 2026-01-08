"""
Data models for C# file information.
"""

from dataclasses import dataclass, field
from pathlib import Path
from typing import List, Optional, Set
from enum import Enum


class MemberType(Enum):
    """Types of C# members."""
    CLASS = "class"
    INTERFACE = "interface"
    STRUCT = "struct"
    ENUM = "enum"
    RECORD = "record"
    DELEGATE = "delegate"


class TestFramework(Enum):
    """Test frameworks."""
    XUNIT = "xunit"
    NUNIT = "nunit"
    MSTEST = "mstest"
    UNKNOWN = "unknown"


@dataclass
class TestInfo:
    """Information about a test method."""
    name: str
    class_name: str
    framework: TestFramework = TestFramework.UNKNOWN
    is_theory: bool = False  # xUnit [Theory] or NUnit [TestCase]
    traits: List[str] = field(default_factory=list)  # [Trait("Category", "Unit")]
    line_number: int = 0
    
    @property
    def full_name(self) -> str:
        """Get fully qualified test name."""
        return f"{self.class_name}.{self.name}"
    
    def __hash__(self):
        return hash(self.full_name)
    
    def __eq__(self, other):
        if not isinstance(other, TestInfo):
            return False
        return self.full_name == other.full_name


@dataclass
class ClassInfo:
    """Information about a C# class/interface."""
    name: str
    namespace: Optional[str] = None
    member_type: MemberType = MemberType.CLASS
    
    # Inheritance
    base_class: Optional[str] = None
    interfaces: List[str] = field(default_factory=list)
    
    # Modifiers
    is_public: bool = True
    is_abstract: bool = False
    is_static: bool = False
    is_partial: bool = False
    is_sealed: bool = False
    
    # Location
    line_number: int = 0
    
    # For test classes
    tests: List[TestInfo] = field(default_factory=list)
    
    @property
    def full_name(self) -> str:
        """Get fully qualified name."""
        if self.namespace:
            return f"{self.namespace}.{self.name}"
        return self.name
    
    @property
    def is_test_class(self) -> bool:
        """Check if this is a test class."""
        return len(self.tests) > 0
    
    @property
    def test_count(self) -> int:
        """Get number of tests in this class."""
        return len(self.tests)
    
    def __hash__(self):
        return hash(self.full_name)
    
    def __eq__(self, other):
        if not isinstance(other, ClassInfo):
            return False
        return self.full_name == other.full_name


@dataclass
class FileInfo:
    """Information about a C# source file."""
    path: Path
    
    # Content analysis
    namespace: Optional[str] = None
    classes: List[ClassInfo] = field(default_factory=list)
    
    # Dependencies
    using_directives: Set[str] = field(default_factory=set)
    
    # Metadata
    line_count: int = 0
    
    @property
    def name(self) -> str:
        """Get file name."""
        return self.path.name
    
    @property
    def relative_path(self) -> str:
        """Get relative path as string."""
        return str(self.path)
    
    @property
    def class_count(self) -> int:
        """Get number of classes in file."""
        return len(self.classes)
    
    @property
    def test_count(self) -> int:
        """Get total number of tests in file."""
        return sum(c.test_count for c in self.classes)
    
    @property
    def is_test_file(self) -> bool:
        """Check if this file contains tests."""
        return self.test_count > 0
    
    @property
    def primary_class(self) -> Optional[ClassInfo]:
        """Get the primary (first public) class in the file."""
        public_classes = [c for c in self.classes if c.is_public]
        return public_classes[0] if public_classes else (self.classes[0] if self.classes else None)
    
    def get_class_by_name(self, name: str) -> Optional[ClassInfo]:
        """Find a class by name."""
        for cls in self.classes:
            if cls.name == name:
                return cls
        return None
    
    def __hash__(self):
        return hash(self.path)
    
    def __eq__(self, other):
        if not isinstance(other, FileInfo):
            return False
        return self.path == other.path
    
    def __repr__(self):
        return f"FileInfo(path='{self.name}', classes={self.class_count}, tests={self.test_count})"
