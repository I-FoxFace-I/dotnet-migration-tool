# MigrationTool Deployment Guide

This directory contains deployment scripts and release versions of the MigrationTool.

## Quick Start

### Deploy Blazor Server (Web)

```bash
python deploy.py --platform blazor --version 1.0.0
```

### Deploy MAUI (Desktop)

```bash
python deploy.py --platform maui --version 1.0.0
```

### Deploy Both

```bash
python deploy.py --platform all --version 1.0.0
```

## Deployment Options

### Python Script (Recommended)

```bash
python deploy.py [options]
```

**Options:**
- `--platform {blazor|maui|all}` - Platform to deploy (default: blazor)
- `--version VERSION` - Version number (default: 1.0.0)
- `--configuration {Debug|Release}` - Build configuration (default: Release)

**Examples:**

```bash
# Deploy Blazor Server version 1.2.3
python deploy.py --platform blazor --version 1.2.3

# Deploy MAUI with Debug configuration
python deploy.py --platform maui --version 1.0.0 --configuration Debug

# Deploy both platforms
python deploy.py --platform all --version 1.0.0
```

### PowerShell Script (Legacy)

```powershell
.\deploy-blazor.ps1 -Version "1.0.0" -Configuration "Release"
```

## Output Structure

```
Deploy/
├── Blazor/
│   └── v1.0.0/
│       ├── MigrationTool.Blazor.Server.dll
│       ├── wwwroot/
│       ├── appsettings.json
│       ├── deploy-info.json
│       └── run.bat
│
└── Maui/
    └── v1.0.0/
        ├── MigrationTool.Maui.exe
        ├── MigrationTool.Maui.dll
        ├── deploy-info.json
        └── run.bat
```

## Running Deployed Applications

### Blazor Server

#### Option 1: Direct Execution

```bash
cd Deploy/Blazor/v1.0.0
dotnet MigrationTool.Blazor.Server.dll
```

The application will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

#### Option 2: Windows Service

Install as a Windows Service using [NSSM](https://nssm.cc/):

```powershell
# Install NSSM
choco install nssm

# Create service
nssm install MigrationTool "C:\path\to\dotnet.exe" "C:\path\to\MigrationTool.Blazor.Server.dll"
nssm set MigrationTool AppDirectory "C:\path\to\deploy\folder"
nssm set MigrationTool DisplayName "MigrationTool Blazor Server"
nssm start MigrationTool
```

#### Option 3: IIS Deployment

1. Install [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Create an IIS Application Pool:
   - .NET CLR Version: No Managed Code
   - Managed Pipeline Mode: Integrated
3. Create an IIS Application:
   - Physical path: Point to deployment folder
   - Application pool: Select the created pool
4. Configure bindings (HTTP/HTTPS)

**web.config example:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\MigrationTool.Blazor.Server.dll"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

### MAUI Desktop

#### Windows

Simply double-click `run.bat` or execute `MigrationTool.Maui.exe`.

**System Requirements:**
- Windows 10 version 1809 (build 17763) or later
- Windows 11 (recommended)
- .NET 9.0 Runtime (included if self-contained)

#### Create Desktop Shortcut

Right-click `MigrationTool.Maui.exe` → Send to → Desktop (create shortcut)

## Configuration

### Blazor Server

Edit `appsettings.json` in the deployment folder:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001"
      }
    }
  }
}
```

### MAUI

Configuration is embedded in the application. Settings can be changed through the Settings page.

## Troubleshooting

### Blazor Server

**Port already in use:**
```bash
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

**Missing .NET Runtime:**
Install .NET 9.0 Runtime from https://dotnet.microsoft.com/download

### MAUI

**Application doesn't start:**
- Check Windows version (must be 1809+)
- Install .NET 9.0 Desktop Runtime
- Check Windows Event Viewer for errors

**Performance issues:**
- Close other applications
- Check antivirus exceptions
- Run as administrator if file access issues

## Security Considerations

### Blazor Server

- Change default ports for production
- Use HTTPS in production (configure certificate)
- Implement authentication/authorization if needed
- Configure CORS if accessed from other domains
- Set up firewall rules

### MAUI

- Application runs with user privileges
- File system access limited to user permissions
- No network access required (runs locally)

## Update Process

1. Deploy new version to new folder (e.g., `v1.1.0`)
2. Test the new version
3. Update service/IIS configuration to point to new version
4. Keep old version for rollback
5. After verification, delete old version

## Monitoring

### Blazor Server

Monitor application logs:
```bash
tail -f logs/stdout.log
```

Use Application Insights or similar for production monitoring.

### MAUI

Application logs are written to:
- Debug output in development
- Application data folder in production

## Backup

Before upgrading:

1. Backup current deployment folder
2. Export any user data (migration plans, settings)
3. Note current configuration

## Rollback

To rollback to previous version:

1. Stop the application/service
2. Restore previous deployment folder
3. Update service/IIS configuration
4. Restart application/service

---

**Last Updated:** 2025-01-06  
**Version:** 1.0.0
