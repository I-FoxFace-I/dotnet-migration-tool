#!/usr/bin/env python3
"""Analyze Humanizer ZIP to see what can be removed."""

import zipfile
from pathlib import Path
from collections import defaultdict

zip_path = "datasets/test-fixtures/Humanizer-main.zip"

with zipfile.ZipFile(zip_path, 'r') as zf:
    # Group by top-level folder
    folders = defaultdict(lambda: {'count': 0, 'size': 0, 'extensions': defaultdict(int)})
    
    for entry in zf.infolist():
        parts = entry.filename.split('/')
        if len(parts) >= 2:
            folder = parts[1] if parts[0] == 'Humanizer-main' else parts[0]
            folders[folder]['count'] += 1
            folders[folder]['size'] += entry.file_size
            ext = Path(entry.filename).suffix.lower()
            folders[folder]['extensions'][ext] += 1
    
    print("Folder analysis:")
    print("-" * 70)
    total_size = 0
    for folder, data in sorted(folders.items(), key=lambda x: -x[1]['size']):
        size_kb = data['size'] / 1024
        total_size += data['size']
        exts = ", ".join(f"{k}:{v}" for k, v in sorted(data['extensions'].items(), key=lambda x: -x[1])[:3])
        print(f"  {folder:30} {data['count']:4} files  {size_kb:8.1f} KB  [{exts}]")
    
    print("-" * 70)
    print(f"  TOTAL: {total_size / 1024:.1f} KB")
