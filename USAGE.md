# MigrationTool Usage Guide

Step-by-step guide to using MigrationTool for .NET project migration and refactoring.

## Getting Started

### First Launch

1. **Launch the application**
   - **Blazor**: Navigate to `http://localhost:5000`
   - **MAUI**: Double-click `MigrationTool.Maui.exe`

2. **Select language** (optional)
   - Click language selector in sidebar
   - Choose: English, Čeština, Polski, or Українська

3. **Configure workspace**
   - Navigate to Settings
   - Enter workspace path (e.g., `C:\Projects\MyApp`)
   - Click "Apply"

4. **Load a solution**
   - Select solution from dropdown
   - Click "Load Solution" or navigate to Dashboard

## Main Features

### 📊 Dashboard

**Purpose:** Overview of your solution

**What you'll see:**
- Total projects count
- Test vs. source projects breakdown
- Files, classes, and tests count
- Solution path and name

**Actions:**
- Click on metrics to see details
- Navigate to Explorer for deep dive

### 📁 Project Explorer

**Purpose:** Browse and analyze project structure

**Features:**
- **Project List** (left panel)
  - Filter by test/source projects
  - Click project to see details

- **Project Details** (right panel)
  - File count, class count, test count
  - Project references and packages
  - Browse source files
  - Inspect classes and methods

**Tips:**
- Use the filter to focus on specific project types
- Click on files to see their classes
- Test projects are marked with 🧪 badge

### 📋 Migration Planner

**Purpose:** Plan and execute migrations

**Workflow:**

1. **Create a Plan**
   - Click "Create Plan"
   - Give your plan a name

2. **Add Migration Steps**
   - Click "+ Add"
   - Select action type:
     - Move File
     - Move Folder
     - Copy File/Folder
     - Create Project
     - Rename Namespace
   - Specify source and target

3. **Review Plan**
   - Check each step
   - Remove unwanted steps
   - Reorder if needed

4. **Validate** (coming in v1.1)
   - Click "Validate"
   - Review warnings and errors
   - Fix issues

5. **Execute** (coming in v1.1)
   - Click "Execute Plan"
   - Monitor progress
   - Review results

6. **Save/Load Plans**
   - Export plan as JSON
   - Share with team
   - Reuse for similar migrations

## Common Workflows

### Workflow 1: Reorganize Test Projects

**Goal:** Split a monolithic test project into Unit/Integration/E2E projects

**Steps:**

1. **Analyze current structure**
   - Load solution in Project Explorer
   - Select your test project
   - Browse test files

2. **Create migration plan**
   - Create New Plan: "Split Test Projects"
   - Add step: Create Project "MyApp.Unit.Tests"
   - Add step: Create Project "MyApp.Integration.Tests"
   - Add step: Move Folder "Unit/*" → "MyApp.Unit.Tests/"
   - Add step: Move Folder "Integration/*" → "MyApp.Integration.Tests/"
   - Add step: Rename Namespace "MyApp.Tests.Unit" → "MyApp.Unit.Tests"

3. **Execute**
   - Validate plan
   - Review changes
   - Execute
   - Verify build

### Workflow 2: Extract Shared Library

**Goal:** Extract common code into a shared library

**Steps:**

1. **Identify shared code**
   - Use Project Explorer
   - Find classes used by multiple projects

2. **Create migration plan**
   - Create New Plan: "Extract Core Library"
   - Add step: Create Project "MyApp.Core"
   - Add steps: Move files to new project
   - Add steps: Update namespaces
   - Add steps: Add project references

3. **Execute**
   - Validate dependencies
   - Execute plan
   - Update consuming projects

### Workflow 3: Migrate to New Namespace Structure

**Goal:** Reorganize namespaces to follow naming conventions

**Steps:**

1. **Analyze current namespaces**
   - Project Explorer shows all namespaces
   - Identify inconsistencies

2. **Create migration plan**
   - Add steps: Rename Namespace (for each namespace)
   - Example: "Company.OldStructure.Services" → "Company.NewStructure.Application.Services"

3. **Execute**
   - Roslyn handles all references automatically
   - Build and verify

## Tips & Best Practices

### Before Migration

- ✅ **Commit to Git** - Always have a clean working directory
- ✅ **Create a branch** - Don't migrate on main branch
- ✅ **Backup** - Keep a copy of your solution
- ✅ **Run tests** - Ensure all tests pass before migration

### During Migration

- ✅ **Small steps** - Break large migrations into smaller plans
- ✅ **Test frequently** - Validate after each major step
- ✅ **Review changes** - Check git diff before committing
- ✅ **Document** - Add comments to your migration plan

### After Migration

- ✅ **Build solution** - Ensure everything compiles
- ✅ **Run tests** - Verify functionality
- ✅ **Code review** - Have someone review the changes
- ✅ **Commit** - Commit with descriptive message

## Keyboard Shortcuts (MAUI only)

| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open Solution |
| Ctrl+R | Refresh |
| Ctrl+S | Save Plan |
| F5 | Execute Plan |
| Escape | Cancel |

## Language-Specific Features

### Czech (Čeština)
- Full UI translation
- Czech naming conventions support

### Polish (Polski)
- Full UI translation
- Polish diacritics support

### Ukrainian (Українська)
- Full UI translation
- Cyrillic alphabet support

## Known Limitations (v1.0.0)

- Migration execution not yet implemented (coming in v1.1)
- No undo/rollback (use Git)
- Windows only for MAUI
- No batch operations
- No AI suggestions

## FAQ

**Q: Can I migrate from .NET Framework to .NET Core?**  
A: Partially. The tool can help reorganize projects, but framework-specific code must be migrated manually.

**Q: Does it support VB.NET or F#?**  
A: Not yet. C# only in v1.0. VB.NET and F# support planned for v2.0.

**Q: Can I use this in CI/CD?**  
A: CLI version planned for v2.0. Currently, it's interactive only.

**Q: Is it safe to use on production code?**  
A: Always use with version control. The tool is in v1.0 - test thoroughly before production use.

**Q: Can I extend it with custom analyzers?**  
A: Not yet. Plugin system planned for v2.0.

## Support

- **Issues:** Open a GitHub issue
- **Questions:** Check existing issues or create new one
- **Feature Requests:** See ROADMAP.md and submit suggestions

## Contributing

See main README.md for contribution guidelines.

---

**Happy migrating! 🚀**
