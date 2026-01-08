# Run MigrationTool CLI tests
# Usage: .\run-cli-tests.ps1 [-Filter <pattern>] [-Verbose] [-NoBuild]

param(
    [string]$Filter = "",
    [switch]$Verbose,
    [switch]$NoBuild,
    [switch]$Coverage
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProject = Join-Path $scriptDir "MigrationTool\MigrationTool.Cli.Tests\MigrationTool.Cli.Tests.csproj"
$runsettings = Join-Path $scriptDir "test.runsettings"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MigrationTool CLI Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Build arguments
$args = @("test", $testProject)

if (-not $NoBuild) {
    Write-Host "Building test project..." -ForegroundColor Yellow
} else {
    $args += "--no-build"
}

# Add runsettings
$args += "--settings"
$args += $runsettings

# Add filter if specified
if ($Filter) {
    $args += "--filter"
    $args += $Filter
    Write-Host "Filter: $Filter" -ForegroundColor Yellow
}

# Verbosity
if ($Verbose) {
    $args += "--verbosity"
    $args += "normal"
} else {
    $args += "--verbosity"
    $args += "minimal"
}

# Coverage
if ($Coverage) {
    $args += "--collect"
    $args += "XPlat Code Coverage"
    Write-Host "Code coverage enabled" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Running: dotnet $($args -join ' ')" -ForegroundColor Gray
Write-Host ""

# Run tests
& dotnet @args

$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "All tests passed!" -ForegroundColor Green
} else {
    Write-Host "Some tests failed (exit code: $exitCode)" -ForegroundColor Red
}

exit $exitCode
