"""
Tests for migration_engine module.

Uses a temporary directory with generated fake projects to test
folder moves, reference updates, and solution file updates.
Uses the same FakeSolutionGenerator as .NET tests for consistency.
"""

import os
import sys
import shutil
import tempfile
from pathlib import Path
from typing import List, Tuple

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))
sys.path.insert(0, str(Path(__file__).parent))

from core.migration_engine import MigrationEngine, MigrationResult
from utils.logging_config import setup_logging
from fake_solution_generator import FakeSolutionGenerator

# Setup logging for tests
setup_logging()


class TempWorkspace:
    """Context manager for temporary test workspace with FakeSolutionGenerator."""
    
    def __init__(self):
        self.path = None
        self.generator = None
    
    def __enter__(self):
        self.path = Path(tempfile.mkdtemp(prefix="migration_test_"))
        self.generator = FakeSolutionGenerator(self.path)
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        if self.path and self.path.exists():
            shutil.rmtree(self.path, ignore_errors=True)
    
    def create_file(self, relative_path: str, content: str) -> Path:
        """Helper to create a file in the workspace."""
        full_path = self.path / relative_path
        full_path.parent.mkdir(parents=True, exist_ok=True)
        full_path.write_text(content, encoding='utf-8')
        return full_path


def test_move_folder_success():
    """Test successful folder move."""
    print("Testing folder move...")
    
    with TempWorkspace() as ws:
        # Use new generator to create minimal solution
        solution = ws.generator.create_minimal_solution("TestApp")
        project = solution.projects[0]
        
        # Get relative path for the project folder
        project_folder = project.path.parent.relative_to(ws.path)
        
        engine = MigrationEngine(ws.path, dry_run=False)
        
        # Move the folder
        result = engine.move_folder(
            project_folder,
            Path("lib/TestApp.Core")
        )
        
        assert result.success, f"Move failed: {result.message}"
        assert (ws.path / "lib/TestApp.Core").exists(), "Target folder not created"
        assert not (ws.path / project_folder).exists(), "Source folder still exists"
        assert (ws.path / "lib/TestApp.Core/TestApp.Core.csproj").exists(), "Project file not moved"
        
    print("✅ test_move_folder_success PASSED")
    return True


def test_move_folder_dry_run():
    """Test folder move in dry-run mode."""
    print("Testing folder move (dry-run)...")
    
    with TempWorkspace() as ws:
        # Use new generator
        solution = ws.generator.create_minimal_solution("TestApp")
        project = solution.projects[0]
        project_folder = project.path.parent.relative_to(ws.path)
        
        # Create engine in dry-run mode
        engine = MigrationEngine(ws.path, dry_run=True)
        
        # Move the folder (dry-run)
        result = engine.move_folder(
            project_folder,
            Path("lib/TestApp.Core")
        )
        
        assert result.success, f"Dry-run failed: {result.message}"
        assert "[DRY RUN]" in result.message, "Message should indicate dry-run"
        assert (ws.path / project_folder).exists(), "Source should still exist in dry-run"
        assert not (ws.path / "lib/TestApp.Core").exists(), "Target should not exist in dry-run"
        
    print("✅ test_move_folder_dry_run PASSED")
    return True


def test_move_folder_source_not_exists():
    """Test folder move when source doesn't exist."""
    print("Testing folder move (source not exists)...")
    
    with TempWorkspace() as ws:
        engine = MigrationEngine(ws.path, dry_run=False)
        
        result = engine.move_folder(
            Path("nonexistent/folder"),
            Path("target/folder")
        )
        
        assert not result.success, "Should fail when source doesn't exist"
        assert "does not exist" in result.message.lower()
        
    print("✅ test_move_folder_source_not_exists PASSED")
    return True


def test_move_folder_target_exists():
    """Test folder move when target already exists."""
    print("Testing folder move (target exists)...")
    
    with TempWorkspace() as ws:
        # Create both solutions (source and target folders)
        solution1 = ws.generator.create_minimal_solution("App1")
        solution2 = ws.generator.create_minimal_solution("App2")
        
        project1_folder = solution1.projects[0].path.parent.relative_to(ws.path)
        project2_folder = solution2.projects[0].path.parent.relative_to(ws.path)
        
        engine = MigrationEngine(ws.path, dry_run=False)
        
        result = engine.move_folder(
            project1_folder,
            project2_folder  # This already exists
        )
        
        assert not result.success, "Should fail when target exists"
        assert "already exists" in result.message.lower()
        
    print("✅ test_move_folder_target_exists PASSED")
    return True


def test_update_project_reference():
    """Test updating project references in .csproj file using EF Core solution."""
    print("Testing project reference update...")
    
    with TempWorkspace() as ws:
        # Create EF Core solution (has Domain, Infrastructure, Tests with references)
        solution = ws.generator.create_ef_core_solution("MyApp")
        
        # Find Infrastructure project (references Domain)
        infra = next(p for p in solution.projects if "Infrastructure" in p.name)
        domain = next(p for p in solution.projects if "Domain" in p.name)
        
        engine = MigrationEngine(ws.path, dry_run=False)
        
        # Update reference from Domain to new location
        result = engine.update_project_reference(
            infra.path.relative_to(ws.path),
            f"..\\{domain.name}\\{domain.name}.csproj",
            f"..\\..\\core\\{domain.name}\\{domain.name}.csproj"
        )
        
        assert result.success, f"Update failed: {result.message}"
        
        # Verify the change
        content = infra.path.read_text()
        assert f"core\\{domain.name}" in content, "New reference not found"
        
    print("✅ test_update_project_reference PASSED")
    return True


def test_update_solution_project_path():
    """Test updating project path in solution file."""
    print("Testing solution path update...")
    
    with TempWorkspace() as ws:
        # Create EF Core solution
        solution = ws.generator.create_ef_core_solution("MyApp")
        domain = next(p for p in solution.projects if "Domain" in p.name)
        
        engine = MigrationEngine(ws.path, dry_run=False)
        
        # Update solution - move Domain to core folder
        result = engine.update_solution_project_path(
            solution.path.relative_to(ws.path),
            f"src\\{domain.name}",
            f"core\\{domain.name}"
        )
        
        assert result.success, f"Update failed: {result.message}"
        
        # Verify the change
        content = solution.path.read_text()
        assert f"core\\{domain.name}" in content, "New path not found in solution"
        
    print("✅ test_update_solution_project_path PASSED")
    return True


def test_find_affected_projects():
    """Test finding projects that reference a moved project."""
    print("Testing find affected projects...")
    
    with TempWorkspace() as ws:
        # Create EF Core solution (has proper project references)
        solution = ws.generator.create_ef_core_solution("MyApp")
        domain = next(p for p in solution.projects if "Domain" in p.name)
        domain_folder = domain.path.parent.relative_to(ws.path)
        
        engine = MigrationEngine(ws.path, dry_run=False)
        
        # Find projects affected by moving Domain
        affected = engine.find_affected_projects(domain_folder)
        
        # Infrastructure and Tests reference Domain
        affected_names = [p.name for p in affected]
        assert any("Infrastructure" in str(p) for p in affected), \
            "Infrastructure should be affected"
        assert any("Tests" in str(p) for p in affected), \
            "Tests should be affected"
        
    print("✅ test_find_affected_projects PASSED")
    return True


def test_find_solution_files():
    """Test finding all solution files."""
    print("Testing find solution files...")
    
    with TempWorkspace() as ws:
        # Create multiple solutions
        ws.generator.create_ef_core_solution("App1")
        ws.generator.create_minimal_solution("App2")
        
        engine = MigrationEngine(ws.path, dry_run=False)
        
        # Find solutions
        solutions = engine.find_solution_files()
        
        assert len(solutions) == 2, f"Expected 2 solutions, found {len(solutions)}"
        solution_names = [s.name for s in solutions]
        assert "App1.sln" in solution_names
        assert "App2.sln" in solution_names
        
    print("✅ test_find_solution_files PASSED")
    return True


def test_rename_namespace_fallback():
    """Test namespace rename using fallback (regex-based)."""
    print("Testing namespace rename (fallback)...")
    
    with TempWorkspace() as ws:
        # Create a project with specific namespace
        cs_content = """using System;
using OldNamespace.Utils;

namespace OldNamespace;

public class MyClass
{
    public void DoSomething() { }
}
"""
        cs_file = ws.create_file("src/MyProject/MyClass.cs", cs_content)
        
        engine = MigrationEngine(ws.path, dry_run=False)
        
        # Rename namespace
        result = engine.rename_namespace(
            Path("src/MyProject/MyClass.cs"),
            "OldNamespace",
            "NewNamespace"
        )
        
        assert result.success, f"Rename failed: {result.message}"
        
        # Verify changes
        updated = cs_file.read_text()
        assert "namespace NewNamespace;" in updated, "Namespace not updated"
        assert "using NewNamespace.Utils;" in updated, "Using not updated"
        assert "OldNamespace" not in updated, "Old namespace still present"
        
    print("✅ test_rename_namespace_fallback PASSED")
    return True


def test_move_file():
    """Test moving a single file."""
    print("Testing file move...")
    
    with TempWorkspace() as ws:
        # Create a file using helper
        ws.create_file("src/test.txt", "test content")
        
        engine = MigrationEngine(ws.path, dry_run=False)
        
        result = engine.move_file(
            Path("src/test.txt"),
            Path("dest/test.txt")
        )
        
        assert result.success, f"Move failed: {result.message}"
        assert (ws.path / "dest/test.txt").exists(), "Target file not created"
        assert not (ws.path / "src/test.txt").exists(), "Source file still exists"
        
    print("✅ test_move_file PASSED")
    return True


def test_complete_migration_workflow():
    """Test a complete migration workflow using EF Core solution."""
    print("Testing complete migration workflow...")
    
    with TempWorkspace() as ws:
        # Create EF Core solution with realistic structure
        solution = ws.generator.create_ef_core_solution("MyApp")
        tests = next(p for p in solution.projects if "Tests" in p.name)
        
        tests_folder = tests.path.parent.relative_to(ws.path)
        
        engine = MigrationEngine(ws.path, dry_run=False)
        
        # Step 1: Move Tests to test/Unit/Tests
        result = engine.move_folder(
            tests_folder,
            Path("test/Unit/MyApp.Tests")
        )
        assert result.success, f"Move failed: {result.message}"
        
        # Step 2: Update solution file
        result = engine.update_solution_project_path(
            solution.path.relative_to(ws.path),
            str(tests_folder).replace('/', '\\'),
            "test\\Unit\\MyApp.Tests"
        )
        assert result.success, f"Solution update failed: {result.message}"
        
        # Verify final state
        assert (ws.path / "test/Unit/MyApp.Tests").exists()
        assert not (ws.path / tests_folder).exists()
        
        sln_content = solution.path.read_text()
        assert "test\\Unit\\MyApp.Tests" in sln_content
        
    print("✅ test_complete_migration_workflow PASSED")
    return True


def test_ef_core_solution_structure():
    """Test that EF Core solution is properly generated."""
    print("Testing EF Core solution generation...")
    
    with TempWorkspace() as ws:
        solution = ws.generator.create_ef_core_solution("TestApp")
        
        # Verify structure
        assert len(solution.projects) == 3, f"Expected 3 projects, got {len(solution.projects)}"
        
        project_names = [p.name for p in solution.projects]
        assert "TestApp.Domain" in project_names, "Domain project missing"
        assert "TestApp.Infrastructure" in project_names, "Infrastructure project missing"
        assert "TestApp.Tests" in project_names, "Tests project missing"
        
        # Verify Domain has entities
        domain = next(p for p in solution.projects if "Domain" in p.name)
        domain_entities = list((domain.path.parent / "Entities").glob("*.cs"))
        assert len(domain_entities) >= 4, f"Expected at least 4 entities, got {len(domain_entities)}"
        
        # Verify Infrastructure has repositories
        infra = next(p for p in solution.projects if "Infrastructure" in p.name)
        infra_repos = list((infra.path.parent / "Repositories").glob("*.cs"))
        assert len(infra_repos) >= 3, f"Expected at least 3 repositories, got {len(infra_repos)}"
        
        # Verify solution file exists and contains all projects
        sln_content = solution.path.read_text()
        for proj in solution.projects:
            assert proj.name in sln_content, f"{proj.name} not in solution file"
        
    print("✅ test_ef_core_solution_structure PASSED")
    return True


def run_all_tests():
    """Run all migration engine tests."""
    print("=" * 60)
    print("Running Migration Engine Tests (with FakeSolutionGenerator)")
    print("=" * 60)
    
    tests = [
        test_ef_core_solution_structure,  # Test generator first
        test_move_folder_success,
        test_move_folder_dry_run,
        test_move_folder_source_not_exists,
        test_move_folder_target_exists,
        test_update_project_reference,
        test_update_solution_project_path,
        test_find_affected_projects,
        test_find_solution_files,
        test_rename_namespace_fallback,
        test_move_file,
        test_complete_migration_workflow,
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
