#!/usr/bin/env python3
"""
Create a slim version of Humanizer ZIP for testing.
Removes tests, benchmarks, docs, and other unnecessary files.
"""

import zipfile
from pathlib import Path

INPUT_ZIP = "datasets/test-fixtures/Humanizer-main.zip"
OUTPUT_ZIP = "datasets/test-fixtures/Humanizer-slim.zip"

# Patterns to exclude
EXCLUDE_PATTERNS = [
    # Test projects
    "/Humanizer.Tests/",
    "/Humanizer.Analyzers.Tests/",
    "/Benchmarks/",
    # Documentation
    "/docs/",
    "/NuSpecs/",
    "readme.md",
    "release_notes.md",
    "AGENTS.md",
    # CI/CD
    "/.github/",
    "azure-pipelines.yml",
    # Other
    "logo.png",
    "verify-packages.ps1",
    ".gitattributes",
]

def should_include(filename: str) -> bool:
    """Check if file should be included in slim version."""
    lower = filename.lower()
    for pattern in EXCLUDE_PATTERNS:
        if pattern.lower() in lower:
            return False
    return True

def main():
    input_path = Path(INPUT_ZIP)
    output_path = Path(OUTPUT_ZIP)
    
    if not input_path.exists():
        print(f"❌ Input ZIP not found: {INPUT_ZIP}")
        return
    
    included = 0
    excluded = 0
    original_size = 0
    new_size = 0
    
    with zipfile.ZipFile(input_path, 'r') as zf_in:
        with zipfile.ZipFile(output_path, 'w', zipfile.ZIP_DEFLATED) as zf_out:
            for entry in zf_in.infolist():
                original_size += entry.file_size
                
                if should_include(entry.filename):
                    # Copy entry to output
                    data = zf_in.read(entry.filename)
                    zf_out.writestr(entry, data)
                    included += 1
                    new_size += entry.file_size
                else:
                    excluded += 1
    
    # Report
    print(f"✅ Created slim ZIP: {output_path}")
    print(f"   Files: {included} included, {excluded} excluded")
    print(f"   Size: {original_size / 1024:.1f} KB → {new_size / 1024:.1f} KB")
    print(f"   Reduction: {(1 - new_size / original_size) * 100:.1f}%")
    
    # Show actual file sizes
    orig_file_size = input_path.stat().st_size / 1024
    new_file_size = output_path.stat().st_size / 1024
    print(f"   ZIP file: {orig_file_size:.1f} KB → {new_file_size:.1f} KB")

if __name__ == "__main__":
    main()
