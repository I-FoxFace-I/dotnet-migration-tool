#!/usr/bin/env python3
"""
Script to migrate test projects to proper directory structure.

Target structure:
  test/
  ├── Unit/           - Unit tests
  ├── Integration/    - Integration tests  
  ├── Performance/    - Performance tests
  ├── Scenarios/      - E2E scenario tests
  └── Helpers/        - Shared test helpers
"""

import os
import sys
from pathlib import Path

# Add parent to path for imports
sys.path.insert(0, str(Path(__file__).parent))

from core.migration_engine import MigrationEngine, MigrationResult


def create_migration_plan(phase: int = 1):
    """
    Create the migration plan for test reorganization.
    
    Phase 1: Clear-cut Unit tests (using Moq only, no real DI)
    Phase 2: Integration tests (using real DI containers)
    Phase 3: Helpers and complex cases
    """
    
    if phase == 1:
        # PHASE 1: Clear Unit tests - only Moq, no real DI container
        return [
            ("test/Wpf.Abstractions.Tests", "test/Unit/Wpf.Abstractions.Tests"),
            ("test/Wpf.Contracts.Tests", "test/Unit/Wpf.Contracts.Tests"),
            ("test/Wpf.ViewModels.Tests", "test/Unit/Wpf.ViewModels.Tests"),
            ("test/Wpf.Services.Tests", "test/Unit/Wpf.Services.Tests"),
        ]
    
    elif phase == 2:
        # PHASE 2: Integration tests - use real DI containers
        return [
            ("test/Wpf.Integration.Tests", "test/Integration/Wpf.Integration.Tests"),
            ("test/Wpf.Services.Autofac.Tests", "test/Integration/Wpf.Services.Autofac.Tests"),
            ("test/Wpf.Services.MicrosoftDI.Tests", "test/Integration/Wpf.Services.MicrosoftDI.Tests"),
            ("test/Wpf.Services.Async.Tests", "test/Integration/Wpf.Services.Async.Tests"),
            ("test/WpfEngine.IntegrationTests", "test/Integration/WpfEngine.IntegrationTests"),
        ]
    
    elif phase == 3:
        # PHASE 3: Helpers and remaining projects
        return [
            ("test/Wpf.Tests.Data", "test/Helpers/Wpf.Tests.Data"),
            ("test/Wpf.Tests.Helpers", "test/Helpers/Wpf.Tests.Helpers"),
            ("test/Wpf.TestRunner", "test/Helpers/Wpf.TestRunner"),
            # Wpf.Scopes.Tests - mixed content, needs manual review
            # ("test/Wpf.Scopes.Tests", "test/Unit/Wpf.Scopes.Tests"),
        ]
    
    else:
        # All phases combined (not recommended)
        return (create_migration_plan(1) + 
                create_migration_plan(2) + 
                create_migration_plan(3))


def calculate_reference_updates(workspace: Path, old_path: str, new_path: str):
    """Calculate which references need to be updated after a move."""
    updates = []
    project_name = Path(old_path).name
    
    # Find all .csproj files
    for csproj in workspace.rglob("*.csproj"):
        try:
            content = csproj.read_text(encoding='utf-8')
            if project_name in content and f"{project_name}.csproj" in content:
                updates.append(csproj.relative_to(workspace))
        except Exception:
            pass
    
    return updates


def fix_moved_project_references(workspace: Path, old_path: str, new_path: str, dry_run: bool = False):
    """
    Fix project references inside the moved project's .csproj file.
    When a project moves deeper (e.g., test/X -> test/Unit/X), 
    all relative paths need an extra '..' prefix.
    """
    import re
    
    old_depth = len(Path(old_path).parts)
    new_depth = len(Path(new_path).parts)
    depth_diff = new_depth - old_depth  # How many levels deeper
    
    if depth_diff == 0:
        return  # No change needed
    
    project_name = Path(new_path).name
    csproj_path = workspace / new_path / f"{project_name}.csproj"
    
    if not csproj_path.exists():
        print(f"  ⚠️  Cannot find .csproj at {csproj_path}")
        return
    
    content = csproj_path.read_text(encoding='utf-8')
    original = content
    
    # Patterns to fix: ProjectReference Include, RunSettingsFilePath
    # Add extra '..\' for each level deeper
    prefix = '..\\' * depth_diff
    
    # Fix ProjectReference paths
    def fix_project_ref(match):
        path = match.group(1)
        if path.startswith('..'):
            return f'<ProjectReference Include="{prefix}{path}" />'
        return match.group(0)
    
    content = re.sub(
        r'<ProjectReference Include="([^"]+)" />',
        fix_project_ref,
        content
    )
    
    # Fix RunSettingsFilePath
    def fix_runsettings(match):
        path = match.group(1)
        if path.startswith('..'):
            return f'<RunSettingsFilePath>$(MSBuildThisFileDirectory){prefix}{path}</RunSettingsFilePath>'
        return match.group(0)
    
    content = re.sub(
        r'<RunSettingsFilePath>\$\(MSBuildThisFileDirectory\)([^<]+)</RunSettingsFilePath>',
        fix_runsettings,
        content
    )
    
    if content != original:
        if dry_run:
            print(f"  📝 Would update references in {project_name}.csproj")
        else:
            csproj_path.write_text(content, encoding='utf-8')
            print(f"  📝 Updated references in {project_name}.csproj")


def run_migration(workspace: Path, dry_run: bool = True, phase: int = 1):
    """Run the test migration for specified phase."""
    
    print("=" * 60)
    print(f"  Test Project Migration - PHASE {phase}")
    print("=" * 60)
    print(f"\nWorkspace: {workspace}")
    print(f"Mode: {'DRY RUN' if dry_run else 'LIVE'}")
    print(f"Phase: {phase}")
    print()
    
    engine = MigrationEngine(workspace, dry_run=dry_run)
    plan = create_migration_plan(phase)
    
    results = []
    
    for i, (source, target) in enumerate(plan, 1):
        print(f"\n[{i}/{len(plan)}] Moving: {source} -> {target}")
        
        source_path = Path(source)
        target_path = Path(target)
        
        # Check if source exists
        if not (workspace / source_path).exists():
            print(f"  ⚠️  Source does not exist, skipping")
            continue
        
        # Check if already moved
        if (workspace / target_path).exists():
            print(f"  ✅ Already at target location")
            continue
        
        # Move the folder
        result = engine.move_folder(source_path, target_path)
        results.append((source, target, result))
        
        if result.success:
            print(f"  ✅ {result.message}")
            
            # Fix references inside the moved project
            fix_moved_project_references(workspace, source, target, dry_run)
            
            # Find affected projects (other projects referencing this one)
            if not dry_run:
                affected = calculate_reference_updates(workspace, source, target)
                if affected:
                    print(f"  📋 Other projects referencing this: {len(affected)}")
                    for proj in affected[:5]:  # Show first 5
                        print(f"      - {proj}")
                    if len(affected) > 5:
                        print(f"      ... and {len(affected) - 5} more")
        else:
            print(f"  ❌ {result.message}")
    
    # Summary
    print("\n" + "=" * 60)
    print("  Summary")
    print("=" * 60)
    
    successful = sum(1 for _, _, r in results if r.success)
    failed = sum(1 for _, _, r in results if not r.success)
    
    print(f"\n✅ Successful: {successful}")
    print(f"❌ Failed: {failed}")
    
    if dry_run:
        print("\n⚠️  This was a DRY RUN. No changes were made.")
        print("   Run with --execute to apply changes.")
    
    return failed == 0


def update_all_references(workspace: Path, dry_run: bool = True, phase: int = 1):
    """Update all project references after migration."""
    
    print("\n" + "=" * 60)
    print(f"  Updating Project References - PHASE {phase}")
    print("=" * 60)
    
    engine = MigrationEngine(workspace, dry_run=dry_run)
    plan = create_migration_plan(phase)
    
    # Build a map of old -> new paths
    path_map = {Path(old).name: (old, new) for old, new in plan}
    
    # Find all .csproj files
    for csproj in workspace.rglob("*.csproj"):
        try:
            content = csproj.read_text(encoding='utf-8')
            updated = content
            changes = []
            
            for project_name, (old_path, new_path) in path_map.items():
                if f"{project_name}.csproj" in content:
                    # Calculate relative paths
                    csproj_dir = csproj.parent
                    
                    old_abs = (workspace / old_path / f"{project_name}.csproj")
                    new_abs = (workspace / new_path / f"{project_name}.csproj")
                    
                    try:
                        old_rel = os.path.relpath(old_abs, csproj_dir)
                        new_rel = os.path.relpath(new_abs, csproj_dir)
                        
                        if old_rel in content:
                            updated = updated.replace(old_rel, new_rel)
                            changes.append(f"{old_rel} -> {new_rel}")
                    except ValueError:
                        pass  # Different drives on Windows
            
            if changes and content != updated:
                rel_csproj = csproj.relative_to(workspace)
                print(f"\n📄 {rel_csproj}")
                for change in changes:
                    print(f"   {change}")
                
                if not dry_run:
                    csproj.write_text(updated, encoding='utf-8')
                    print("   ✅ Updated")
                    
        except Exception as e:
            print(f"⚠️  Error processing {csproj}: {e}")


def update_solution_files(workspace: Path, dry_run: bool = True, phase: int = 1):
    """Update all solution files after migration."""
    
    print("\n" + "=" * 60)
    print(f"  Updating Solution Files - PHASE {phase}")
    print("=" * 60)
    
    plan = create_migration_plan(phase)
    
    for sln in workspace.glob("*.sln"):
        print(f"\n📄 {sln.name}")
        
        try:
            content = sln.read_text(encoding='utf-8')
            updated = content
            changes = []
            
            for old_path, new_path in plan:
                # Normalize to backslashes for .sln files
                old_normalized = old_path.replace('/', '\\')
                new_normalized = new_path.replace('/', '\\')
                
                if old_normalized in content:
                    updated = updated.replace(old_normalized, new_normalized)
                    changes.append(f"{old_normalized} -> {new_normalized}")
            
            if changes:
                for change in changes:
                    print(f"   {change}")
                
                if not dry_run:
                    sln.write_text(updated, encoding='utf-8')
                    print("   ✅ Updated")
            else:
                print("   No changes needed")
                
        except Exception as e:
            print(f"⚠️  Error: {e}")


def main():
    import argparse
    
    parser = argparse.ArgumentParser(
        description="Migrate test projects to proper directory structure"
    )
    parser.add_argument(
        "--workspace",
        type=Path,
        default=Path(__file__).parent.parent.parent,
        help="Workspace root path"
    )
    parser.add_argument(
        "--execute",
        action="store_true",
        help="Actually execute the migration (default is dry-run)"
    )
    parser.add_argument(
        "--phase",
        type=int,
        default=1,
        choices=[1, 2, 3],
        help="Migration phase: 1=Unit tests, 2=Integration tests, 3=Helpers"
    )
    parser.add_argument(
        "--skip-move",
        action="store_true",
        help="Skip folder moves, only update references"
    )
    
    args = parser.parse_args()
    
    workspace = args.workspace.resolve()
    dry_run = not args.execute
    phase = args.phase
    
    # Show phase info
    phase_info = {
        1: "Unit tests (Moq only, no real DI)",
        2: "Integration tests (real DI containers)",
        3: "Helpers and complex cases"
    }
    print(f"\n🎯 Phase {phase}: {phase_info[phase]}")
    print(f"📋 Projects in this phase: {len(create_migration_plan(phase))}\n")
    
    if not args.skip_move:
        success = run_migration(workspace, dry_run, phase)
        if not success and not dry_run:
            print("\n❌ Migration failed!")
            return 1
    
    # Update references
    update_all_references(workspace, dry_run, phase)
    
    # Update solution files
    update_solution_files(workspace, dry_run, phase)
    
    if dry_run:
        print("\n" + "=" * 60)
        print(f"  DRY RUN COMPLETE - PHASE {phase}")
        print("=" * 60)
        print("\nTo execute the migration, run:")
        print(f"  python {__file__} --phase {phase} --execute")
    else:
        print("\n" + "=" * 60)
        print(f"  PHASE {phase} MIGRATION COMPLETE")
        print("=" * 60)
        print("\nNext steps:")
        print("  1. Run: dotnet build AutofacWpfDemo.sln")
        print("  2. Run: dotnet test AutofacWpfDemo.sln")
        print("  3. If successful, commit the changes")
        if phase < 3:
            print(f"  4. Continue with: python {__file__} --phase {phase + 1}")
    
    return 0


if __name__ == "__main__":
    sys.exit(main())
