#!/usr/bin/env python3
"""
MigrationTool Deployment Script
Builds and publishes Blazor Server, MAUI, and CLI applications.
"""

import argparse
import json
import os
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path
from typing import Optional


class DeployConfig:
    """Deployment configuration."""
    def __init__(self, version: str, configuration: str, platform: str):
        self.version = version
        self.configuration = configuration
        self.platform = platform  # 'blazor', 'maui', or 'cli'
        
        # Paths
        self.script_dir = Path(__file__).parent
        self.tools_dir = self.script_dir.parent
        self.solution_path = self.tools_dir / "MigrationTool.sln"
        
        # Project paths
        self.blazor_project = self.tools_dir / "src/MigrationTool/MigrationTool.Blazor.Server/MigrationTool.Blazor.Server.csproj"
        self.maui_project = self.tools_dir / "src/MigrationTool/MigrationTool.Maui/MigrationTool.Maui.csproj"
        self.cli_project = self.tools_dir / "src/PythonTools/MigrationTool.Cli/MigrationTool.Cli.csproj"
        
        # Output paths
        if platform == "cli":
            self.output_dir = self.script_dir / "bin" / "migration-tool-cli"
        else:
            self.output_dir = self.script_dir / platform.capitalize() / f"v{version}"


def print_banner(text: str, char: str = "="):
    """Print a banner."""
    print(f"\n{char * 60}")
    print(f"  {text}")
    print(f"{char * 60}\n")


def run_command(cmd: list[str], cwd: Optional[Path] = None) -> bool:
    """Run a command and return success status."""
    print(f"Running: {' '.join(cmd)}")
    result = subprocess.run(cmd, cwd=cwd, capture_output=False)
    return result.returncode == 0


def deploy_blazor(config: DeployConfig) -> bool:
    """Deploy Blazor Server application."""
    print_banner(f"Deploying MigrationTool Blazor Server v{config.version}")
    
    print(f"Configuration: {config.configuration}")
    print(f"Output: {config.output_dir}")
    
    # Verify project exists
    if not config.blazor_project.exists():
        print(f"❌ Error: Project not found at {config.blazor_project}")
        return False
    
    # Clean output directory
    if config.output_dir.exists():
        print(f"🧹 Cleaning output directory...")
        shutil.rmtree(config.output_dir)
    
    config.output_dir.mkdir(parents=True, exist_ok=True)
    
    # Build
    print("\n📦 Building solution...")
    if not run_command([
        "dotnet", "build",
        str(config.solution_path),
        "--configuration", config.configuration,
        "--no-incremental"
    ]):
        print("❌ Build failed!")
        return False
    
    print("✅ Build successful!")
    
    # Publish
    print("\n🚀 Publishing Blazor Server application...")
    if not run_command([
        "dotnet", "publish",
        str(config.blazor_project),
        "--configuration", config.configuration,
        "--output", str(config.output_dir),
        "--self-contained", "false",
        "--runtime", "win-x64",
        f"/p:Version={config.version}"
    ]):
        print("❌ Publish failed!")
        return False
    
    print("✅ Publish successful!")
    
    # Create deployment info
    deploy_info = {
        "version": config.version,
        "configuration": config.configuration,
        "platform": "Blazor Server",
        "build_date": datetime.now().isoformat(),
        "output_path": str(config.output_dir),
        "runtime": "win-x64",
        "self_contained": False
    }
    
    with open(config.output_dir / "deploy-info.json", "w") as f:
        json.dump(deploy_info, f, indent=2)
    
    # Create run script
    run_script = f"""@echo off
echo Starting MigrationTool Blazor Server v{config.version}
dotnet MigrationTool.Blazor.Server.dll
pause
"""
    with open(config.output_dir / "run.bat", "w") as f:
        f.write(run_script)
    
    return True


def deploy_cli(config: DeployConfig) -> bool:
    """Deploy CLI tool for Python integration."""
    print_banner(f"Deploying MigrationTool CLI v{config.version}")
    
    print(f"Configuration: {config.configuration}")
    print(f"Output: {config.output_dir}")
    
    # Verify project exists
    if not config.cli_project.exists():
        print(f"❌ Error: Project not found at {config.cli_project}")
        return False
    
    # Clean output directory
    if config.output_dir.exists():
        print(f"🧹 Cleaning output directory...")
        shutil.rmtree(config.output_dir)
    
    config.output_dir.mkdir(parents=True, exist_ok=True)
    
    # Build
    print("\n📦 Building CLI project...")
    if not run_command([
        "dotnet", "build",
        str(config.cli_project),
        "--configuration", config.configuration
    ]):
        print("❌ Build failed!")
        return False
    
    print("✅ Build successful!")
    
    # Publish - framework-dependent (smaller, requires .NET runtime)
    print("\n🚀 Publishing CLI application...")
    if not run_command([
        "dotnet", "publish",
        str(config.cli_project),
        "--configuration", config.configuration,
        "--output", str(config.output_dir),
        "--self-contained", "false",
        f"/p:Version={config.version}"
    ]):
        print("❌ Publish failed!")
        return False
    
    print("✅ Publish successful!")
    
    # Create deployment info
    deploy_info = {
        "version": config.version,
        "configuration": config.configuration,
        "platform": "CLI Tool",
        "build_date": datetime.now().isoformat(),
        "output_path": str(config.output_dir),
        "executable": "migration-tool-cli.exe" if os.name == "nt" else "migration-tool-cli",
        "self_contained": False
    }
    
    with open(config.output_dir / "deploy-info.json", "w") as f:
        json.dump(deploy_info, f, indent=2)
    
    return True


def deploy_maui(config: DeployConfig) -> bool:
    """Deploy MAUI desktop application."""
    print_banner(f"Deploying MigrationTool MAUI v{config.version}")
    
    print(f"Configuration: {config.configuration}")
    print(f"Output: {config.output_dir}")
    
    # Verify project exists
    if not config.maui_project.exists():
        print(f"❌ Error: Project not found at {config.maui_project}")
        return False
    
    # Clean output directory
    if config.output_dir.exists():
        print(f"🧹 Cleaning output directory...")
        shutil.rmtree(config.output_dir)
    
    config.output_dir.mkdir(parents=True, exist_ok=True)
    
    # Build
    print("\n📦 Building solution...")
    if not run_command([
        "dotnet", "build",
        str(config.solution_path),
        "--configuration", config.configuration,
        "--no-incremental"
    ]):
        print("❌ Build failed!")
        return False
    
    print("✅ Build successful!")
    
    # Publish for Windows
    print("\n🚀 Publishing MAUI application for Windows...")
    if not run_command([
        "dotnet", "publish",
        str(config.maui_project),
        "--configuration", config.configuration,
        "--output", str(config.output_dir),
        "--framework", "net9.0-windows10.0.19041.0",
        f"/p:Version={config.version}",
        "/p:RuntimeIdentifierOverride=win10-x64"
    ]):
        print("❌ Publish failed!")
        return False
    
    print("✅ Publish successful!")
    
    # Create deployment info
    deploy_info = {
        "version": config.version,
        "configuration": config.configuration,
        "platform": "MAUI Desktop",
        "build_date": datetime.now().isoformat(),
        "output_path": str(config.output_dir),
        "framework": "net9.0-windows10.0.19041.0",
        "runtime": "win10-x64"
    }
    
    with open(config.output_dir / "deploy-info.json", "w") as f:
        json.dump(deploy_info, f, indent=2)
    
    # Create run script
    run_script = f"""@echo off
echo Starting MigrationTool MAUI v{config.version}
start MigrationTool.Maui.exe
"""
    with open(config.output_dir / "run.bat", "w") as f:
        f.write(run_script)
    
    return True


def print_usage_instructions(config: DeployConfig):
    """Print usage instructions after deployment."""
    print_banner("Deployment Completed Successfully!", "=")
    
    print(f"📁 Output location: {config.output_dir}")
    print()
    
    if config.platform == "blazor":
        print("To run the Blazor Server application:")
        print(f"  cd {config.output_dir}")
        print("  dotnet MigrationTool.Blazor.Server.dll")
        print()
        print("The application will be available at:")
        print("  - HTTP:  http://localhost:5000")
        print("  - HTTPS: https://localhost:5001")
        print()
        print("For production deployment:")
        print("  - Configure as Windows Service (using NSSM)")
        print("  - Deploy to IIS with ASP.NET Core Hosting Bundle")
        print("  - Use reverse proxy (nginx, Apache)")
    
    elif config.platform == "maui":
        print("To run the MAUI application:")
        print(f"  cd {config.output_dir}")
        print("  Double-click run.bat")
        print("  Or execute MigrationTool.Maui.exe directly")
        print()
        print("System requirements:")
        print("  - Windows 10 version 1809 or later")
        print("  - .NET 9.0 Runtime")
    
    elif config.platform == "cli":
        print("CLI tool deployed for Python integration.")
        print()
        print("Usage from command line:")
        print(f"  {config.output_dir / 'migration-tool-cli'} --help")
        print()
        print("Available commands:")
        print("  analyze-solution     Analyze a .NET solution")
        print("  update-namespace     Update namespace in C# files")
        print("  update-project-refs  Update project references in .csproj")
        print("  find-usages          Find all usages of a symbol")
        print()
        print("Python integration:")
        print("  The CLI is automatically used by scripts/migration_tool/")
    
    print()


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Deploy MigrationTool Blazor Server, MAUI, or CLI application"
    )
    parser.add_argument(
        "--platform",
        choices=["blazor", "maui", "cli", "all"],
        default="blazor",
        help="Platform to deploy (default: blazor)"
    )
    parser.add_argument(
        "--version",
        default="1.0.0",
        help="Version number (default: 1.0.0)"
    )
    parser.add_argument(
        "--configuration",
        choices=["Debug", "Release"],
        default="Release",
        help="Build configuration (default: Release)"
    )
    
    args = parser.parse_args()
    
    success = True
    
    # Deploy CLI first (used by Python migration tool)
    if args.platform in ("cli", "all"):
        config = DeployConfig(args.version, args.configuration, "cli")
        if not deploy_cli(config):
            success = False
        else:
            print_usage_instructions(config)
    
    if args.platform in ("blazor", "all"):
        config = DeployConfig(args.version, args.configuration, "blazor")
        if not deploy_blazor(config):
            success = False
        else:
            print_usage_instructions(config)
    
    if args.platform in ("maui", "all"):
        config = DeployConfig(args.version, args.configuration, "maui")
        if not deploy_maui(config):
            success = False
        else:
            print_usage_instructions(config)
    
    if success:
        print("\n✅ All deployments completed successfully!")
        return 0
    else:
        print("\n❌ Some deployments failed!")
        return 1


if __name__ == "__main__":
    sys.exit(main())
