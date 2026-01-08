# 🔄 Migration Tool - React + gRPC Edition

Modern React frontend with .NET gRPC backend for migrating and refactoring .NET projects.

## 📁 Project Structure

```
migration-tool-react/
├── backend/                         # .NET gRPC Server
│   ├── Protos/
│   │   └── migration_service.proto  # gRPC API contracts
│   ├── Services/
│   │   └── MigrationServiceImpl.cs  # Service implementation
│   ├── Program.cs                   # Server entry point
│   └── MigrationTool.GrpcServer.csproj
│
└── frontend/                        # React Frontend
    ├── src/
    │   ├── components/
    │   │   └── layout/
    │   │       └── Layout.tsx       # Main layout with sidebar
    │   ├── pages/
    │   │   ├── Dashboard.tsx        # Solution statistics
    │   │   ├── Explorer.tsx         # Project browser
    │   │   ├── Analysis.tsx         # Code analysis
    │   │   ├── Planner.tsx          # Migration planner (3-panel)
    │   │   └── Settings.tsx         # Load solutions
    │   ├── store/
    │   │   └── useAppStore.ts       # Zustand state management
    │   ├── lib/
    │   │   └── grpc-client.ts       # gRPC client wrapper
    │   ├── App.tsx                  # Main app component
    │   ├── main.tsx                 # Entry point
    │   └── index.css                # Global styles
    ├── package.json
    └── vite.config.ts
```

## 🚀 Quick Start

### Prerequisites

- **.NET 9 SDK** or later
- **Node.js 18+** and npm
- **Protocol Buffers Compiler** (protoc) - for generating TypeScript clients

### 1. Start the Backend (gRPC Server)

```bash
cd backend

# Restore packages
dotnet restore

# Run the server
dotnet run
```

The gRPC server will start on:
- **HTTP/2 (gRPC):** `http://localhost:5001`
- **HTTP/1.1 (gRPC-Web):** `http://localhost:5000`

### 2. Install Frontend Dependencies

```bash
cd frontend

# Install packages
npm install
```

### 3. Generate TypeScript gRPC Clients (Optional)

If you want to connect to the real backend instead of using mock data:

```bash
# Install protoc and plugins (one-time setup)
npm install -g grpc-tools
npm install -g protoc-gen-ts

# Generate TypeScript clients from proto files
cd frontend
npm run proto:generate
```

This will generate TypeScript clients in `src/generated/`.

### 4. Start the Frontend

```bash
cd frontend

# Start development server
npm run dev
```

The React app will open at **http://localhost:3000**

## 🎨 Features

### ✅ Implemented

- **Dashboard** - Solution statistics and quick actions
- **Explorer** - Browse projects with hierarchical view
- **Analysis** - Analyze namespaces, dependencies, and packages
- **Planner** - 3-panel migration planning interface
- **Settings** - Load solutions and configure app
- **Modern UI** - Tailwind CSS with custom design system matching Blazor prototype
- **State Management** - Zustand for global state
- **Routing** - React Router for navigation

### 🔄 To Connect to Your Core

The backend currently returns mock data. To connect to your actual `MigrationTool.Core`:

1. **Uncomment project references** in `backend/MigrationTool.GrpcServer.csproj`:
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\..\MigrationTool.Core\MigrationTool.Core.csproj" />
     <ProjectReference Include="..\..\MigrationTool.Core.Abstractions\MigrationTool.Core.Abstractions.csproj" />
   </ItemGroup>
   ```

2. **Inject your services** in `backend/Program.cs`:
   ```csharp
   builder.Services.AddSingleton<ISolutionAnalyzer, SolutionAnalyzer>();
   builder.Services.AddSingleton<IProjectAnalyzer, ProjectAnalyzer>();
   builder.Services.AddSingleton<IMigrationExecutor, MigrationExecutor>();
   ```

3. **Update service implementation** in `backend/Services/MigrationServiceImpl.cs`:
   - Replace mock data with actual calls to your Core services
   - Example:
     ```csharp
     var solution = await _solutionAnalyzer.AnalyzeSolutionAsync(request.SolutionPath);
     return MapToProtoResponse(solution);
     ```

## 🎯 Design System

The UI maintains the same design language as the Blazor prototype:

### Colors
- **Primary:** `#3498db` (blue)
- **Success:** `#27ae60` (green)
- **Warning:** `#f39c12` (orange)
- **Danger:** `#e74c3c` (red)

### Icons (same as Blazor)
- 📊 Dashboard
- 📁 Explorer
- 🔬 Analysis
- 📋 Planner
- ⚙️ Settings
- 📦 Library, 🧪 Test, 🖼️ WPF, etc.

### Layout
- **Sidebar Navigation** - Fixed left sidebar with icons
- **3-Panel Planner** - Actions → Steps → Details
- **Stats Cards** - Large numbers with icons
- **Gradient Headers** - Blue → Green gradients

## 📦 Key Technologies

### Frontend
- **React 18** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool (50ms hot reload!)
- **Tailwind CSS** - Styling
- **Zustand** - State management
- **React Router** - Navigation
- **gRPC-Web** - API communication
- **Lucide React** - Icons

### Backend
- **.NET 9** - Runtime
- **ASP.NET Core gRPC** - API framework
- **Protocol Buffers** - Serialization
- **Roslyn** - Code analysis (your existing Core)

## 🔗 gRPC API Endpoints

See `backend/Protos/migration_service.proto` for full API specification:

- `AnalyzeSolution` - Load and analyze .sln file
- `GetProjectDetails` - Get detailed project info
- `GetDependencyGraph` - Build dependency graph
- `ValidatePlan` - Validate migration plan
- `ExecuteMigration` - Execute plan with streaming progress
- `ListProjects` - Get all projects
- `AnalyzeNamespaces` - Analyze namespace usage
- `GetPackages` - Get package references

## 🚧 Next Steps

### Phase 1: Connect Backend
1. Link to your `MigrationTool.Core` projects
2. Replace mock data in `MigrationServiceImpl.cs`
3. Add proper error handling

### Phase 2: Advanced Features
1. **React Flow** - Interactive dependency graph visualization
2. **TanStack Table** - Advanced data tables with sorting/filtering
3. **Drag & Drop** - Reorder migration steps
4. **Real-time Streaming** - Show live progress during execution

### Phase 3: Polish
1. Loading states and spinners
2. Toast notifications
3. Keyboard shortcuts (Cmd+K command palette)
4. Export/Import migration plans
5. Dark mode

## 📝 Development Tips

### Hot Reload
- Frontend changes: **Instant** (50ms with Vite)
- Backend changes: **2-3 seconds** (.NET hot reload)

### Debugging
- Frontend: Browser DevTools (React DevTools extension recommended)
- Backend: Visual Studio / Rider / VS Code with C# extension

### State Management
- Global state: `useAppStore` (Zustand)
- Component state: `useState`
- No Redux needed - Zustand is simpler!

### Styling
- Use Tailwind utility classes
- Custom components in `index.css`
- Responsive: `md:`, `lg:` breakpoints

## 🎓 Learning Resources

- [React Docs](https://react.dev)
- [Vite Guide](https://vitejs.dev/guide/)
- [Tailwind CSS](https://tailwindcss.com/docs)
- [gRPC-Web](https://github.com/grpc/grpc-web)
- [Zustand](https://github.com/pmndrs/zustand)

## 🤝 Contributing

This is a migration from Blazor Server → React + gRPC. The Core .NET logic remains unchanged.

Key principles:
1. **Backend first** - Ensure gRPC endpoints work before UI
2. **Type safety** - Use TypeScript strictly
3. **Component-driven** - Build reusable components
4. **Performance** - Lazy load, memoize, virtualize large lists

## 📄 License

Same as your existing MigrationTool project.

---

**Built with ❤️ for better .NET project migrations**

🔗 Original Blazor prototype: `../tools/src/MigrationTool/MigrationTool.Blazor.Server/`
