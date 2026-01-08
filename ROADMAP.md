# MigrationTool Roadmap

This document outlines the current status and planned features for MigrationTool.

**Last Updated:** January 8, 2026  
**Current Version:** 1.1.0 (CLI + Core)  
**Next Version:** 2.0.0 (React Frontend)

---

## Current Status (v1.1.0)

### ✅ Core Library - COMPLETED

| Feature | Status | Description |
|---------|--------|-------------|
| Solution Graph | ✅ Done | In-memory graph model of solution dependencies |
| Graph Builder | ✅ Done | Build graph using MSBuildWorkspace + Roslyn |
| Impact Analyzer | ✅ Done | Predict impact of move/rename/delete operations |
| File Operations | ✅ Done | Move, copy, delete, rename with namespace updates |
| Folder Operations | ✅ Done | Recursive folder operations |
| Namespace Rewriter | ✅ Done | Roslyn-based namespace updates |
| Class Renamer | ✅ Done | Rename types including constructors |
| Cross-Solution Migration | ✅ Done | Migrate between different solutions |
| Using Management | ✅ Done | Add/remove using directives |

### ✅ CLI Tool - COMPLETED

| Command | Status | Description |
|---------|--------|-------------|
| `analyze-solution` | ✅ Done | Analyze solution structure |
| `analyze-graph` | ✅ Done | Build dependency graph |
| `analyze-impact` | ✅ Done | Impact analysis |
| `move-file` | ✅ Done | Move file with namespace update |
| `copy-file` | ✅ Done | Copy file |
| `rename-file` | ✅ Done | Rename file and class |
| `delete-file` | ✅ Done | Delete with reference check |
| `move-folder` | ✅ Done | Move folder recursively |
| `copy-folder` | ✅ Done | Copy folder |
| `update-namespace` | ✅ Done | Update namespaces |
| `find-usages` | ✅ Done | Find type usages |

### ✅ Testing - COMPLETED

| Feature | Status | Description |
|---------|--------|-------------|
| Test Fixtures | ✅ Done | Humanizer project as test data |
| Graph Tests | ✅ Done | 271 types, 236 files analyzed |
| Impact Tests | ✅ Done | Move/rename/delete scenarios |
| Auto-cleanup | ✅ Done | MSBuild target cleans before tests |

### 📊 Test Results (Humanizer Project)

```
Projects:    2
Files:       236
Types:       271
Namespaces:  26
Inheritance: 110
Interfaces:  40
Total Edges: 985
```

---

## Version 2.0.0 (Planned - Q1-Q2 2026)

**React Frontend with REST/gRPC Backend**

### Phase 1: Backend API (4-6 hours)

- [ ] Create `MigrationTool.Api` project (ASP.NET Core)
- [ ] REST endpoints for:
  - `GET /api/graph` - Get solution graph
  - `GET /api/impact` - Get impact analysis
  - `POST /api/migrate` - Execute migration
- [ ] Optional: gRPC for streaming progress
- [ ] Swagger/OpenAPI documentation

### Phase 2: React Frontend Setup (2-3 hours)

- [ ] Vite + React + TypeScript project
- [ ] Dependencies:
  - `reactflow` - Dependency graph visualization
  - `@tanstack/react-table` - Data tables
  - `zustand` - State management
  - `tailwindcss` - Styling
- [ ] API client generation from OpenAPI

### Phase 3: UI Pages (8-12 hours)

#### Dashboard
- [ ] Solution statistics cards
- [ ] Project list with filtering
- [ ] Quick actions

#### Graph Explorer
- [ ] Interactive dependency graph (React Flow)
- [ ] Zoom, pan, auto-layout
- [ ] Node details on click
- [ ] Filter by type (projects, files, types)

#### Impact Analyzer
- [ ] Select operation type (move, rename, delete)
- [ ] Select target (type, file, namespace)
- [ ] Visual impact preview
- [ ] Affected files list

#### Migration Planner
- [ ] Create migration plan
- [ ] Add steps (drag & drop)
- [ ] Validate plan
- [ ] Execute with progress

### Phase 4: Polish (4-6 hours)

- [ ] Error handling
- [ ] Loading states
- [ ] Dark mode
- [ ] Keyboard shortcuts
- [ ] Export/import plans

---

## Version 2.1.0 (Planned - Q2 2026)

### Reference Updates (Hard)

When moving a type, automatically update all files that reference it:
- [ ] Find all files using the type
- [ ] Update `using` directives
- [ ] Update fully qualified names
- [ ] Handle partial classes

### Execution Plan

Orchestrated migration with rollback:
- [ ] Plan validation
- [ ] Step-by-step execution
- [ ] Progress tracking
- [ ] Rollback on failure
- [ ] Execution log

### Enhanced Analysis

- [ ] Full semantic analysis (who calls what)
- [ ] Breaking change detection
- [ ] Circular dependency detection
- [ ] Package consolidation suggestions

---

## Version 3.0.0 (Future)

### AI-Powered Features

- [ ] Migration plan suggestions (Claude/GPT)
- [ ] Smart namespace recommendations
- [ ] Code smell detection
- [ ] Complexity estimation

### Advanced Refactoring

- [ ] Extract interface from class
- [ ] Split large files
- [ ] Convert to record/struct
- [ ] Partial class management

### Integrations

- [ ] VS Code extension
- [ ] Visual Studio extension
- [ ] Git integration (auto-commit)
- [ ] CI/CD pipeline support

---

## Architecture

### Current (v1.1.0)

```
┌─────────────────────────────────────┐
│   CLI (System.CommandLine)          │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│   MigrationTool.Core                │
│   ├── Graph (SolutionGraph)         │
│   ├── Services (FileOps, Migration) │
│   └── Rewriters (Roslyn)            │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│   Roslyn + MSBuild                  │
│   (Code analysis & manipulation)    │
└─────────────────────────────────────┘
```

### Planned (v2.0.0)

```
┌─────────────────────────────────────┐
│   React Frontend                    │
│   (TypeScript, React Flow)          │
└────────────┬────────────────────────┘
             │ REST/gRPC
┌────────────▼────────────────────────┐
│   ASP.NET Core API                  │
│   (Controllers, gRPC services)      │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│   MigrationTool.Core (UNCHANGED)    │
└─────────────────────────────────────┘
```

---

## Technology Stack

### Current
- .NET 9.0
- Roslyn (Microsoft.CodeAnalysis) 4.12.0
- MSBuild (Microsoft.Build) 17.x
- System.CommandLine 2.0
- xUnit + FluentAssertions

### Planned (v2.0.0)
- React 18 + TypeScript 5
- Vite 5
- React Flow 11
- TanStack Table 8
- Tailwind CSS 3
- ASP.NET Core 9 (API)

---

## Performance Targets

| Operation | Current | Target (v2.0) |
|-----------|---------|---------------|
| Graph Build (50 projects) | ~2s | ~1s |
| Impact Analysis | ~500ms | ~200ms |
| File Move | ~100ms | ~100ms |
| UI Response | N/A (CLI) | <100ms |

---

## Quality Goals

| Metric | Current | Target |
|--------|---------|--------|
| Test Coverage | ~70% | 85% |
| Unit Tests | 85+ | 150+ |
| Integration Tests | 10+ | 30+ |
| E2E Tests | 0 | 20+ |

---

## Breaking Changes

### v2.0.0
- CLI remains compatible
- New REST API (addition, not breaking)
- Core library API unchanged

### v3.0.0
- May introduce new graph model
- Will maintain CLI compatibility

---

## Contributing

We welcome contributions! Priority areas:

1. **Reference Updates** - Most impactful feature
2. **React Frontend** - Better UX
3. **Additional Tests** - More coverage
4. **Documentation** - Examples and guides

See [README.md](README.md) for contribution guidelines.

---

## Timeline

### January 2026 (Current)
- ✅ Core library completed
- ✅ CLI tool completed
- ✅ Humanizer test fixtures
- ✅ Graph and impact analysis

### February-March 2026
- [ ] REST API
- [ ] React frontend MVP
- [ ] Graph visualization

### Q2 2026
- [ ] Reference updates
- [ ] Execution plan
- [ ] Polish and testing

---

**Maintained by:** MigrationTool Team  
**Next Review:** After React MVP
