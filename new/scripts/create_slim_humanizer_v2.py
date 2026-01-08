#!/usr/bin/env python3
"""
Creates a slim version of Humanizer ZIP for testing.
- Only includes src/ folder
- Removes net10.0 from TargetFrameworks (not yet released)
- Excludes tests, benchmarks, docs
"""

import zipfile
import os
import re
from pathlib import Path

def main():
    # Find paths
    script_dir = Path(__file__).parent
    repo_root = script_dir.parent.parent
    
    source_zip = repo_root / "datasets" / "test-fixtures" / "Humanizer-main.zip"
    target_zip = repo_root / "datasets" / "test-fixtures" / "Humanizer-slim.zip"
    
    if not source_zip.exists():
        print(f"Source ZIP not found: {source_zip}")
        return
    
    # Folders to include (relative to Humanizer-main/)
    include_prefixes = [
        "Humanizer-main/src/Humanizer/",
        "Humanizer-main/src/Humanizer.Analyzers/",
    ]
    
    # Files to include from root
    include_root_files = [
        "Humanizer-main/Directory.Build.props",
        "Humanizer-main/Directory.Packages.props",
    ]
    
    print(f"Creating slim Humanizer ZIP...")
    print(f"Source: {source_zip}")
    print(f"Target: {target_zip}")
    
    with zipfile.ZipFile(source_zip, 'r') as source:
        with zipfile.ZipFile(target_zip, 'w', zipfile.ZIP_DEFLATED) as target:
            for item in source.namelist():
                # Skip directories
                if item.endswith('/'):
                    continue
                
                # Check if file should be included
                include = False
                
                # Check prefixes
                for prefix in include_prefixes:
                    if item.startswith(prefix):
                        include = True
                        break
                
                # Check root files
                if item in include_root_files:
                    include = True
                
                if not include:
                    continue
                
                # Read content
                content = source.read(item)
                
                # Modify .csproj files to remove net10.0
                if item.endswith('.csproj'):
                    content_str = content.decode('utf-8')
                    
                    # Remove net10.0 from TargetFrameworks
                    # Pattern: net10.0; or ;net10.0 or just net10.0
                    original = content_str
                    content_str = re.sub(r'net10\.0;?', '', content_str)
                    content_str = re.sub(r';net10\.0', '', content_str)
                    
                    if original != content_str:
                        print(f"  Modified: {item} (removed net10.0)")
                    
                    content = content_str.encode('utf-8')
                
                # Write to target
                target.writestr(item, content)
                print(f"  Added: {item}")
    
    # Show result
    target_size = target_zip.stat().st_size / 1024
    print(f"\nCreated: {target_zip}")
    print(f"Size: {target_size:.1f} KB")

if __name__ == "__main__":
    main()
