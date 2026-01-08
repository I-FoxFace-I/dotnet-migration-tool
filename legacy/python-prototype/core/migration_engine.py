"""
Migration Engine - executes migration operations.

This module handles:
- File/folder move operations
- Project reference updates in .csproj files
- Solution file (.sln) updates
- Integration with .NET CLI for Roslyn operations
"""

import os
import re
import shutil
import subprocess
import json
from pathlib import Path
from typing import List, Dict, Optional, Tuple
from dataclasses import dataclass
from enum import Enum

import sys
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from utils.logging_config import get_logger

logger = get_logger('core.migration_engine')


class MigrationAction(Enum):
    """Migration action types."""
    MOVE_FILE = "move_file"
    MOVE_FOLDER = "move_folder"
    CREATE_PROJECT = "create_project"
    DELETE_PROJECT = "delete_project"
    RENAME_NAMESPACE = "rename_namespace"
    ADD_REFERENCE = "add_reference"
    REMOVE_REFERENCE = "remove_reference"
    UPDATE_SOLUTION = "update_solution"


@dataclass
class MigrationResult:
    """Result of a migration operation."""
    success: bool
    message: str
    details: Optional[Dict] = None


class MigrationEngine:
    """
    Executes migration operations.
    
    Uses Python for file operations and optionally calls .NET CLI
    for Roslyn-based operations (namespace updates, etc.)
    """
    
    def __init__(self, workspace_root: Path, dry_run: bool = False):
        """
        Initialize the migration engine.
        
        Args:
            workspace_root: Root directory of the workspace
            dry_run: If True, only simulate operations without making changes
        """
        self.workspace_root = Path(workspace_root)
        self.dry_run = dry_run
        self._cli_path = self._find_cli_path()
        
    def _find_cli_path(self) -> Optional[Path]:
        """Find the .NET CLI tool path."""
        # First, look for deployed CLI binary
        deployed_cli = self.workspace_root / "tools" / "Deploy" / "bin" / "migration-tool-cli"
        if deployed_cli.exists():
            exe_name = "migration-tool-cli.exe" if os.name == "nt" else "migration-tool-cli"
            exe_path = deployed_cli / exe_name
            if exe_path.exists():
                return exe_path
        
        # Fallback to source project (for development)
        cli_project = self.workspace_root / "tools" / "src" / "PythonTools" / "MigrationTool.Cli"
        if cli_project.exists():
            return cli_project
        
        return None
    
    def move_folder(self, source: Path, target: Path) -> MigrationResult:
        """
        Move a folder from source to target.
        
        Args:
            source: Source folder path (relative to workspace)
            target: Target folder path (relative to workspace)
            
        Returns:
            MigrationResult with success status
        """
        source_abs = self.workspace_root / source
        target_abs = self.workspace_root / target
        
        logger.info(f"Moving folder: {source} -> {target}")
        
        if not source_abs.exists():
            return MigrationResult(False, f"Source folder does not exist: {source}")
        
        if target_abs.exists():
            return MigrationResult(False, f"Target folder already exists: {target}")
        
        if self.dry_run:
            return MigrationResult(True, f"[DRY RUN] Would move: {source} -> {target}")
        
        try:
            # Create parent directories
            target_abs.parent.mkdir(parents=True, exist_ok=True)
            
            # Move the folder
            shutil.move(str(source_abs), str(target_abs))
            
            logger.info(f"Successfully moved folder: {source} -> {target}")
            return MigrationResult(True, f"Moved: {source} -> {target}")
            
        except Exception as e:
            logger.error(f"Failed to move folder: {e}")
            return MigrationResult(False, f"Failed to move folder: {e}")
    
    def move_file(self, source: Path, target: Path) -> MigrationResult:
        """
        Move a file from source to target.
        
        Args:
            source: Source file path (relative to workspace)
            target: Target file path (relative to workspace)
            
        Returns:
            MigrationResult with success status
        """
        source_abs = self.workspace_root / source
        target_abs = self.workspace_root / target
        
        logger.info(f"Moving file: {source} -> {target}")
        
        if not source_abs.exists():
            return MigrationResult(False, f"Source file does not exist: {source}")
        
        if target_abs.exists():
            return MigrationResult(False, f"Target file already exists: {target}")
        
        if self.dry_run:
            return MigrationResult(True, f"[DRY RUN] Would move: {source} -> {target}")
        
        try:
            # Create parent directories
            target_abs.parent.mkdir(parents=True, exist_ok=True)
            
            # Move the file
            shutil.move(str(source_abs), str(target_abs))
            
            logger.info(f"Successfully moved file: {source} -> {target}")
            return MigrationResult(True, f"Moved: {source} -> {target}")
            
        except Exception as e:
            logger.error(f"Failed to move file: {e}")
            return MigrationResult(False, f"Failed to move file: {e}")
    
    def update_project_reference(
        self, 
        project_path: Path, 
        old_ref: str, 
        new_ref: str
    ) -> MigrationResult:
        """
        Update a project reference in a .csproj file.
        
        Args:
            project_path: Path to .csproj file (relative to workspace)
            old_ref: Old reference path
            new_ref: New reference path
            
        Returns:
            MigrationResult with success status
        """
        project_abs = self.workspace_root / project_path
        
        logger.info(f"Updating reference in {project_path}: {old_ref} -> {new_ref}")
        
        if not project_abs.exists():
            return MigrationResult(False, f"Project file does not exist: {project_path}")
        
        if self.dry_run:
            return MigrationResult(
                True, 
                f"[DRY RUN] Would update reference in {project_path}"
            )
        
        try:
            content = project_abs.read_text(encoding='utf-8')
            
            # Replace the reference
            updated = content.replace(old_ref, new_ref)
            
            if content != updated:
                project_abs.write_text(updated, encoding='utf-8')
                logger.info(f"Updated reference in {project_path}")
                return MigrationResult(True, f"Updated reference in {project_path}")
            else:
                return MigrationResult(True, f"No changes needed in {project_path}")
                
        except Exception as e:
            logger.error(f"Failed to update reference: {e}")
            return MigrationResult(False, f"Failed to update reference: {e}")
    
    def update_solution_project_path(
        self,
        solution_path: Path,
        old_path: str,
        new_path: str,
        project_name: Optional[str] = None
    ) -> MigrationResult:
        """
        Update a project path in a .sln file.
        
        Args:
            solution_path: Path to .sln file (relative to workspace)
            old_path: Old project path
            new_path: New project path
            project_name: Optional project name for logging
            
        Returns:
            MigrationResult with success status
        """
        solution_abs = self.workspace_root / solution_path
        
        logger.info(f"Updating solution {solution_path}: {old_path} -> {new_path}")
        
        if not solution_abs.exists():
            return MigrationResult(False, f"Solution file does not exist: {solution_path}")
        
        if self.dry_run:
            return MigrationResult(
                True,
                f"[DRY RUN] Would update solution {solution_path}"
            )
        
        try:
            content = solution_abs.read_text(encoding='utf-8')
            
            # Normalize paths for comparison (handle both / and \)
            old_normalized = old_path.replace('/', '\\')
            new_normalized = new_path.replace('/', '\\')
            
            # Replace the path
            updated = content.replace(old_normalized, new_normalized)
            
            # Also try with forward slashes
            if content == updated:
                old_forward = old_path.replace('\\', '/')
                new_forward = new_path.replace('\\', '/')
                updated = content.replace(old_forward, new_forward)
            
            if content != updated:
                solution_abs.write_text(updated, encoding='utf-8')
                logger.info(f"Updated project path in {solution_path}")
                return MigrationResult(True, f"Updated project path in {solution_path}")
            else:
                return MigrationResult(True, f"No changes needed in {solution_path}")
                
        except Exception as e:
            logger.error(f"Failed to update solution: {e}")
            return MigrationResult(False, f"Failed to update solution: {e}")
    
    def rename_namespace(
        self,
        file_path: Path,
        old_namespace: str,
        new_namespace: str
    ) -> MigrationResult:
        """
        Rename namespace in a C# file using .NET CLI.
        
        Args:
            file_path: Path to C# file (relative to workspace)
            old_namespace: Old namespace
            new_namespace: New namespace
            
        Returns:
            MigrationResult with success status
        """
        file_abs = self.workspace_root / file_path
        
        logger.info(f"Renaming namespace in {file_path}: {old_namespace} -> {new_namespace}")
        
        if not file_abs.exists():
            return MigrationResult(False, f"File does not exist: {file_path}")
        
        if self.dry_run:
            return MigrationResult(
                True,
                f"[DRY RUN] Would rename namespace in {file_path}"
            )
        
        # Try using .NET CLI if available
        if self._cli_path:
            result = self._call_cli([
                "update-namespace",
                "--file", str(file_abs),
                "--old", old_namespace,
                "--new", new_namespace
            ])
            if result.success:
                return result
        
        # Fallback: simple text replacement
        try:
            content = file_abs.read_text(encoding='utf-8')
            
            # Replace namespace declaration
            updated = re.sub(
                rf'namespace\s+{re.escape(old_namespace)}',
                f'namespace {new_namespace}',
                content
            )
            
            # Replace using statements
            updated = re.sub(
                rf'using\s+{re.escape(old_namespace)}',
                f'using {new_namespace}',
                updated
            )
            
            if content != updated:
                file_abs.write_text(updated, encoding='utf-8')
                logger.info(f"Renamed namespace in {file_path}")
                return MigrationResult(True, f"Renamed namespace in {file_path}")
            else:
                return MigrationResult(True, f"No changes needed in {file_path}")
                
        except Exception as e:
            logger.error(f"Failed to rename namespace: {e}")
            return MigrationResult(False, f"Failed to rename namespace: {e}")
    
    def _call_cli(self, args: List[str]) -> MigrationResult:
        """
        Call the .NET CLI tool.
        
        Args:
            args: Command line arguments
            
        Returns:
            MigrationResult with CLI output
        """
        if not self._cli_path:
            return MigrationResult(False, ".NET CLI not found")
        
        try:
            # Check if it's a compiled binary or a project path
            if self._cli_path.suffix in ('.exe', ''):
                # Deployed binary
                cmd = [str(self._cli_path)] + args
            else:
                # Development mode - use dotnet run
                cmd = ["dotnet", "run", "--project", str(self._cli_path), "--"] + args
            
            logger.debug(f"Running CLI: {' '.join(cmd)}")
            
            result = subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                cwd=str(self.workspace_root)
            )
            
            if result.returncode == 0:
                try:
                    output = json.loads(result.stdout)
                    return MigrationResult(True, "CLI command succeeded", output)
                except json.JSONDecodeError:
                    return MigrationResult(True, result.stdout)
            else:
                return MigrationResult(False, f"CLI error: {result.stderr}")
                
        except Exception as e:
            logger.error(f"Failed to call CLI: {e}")
            return MigrationResult(False, f"Failed to call CLI: {e}")
    
    def find_affected_projects(
        self,
        moved_project_path: Path
    ) -> List[Path]:
        """
        Find all projects that reference a moved project.
        
        Args:
            moved_project_path: Path to the moved project
            
        Returns:
            List of paths to affected .csproj files
        """
        affected = []
        project_name = moved_project_path.stem
        
        # Search for all .csproj files
        for csproj in self.workspace_root.rglob("*.csproj"):
            try:
                content = csproj.read_text(encoding='utf-8')
                if project_name in content:
                    affected.append(csproj.relative_to(self.workspace_root))
            except Exception:
                pass
        
        return affected
    
    def find_solution_files(self) -> List[Path]:
        """
        Find all solution files in the workspace.
        
        Returns:
            List of paths to .sln files
        """
        solutions = []
        for sln in self.workspace_root.rglob("*.sln"):
            solutions.append(sln.relative_to(self.workspace_root))
        return solutions


def create_test_migration_plan() -> List[Dict]:
    """
    Create a migration plan for reorganizing tests.
    
    Returns:
        List of migration steps
    """
    # Projects to move to test/Unit/
    unit_test_projects = [
        ("test/Wpf.Abstractions.Tests", "test/Unit/Wpf.Abstractions.Tests"),
        ("test/Wpf.Contracts.Tests", "test/Unit/Wpf.Contracts.Tests"),
        ("test/Wpf.ViewModels.Tests", "test/Unit/Wpf.ViewModels.Tests"),
        ("test/Wpf.Services.Tests", "test/Unit/Wpf.Services.Tests"),
    ]
    
    # Projects to move to test/Integration/
    integration_test_projects = [
        ("test/Wpf.Services.Autofac.Tests", "test/Integration/Wpf.Services.Autofac.Tests"),
        ("test/Wpf.Services.MicrosoftDI.Tests", "test/Integration/Wpf.Services.MicrosoftDI.Tests"),
        ("test/Wpf.Services.Async.Tests", "test/Integration/Wpf.Services.Async.Tests"),
        ("test/Wpf.Integration.Tests", "test/Integration/Wpf.Integration.Tests"),
        ("test/WpfEngine.IntegrationTests", "test/Integration/WpfEngine.IntegrationTests"),
    ]
    
    # Projects to move to test/Helpers/
    helper_projects = [
        ("test/Wpf.Tests.Data", "test/Helpers/Wpf.Tests.Data"),
        ("test/Wpf.Tests.Helpers", "test/Helpers/Wpf.Tests.Helpers"),
        ("test/Wpf.TestRunner", "test/Helpers/Wpf.TestRunner"),
    ]
    
    plan = []
    step_id = 1
    
    for source, target in unit_test_projects + integration_test_projects + helper_projects:
        plan.append({
            "id": step_id,
            "action": MigrationAction.MOVE_FOLDER.value,
            "source": source,
            "target": target,
            "metadata": {"type": "project_move"}
        })
        step_id += 1
    
    return plan


if __name__ == "__main__":
    # Test the migration engine
    import argparse
    
    parser = argparse.ArgumentParser(description="Migration Engine")
    parser.add_argument("--workspace", required=True, help="Workspace root path")
    parser.add_argument("--dry-run", action="store_true", help="Simulate without changes")
    parser.add_argument("--create-plan", action="store_true", help="Create test migration plan")
    
    args = parser.parse_args()
    
    engine = MigrationEngine(Path(args.workspace), dry_run=args.dry_run)
    
    if args.create_plan:
        plan = create_test_migration_plan()
        print(json.dumps(plan, indent=2))
