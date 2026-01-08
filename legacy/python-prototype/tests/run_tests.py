"""
Test runner for Migration Tool.

Runs all tests and reports results.
"""

import sys
from pathlib import Path

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from test_solution_parser import run_all_tests as run_solution_tests
from test_project_parser import run_all_tests as run_project_tests
from test_file_scanner import run_all_tests as run_scanner_tests
from test_ui_components import run_all_tests as run_ui_tests
from test_i18n import run_all_tests as run_i18n_tests
from test_migration_engine import run_all_tests as run_migration_engine_tests


def main():
    """Run all test suites."""
    print("=" * 70)
    print("MIGRATION TOOL - TEST SUITE")
    print("=" * 70)
    
    results = []
    
    # Run each test suite
    print("\n" + "=" * 70)
    results.append(("Solution Parser", run_solution_tests()))
    
    print("\n" + "=" * 70)
    results.append(("Project Parser", run_project_tests()))
    
    print("\n" + "=" * 70)
    results.append(("File Scanner", run_scanner_tests()))
    
    print("\n" + "=" * 70)
    results.append(("i18n", run_i18n_tests()))
    
    print("\n" + "=" * 70)
    results.append(("UI Components", run_ui_tests()))
    
    print("\n" + "=" * 70)
    results.append(("Migration Engine", run_migration_engine_tests()))
    
    # Summary
    print("\n" + "=" * 70)
    print("FINAL SUMMARY")
    print("=" * 70)
    
    all_passed = True
    for name, passed in results:
        status = "✅ PASSED" if passed else "❌ FAILED"
        print(f"  {name}: {status}")
        if not passed:
            all_passed = False
    
    print("=" * 70)
    
    if all_passed:
        print("🎉 All tests passed!")
    else:
        print("⚠️  Some tests failed!")
    
    return all_passed


if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)
