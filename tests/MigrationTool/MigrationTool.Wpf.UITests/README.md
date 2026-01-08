# MigrationTool.Wpf UI Tests

UI tests for the WPF Migration Tool application using Appium and WinAppDriver.

## Prerequisites

1. **WinAppDriver** - Download and install from:
   https://github.com/microsoft/WinAppDriver/releases

2. **Developer Mode** - Enable Developer Mode in Windows Settings:
   - Settings → Update & Security → For developers → Developer Mode

## Running Tests

### 1. Start WinAppDriver

```powershell
# Run as Administrator
"C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe"
```

WinAppDriver will start listening on `http://127.0.0.1:4723`

### 2. Build the WPF Application

```powershell
dotnet build tools/src/MigrationTool/MigrationTool.Wpf/MigrationTool.Wpf.csproj
```

### 3. Run UI Tests

```powershell
dotnet test tools/tests/MigrationTool/MigrationTool.Wpf.UITests/MigrationTool.Wpf.UITests.csproj
```

## Test Categories

Tests are organized by category using xUnit traits:

- `Startup` - Application startup tests
- `Navigation` - Page navigation tests
- `Settings` - Settings page functionality

Run specific categories:

```powershell
dotnet test --filter "Category=Navigation"
```

## Troubleshooting

### WinAppDriver not found

Make sure WinAppDriver is installed and running. The default path is:
`C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe`

### Tests are skipped

If tests are skipped with "WinAppDriver is not running", ensure:
1. WinAppDriver is running
2. It's accessible at `http://127.0.0.1:4723`
3. Developer Mode is enabled in Windows

### Application not found

Verify the application path in `Infrastructure/AppiumSetup.cs`:
- Default: `tools/src/MigrationTool/MigrationTool.Wpf/bin/Debug/net9.0-windows/MigrationTool.Wpf.exe`

## Test Structure

```
MigrationTool.Wpf.UITests/
├── Infrastructure/
│   ├── AppiumSetup.cs      # Driver configuration
│   └── UITestBase.cs       # Base class with helpers
├── Tests/
│   ├── ApplicationStartupTests.cs
│   ├── NavigationTests.cs
│   └── SettingsPageTests.cs
├── UITestCollection.cs     # Sequential test execution
└── README.md
```
