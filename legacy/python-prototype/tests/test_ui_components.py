"""
Tests for UI components - verifies that components can be imported and basic functions work.
"""

import sys
from pathlib import Path

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from utils.logging_config import setup_logging

# Setup logging for tests
setup_logging()


def test_imports():
    """Test that all UI modules can be imported."""
    print("Testing imports...")
    
    try:
        from ui.sidebar import render_sidebar, _scan_for_solutions, _get_default_workspace_path
        print("  ✅ sidebar imports OK")
    except ImportError as e:
        print(f"  ❌ sidebar import failed: {e}")
        return False
    
    try:
        from ui.dashboard import render_dashboard
        print("  ✅ dashboard imports OK")
    except ImportError as e:
        print(f"  ❌ dashboard import failed: {e}")
        return False
    
    try:
        from ui.project_explorer import render_project_explorer
        print("  ✅ project_explorer imports OK")
    except ImportError as e:
        print(f"  ❌ project_explorer import failed: {e}")
        return False
    
    try:
        from ui.migration_planner import render_migration_planner
        print("  ✅ migration_planner imports OK")
    except ImportError as e:
        print(f"  ❌ migration_planner import failed: {e}")
        return False
    
    print("✅ test_imports PASSED")
    return True


def test_scan_for_solutions():
    """Test solution scanning function."""
    print("\nTesting solution scanning...")
    
    from ui.sidebar import _scan_for_solutions
    
    # Get workspace root
    workspace_root = Path(__file__).parent.parent.parent.parent
    
    solutions = _scan_for_solutions(str(workspace_root))
    
    print(f"  Found {len(solutions)} solutions in {workspace_root}")
    
    if solutions:
        for sln in solutions[:5]:
            print(f"    - {sln.name}")
        if len(solutions) > 5:
            print(f"    ... and {len(solutions) - 5} more")
    
    # Should find at least one solution
    assert len(solutions) > 0, "Should find at least one .sln file"
    
    # All results should be .sln files
    for sln in solutions:
        assert sln.suffix.lower() == '.sln', f"Expected .sln file, got {sln}"
    
    print("✅ test_scan_for_solutions PASSED")
    return True


def test_get_default_workspace_path():
    """Test default workspace path function."""
    print("\nTesting default workspace path...")
    
    from ui.sidebar import _get_default_workspace_path
    
    path = _get_default_workspace_path()
    
    print(f"  Default path: {path}")
    
    # Should be a valid directory
    assert Path(path).exists(), f"Path should exist: {path}"
    assert Path(path).is_dir(), f"Path should be a directory: {path}"
    
    print("✅ test_get_default_workspace_path PASSED")
    return True


def test_core_integration():
    """Test integration between core modules."""
    print("\nTesting core integration...")
    
    from core.solution_parser import SolutionParser
    from core.project_parser import ProjectParser
    from core.file_scanner import FileScanner
    
    workspace_root = Path(__file__).parent.parent.parent.parent
    
    # Find a solution
    sln_path = workspace_root / "Framework.sln"
    if not sln_path.exists():
        sln_path = workspace_root / "AutofacWpfDemo.sln"
    
    if not sln_path.exists():
        print("  ⚠️ No solution found, skipping integration test")
        return True
    
    # Parse solution
    print(f"  Parsing solution: {sln_path.name}")
    solution_parser = SolutionParser()
    solution = solution_parser.parse(sln_path)
    
    assert solution is not None, "Solution should be parsed"
    assert solution.project_count > 0, "Solution should have projects"
    print(f"    Found {solution.project_count} projects")
    
    # Parse first project
    if solution.projects:
        project = solution.projects[0]
        print(f"  Parsing project: {project.name}")
        
        project_parser = ProjectParser()
        project_parser.enrich_project(project)
        
        print(f"    Framework: {project.target_framework}")
        print(f"    Is test: {project.is_test_project}")
    
    # Scan a test directory
    test_dir = workspace_root / "test" / "Wpf.Scopes.Tests" / "Core"
    if test_dir.exists():
        print(f"  Scanning directory: {test_dir.name}")
        
        scanner = FileScanner()
        files = scanner.scan_directory(test_dir, recursive=False)
        
        total_tests = sum(f.test_count for f in files)
        print(f"    Found {len(files)} files with {total_tests} tests")
    
    print("✅ test_core_integration PASSED")
    return True


def test_models():
    """Test data models."""
    print("\nTesting data models...")
    
    from models.solution import Solution, Project, ProjectReference, PackageReference, ProjectType
    from models.file_info import FileInfo, ClassInfo, TestInfo, MemberType, TestFramework
    
    # Test Solution
    solution = Solution(
        name="TestSolution",
        path=Path("test.sln")
    )
    assert solution.name == "TestSolution"
    assert solution.project_count == 0
    print("  ✅ Solution model OK")
    
    # Test Project
    project = Project(
        name="TestProject",
        path=Path("test.csproj"),
        project_type=ProjectType.TEST
    )
    assert project.name == "TestProject"
    assert project.is_test_project
    print("  ✅ Project model OK")
    
    # Test FileInfo
    file_info = FileInfo(
        path=Path("test.cs"),
        namespace="Test.Namespace"
    )
    assert file_info.name == "test.cs"
    assert file_info.namespace == "Test.Namespace"
    print("  ✅ FileInfo model OK")
    
    # Test ClassInfo
    class_info = ClassInfo(
        name="TestClass",
        namespace="Test.Namespace",
        member_type=MemberType.CLASS
    )
    assert class_info.full_name == "Test.Namespace.TestClass"
    print("  ✅ ClassInfo model OK")
    
    # Test TestInfo
    test_info = TestInfo(
        name="TestMethod",
        class_name="TestClass",
        framework=TestFramework.XUNIT
    )
    assert test_info.full_name == "TestClass.TestMethod"
    print("  ✅ TestInfo model OK")
    
    print("✅ test_models PASSED")
    return True


def run_all_tests():
    """Run all tests."""
    print("=" * 60)
    print("Running UI Component Tests")
    print("=" * 60)
    
    tests = [
        test_imports,
        test_scan_for_solutions,
        test_get_default_workspace_path,
        test_models,
        test_core_integration,
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
