# MigrationTool Roadmap

This document outlines the planned features and improvements for MigrationTool.

**Last Updated:** January 8, 2026  
**Current Version:** 1.0.0 (Blazor Prototype)  
**Next Version:** 2.0.0 (React + gRPC Architecture)

---

## Version 1.0.0 (Completed - January 2026)

**Blazor Server Prototype - Proof of Concept**

### Core Features ✅
- [x] Core analyzers (Solution, Project, Code) using Roslyn
- [x] Solution parsing with project discovery
- [x] C# source file analysis with test detection
- [x] Project reference and package reference tracking
- [x] 160+ unit and integration tests

### UI - Blazor Server ✅
- [x] Dashboard with statistics and filtering
- [x] Project Explorer with 5 view modes (Tree, Files, List, Classes, Tests)
- [x] Migration Planner with 8 Quick Action templates
- [x] Analysis page (Namespaces, Conflicts, Packages, Dependencies)
- [x] SVG-based Dependency Graph
- [x] TreeView and FileDetail components
- [x] Multi-language support (EN, CS, PL, UK)

### Migration Operations ✅
- [x] Move File/Folder with auto-reference updates
- [x] Copy File/Folder (preserves originals)
- [x] Rename Namespace using Roslyn
- [x] Add/Remove Project References
- [x] Update Project Properties
- [x] Migration plan validation
- [x] Execution with real-time progress tracking
- [x] Export/Import plans as JSON
- [x] Rollback support

### Developer Experience ✅
- [x] Debug auto-load (Framework.sln - 46 projects)
- [x] Drag & Drop UI for solution loading
- [x] Recent paths list
- [x] Professional UI with gradients and animations
- [x] Toast notifications
- [x] Execution modal with step tracking

**Achievements:**
- Successfully tested with 46-project solution
- Professional UI design established
- All core migration operations implemented
- Comprehensive analysis features

**Lessons Learned:**
- Blazor Server works but has limitations (UI ecosystem, hot reload speed)
- Manual SVG graph implementation is complex (340 LOC)
- CSS became large (2894 lines, split to 3 files)
- SignalR is good for real-time but gRPC streaming is better
- React ecosystem would provide better long-term ROI

---

## Version 2.0.0 (Planned - Q1-Q2 2026)

**React + gRPC Architecture - Production Implementation**

> **Architecture Decision:** Migrate to React frontend with .NET gRPC backend for better performance, modern UI ecosystem, and improved developer experience. See `docs/ARCHITECTURE_DECISION.md` for detailed analysis.

### Phase 1: Backend - gRPC Server (6-8 hours)

**Goal:** Create .NET gRPC server that exposes Core functionality

- [ ] Create `MigrationTool.GrpcServer` project
- [ ] Define Protocol Buffer contracts (`.proto` files):
  - `migration_service.proto` - Migration operations
  - `analysis_service.proto` - Solution/project analysis  
  - `models.proto` - Shared data models
- [ ] Implement gRPC services:
  - `MigrationService` - Execute migrations with server-side streaming
  - `AnalysisService` - Analyze solutions, projects, files
  - `FileService` - File/folder operations
- [ ] Add gRPC-Web support (CORS for browser)
- [ ] Embed React build for single-exe deployment
- [ ] Reuse existing `MigrationTool.Core` (Roslyn analyzers) ✅

**Benefits:**
- Binary protocol (3x smaller payload than JSON)
- Server-side streaming for real-time progress
- Type-safe code generation (C# + TypeScript)
- HTTP/2 multiplexing

### Phase 2: Frontend - React Setup (2-3 hours)

**Goal:** Modern React frontend with TypeScript

- [ ] Initialize Vite + React + TypeScript project
- [ ] Install core dependencies:
  - `grpc-web` - gRPC client
  - `@tanstack/react-table` - Advanced data tables
  - `reactflow` - Interactive dependency graph
  - `zustand` - State management
  - `react-router-dom` - Routing
  - `react-hook-form` - Form handling
- [ ] Setup Shadcn/ui component library
- [ ] Generate TypeScript gRPC clients from `.proto`
- [ ] Configure Vite for development and production builds

**Benefits:**
- 50ms hot reload (vs 2-5s in Blazor)
- Modern UI component ecosystem
- Type-safe API client (auto-generated)
- Professional components out-of-the-box

### Phase 3: UI Pages Migration (8-12 hours)

**Migrate features from Blazor prototype:**

#### Dashboard (2 hours)
- [ ] Solution statistics cards
- [ ] Project list with TanStack Table (sorting, filtering built-in!)
- [ ] Project type breakdown chart
- [ ] Dependencies overview
- [ ] Search and filter UI

**From Blazor:** 243 lines Razor  
**To React:** ~150 lines TSX (TanStack handles complexity)

#### Explorer (3 hours)
- [ ] Project list with hierarchical grouping
- [ ] File tree view (React component library)
- [ ] File detail panel (classes, methods, usings)
- [ ] Multiple view modes (Tree, Files, List, Classes, Tests)
- [ ] File selection for migration planning

**From Blazor:** 655 lines + TreeView 240 lines + FileDetail 416 lines = 1311 lines  
**To React:** ~400 lines (libraries handle tree rendering)

#### Analysis (2 hours)
- [ ] Tab navigation (Namespaces, Conflicts, Packages, Dependencies)
- [ ] **Dependency Graph with React Flow** 🎯
  - Drag & drop nodes
  - Auto-layout algorithms (dagre, elk)
  - Zoom, pan, minimap
  - Export to PNG
- [ ] Namespace analysis tables
- [ ] Conflict detection
- [ ] Package consolidation view

**From Blazor:** 480 lines + DependencyGraph 340 lines = 820 lines  
**To React:** ~200 lines (React Flow does 85% of work!)

#### Migration Planner (3 hours)
- [ ] 3-panel layout (Quick Actions, Plan Editor, Step Details)
- [ ] 8 Quick Action templates
- [ ] Step cards with inline editing
- [ ] Drag-to-reorder steps (React DnD)
- [ ] Validation panel
- [ ] **Execution modal with gRPC streaming progress** 🎯
- [ ] Export/Import JSON plans

**From Blazor:** 872 lines  
**To React:** ~300 lines (React Hook Form + React DnD)

#### Settings (1 hour)
- [ ] Solution path input with file picker
- [ ] Drag & drop zone
- [ ] Recent paths list
- [ ] Language selector
- [ ] Theme toggle (Dark mode)

### Phase 4: Polish & Integration (4-6 hours)

- [ ] Error boundaries and error handling
- [ ] Loading states and skeletons
- [ ] Toast notifications
- [ ] Keyboard shortcuts (Cmd+K command palette)
- [ ] Accessibility (WCAG AA compliance)
- [ ] E2E tests with Playwright
- [ ] Build and packaging scripts
- [ ] User documentation

---

## Version 2.1.0 (Planned - Q2 2026)

### Enhanced Features

- [ ] **File System Watching** (gRPC streaming)
  - Auto-detect code changes
  - Re-analyze affected files
  - Live UI updates
- [ ] **Batch Operations**
  - Multi-file rename patterns
  - Bulk namespace updates
  - Mass project reference updates
- [ ] **Migration History**
  - Track all executed migrations
  - Rollback to any point
  - Audit log
- [ ] **Git Integration**
  - Auto-commit after migration
  - Create feature branches
  - Diff preview before execution

### Desktop Integration

- [ ] **Tauri Wrapper** (Rust + React)
  - Native file dialogs
  - System tray integration
  - Auto-updater
  - Smaller binary than Electron
- [ ] **VS Code Extension**
  - Consume same gRPC API
  - In-editor migration planning
  - Code lens for migrations

---

## Version 2.2.0 (Planned - Q3 2026)

### AI-Powered Features

- [ ] AI migration plan suggestions (Claude/GPT)
- [ ] Smart namespace recommendations
- [ ] Automatic categorization
- [ ] Code smell detection
- [ ] Migration complexity estimation

### Advanced Refactoring

- [ ] Rename symbols across solution (Roslyn)
- [ ] Extract interface from class
- [ ] Move class to new file
- [ ] Split large files
- [ ] Partial class management
- [ ] Convert to record/struct

---

## Version 3.0.0 (Future)

### Multi-Language Support

- [ ] VB.NET project support
- [ ] F# project support
- [ ] Mixed-language solutions
- [ ] .NET Framework → .NET 9 migration helpers

### Cloud Features (Optional)

- [ ] Cloud storage for migration plans
- [ ] Team collaboration
- [ ] Migration plan marketplace
- [ ] Analytics and insights

---

## Performance Goals

| Version | Solution Size | Analysis Time | Migration Time | UI Responsiveness |
|---------|---------------|---------------|----------------|-------------------|
| 1.0.0 (Blazor) | 46 projects | ~2s | ~5s | Good (SignalR) |
| 2.0.0 (React+gRPC) | 46 projects | ~0.5s | ~2s | Excellent (streaming) |
| 2.1.0 | 100 projects | ~1s | ~5s | Excellent |
| 3.0.0 | 500 projects | ~5s | ~30s | Excellent |

**Target:** React + gRPC version should be **3-5x faster** than Blazor

---

## Technology Stack Evolution

### Version 1.0.0 (Current - Blazor)
```
Frontend: Blazor Server (Razor components)
Backend: ASP.NET Core + SignalR
Core: MigrationTool.Core (Roslyn)
UI Libs: Limited (manual implementations)
Deployment: Single ASP.NET app
```

### Version 2.0.0 (Planned - React + gRPC)
```
Frontend: React 18 + TypeScript + Vite
UI Libs: Shadcn/ui, React Flow, TanStack Table
Backend: ASP.NET Core 9 gRPC Server
Protocol: Protocol Buffers (binary)
Core: MigrationTool.Core (UNCHANGED - Roslyn)
Communication: gRPC-Web with streaming
Deployment: Single .exe with embedded React build
```

**Migration Time:** 20-29 hours  
**Long-term ROI:** 3x faster development, better UX

---

## Dependencies

### Current (Blazor - v1.0.0)
- .NET 9.0
- Microsoft.CodeAnalysis.CSharp 4.12.0
- Microsoft.AspNetCore.Components 9.0.0
- CommunityToolkit.Mvvm 8.4.0 (MAUI)

### Planned (React + gRPC - v2.0.0)

**Backend:**
- .NET 9.0
- Grpc.AspNetCore 2.60.0
- Grpc.AspNetCore.Web 2.60.0
- Microsoft.CodeAnalysis.CSharp 4.12.0 (KEEP)
- Google.Protobuf 3.25.0

**Frontend:**
- React 18.2.0
- TypeScript 5.3.0
- Vite 5.0.0
- grpc-web 1.5.0
- reactflow 11.10.0
- @tanstack/react-table 8.11.0
- zustand 4.4.0
- react-router-dom 6.21.0
- react-hook-form 7.49.0
- tailwindcss 3.4.0

---

## Quality Goals

| Metric | v1.0.0 (Blazor) | v2.0.0 (React) | Target |
|--------|-----------------|----------------|---------|
| Test Coverage | ~70% | 80% | 90% |
| Unit Tests | 160+ | 200+ | 300+ |
| E2E Tests | 0 | 10+ | 50+ |
| Accessibility | Partial | WCAG AA | WCAG AAA |
| Performance | Good | Excellent | Excellent |
| UI Responsiveness | 2-5s | 50ms | <100ms |

---

## Breaking Changes

### Version 2.0.0 (React + gRPC Migration)

**API Changes:**
- REST/SignalR → gRPC (complete rewrite)
- JSON → Protocol Buffers
- Razor components → React components

**Backwards Compatibility:**
- ✅ Migration plan JSON format (UNCHANGED)
- ✅ Core analyzers API (UNCHANGED)
- ✅ File operations (UNCHANGED)
- ❌ Blazor UI components (deprecated)
- ❌ SignalR endpoints (removed)

**Migration Path:**
- Blazor prototype archived in `tools/src/MigrationTool/MigrationTool.Blazor.Server/` (reference only)
- Users upgrade to React version (single .exe)
- No data migration needed (stateless tool)

---

## Success Criteria

### Version 2.0.0 MVP Must-Haves:
- [ ] Load Framework.sln (46 projects) in <1 second
- [ ] Dashboard renders all stats correctly
- [ ] Dependency Graph with React Flow (drag, zoom, auto-layout)
- [ ] Explorer browses files with TreeView
- [ ] Create migration plan with 8 Quick Actions
- [ ] Execute plan with real-time gRPC streaming
- [ ] All Blazor features ported to React
- [ ] Performance 3x better than Blazor
- [ ] UI development 3x faster with hot reload

### Nice to Have:
- [ ] Dark mode toggle
- [ ] Keyboard shortcuts (Cmd/Ctrl+K command palette)
- [ ] Export dependency graph as PNG/SVG
- [ ] Offline-capable PWA
- [ ] Mobile-responsive design

---

## Deprecation Notice

### Blazor Server (v1.0.0)
- **Status:** Prototype - functional but not production-ready
- **Deprecated:** January 2026
- **Archived:** Code kept in `tools/src/MigrationTool/MigrationTool.Blazor.Server/` for reference
- **Reason:** Limited UI ecosystem, slower development, manual graph implementation
- **Replacement:** React + gRPC (v2.0.0+)

### MAUI Desktop
- **Status:** Experimental
- **Future:** May be replaced by Tauri wrapper (Rust + React)
- **Reason:** Tauri is lighter, cross-platform, uses same React UI

### WPF Desktop
- **Status:** Functional
- **Future:** Maintained for Windows-only scenarios
- **Use Case:** Enterprise environments without web browser access

---

## Migration Timeline

### January 2026 (Current)
- ✅ Blazor prototype completed
- ✅ Architecture decision made (React + gRPC)
- ✅ Documentation created (ARCHITECTURE_DECISION.md, NEXT_STEPS.md)

### February 2026
- Week 1: gRPC Server setup, proto definitions
- Week 2: React frontend setup, Dashboard page
- Week 3: Explorer + Analysis pages
- Week 4: Migration Planner page

### March 2026
- Week 1: Polish, testing, bug fixes
- Week 2: Documentation, deployment
- Week 3: User testing, feedback
- Week 4: Release v2.0.0

**Estimated Total Time:** 20-29 hours of focused development

---

## Community Requests

### High Priority
- [x] ~~Blazor UI~~ → React UI (better ecosystem)
- [x] ~~Dependency graph~~ → React Flow implementation
- [ ] NuGet package extraction
- [ ] Bulk file operations
- [ ] Git integration

### Medium Priority
- [ ] Solution merger
- [ ] Project splitter
- [ ] Configuration transformer
- [ ] VS Code extension (consuming gRPC API)
- [ ] CLI tool (consuming gRPC API)

### Low Priority
- [x] ~~Dark theme~~ → Will be in React version
- [x] ~~Keyboard shortcuts~~ → Planned for React (Cmd+K)
- [ ] Custom color schemes
- [ ] Plugin system

---

## Performance Benchmarks

**Test Solution:** Framework.sln (46 projects)

| Operation | Blazor v1.0.0 | React+gRPC v2.0.0 (target) | Improvement |
|-----------|---------------|----------------------------|-------------|
| Solution Analysis | ~2s | ~0.5s | **4x faster** |
| Data Transfer | 500KB (JSON) | 150KB (Protobuf) | **70% smaller** |
| Hot Reload | 2-5s | 50ms | **40-100x faster** |
| Dependency Graph Render | ~500ms | ~100ms | **5x faster** |
| Migration Execution | ~5s | ~2s | **2.5x faster** |
| First Paint | 1.5s | 0.3s | **5x faster** |

---

## What We Learned from Blazor Prototype

### ✅ What Worked:
- SignalR for real-time updates (but gRPC streaming is better)
- 3-panel Planner layout (will reuse in React)
- Quick Actions template concept
- Hierarchical project grouping
- Step cards visual design
- Color scheme (blue-green gradients)

### ❌ What Didn't Scale:
- Manual SVG graph (340 LOC → React Flow 50 LOC)
- Limited component libraries
- Slow hot reload hurts productivity
- CSS complexity (2894 lines)
- Blazor-specific state management quirks

### 🎯 Reusable Assets:
- **Design system:** All colors, layouts, spacing defined
- **UX flows:** Navigation, interactions validated
- **Backend:** 100% of Core logic reusable
- **Knowledge:** Requirements validated, edge cases discovered

**The Blazor code was NOT wasted** - it was essential R&D that informed the production architecture!

---

## Technical Architecture

### Current (v1.0.0 - Blazor):
```
┌────────────────────────────────────┐
│   Blazor Server (SignalR)          │
│   - Razor components               │
│   - C# code-behind                 │
└────────────┬───────────────────────┘
             │ (in-process)
┌────────────▼───────────────────────┐
│   MigrationTool.Core               │
│   - Roslyn analyzers               │
└────────────────────────────────────┘
```

### Planned (v2.0.0 - React + gRPC):
```
┌────────────────────────────────────┐
│   React Frontend (TypeScript)      │
│   - Modern UI components           │
│   - Fast Vite hot reload           │
│   - React Flow, TanStack Table     │
└────────────┬───────────────────────┘
             │ gRPC-Web (HTTP/2, Protobuf)
┌────────────▼───────────────────────┐
│   gRPC Server (.NET 9)             │
│   - Protocol Buffers               │
│   - Streaming support              │
│   - Generated types                │
└────────────┬───────────────────────┘
             │ (in-process)
┌────────────▼───────────────────────┐
│   MigrationTool.Core               │
│   - Roslyn analyzers (UNCHANGED)   │
└────────────────────────────────────┘
```

---

## Long-Term Vision

### Version 3.0.0 and Beyond

**Extensibility:**
- Plugin system for custom analyzers
- Extension API for VS Code/Visual Studio
- CLI tool sharing same gRPC backend
- Mobile app (React Native) for code review

**Advanced Features:**
- AI-powered migration suggestions
- Framework migration wizards (.NET Framework → .NET 9)
- Dependency injection container migration
- Multi-repository migrations

**Enterprise:**
- Team collaboration features
- Migration plan approvals
- Compliance and audit logging
- Integration with Azure DevOps/GitHub Actions

---

## Resource Links

### Current Implementation (Blazor)
- Code: `tools/src/MigrationTool/MigrationTool.Blazor.Server/`
- Docs: `tools/README.md`
- Tests: `tools/tests/MigrationTool/`

### Architecture Documentation
- Decision: `docs/ARCHITECTURE_DECISION.md`
- Migration Plan: `docs/NEXT_STEPS.md`

### Future Implementation (React + gRPC)
- To be created in: `tools/src/MigrationTool/MigrationTool.GrpcServer/`
- Frontend in: `frontend/migration-tool-ui/`

---

## Conclusion

**Version 1.0.0 (Blazor)** successfully validated:
- ✅ Migration workflows
- ✅ UI/UX requirements  
- ✅ Core functionality
- ✅ 46-project scalability

**Version 2.0.0 (React + gRPC)** will deliver:
- 🚀 3-5x better performance
- 🎨 Modern UI with best-in-class libraries
- ⚡ 40-100x faster development cycles
- 📦 Smaller payloads (70% reduction)
- 🔄 Real-time streaming natively

**The journey continues!** 🎯

---

**Maintained by:** MigrationTool Development Team  
**Next Review:** After React + gRPC MVP completion
