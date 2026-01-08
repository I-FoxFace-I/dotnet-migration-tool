# DotNet Migration Tool

Interactive Streamlit application for migrating and reorganizing .NET projects.

## Features

- 📁 **Browse** project structure (solutions, projects, files)
- 🔍 **Scan** C# files for classes, interfaces, and tests
- 🔀 **Plan** file migrations with visual interface
- 📝 **Fix** namespaces automatically
- ✅ **Verify** changes before and after migration

## Installation

```bash
cd scripts/migration_tool
pip install -r requirements.txt
```

## Usage

```bash
cd scripts/migration_tool
python -m streamlit run app.py --server.port 8502
```

Then open http://localhost:8502 in your browser.

## Project Structure

```
scripts/migration_tool/
├── app.py                      # Main Streamlit application
├── requirements.txt            # Python dependencies
├── README.md                   # This file
├── DEVELOPMENT_PLAN.md         # Development roadmap
├── SPECIFICATION.md            # Requirements specification
├── UI_DESIGN.md                # UI wireframes
│
├── core/                       # Core business logic
│   ├── solution_parser.py      # .sln file parsing
│   ├── project_parser.py       # .csproj file parsing
│   └── file_scanner.py         # C# file scanning
│
├── models/                     # Data models
│   ├── solution.py             # Solution, Project models
│   └── file_info.py            # FileInfo, ClassInfo models
│
├── ui/                         # Streamlit UI components
│   ├── sidebar.py              # Sidebar configuration
│   ├── dashboard.py            # Dashboard view
│   ├── project_explorer.py     # Project tree view
│   └── migration_planner.py    # Migration planning UI
│
├── utils/                      # Utilities
│   ├── logging_config.py       # Logging setup
│   └── file_utils.py           # File operations
│
└── tests/                      # Test suite
    ├── test_solution_parser.py
    ├── test_project_parser.py
    ├── test_file_scanner.py
    └── run_tests.py            # Test runner
```

## Running Tests

```bash
cd scripts/migration_tool
python tests/run_tests.py
```

## Workflow

1. **Load Solution** - Enter path to .sln file and click "Load"
2. **Browse Projects** - Use Project Explorer to view structure
3. **Select Files** - Check files you want to migrate
4. **Plan Migration** - Set target project and namespace mappings
5. **Execute** - Run migration with automatic namespace fixes

## Status

🚧 **MVP in development**

- ✅ Solution parsing
- ✅ Project parsing
- ✅ File scanning (classes, tests)
- ✅ Basic UI (dashboard, explorer, planner)
- ⏳ Migration execution
- ⏳ Git integration
- ⏳ Namespace fixing

## License

Part of the Autofac sandbox repository.
