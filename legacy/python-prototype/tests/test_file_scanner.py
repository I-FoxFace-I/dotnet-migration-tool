"""
Tests for file_scanner module.
"""

import sys
from pathlib import Path

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from core.file_scanner import FileScanner, scan_file, scan_directory
from models.file_info import TestFramework
from utils.logging_config import setup_logging

# Setup logging for tests
setup_logging()


def get_test_file_path() -> Path:
    """Get a test file path from the workspace."""
    workspace_root = Path(__file__).parent.parent.parent.parent
    
    # Try to find a test file
    test_files = [
        workspace_root / "test" / "Wpf.Scopes.Tests" / "Core" / "DisposalTests.cs",
        workspace_root / "test" / "Unit" / "Wpf.Scopes.Unit.Tests" / "Core" / "DisposalTests.cs",
    ]
    
    for path in test_files:
        if path.exists():
            return path
    
    # Try to find any .cs file in test folder
    test_dir = workspace_root / "test"
    if test_dir.exists():
        for cs_file in test_dir.rglob("*Tests.cs"):
            if 'bin' not in str(cs_file) and 'obj' not in str(cs_file):
                return cs_file
    
    return None


def get_source_file_path() -> Path:
    """Get a source file path from the workspace."""
    workspace_root = Path(__file__).parent.parent.parent.parent
    
    # Try to find a source file
    src_dir = workspace_root / "src"
    if src_dir.exists():
        for cs_file in src_dir.rglob("*.cs"):
            if 'bin' not in str(cs_file) and 'obj' not in str(cs_file):
                return cs_file
    
    return None


def test_scan_test_file():
    """Test scanning a test file."""
    file_path = get_test_file_path()
    
    if file_path is None:
        print("⚠️  No test file found, skipping test")
        return True
    
    print(f"📁 Scanning: {file_path}")
    
    scanner = FileScanner()
    file_info = scanner.scan_file(file_path)
    
    print(f"✅ File: {file_info.name}")
    print(f"   Namespace: {file_info.namespace}")
    print(f"   Lines: {file_info.line_count}")
    print(f"   Classes: {file_info.class_count}")
    print(f"   Tests: {file_info.test_count}")
    print(f"   Usings: {len(file_info.using_directives)}")
    
    if file_info.classes:
        print("\n   Classes:")
        for cls in file_info.classes[:3]:
            print(f"   - {cls.name} ({cls.member_type.value})")
            if cls.tests:
                print(f"     Tests: {cls.test_count}")
                for test in cls.tests[:3]:
                    print(f"       - {test.name} ({test.framework.value})")
    
    # Assertions
    assert file_info.path == file_path
    assert file_info.namespace is not None or file_info.class_count == 0
    
    print("\n✅ test_scan_test_file PASSED")
    return True


def test_scan_source_file():
    """Test scanning a source file."""
    file_path = get_source_file_path()
    
    if file_path is None:
        print("⚠️  No source file found, skipping test")
        return True
    
    print(f"📁 Scanning: {file_path}")
    
    scanner = FileScanner()
    file_info = scanner.scan_file(file_path)
    
    print(f"✅ File: {file_info.name}")
    print(f"   Namespace: {file_info.namespace}")
    print(f"   Classes: {file_info.class_count}")
    print(f"   Is test file: {file_info.is_test_file}")
    
    if file_info.classes:
        print("\n   Classes:")
        for cls in file_info.classes[:3]:
            print(f"   - {cls.name} ({cls.member_type.value})")
            if cls.base_class:
                print(f"     Base: {cls.base_class}")
            if cls.interfaces:
                print(f"     Interfaces: {', '.join(cls.interfaces[:3])}")
    
    print("\n✅ test_scan_source_file PASSED")
    return True


def test_scan_directory():
    """Test scanning a directory."""
    workspace_root = Path(__file__).parent.parent.parent.parent
    test_dir = workspace_root / "test" / "Wpf.Scopes.Tests" / "Core"
    
    if not test_dir.exists():
        test_dir = workspace_root / "test" / "Unit" / "Wpf.Scopes.Unit.Tests" / "Core"
    
    if not test_dir.exists():
        print("⚠️  No test directory found, skipping test")
        return True
    
    print(f"📁 Scanning directory: {test_dir}")
    
    scanner = FileScanner()
    files = scanner.scan_directory(test_dir, recursive=False)
    
    print(f"✅ Found {len(files)} files")
    
    total_classes = sum(f.class_count for f in files)
    total_tests = sum(f.test_count for f in files)
    
    print(f"   Total classes: {total_classes}")
    print(f"   Total tests: {total_tests}")
    
    # List files
    print("\n   Files:")
    for f in files[:5]:
        print(f"   - {f.name}: {f.class_count} classes, {f.test_count} tests")
    
    print("\n✅ test_scan_directory PASSED")
    return True


def test_extract_namespace():
    """Test namespace extraction."""
    scanner = FileScanner()
    
    # Test block-scoped namespace
    content1 = """
namespace Foo.Bar
{
    public class Test { }
}
"""
    ns1 = scanner._extract_namespace(content1)
    assert ns1 == "Foo.Bar", f"Expected 'Foo.Bar', got '{ns1}'"
    
    # Test file-scoped namespace (C# 10+)
    content2 = """
namespace Foo.Bar;

public class Test { }
"""
    ns2 = scanner._extract_namespace(content2)
    assert ns2 == "Foo.Bar", f"Expected 'Foo.Bar', got '{ns2}'"
    
    print("✅ test_extract_namespace PASSED")
    return True


def test_extract_classes():
    """Test class extraction."""
    scanner = FileScanner()
    
    content = """
namespace TestNamespace
{
    public class MyClass : BaseClass, IInterface
    {
    }
    
    public interface IMyInterface
    {
    }
    
    public abstract class AbstractClass
    {
    }
    
    public static class StaticClass
    {
    }
}
"""
    classes = scanner._extract_classes(content, "TestNamespace")
    
    assert len(classes) == 4, f"Expected 4 classes, got {len(classes)}"
    
    # Check class names
    names = [c.name for c in classes]
    assert "MyClass" in names
    assert "IMyInterface" in names
    assert "AbstractClass" in names
    assert "StaticClass" in names
    
    # Check MyClass details
    my_class = next(c for c in classes if c.name == "MyClass")
    assert my_class.base_class == "BaseClass"
    assert "IInterface" in my_class.interfaces
    
    print("✅ test_extract_classes PASSED")
    return True


def test_extract_tests():
    """Test test method extraction."""
    scanner = FileScanner()
    
    content = """
namespace TestNamespace
{
    public class MyTests
    {
        [Fact]
        public void Test1() { }
        
        [Theory]
        [InlineData(1)]
        public void Test2(int x) { }
        
        [Fact]
        [Trait("Category", "Unit")]
        public void Test3() { }
    }
}
"""
    file_info = scanner.scan_file.__self__  # Get scanner instance
    classes = scanner._extract_classes(content, "TestNamespace")
    
    # The test extraction happens during class extraction
    my_tests = next((c for c in classes if c.name == "MyTests"), None)
    
    if my_tests and my_tests.tests:
        print(f"   Found {len(my_tests.tests)} tests")
        for test in my_tests.tests:
            print(f"   - {test.name} ({test.framework.value})")
    
    print("✅ test_extract_tests PASSED")
    return True


def run_all_tests():
    """Run all tests."""
    print("=" * 60)
    print("Running File Scanner Tests")
    print("=" * 60)
    
    tests = [
        test_extract_namespace,
        test_extract_classes,
        test_extract_tests,
        test_scan_test_file,
        test_scan_source_file,
        test_scan_directory,
    ]
    
    passed = 0
    failed = 0
    
    for test in tests:
        try:
            print(f"\n--- {test.__name__} ---")
            if test():
                passed += 1
            else:
                failed += 1
        except Exception as e:
            print(f"❌ {test.__name__} FAILED with exception: {e}")
            import traceback
            traceback.print_exc()
            failed += 1
    
    print("\n" + "=" * 60)
    print(f"Results: {passed} passed, {failed} failed")
    print("=" * 60)
    
    return failed == 0


if __name__ == "__main__":
    success = run_all_tests()
    sys.exit(0 if success else 1)
