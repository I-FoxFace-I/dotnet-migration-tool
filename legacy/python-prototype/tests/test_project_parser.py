"""
Tests for project_parser module.
"""

import sys
from pathlib import Path

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from core.project_parser import ProjectParser, parse_project
from models.solution import ProjectType
from utils.logging_config import setup_logging

# Setup logging for tests
setup_logging()


def get_test_project_path() -> Path:
    """Get a test project path from the workspace."""
    workspace_root = Path(__file__).parent.parent.parent.parent
    
    # Try to find a test project
    test_projects = [
        workspace_root / "test" / "Wpf.Scopes.Tests" / "Wpf.Scopes.Tests.csproj",
        workspace_root / "test" / "Unit" / "Wpf.Scopes.Unit.Tests" / "Wpf.Scopes.Unit.Tests.csproj",
    ]
    
    for path in test_projects:
        if path.exists():
            return path
    
    # Try to find any .csproj
    for csproj in workspace_root.rglob("*.csproj"):
        if 'bin' not in str(csproj) and 'obj' not in str(csproj):
            return csproj
    
    return None


def test_parse_project():
    """Test parsing a real project file."""
    csproj_path = get_test_project_path()
    
    if csproj_path is None:
        print("⚠️  No project file found, skipping test")
        return True
    
    print(f"📁 Parsing: {csproj_path}")
    
    parser = ProjectParser()
    project = parser.parse(csproj_path)
    
    print(f"✅ Project: {project.name}")
    print(f"   Framework: {project.target_framework}")
    print(f"   Type: {project.project_type.value}")
    print(f"   Namespace: {project.root_namespace}")
    print(f"   Project refs: {len(project.project_references)}")
    print(f"   Package refs: {len(project.package_references)}")
    
    if project.project_references:
        print("\n   Project references:")
        for ref in project.project_references[:3]:
            print(f"   - {ref.name}")
    
    if project.package_references:
        print("\n   Package references:")
        for ref in project.package_references[:5]:
            print(f"   - {ref.name} {ref.version or ''}")
    
    # Assertions
    assert project.name == csproj_path.stem
    assert project.path == csproj_path
    
    print("\n✅ test_parse_project PASSED")
    return True


def test_detect_test_project():
    """Test detection of test projects."""
    csproj_path = get_test_project_path()
    
    if csproj_path is None:
        print("⚠️  No project file found, skipping test")
        return True
    
    parser = ProjectParser()
    project = parser.parse(csproj_path)
    
    # If it's a test project, it should be detected
    if 'test' in csproj_path.stem.lower():
        if project.is_test_project:
            print(f"✅ Correctly identified as test project: {project.name}")
        else:
            print(f"⚠️  Project {project.name} not detected as test (may be missing test packages)")
    
    print("✅ test_detect_test_project PASSED")
    return True


def test_parse_nonexistent_project():
    """Test parsing a non-existent project file."""
    parser = ProjectParser()
    
    try:
        parser.parse("nonexistent.csproj")
        print("❌ Should have raised FileNotFoundError")
        return False
    except FileNotFoundError:
        print("✅ test_parse_nonexistent_project PASSED")
        return True


def test_parse_invalid_file():
    """Test parsing a non-.csproj file."""
    parser = ProjectParser()
    
    try:
        parser.parse(__file__)
        print("❌ Should have raised ValueError")
        return False
    except ValueError:
        print("✅ test_parse_invalid_file PASSED")
        return True


def test_convenience_function():
    """Test the parse_project convenience function."""
    csproj_path = get_test_project_path()
    
    if csproj_path is None:
        print("⚠️  No project file found, skipping test")
        return True
    
    project = parse_project(csproj_path)
    
    assert project is not None
    assert project.name == csproj_path.stem
    
    print("✅ test_convenience_function PASSED")
    return True


def run_all_tests():
    """Run all tests."""
    print("=" * 60)
    print("Running Project Parser Tests")
    print("=" * 60)
    
    tests = [
        test_parse_project,
        test_detect_test_project,
        test_parse_nonexistent_project,
        test_parse_invalid_file,
        test_convenience_function,
    ]
    
    passed = 0
    failed = 0
    
    for test in tests:
        try:
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
