#!/usr/bin/env python3
"""
Sync MigrationTool changes to the standalone dotnet-migration-tool repository.

Usage:
    python sync_to_migration_tool_repo.py [--commit "message"] [--push] [--dry-run]
"""

import argparse
import os
import shutil
import subprocess
from pathlib import Path


# Configuration
DEFAULT_TARGET_REPO = r"C:\Users\mrmar\source\repos\dotnet-migration-tool"

# Folders to sync from src/MigrationTool
SRC_FOLDERS = [
    "MigrationTool.Cli",
    "MigrationTool.Core",
    "MigrationTool.Core.Abstractions",
]

# Folders to sync from tests/MigrationTool
TEST_FOLDERS = [
    "MigrationTool.Cli.Tests",
]

# Additional directories to sync (from tools root)
ADDITIONAL_DIRS = [
    ("scripts", "scripts"),  # Copy scripts/ folder
]

# Files to exclude
EXCLUDE_PATTERNS = [
    "bin",
    "obj",
    ".vs",
    "*.user",
    "__pycache__",
    "node_modules",
    ".git",
]

# Additional files to sync
ADDITIONAL_FILES = [
    ("tests/test.runsettings", "tests/test.runsettings"),
    ("tests/run-cli-tests.ps1", "tests/run-cli-tests.ps1"),
    ("Directory.Packages.props", "Directory.Packages.props"),
    ("README.md", "README.md"),
    ("ROADMAP.md", "ROADMAP.md"),
    ("USAGE.md", "USAGE.md"),
    ("MigrationTool.sln", "MigrationTool.sln"),
]


def should_exclude(path: Path, exclude_patterns: list[str]) -> bool:
    """Check if path should be excluded."""
    path_str = str(path)
    for pattern in exclude_patterns:
        if pattern.startswith("*"):
            if path_str.endswith(pattern[1:]):
                return True
        elif pattern in path_str.split(os.sep):
            return True
    return False


def copy_directory(source: Path, target: Path, exclude_patterns: list[str], dry_run: bool) -> int:
    """Copy directory contents, excluding specified patterns."""
    if not source.exists():
        print(f"  SKIP: Source not found: {source}")
        return 0
    
    files_copied = 0
    
    for src_file in source.rglob("*"):
        if src_file.is_dir():
            continue
        
        relative_path = src_file.relative_to(source)
        
        if should_exclude(relative_path, exclude_patterns):
            continue
        
        target_file = target / relative_path
        
        if not dry_run:
            target_file.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src_file, target_file)
        
        files_copied += 1
    
    return files_copied


def run_git_command(repo_path: Path, *args) -> tuple[int, str]:
    """Run a git command in the specified repository."""
    result = subprocess.run(
        ["git"] + list(args),
        cwd=repo_path,
        capture_output=True,
        text=True
    )
    return result.returncode, result.stdout + result.stderr


def main():
    parser = argparse.ArgumentParser(description="Sync MigrationTool to standalone repo")
    parser.add_argument("--commit", "-c", help="Commit message")
    parser.add_argument("--push", "-p", action="store_true", help="Push after commit")
    parser.add_argument("--dry-run", "-n", action="store_true", help="Preview without changes")
    parser.add_argument("--target", "-t", default=DEFAULT_TARGET_REPO, help="Target repository path")
    args = parser.parse_args()
    
    # Determine paths
    script_dir = Path(__file__).parent
    tools_dir = script_dir.parent
    source_dir = tools_dir / "src" / "MigrationTool"
    tests_source_dir = tools_dir / "tests" / "MigrationTool"
    target_repo = Path(args.target)
    
    print("=" * 40)
    print("  Sync to dotnet-migration-tool repo")
    print("=" * 40)
    print()
    
    # Validate paths
    if not source_dir.exists():
        print(f"ERROR: Source directory not found: {source_dir}")
        return 1
    
    if not target_repo.exists():
        print(f"ERROR: Target repository not found: {target_repo}")
        return 1
    
    target_src_dir = target_repo / "src" / "MigrationTool"
    target_tests_dir = target_repo / "tests" / "MigrationTool"
    
    print(f"Source: {source_dir}")
    print(f"Target: {target_src_dir}")
    print()
    
    if args.dry_run:
        print("[DRY RUN] No changes will be made")
        print()
    
    total_files = 0
    
    # Sync source folders
    print("Syncing source folders...")
    for folder in SRC_FOLDERS:
        src_path = source_dir / folder
        tgt_path = target_src_dir / folder
        
        print(f"  {folder}")
        count = copy_directory(src_path, tgt_path, EXCLUDE_PATTERNS, args.dry_run)
        print(f"    -> {count} files")
        total_files += count
    
    # Sync test folders
    print()
    print("Syncing test folders...")
    for folder in TEST_FOLDERS:
        src_path = tests_source_dir / folder
        tgt_path = target_tests_dir / folder
        
        print(f"  {folder}")
        count = copy_directory(src_path, tgt_path, EXCLUDE_PATTERNS, args.dry_run)
        print(f"    -> {count} files")
        total_files += count
    
    # Sync additional directories
    print()
    print("Syncing additional directories...")
    for src_rel, tgt_rel in ADDITIONAL_DIRS:
        src_dir = tools_dir / src_rel
        tgt_dir = target_repo / tgt_rel
        
        if src_dir.exists():
            print(f"  {src_rel}/")
            count = copy_directory(src_dir, tgt_dir, EXCLUDE_PATTERNS, args.dry_run)
            print(f"    -> {count} files")
            total_files += count
        else:
            print(f"  SKIP: {src_rel}/ (not found)")
    
    # Sync additional files
    print()
    print("Syncing additional files...")
    for src_rel, tgt_rel in ADDITIONAL_FILES:
        src_file = tools_dir / src_rel
        tgt_file = target_repo / tgt_rel
        
        if src_file.exists():
            if not args.dry_run:
                tgt_file.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(src_file, tgt_file)
            print(f"  {src_rel}")
            total_files += 1
        else:
            print(f"  SKIP: {src_rel} (not found)")
    
    print()
    print(f"Total files synced: {total_files}")
    
    # Git operations
    if not args.dry_run:
        print()
        print("Git operations...")
        
        # Check for changes
        returncode, output = run_git_command(target_repo, "status", "--porcelain")
        
        if output.strip():
            print("  Changes detected:")
            _, status = run_git_command(target_repo, "status", "--short")
            print(status)
            
            # Stage all changes
            run_git_command(target_repo, "add", "-A")
            
            # Commit if message provided
            if args.commit:
                print()
                print("  Committing...")
                returncode, output = run_git_command(target_repo, "commit", "-m", args.commit)
                print(output)
                
                if args.push:
                    print()
                    print("  Pushing to origin...")
                    returncode, output = run_git_command(target_repo, "push", "origin", "master")
                    print(output)
            else:
                print()
                print("  Changes staged. Use --commit to commit.")
        else:
            print("  No changes detected")
    
    print()
    print("Done!")
    return 0


if __name__ == "__main__":
    exit(main())