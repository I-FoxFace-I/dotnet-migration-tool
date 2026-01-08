# run-ui-tests.ps1
# Runner script for WPF UI tests with WinAppDriver

param(
    [switch]$SkipBuild,
    [switch]$SkipWinAppDriver,
    [string]$Filter,
    [ValidateSet("Minimal", "Normal", "Detailed", "Diagnostic")]
    [string]$Verbosity = "Normal"
)

$ErrorActionPreference = "Stop"
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Resolve-Path (Join-Path $scriptPath "..\..\..\..\")

Write-Host "=== WPF UI Tests Runner ===" -ForegroundColor Cyan
Write-Host "Root path: $rootPath" -ForegroundColor Gray
Write-Host ""

# Configuration
$WinAppDriverUrl = "http://127.0.0.1:4723"
$WinAppDriverPath = "C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe"
$WpfProjectPath = Join-Path $rootPath "tools\src\MigrationTool\MigrationTool.Wpf\MigrationTool.Wpf.csproj"
$TestProjectPath = Join-Path $rootPath "tools\tests\MigrationTool\MigrationTool.Wpf.UITests\MigrationTool.Wpf.UITests.csproj"

# Function to check if WinAppDriver is running
function Test-WinAppDriverRunning {
    try {
        $response = Invoke-WebRequest -Uri "$WinAppDriverUrl/status" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

# Function to start WinAppDriver
function Start-WinAppDriver {
    Write-Host "Checking WinAppDriver..." -ForegroundColor Yellow
    
    if (Test-WinAppDriverRunning) {
        Write-Host "✓ WinAppDriver is already running" -ForegroundColor Green
        return $true
    }
    
    if (-not (Test-Path $WinAppDriverPath)) {
        Write-Host "✗ WinAppDriver not found at: $WinAppDriverPath" -ForegroundColor Red
        Write-Host "  Please install WinAppDriver from:" -ForegroundColor Yellow
        Write-Host "  https://github.com/microsoft/WinAppDriver/releases" -ForegroundColor Yellow
        return $false
    }
    
    Write-Host "Starting WinAppDriver..." -ForegroundColor Yellow
    Write-Host "  Note: WinAppDriver requires Administrator privileges" -ForegroundColor Gray
    
    try {
        # Check if running as admin
        $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        
        if ($isAdmin) {
            Start-Process -FilePath $WinAppDriverPath -WindowStyle Minimized
            Start-Sleep -Seconds 3
            
            if (Test-WinAppDriverRunning) {
                Write-Host "✓ WinAppDriver started successfully" -ForegroundColor Green
                return $true
            }
            else {
                Write-Host "✗ Failed to start WinAppDriver" -ForegroundColor Red
                return $false
            }
        }
        else {
            Write-Host "⚠ Not running as Administrator. Attempting to start WinAppDriver..." -ForegroundColor Yellow
            Start-Process -FilePath "powershell" -ArgumentList "-Command", "Start-Process -FilePath '$WinAppDriverPath' -Verb RunAs -WindowStyle Minimized" -Verb RunAs
            Start-Sleep -Seconds 3
            
            if (Test-WinAppDriverRunning) {
                Write-Host "✓ WinAppDriver started successfully" -ForegroundColor Green
                return $true
            }
            else {
                Write-Host "✗ Failed to start WinAppDriver. Please start it manually:" -ForegroundColor Red
                Write-Host "  Run as Administrator: '$WinAppDriverPath'" -ForegroundColor Yellow
                return $false
            }
        }
    }
    catch {
        Write-Host "✗ Error starting WinAppDriver: $_" -ForegroundColor Red
        Write-Host "  Please start WinAppDriver manually as Administrator" -ForegroundColor Yellow
        return $false
    }
}

# Function to build WPF application
function Build-WpfApplication {
    if ($SkipBuild) {
        Write-Host "⏭ Skipping build (--SkipBuild specified)" -ForegroundColor Yellow
        return $true
    }
    
    Write-Host "Building WPF application..." -ForegroundColor Yellow
    
    if (-not (Test-Path $WpfProjectPath)) {
        Write-Host "✗ WPF project not found: $WpfProjectPath" -ForegroundColor Red
        return $false
    }
    
    try {
        $buildResult = dotnet build $WpfProjectPath -c Debug --verbosity Minimal 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Build successful" -ForegroundColor Green
            return $true
        }
        else {
            Write-Host "✗ Build failed" -ForegroundColor Red
            Write-Host $buildResult
            return $false
        }
    }
    catch {
        Write-Host "✗ Build error: $_" -ForegroundColor Red
        return $false
    }
}

# Function to run tests
function Run-UITests {
    Write-Host "Running UI tests..." -ForegroundColor Yellow
    
    if (-not (Test-Path $TestProjectPath)) {
        Write-Host "✗ Test project not found: $TestProjectPath" -ForegroundColor Red
        return $false
    }
    
    if (-not (Test-WinAppDriverRunning)) {
        Write-Host "✗ WinAppDriver is not running. Tests will be skipped." -ForegroundColor Red
        Write-Host "  Run this script with WinAppDriver running or allow it to start automatically" -ForegroundColor Yellow
        return $false
    }
    
    try {
        $testArgs = @(
            "test",
            $TestProjectPath,
            "--verbosity", $Verbosity,
            "--no-build"
        )
        
        if ($Filter) {
            $testArgs += "--filter"
            $testArgs += $Filter
            Write-Host "  Filter: $Filter" -ForegroundColor Gray
        }
        
        Write-Host ""
        $testResult = dotnet $testArgs 2>&1
        Write-Host $testResult
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "✓ All tests passed!" -ForegroundColor Green
            return $true
        }
        else {
            Write-Host ""
            Write-Host "✗ Some tests failed" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "✗ Test execution error: $_" -ForegroundColor Red
        return $false
    }
}

# Main execution
Write-Host "Step 1: WinAppDriver Setup" -ForegroundColor Cyan
Write-Host "------------------------" -ForegroundColor Cyan

if ($SkipWinAppDriver) {
    Write-Host "⏭ Skipping WinAppDriver check (--SkipWinAppDriver specified)" -ForegroundColor Yellow
    if (-not (Test-WinAppDriverRunning)) {
        Write-Host "⚠ Warning: WinAppDriver does not appear to be running" -ForegroundColor Yellow
    }
}
else {
    if (-not (Start-WinAppDriver)) {
        Write-Host ""
        Write-Host "⚠ Cannot proceed without WinAppDriver. Exiting." -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""
Write-Host "Step 2: Build Application" -ForegroundColor Cyan
Write-Host "------------------------" -ForegroundColor Cyan

if (-not (Build-WpfApplication)) {
    Write-Host ""
    Write-Host "✗ Cannot proceed without successful build. Exiting." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 3: Run Tests" -ForegroundColor Cyan
Write-Host "------------------------" -ForegroundColor Cyan

$testSuccess = Run-UITests

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "------------------------" -ForegroundColor Cyan

if ($testSuccess) {
    Write-Host "✓ All checks passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "✗ Tests failed or skipped" -ForegroundColor Red
    exit 1
}