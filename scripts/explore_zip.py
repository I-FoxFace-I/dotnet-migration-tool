#!/usr/bin/env python3
"""
Simple utility to explore ZIP archives containing test fixtures.
Usage: python explore_zip.py <path_to_zip> [--extract <output_dir>]
"""

import argparse
import zipfile
import sys
from pathlib import Path
from collections import defaultdict


def explore_zip(zip_path: str, max_entries: int = 100) -> None:
    """Display the structure and contents of a ZIP archive."""
    
    path = Path(zip_path)
    if not path.exists():
        print(f"❌ File not found: {zip_path}")
        sys.exit(1)
    
    size_mb = path.stat().st_size / (1024 * 1024)
    print(f"📦 ZIP Archive: {path.name}")
    print(f"   Size: {size_mb:.2f} MB")
    print()
    
    with zipfile.ZipFile(path, 'r') as zf:
        entries = zf.namelist()
        print(f"📊 Total entries: {len(entries)}")
        print()
        
        # Group by extension
        extensions = defaultdict(int)
        for entry in entries:
            if not entry.endswith('/'):
                ext = Path(entry).suffix.lower() or '(no extension)'
                extensions[ext] += 1
        
        print("📁 File types:")
        for ext, count in sorted(extensions.items(), key=lambda x: -x[1])[:15]:
            print(f"   {ext}: {count}")
        print()
        
        # Find solution files
        sln_files = [e for e in entries if e.endswith('.sln')]
        if sln_files:
            print("🔷 Solution files (.sln):")
            for sln in sln_files:
                print(f"   {sln}")
            print()
        
        # Find project files
        csproj_files = [e for e in entries if e.endswith('.csproj')]
        if csproj_files:
            print(f"📘 Project files (.csproj): {len(csproj_files)}")
            for proj in csproj_files[:20]:
                print(f"   {proj}")
            if len(csproj_files) > 20:
                print(f"   ... and {len(csproj_files) - 20} more")
            print()
        
        # Show top-level structure
        print("📂 Top-level structure:")
        top_level = set()
        for entry in entries:
            parts = entry.split('/')
            if len(parts) >= 2:
                top_level.add(f"{parts[0]}/{parts[1]}")
            elif parts[0]:
                top_level.add(parts[0])
        
        for item in sorted(top_level)[:30]:
            print(f"   {item}")
        if len(top_level) > 30:
            print(f"   ... and {len(top_level) - 30} more")


def extract_zip(zip_path: str, output_dir: str) -> None:
    """Extract ZIP archive to specified directory."""
    
    path = Path(zip_path)
    output = Path(output_dir)
    
    if not path.exists():
        print(f"❌ File not found: {zip_path}")
        sys.exit(1)
    
    print(f"📦 Extracting: {path.name}")
    print(f"📂 To: {output}")
    
    output.mkdir(parents=True, exist_ok=True)
    
    with zipfile.ZipFile(path, 'r') as zf:
        zf.extractall(output)
    
    print(f"✅ Extracted {len(list(output.rglob('*')))} files")


def main():
    parser = argparse.ArgumentParser(
        description="Explore or extract ZIP archives containing test fixtures"
    )
    parser.add_argument("zip_path", help="Path to the ZIP archive")
    parser.add_argument(
        "--extract", "-e",
        metavar="OUTPUT_DIR",
        help="Extract to specified directory"
    )
    parser.add_argument(
        "--max-entries", "-m",
        type=int,
        default=100,
        help="Maximum entries to display (default: 100)"
    )
    
    args = parser.parse_args()
    
    if args.extract:
        extract_zip(args.zip_path, args.extract)
    else:
        explore_zip(args.zip_path, args.max_entries)


if __name__ == "__main__":
    main()
