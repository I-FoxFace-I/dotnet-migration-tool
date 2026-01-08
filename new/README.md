# MigrationTool

**A powerful .NET project migration and refactoring tool with CLI, Web, and Desktop interfaces**

MigrationTool helps you analyze, reorganize, and migrate .NET projects with ease. Built with Roslyn for accurate code analysis, MSBuild for project manipulation, and available as CLI, web application (Blazor Server), and desktop application (MAUI/WPF).

## Features

### 🔍 Analysis
- **Solution Graph** - Model entire solution as a dependency graph (projects, files, types, namespaces)
- **Impact Analysis** - Predict what will be affected by move/rename/delete operations
- **Solution Parsing** - Parse .sln files and discover all projects
- **Project Analysis** - Analyze .csproj files, dependencies, and references
- **Code Analysis** - Use Roslyn to analyze C# source code
- **Test Detection** - Automatically identify test projects and test methods (xUnit, NUnit, MSTest)

### 📊 Visualization
- **Dashboard** - Overview of solution statistics
- **Project Explorer** - Browse projects, files, and code structure
- **Dependency Graph** - Visualize project and type dependencies

### 🔄 Migration Operations
- **File Operations** - Move, copy, rename, delete files with auto-namespace updates
- **Folder Operations** - Move, copy, delete folders recursively
- **Namespace Refactoring** - Automatically update namespaces using Roslyn
- **Class Renaming** - Rename types including constructors
- **Cross-Solution Migration** - Migrate code between different solutions
- **Using Management** - Add/remove using directives

### 🛠️ CLI Tool
- **analyze-solution** - Analyze solution structure
- **analyze-graph** - Build and display solution dependency graph
- **analyze-impact** - Predict impact of migration operations
- **move-file/folder** - Move files/folders with namespace updates
- **copy-file/folder** - Copy files/folders
- **rename-file** - Rename files with optional class renaming
- **update-namespace** - Update namespaces in files
- **find-usages** - Find where types/symbols are used

### 🌐 Internationalization
- **4 Languages** - English, Czech, Polish, Ukrainian
- **Runtime Language Switching** - Change language without restart

## Architecture

```
tools/
├── src/MigrationTool/
│   ├── MigrationTool.Core.Abstractions/   # Interfaces, Models, Graph types
│   ├── MigrationTool.Core/                # Business Logic (Roslyn, MSBuild)
│   │   ├── Graph/                         # SolutionGraph, ImpactAnalyzer
│   │   ├── Services/                      # File, Migration services
│   │   └── Rewriters/                     # Roslyn syntax rewriters
│   ├── MigrationTool.Cli/                 # Command-line interface
│   ├── MigrationTool.Localization/        # i18n Resources
│   ├── MigrationTool.Blazor.Server/       # Web UI
│   ├── MigrationTool.Maui/                # Desktop UI (MAUI)
│   └── MigrationTool.Wpf/                 # Desktop UI (WPF)
│
├── tests/MigrationTool/
│   ├── MigrationTool.Cli.Tests/           # CLI & Graph tests
│   ├── MigrationTool.Core.Tests/          # Unit & Integration tests
│   ├── MigrationTool.Tests.Infrastructure/ # Test helpers & fixtures
│   └── ...
│
└── scripts/
    ├── sync_to_migration_tool_repo.py     # Sync to standalone repo
    └── create_slim_humanizer_v2.py        # Test fixture generator
```

### Core Components

| Component | Description |
|-----------|-------------|
| **SolutionGraph** | In-memory graph of solution dependencies |
| **SolutionGraphBuilder** | Builds graph using MSBuildWorkspace + Roslyn |
| **ImpactAnalyzer** | Analyzes impact of move/rename/delete operations |
| **FileOperationService** | File move/copy/delete with namespace updates |
| **CrossSolutionMigrationService** | Migrate between different solutions |
| **NamespaceRewriter** | Roslyn rewriter for namespace changes |
| **ClassRenamer** | Roslyn rewriter for type renaming |

### Graph Model

```
SolutionGraph
├── Solutions (SolutionNode)
├── Projects (ProjectNode)
├── Files (FileNode)
├── Types (TypeNode) - classes, interfaces, records, enums
├── Namespaces (NamespaceNode)
├── Packages (PackageNode)
└── Edges
    ├── ProjectReferenceEdge
    ├── PackageReferenceEdge
    ├── TypeInheritsEdge
    ├── TypeImplementsEdge
    ├── FileContainsTypeEdge
    └── ...
```

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or Rider (optional, for development)
- Windows 10/11 (for MAUI/WPF desktop apps)

### Building from Source

```bash
cd tools
dotnet build MigrationTool.sln
```

### Running CLI

```bash
cd tools/src/MigrationTool/MigrationTool.Cli
dotnet run -- --help

# Examples:
dotnet run -- analyze-solution "C:\path\to\solution.sln"
dotnet run -- analyze-graph "C:\path\to\solution.sln" --output graph.json
dotnet run -- analyze-impact "C:\path\to\solution.sln" --type "MyNamespace.MyClass" --operation move --target "NewNamespace"
dotnet run -- move-file "source.cs" "target.cs" --update-namespace
```

### Running Tests

```bash
cd tools
dotnet test MigrationTool.sln
```

**Test Results:**
- 85+ tests in CLI.Tests (including Humanizer integration tests)
- Uses real Humanizer project from ZIP for realistic testing
- Graph analysis tested on 271 types, 236 files, 26 namespaces

### Running Blazor Server

```bash
cd tools/src/MigrationTool/MigrationTool.Blazor.Server
dotnet run
```

Then open `http://localhost:5000` in your browser.

### Running MAUI Desktop

```bash
cd tools/src/MigrationTool/MigrationTool.Maui
dotnet run
```

## CLI Commands

### Analysis Commands

```bash
# Analyze solution structure
migration-tool analyze-solution <solution-path>

# Build dependency graph
migration-tool analyze-graph <solution-path> [--output graph.json]

# Analyze impact of operations
migration-tool analyze-impact <solution-path> --type <type-name> --operation <move|rename|delete>
```

### File Operations

```bash
# Move file with namespace update
migration-tool move-file <source> <target> [--update-namespace] [--dry-run]

# Copy file
migration-tool copy-file <source> <target> [--update-namespace]

# Rename file (optionally rename class too)
migration-tool rename-file <path> <new-name> [--rename-class]

# Delete file (with reference check)
migration-tool delete-file <path> [--check-references]
```

### Folder Operations

```bash
# Move folder recursively
migration-tool move-folder <source> <target> [--update-namespace <old> <new>]

# Copy folder
migration-tool copy-folder <source> <target>
```

### Namespace Operations

```bash
# Update namespace in files
migration-tool update-namespace <path> --old <old-ns> --new <new-ns>

# Find usages of a type
migration-tool find-usages <solution-path> --type <type-name>
```

## Test Fixtures

Tests use real open-source projects for realistic validation:

### Humanizer Project
- **Source:** [Humanizer GitHub](https://github.com/Humanizr/Humanizer)
- **Location:** `datasets/test-fixtures/Humanizer-slim.zip`
- **Size:** 271 types, 236 files, 26 namespaces
- **Usage:** Graph building, impact analysis testing

```csharp
// In tests
using var fixture = TestProjectFixture.Humanizer();
var graph = await builder.BuildGraphAsync(fixture.EntryPoint);
// Graph contains real Humanizer project structure
```

## Technology Stack

- **.NET 9.0** - Latest .NET framework
- **Roslyn** - Microsoft.CodeAnalysis for C# parsing and rewriting
- **MSBuild** - Microsoft.Build for project file manipulation
- **System.CommandLine** - CLI framework
- **Blazor Server** - Web UI with SignalR
- **.NET MAUI** - Cross-platform desktop UI
- **xUnit** - Testing framework
- **FluentAssertions** - Test assertions

## Project Status

### ✅ Completed
- Core analyzers (Solution, Project, Code)
- Solution Graph model and builder
- Impact Analyzer
- File/Folder operations with namespace updates
- Cross-solution migration
- CLI interface
- Humanizer test fixtures
- 85+ tests

### 🔜 Planned
- React frontend (see ROADMAP.md)
- Reference updates after type moves
- Execution plan with rollback
- Full semantic analysis

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

This project is part of the Autofac sandbox repository.

## Roadmap

See [ROADMAP.md](ROADMAP.md) for planned features and improvements.

---

**Built with ❤️ using .NET 9.0, Roslyn, and MSBuild**
