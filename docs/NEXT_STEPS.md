# Migration Tool - Next Steps

**Created:** January 7, 2026  
**Goal:** Migrate from Blazor Server to React + gRPC architecture

---

## ✅ Completed Today (Session Summary)

### Features Implemented:
1. ✅ Planner UI redesign (3-panel professional layout)
2. ✅ CSS refactoring (split 2894 lines → 3 files)
3. ✅ TreeView + FileDetail components
4. ✅ Files view mode in Explorer
5. ✅ Settings with drag & drop UI
6. ✅ Debug auto-load (Framework.sln - 46 projects)
7. ✅ Copy File/Folder operations in UI
8. ✅ Dependency Graph with 46 projects
9. ✅ Analysis page (Namespaces, Conflicts, Packages)

### Commits Made: 9
- Quick Wins phase (Dashboard enhancements)
- Cross-project views (Analysis, Dependency Graph)
- Explorer UI improvements (hierarchy, scrolling)
- Planner redesign (3-panel layout)
- CSS refactoring (planner.css, analysis.css)
- Settings enhancements (drag & drop)
- TreeView + FileDetail components
- Debug default path (Framework.sln)
- Copy operations support

---

## 🎯 Next Session Plan

### Step 1: Setup React + gRPC (First 2-3 hours)

**Backend:**
```bash
cd tools/src/MigrationTool
dotnet new grpc -n MigrationTool.GrpcServer
cd MigrationTool.GrpcServer

# Add references to Core
dotnet add reference ../MigrationTool.Core/MigrationTool.Core.csproj
dotnet add reference ../MigrationTool.Core.Abstractions/MigrationTool.Core.Abstractions.csproj

# Install gRPC-Web support
dotnet add package Grpc.AspNetCore.Web
```

**Frontend:**
```bash
cd ../../..
npm create vite@latest frontend/migration-tool-ui -- --template react-ts
cd frontend/migration-tool-ui
npm install

# Install dependencies
npm install grpc-web
npm install @tanstack/react-table reactflow
npm install zustand react-router-dom react-hook-form
npm install -D @types/google-protobuf

# Install Shadcn/ui
npx shadcn@latest init
npx shadcn@latest add button card table dialog select tabs
```

### Step 2: Define Proto Contracts (1-2 hours)

Create `Protos/migration_service.proto`:
```protobuf
syntax = "proto3";

package migration;

service MigrationService {
  rpc AnalyzeSolution(SolutionPath) returns (SolutionInfo);
  rpc ListProjects(Empty) returns (ProjectList);
  rpc GetProjectDetails(ProjectId) returns (ProjectDetails);
  rpc ExecuteMigrationPlan(MigrationPlan) returns (stream MigrationProgress);
  rpc ValidatePlan(MigrationPlan) returns (ValidationResult);
}

message SolutionInfo {
  string name = 1;
  string path = 2;
  int32 project_count = 3;
  int32 test_project_count = 4;
  repeated ProjectInfo projects = 5;
}

message MigrationProgress {
  int32 current_step = 1;
  int32 total_steps = 2;
  double percent_complete = 3;
  string current_action = 4;
  bool completed = 5;
  bool success = 6;
  string error_message = 7;
}
```

### Step 3: Implement First gRPC Service (2 hours)

Start with simplest: Solution Analysis
- AnalyzeSolution endpoint
- List projects
- Return data to React frontend

### Step 4: Build First React Page (2 hours)

Dashboard (simplest):
- Connect to gRPC
- Display solution stats
- Show projects table

**Goal:** End-to-end working before building rest

---

## 📚 Reference Materials

### For gRPC Implementation:
- Current Blazor Server in: `tools/src/MigrationTool/MigrationTool.Blazor.Server/`
- Core logic in: `tools/src/MigrationTool/MigrationTool.Core/`
- Models in: `tools/src/MigrationTool/MigrationTool.Core.Abstractions/Models/`

### For React UI:
- Screenshots of current UI (use as design reference)
- CSS files (color scheme, spacing):
  - `app.css` - base styles
  - `planner.css` - Planner layout
  - `analysis.css` - Analysis styles

---

## ⚠️ Important Notes

### DO NOT DELETE Blazor Code Yet!
- Keep as reference during migration
- Useful for comparing implementations
- Design system is defined there
- Can run side-by-side during development

### Reuse Strategy:
1. **Visual Design:** Copy exact layouts, colors, spacing
2. **UX Flows:** Keep same navigation, interactions
3. **Business Logic:** gRPC calls to existing Core services
4. **Icons & Emojis:** Keep the same visual language

---

## 🎯 Success Metrics

**Must Have (MVP):**
- [ ] Load Framework.sln (46 projects) in <1 second
- [ ] Dashboard shows all stats correctly
- [ ] Dependency Graph with React Flow (drag, zoom, pan)
- [ ] Create migration plan with Quick Actions
- [ ] Execute plan with real-time streaming progress
- [ ] All features from Blazor prototype working

**Nice to Have:**
- [ ] Faster than Blazor (target: 5x)
- [ ] Better UX (React Flow graph)
- [ ] Keyboard shortcuts (Cmd+K command palette)
- [ ] Export dependency graph as PNG
- [ ] Dark mode toggle

---

## 🛠️ Development Workflow

### Daily Development Loop:
1. **Backend:** Add gRPC endpoint
2. **Generate:** Run protoc to update TS clients
3. **Frontend:** Use generated client in React
4. **Test:** Verify in browser (instant hot reload)
5. **Commit:** Small, focused commits

### Testing:
- Backend: Use Postman/BloomRPC for gRPC testing
- Frontend: Use React Testing Library
- E2E: Playwright (automate browser testing)

---

## 📅 Estimated Timeline

**Week 1:**
- ✅ Architecture decision (DONE)
- Setup gRPC Server
- Define proto contracts
- Implement Solution Analysis service
- Setup React frontend
- Build Dashboard page

**Week 2:**
- Explorer page with file tree
- Analysis page with tabs
- Dependency Graph (React Flow)
- Testing and polish

**Week 3:**
- Migration Planner page
- Execution with streaming
- Settings page
- Final polish and testing

**Week 4:**
- Bug fixes
- Documentation
- Deployment setup
- Archive Blazor prototype

---

**Ready to start next session with gRPC server setup!** 🚀
