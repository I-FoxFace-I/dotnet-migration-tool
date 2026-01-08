# MigrationTool

**A powerful .NET project migration and refactoring tool with dual UI (Web & Desktop)**

MigrationTool helps you analyze, reorganize, and migrate .NET projects with ease. Built with Roslyn for accurate code analysis and available as both a web application (Blazor Server) and desktop application (MAUI).

## Features

### 🔍 Analysis
- **Solution Parsing** - Parse .sln files and discover all projects
- **Project Analysis** - Analyze .csproj files, dependencies, and references
- **Code Analysis** - Use Roslyn to analyze C# source code
- **Test Detection** - Automatically identify test projects and test methods (xUnit, NUnit, MSTest)

### 📊 Visualization
- **Dashboard** - Overview of solution statistics
- **Project Explorer** - Browse projects, files, and code structure
- **Dependency Graph** - Visualize project dependencies (coming soon)

### 🔄 Migration
- **Migration Planner** - Plan complex refactoring operations
- **File Operations** - Move, copy, rename files and folders
- **Namespace Refactoring** - Automatically update namespaces
- **Project Creation** - Create new projects from templates

### 🌐 Internationalization
- **4 Languages** - English, Czech, Polish, Ukrainian
- **Runtime Language Switching** - Change language without restart

## Architecture

```
tools/
├── src/MigrationTool/
│   ├── MigrationTool.Core.Abstractions/   # Interfaces & Models
│   ├── MigrationTool.Core/                # Business Logic (Roslyn)
│   ├── MigrationTool.Localization/        # i18n Resources
│   ├── MigrationTool.Blazor.Server/       # Web UI
│   └── MigrationTool.Maui/                # Desktop UI
│
└── tests/MigrationTool/
    ├── MigrationTool.Core.Tests/          # Unit & Integration Tests
    ├── MigrationTool.Maui.Tests/          # MAUI ViewModel Tests
    ├── MigrationTool.Blazor.Server.Tests/ # Blazor Component Tests
    └── MigrationTool.Tests.Infrastructure/ # Test Helpers
```

### Shared Core

Both UIs share the same **Core** library:
- **Analyzers**: `SolutionAnalyzer`, `ProjectAnalyzer`, `CodeAnalyzer`
- **Services**: `IFileSystemService`, `IMigrationPlanner`, `IMigrationExecutor`
- **Models**: `SolutionInfo`, `ProjectInfo`, `SourceFileInfo`, `MigrationPlan`

### Dual UI

| UI | Technology | Use Case |
|----|-----------|----------|
| **Blazor Server** | ASP.NET Core | Web-based, accessible from any browser, can be hosted on server |
| **MAUI** | .NET MAUI (XAML) | Native desktop app for Windows (macOS support planned) |

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or Rider (optional, for development)
- Windows 10/11 (for MAUI desktop app)

### Building from Source

```bash
cd tools
dotnet build MigrationTool.sln
```

### Running Tests

```bash
dotnet test MigrationTool.sln
```

**Test Results:**
- 160+ unit and integration tests
- Core, ViewModels, Components, Services coverage
- Uses real file system for integration tests

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

Or use Visual Studio's built-in run button.

## Deployment

Use the Python deployment script to create release builds:

```bash
cd tools/Deploy

# Deploy Blazor Server
python deploy.py --platform blazor --version 1.0.0

# Deploy MAUI Desktop
python deploy.py --platform maui --version 1.0.0

# Deploy both
python deploy.py --platform all --version 1.0.0
```

Output will be in `tools/Deploy/Blazor/v{version}/` and `tools/Deploy/Maui/v{version}/`.

See [Deploy/README.md](Deploy/README.md) for detailed deployment instructions.

## Usage

### 1. Load a Solution

**Blazor**: Navigate to Settings, enter workspace path, select solution  
**MAUI**: Click "Open Solution" button, browse to .sln file

### 2. Explore Projects

Navigate to **Project Explorer** to:
- View all projects in the solution
- Browse source files
- Inspect classes and tests
- Analyze project structure

### 3. Plan Migration

Navigate to **Migration Planner** to:
- Create a new migration plan
- Add migration steps (move file, create project, rename namespace, etc.)
- Validate the plan
- Execute the migration

## Technology Stack

- **.NET 9.0** - Latest .NET framework
- **Roslyn** - Microsoft.CodeAnalysis for C# parsing
- **Blazor Server** - Web UI with SignalR
- **.NET MAUI** - Cross-platform desktop UI
- **CommunityToolkit.Mvvm** - MVVM helpers for MAUI
- **xUnit** - Testing framework
- **bUnit** - Blazor component testing
- **FluentAssertions** - Test assertions

## Project Structure

### Core Projects

- **Core.Abstractions** - Interfaces and contracts
- **Core** - Business logic, analyzers, services
- **Localization** - i18n resources (EN, CS, PL, UK)

### UI Projects

- **Blazor.Server** - Web-based UI (ASP.NET Core)
- **Maui** - Desktop UI (XAML)

### Test Projects

- **Core.Tests** - 60+ unit tests for analyzers and services
- **Maui.Tests** - 60+ tests for ViewModels
- **Blazor.Server.Tests** - 28+ tests for components
- **Tests.Infrastructure** - Shared test utilities

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

## Support

For issues, questions, or feature requests, please open an issue on GitHub.

---

**Built with ❤️ using .NET 9.0 and Roslyn**
