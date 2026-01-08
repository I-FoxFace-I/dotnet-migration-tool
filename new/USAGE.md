# MigrationTool Usage Guide

Step-by-step guide to using MigrationTool for .NET project migration and refactoring.

## Quick Start

### CLI (Recommended)

```bash
cd tools/src/MigrationTool/MigrationTool.Cli

# Analyze a solution
dotnet run -- analyze-solution "C:\path\to\solution.sln"

# Build dependency graph
dotnet run -- analyze-graph "C:\path\to\solution.sln"

# Check impact before moving a type
dotnet run -- analyze-impact "C:\path\to\solution.sln" --type "MyApp.Services.UserService" --operation move
```

### Web UI (Blazor)

```bash
cd tools/src/MigrationTool/MigrationTool.Blazor.Server
dotnet run
# Open http://localhost:5000
```

### Desktop (MAUI/WPF)

```bash
cd tools/src/MigrationTool/MigrationTool.Maui
dotnet run
```

---

## CLI Commands Reference

### Analysis Commands

#### `analyze-solution`
Analyze solution structure and display statistics.

```bash
migration-tool analyze-solution <solution-path> [options]

Options:
  --json          Output as JSON
  --verbose       Show detailed information

Examples:
  migration-tool analyze-solution "C:\Projects\MyApp.sln"
  migration-tool analyze-solution "C:\Projects\MyApp.sln" --json
```

#### `analyze-graph`
Build a dependency graph of the solution.

```bash
migration-tool analyze-graph <solution-path> [options]

Options:
  --output <file>   Save graph to JSON file
  --fast            Skip type usage analysis (faster)
  --verbose         Show detailed progress

Examples:
  migration-tool analyze-graph "C:\Projects\MyApp.sln"
  migration-tool analyze-graph "C:\Projects\MyApp.sln" --output graph.json
```

**Output includes:**
- Projects and their references
- Files and their types
- Namespaces
- Inheritance relationships
- Interface implementations

#### `analyze-impact`
Predict what will be affected by a migration operation.

```bash
migration-tool analyze-impact <solution-path> [options]

Options:
  --type <name>       Type to analyze (fully qualified)
  --file <path>       File to analyze
  --namespace <name>  Namespace to analyze
  --operation <op>    Operation: move, rename, delete
  --target <name>     Target location/name (for move/rename)

Examples:
  # What happens if I move UserService?
  migration-tool analyze-impact "MyApp.sln" --type "MyApp.Services.UserService" --operation move --target "MyApp.Core.Services"
  
  # What happens if I rename a namespace?
  migration-tool analyze-impact "MyApp.sln" --namespace "MyApp.Old" --operation rename --target "MyApp.New"
  
  # What happens if I delete this file?
  migration-tool analyze-impact "MyApp.sln" --file "Services/LegacyService.cs" --operation delete
```

**Output includes:**
- Affected files count
- Affected types
- Required namespace updates
- Breaking changes warnings

---

### File Operations

#### `move-file`
Move a file with optional namespace update.

```bash
migration-tool move-file <source> <target> [options]

Options:
  --update-namespace    Update namespace to match new location
  --dry-run            Show what would happen without making changes

Examples:
  migration-tool move-file "Services/UserService.cs" "Core/Services/UserService.cs" --update-namespace
  migration-tool move-file "Old/Helper.cs" "New/Helper.cs" --dry-run
```

#### `copy-file`
Copy a file with optional namespace update.

```bash
migration-tool copy-file <source> <target> [options]

Options:
  --update-namespace    Update namespace in the copy
  --dry-run            Show what would happen

Examples:
  migration-tool copy-file "Template.cs" "NewFeature/Template.cs" --update-namespace
```

#### `rename-file`
Rename a file and optionally the class inside.

```bash
migration-tool rename-file <path> <new-name> [options]

Options:
  --rename-class    Also rename the primary class to match filename
  --dry-run        Show what would happen

Examples:
  migration-tool rename-file "UserService.cs" "UserManager.cs" --rename-class
```

#### `delete-file`
Delete a file with optional reference checking.

```bash
migration-tool delete-file <path> [options]

Options:
  --check-references    Warn if file is referenced elsewhere
  --force              Delete even if referenced
  --dry-run            Show what would happen

Examples:
  migration-tool delete-file "Legacy/OldService.cs" --check-references
```

---

### Folder Operations

#### `move-folder`
Move a folder recursively with namespace updates.

```bash
migration-tool move-folder <source> <target> [options]

Options:
  --old-namespace <ns>    Old namespace prefix
  --new-namespace <ns>    New namespace prefix
  --dry-run              Show what would happen

Examples:
  migration-tool move-folder "Services/Users" "Core/Users" --old-namespace "MyApp.Services.Users" --new-namespace "MyApp.Core.Users"
```

#### `copy-folder`
Copy a folder recursively.

```bash
migration-tool copy-folder <source> <target> [options]

Options:
  --old-namespace <ns>    Old namespace prefix (for updates)
  --new-namespace <ns>    New namespace prefix
  --dry-run              Show what would happen

Examples:
  migration-tool copy-folder "Templates" "NewProject/Templates"
```

---

### Namespace Operations

#### `update-namespace`
Update namespace declarations in files.

```bash
migration-tool update-namespace <path> [options]

Options:
  --old <namespace>    Old namespace to replace
  --new <namespace>    New namespace
  --recursive         Process subdirectories
  --dry-run          Show what would happen

Examples:
  migration-tool update-namespace "Services/" --old "MyApp.Services" --new "MyApp.Core.Services" --recursive
```

#### `find-usages`
Find where a type or symbol is used.

```bash
migration-tool find-usages <solution-path> [options]

Options:
  --type <name>       Type to find (fully qualified)
  --symbol <name>     Symbol to find
  --json             Output as JSON

Examples:
  migration-tool find-usages "MyApp.sln" --type "MyApp.Services.IUserService"
```

---

## Common Workflows

### Workflow 1: Reorganize Project Structure

**Goal:** Move services from `MyApp.Web/Services` to `MyApp.Core/Services`

```bash
# 1. Analyze impact first
migration-tool analyze-impact "MyApp.sln" --namespace "MyApp.Web.Services" --operation move --target "MyApp.Core.Services"

# 2. If impact is acceptable, do dry-run
migration-tool move-folder "MyApp.Web/Services" "MyApp.Core/Services" \
  --old-namespace "MyApp.Web.Services" \
  --new-namespace "MyApp.Core.Services" \
  --dry-run

# 3. Execute the move
migration-tool move-folder "MyApp.Web/Services" "MyApp.Core/Services" \
  --old-namespace "MyApp.Web.Services" \
  --new-namespace "MyApp.Core.Services"

# 4. Build to verify
dotnet build MyApp.sln
```

### Workflow 2: Extract Shared Code

**Goal:** Copy utility classes to a shared library

```bash
# 1. Build graph to understand dependencies
migration-tool analyze-graph "MyApp.sln" --output graph.json

# 2. Copy utilities to shared project
migration-tool copy-folder "MyApp.Web/Utilities" "MyApp.Shared/Utilities" \
  --old-namespace "MyApp.Web.Utilities" \
  --new-namespace "MyApp.Shared.Utilities"

# 3. Update project references manually in .csproj
# 4. Build and test
```

### Workflow 3: Rename a Service

**Goal:** Rename `UserService` to `UserManager`

```bash
# 1. Check impact
migration-tool analyze-impact "MyApp.sln" --type "MyApp.Services.UserService" --operation rename --target "UserManager"

# 2. Rename file and class
migration-tool rename-file "Services/UserService.cs" "UserManager.cs" --rename-class

# 3. Build to find remaining references
dotnet build MyApp.sln
# Fix any remaining references manually
```

### Workflow 4: Cross-Solution Migration

**Goal:** Copy a feature from one solution to another

```bash
# Use the CLI cross-solution-migrate command
migration-tool cross-solution-migrate \
  --source-solution "C:\OldProject\Old.sln" \
  --target-solution "C:\NewProject\New.sln" \
  --source-path "Features/Authentication" \
  --target-path "Shared/Authentication" \
  --old-namespace "OldProject.Features.Authentication" \
  --new-namespace "NewProject.Shared.Authentication"
```

---

## Understanding the Graph

When you run `analyze-graph`, you get a complete model of your solution:

```
Graph Statistics:
  Solutions: 1
  Projects: 15
  Files: 234
  Types: 312
  Namespaces: 28
  Edges: 1,456
    - Project References: 45
    - Package References: 120
    - Type Usages: 890
    - Inheritance: 156
```

### Node Types

| Node | Description |
|------|-------------|
| `SolutionNode` | The .sln file |
| `ProjectNode` | A .csproj project |
| `FileNode` | A .cs source file |
| `TypeNode` | A class, interface, record, struct, or enum |
| `NamespaceNode` | A namespace |
| `PackageNode` | A NuGet package |

### Edge Types

| Edge | Description |
|------|-------------|
| `ProjectReferenceEdge` | Project A references Project B |
| `PackageReferenceEdge` | Project references a NuGet package |
| `TypeInheritsEdge` | Class A inherits from Class B |
| `TypeImplementsEdge` | Class implements Interface |
| `FileContainsTypeEdge` | File contains Type |
| `TypeUsageEdge` | Type A uses Type B |

---

## Tips & Best Practices

### Before Migration

- ✅ **Commit to Git** - Always have a clean working directory
- ✅ **Create a branch** - Don't migrate on main branch
- ✅ **Run tests** - Ensure all tests pass before migration
- ✅ **Use `--dry-run`** - Preview changes before executing

### During Migration

- ✅ **Small steps** - Break large migrations into smaller operations
- ✅ **Check impact first** - Use `analyze-impact` before each operation
- ✅ **Build frequently** - Verify compilation after each step
- ✅ **Review changes** - Check git diff before committing

### After Migration

- ✅ **Build solution** - Ensure everything compiles
- ✅ **Run tests** - Verify functionality
- ✅ **Code review** - Have someone review the changes
- ✅ **Commit** - Commit with descriptive message

---

## Troubleshooting

### "Type not found in graph"
- Ensure you're using the fully qualified type name
- Run `analyze-graph` first to see available types

### "Namespace update didn't work"
- Check that the old namespace exactly matches
- Namespace rewriter only updates exact matches, not sub-namespaces

### "Build fails after move"
- Some references may need manual updates
- Check for hardcoded paths in .csproj files
- Update project references if needed

### "Graph building is slow"
- Use `--fast` option to skip type usage analysis
- Large solutions (100+ projects) take longer

---

## Programmatic Usage

You can also use MigrationTool.Core in your own code:

```csharp
using MigrationTool.Core.Graph;
using MigrationTool.Core.Services;
using Microsoft.Extensions.Logging;

// Build a solution graph
var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<SolutionGraphBuilder>();
await using var builder = new SolutionGraphBuilder(logger);
var graph = await builder.BuildGraphAsync("path/to/solution.sln");

// Analyze impact
var analyzer = new ImpactAnalyzer(analyzerLogger);
var report = await analyzer.AnalyzeMoveTypeAsync(graph, 
    new MoveTypeOperation("MyApp.OldService", "MyApp.NewNamespace"));

Console.WriteLine(report.ToMarkdown());

// Perform file operations
var fileService = new FileOperationService(fileLogger);
var result = await fileService.MoveFileAsync(
    "source.cs", 
    "target.cs", 
    updateNamespace: true);
```

---

## FAQ

**Q: Can I undo a migration?**  
A: Use Git to revert changes. Always commit before migrating.

**Q: Does it update all references automatically?**  
A: Currently updates namespaces and file locations. Full reference updates (using statements in other files) are planned.

**Q: Can I use this in CI/CD?**  
A: Yes! The CLI is designed for automation. Use `--json` for machine-readable output.

**Q: How large a solution can it handle?**  
A: Tested with 271 types, 236 files. Should handle 500+ project solutions.

**Q: Does it support .NET Framework?**  
A: Yes, it can analyze .NET Framework projects, but runs on .NET 9.

---

**Happy migrating! 🚀**
