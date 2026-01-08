#!/usr/bin/env python3
"""Analyze src folder in Humanizer ZIP."""

import zipfile
from pathlib import Path
from collections import defaultdict

zip_path = "datasets/test-fixtures/Humanizer-main.zip"

with zipfile.ZipFile(zip_path, 'r') as zf:
    src_folders = defaultdict(lambda: {'count': 0, 'size': 0})
    
    for entry in zf.infolist():
        if '/src/' in entry.filename:
            parts = entry.filename.split('/')
            src_idx = parts.index('src') if 'src' in parts else -1
            if src_idx >= 0 and len(parts) > src_idx + 1:
                folder = parts[src_idx + 1]
                src_folders[folder]['count'] += 1
                src_folders[folder]['size'] += entry.file_size
    
    print("src/ folder analysis:")
    print("-" * 60)
    total = 0
    for folder, data in sorted(src_folders.items(), key=lambda x: -x[1]['size']):
        size_kb = data['size'] / 1024
        total += data['size']
        count = data['count']
        print(f"  {folder:40} {count:4} files  {size_kb:7.1f} KB")
    print("-" * 60)
    print(f"  TOTAL: {total / 1024:.1f} KB")
