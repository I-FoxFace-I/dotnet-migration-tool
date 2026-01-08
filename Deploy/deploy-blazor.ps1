# Deploy script for MigrationTool Blazor Server
# This script builds and publishes the Blazor Server application for release

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\Blazor\v$Version"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MigrationTool Blazor Server Deploy" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get the script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionPath = Join-Path $ScriptDir "..\MigrationTool.sln"
$ProjectPath = Join-Path $ScriptDir "..\src\MigrationTool\MigrationTool.Blazor.Server\MigrationTool.Blazor.Server.csproj"
$OutputFullPath = Join-Path $ScriptDir $OutputPath

# Verify solution exists
if (-not (Test-Path $SolutionPath)) {
    Write-Host "Error: Solution file not found at $SolutionPath" -ForegroundColor Red
    exit 1
}

# Verify project exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "Error: Project file not found at $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build $SolutionPath --configuration $Configuration --no-incremental

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

Write-Host "Publishing Blazor Server application..." -ForegroundColor Yellow
Write-Host "Output: $OutputFullPath" -ForegroundColor Gray

# Create output directory
if (Test-Path $OutputFullPath) {
    Write-Host "Cleaning existing output directory..." -ForegroundColor Yellow
    Remove-Item $OutputFullPath -Recurse -Force
}

New-Item -ItemType Directory -Path $OutputFullPath -Force | Out-Null

# Publish the application
dotnet publish $ProjectPath `
    --configuration $Configuration `
    --output $OutputFullPath `
    --self-contained false `
    --runtime win-x64 `
    /p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Publish successful!" -ForegroundColor Green
Write-Host ""

# Create deployment info file
$DeployInfo = @{
    Version = $Version
    Configuration = $Configuration
    BuildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    OutputPath = $OutputFullPath
    ProjectPath = $ProjectPath
}

$DeployInfoPath = Join-Path $OutputFullPath "deploy-info.json"
$DeployInfo | ConvertTo-Json | Out-File $DeployInfoPath -Encoding UTF8

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output location: $OutputFullPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "To run the application:" -ForegroundColor Cyan
Write-Host "  cd `"$OutputFullPath`"" -ForegroundColor White
Write-Host "  dotnet MigrationTool.Blazor.Server.dll" -ForegroundColor White
Write-Host ""
Write-Host "Or configure as a Windows Service or IIS application." -ForegroundColor Gray
Write-Host ""
