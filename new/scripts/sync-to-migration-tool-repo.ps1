# Sync MigrationTool changes to the standalone dotnet-migration-tool repository
# Usage: .\sync-to-migration-tool-repo.ps1 [-CommitMessage "message"] [-Push] [-DryRun]

param(
    [string]$CommitMessage = "",
    [switch]$Push,
    [switch]$DryRun,
    [string]$TargetRepo = "C:\Users\mrmar\source\repos\dotnet-migration-tool"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$toolsDir = Split-Path -Parent $scriptDir
$sourceDir = Join-Path $toolsDir "src\MigrationTool"
$testsSourceDir = Join-Path $toolsDir "tests\MigrationTool"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Sync to dotnet-migration-tool repo" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate paths
if (-not (Test-Path $sourceDir)) {
    Write-Host "ERROR: Source directory not found: $sourceDir" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $TargetRepo)) {
    Write-Host "ERROR: Target repository not found: $TargetRepo" -ForegroundColor Red
    exit 1
}

$targetSrcDir = Join-Path $TargetRepo "src\MigrationTool"
$targetTestsDir = Join-Path $TargetRepo "tests\MigrationTool"

Write-Host "Source: $sourceDir" -ForegroundColor Yellow
Write-Host "Target: $targetSrcDir" -ForegroundColor Yellow
Write-Host ""

if ($DryRun) {
    Write-Host "[DRY RUN] No changes will be made" -ForegroundColor Magenta
    Write-Host ""
}

# Define what to sync (only source files, not bin/obj)
$srcFolders = @(
    "MigrationTool.Cli",
    "MigrationTool.Core",
    "MigrationTool.Core.Abstractions"
)

$testFolders = @(
    "MigrationTool.Cli.Tests"
)

$excludePatterns = @(
    "bin",
    "obj",
    "*.user",
    ".vs"
)

function Copy-FilteredDirectory {
    param(
        [string]$Source,
        [string]$Target,
        [string[]]$Exclude
    )
    
    if (-not (Test-Path $Source)) {
        Write-Host "  SKIP: Source not found: $Source" -ForegroundColor Gray
        return 0
    }
    
    $filesCopied = 0
    
    # Get all files excluding patterns
    $files = Get-ChildItem -Path $Source -Recurse -File | Where-Object {
        $relativePath = $_.FullName.Substring($Source.Length + 1)
        $exclude = $false
        foreach ($pattern in $Exclude) {
            if ($relativePath -like "*$pattern*") {
                $exclude = $true
                break
            }
        }
        -not $exclude
    }
    
    foreach ($file in $files) {
        $relativePath = $file.FullName.Substring($Source.Length + 1)
        $targetPath = Join-Path $Target $relativePath
        $targetDir = Split-Path -Parent $targetPath
        
        if (-not $DryRun) {
            if (-not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            }
            Copy-Item -Path $file.FullName -Destination $targetPath -Force
        }
        
        $filesCopied++
    }
    
    return $filesCopied
}

# Sync source folders
Write-Host "Syncing source folders..." -ForegroundColor Green
$totalFiles = 0

foreach ($folder in $srcFolders) {
    $srcPath = Join-Path $sourceDir $folder
    $tgtPath = Join-Path $targetSrcDir $folder
    
    Write-Host "  $folder" -ForegroundColor White
    $count = Copy-FilteredDirectory -Source $srcPath -Target $tgtPath -Exclude $excludePatterns
    Write-Host "    -> $count files" -ForegroundColor Gray
    $totalFiles += $count
}

# Sync test folders
Write-Host ""
Write-Host "Syncing test folders..." -ForegroundColor Green

foreach ($folder in $testFolders) {
    $srcPath = Join-Path $testsSourceDir $folder
    $tgtPath = Join-Path $targetTestsDir $folder
    
    Write-Host "  $folder" -ForegroundColor White
    $count = Copy-FilteredDirectory -Source $srcPath -Target $tgtPath -Exclude $excludePatterns
    Write-Host "    -> $count files" -ForegroundColor Gray
    $totalFiles += $count
}

# Sync test configuration files
Write-Host ""
Write-Host "Syncing test configuration..." -ForegroundColor Green
$testConfigFiles = @(
    "test.runsettings",
    "run-cli-tests.ps1"
)

$testsRootSource = Join-Path $toolsDir "tests"
$testsRootTarget = Join-Path $TargetRepo "tests"

foreach ($configFile in $testConfigFiles) {
    $srcFile = Join-Path $testsRootSource $configFile
    $tgtFile = Join-Path $testsRootTarget $configFile
    
    if (Test-Path $srcFile) {
        if (-not $DryRun) {
            Copy-Item -Path $srcFile -Destination $tgtFile -Force
        }
        Write-Host "  $configFile" -ForegroundColor White
        $totalFiles++
    }
}

# Sync Directory.Packages.props
Write-Host ""
Write-Host "Syncing package configuration..." -ForegroundColor Green
$packagesPropsSource = Join-Path $toolsDir "Directory.Packages.props"
$packagesPropsTarget = Join-Path $TargetRepo "Directory.Packages.props"

if (Test-Path $packagesPropsSource) {
    if (-not $DryRun) {
        Copy-Item -Path $packagesPropsSource -Destination $packagesPropsTarget -Force
    }
    Write-Host "  Directory.Packages.props" -ForegroundColor White
    $totalFiles++
}

Write-Host ""
Write-Host "Total files synced: $totalFiles" -ForegroundColor Cyan

# Git operations
if (-not $DryRun) {
    Write-Host ""
    Write-Host "Git operations..." -ForegroundColor Green
    
    Push-Location $TargetRepo
    
    try {
        # Check for changes
        $status = git status --porcelain
        
        if ($status) {
            Write-Host "  Changes detected:" -ForegroundColor Yellow
            git status --short
            
            # Stage all changes
            git add -A
            
            # Commit if message provided
            if ($CommitMessage) {
                Write-Host ""
                Write-Host "  Committing..." -ForegroundColor Yellow
                git commit -m $CommitMessage
                
                if ($Push) {
                    Write-Host ""
                    Write-Host "  Pushing to origin..." -ForegroundColor Yellow
                    git push origin master
                }
            } else {
                Write-Host ""
                Write-Host "  Changes staged. Use -CommitMessage to commit." -ForegroundColor Yellow
            }
        } else {
            Write-Host "  No changes detected" -ForegroundColor Gray
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
