"""
Tests for solution_parser module.
"""

import sys
from pathlib import Path

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from core.solution_parser import SolutionParser, parse_solution
from utils.logging_config import setup_logging

# Setup logging for tests
setup_logging()


def test_parse_solution():
    """Test parsing a real solution file."""
    # Use the actual solution in the workspace
    workspace_root = Path(__file__).parent.parent.parent.parent
    sln_path = workspace_root / "Framework.sln"
    
    if not sln_path.exists():
        print(f"⚠️  Solution not found at {sln_path}, trying AutofacWpfDemo.sln")
        sln_path = workspace_root / "AutofacWpfDemo.sln"
    
    if not sln_path.exists():
        print(f"❌ No solution file found in {workspace_root}")
        return False
    
    print(f"📁 Parsing: {sln_path}")
    
    parser = SolutionParser()
    solution = parser.parse(sln_path)
    
    print(f"✅ Solution: {solution.name}")
    print(f"   Projects: {solution.project_count}")
    print(f"   Test projects: {len(solution.test_projects)}")
    print(f"   Source projects: {len(solution.source_projects)}")
    
    # List first 5 projects
    print("\n   First 5 projects:")
    for project in solution.projects[:5]:
        print(f"   - {project.name}")
    
    # Assertions
    assert solution.name == sln_path.stem
    assert solution.project_count > 0
    
    print("\n✅ test_parse_solution PASSED")
    return True


def test_parse_nonexistent_solution():
    """Test parsing a non-existent solution file."""
    parser = SolutionParser()
    
    try:
        parser.parse("nonexistent.sln")
        print("❌ Should have raised FileNotFoundError")
        return False
    except FileNotFoundError:
        print("✅ test_parse_nonexistent_solution PASSED")
        return True


def test_parse_invalid_file():
    """Test parsing a non-.sln file."""
    parser = SolutionParser()
    
    try:
        # Try to parse this Python file as a solution
        parser.parse(__file__)
        print("❌ Should have raised ValueError")
        return False
    except ValueError:
        print("✅ test_parse_invalid_file PASSED")
        return True


def test_convenience_function():
    """Test the parse_solution convenience function."""
    workspace_root = Path(__file__).parent.parent.parent.parent
    sln_path = workspace_root / "Framework.sln"
    
    if not sln_path.exists():
        sln_path = workspace_root / "AutofacWpfDemo.sln"
    
    if not sln_path.exists():
        print("⚠️  Skipping test - no solution file found")
        return True
    
    solution = parse_solution(sln_path)
    
    assert solution is not None
    assert solution.project_count > 0
    
    print("✅ test_convenience_function PASSED")
    return True


def run_all_tests():
    """Run all tests."""
    print("=" * 60)
    print("Running Solution Parser Tests")
    print("=" * 60)
    
    tests = [
        test_parse_solution,
        test_parse_nonexistent_solution,
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
            failed += 1
    
    print("\n" + "=" * 60)
    print(f"Results: {passed} passed, {failed} failed")
    print("=" * 60)
    
    return failed == 0


if __name__ == "__main__":
    success = run_all_tests()
    sys.exit(0 if success else 1)
