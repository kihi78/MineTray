# MineTray Build Script
# This script builds the release package

param(
    [switch]$Clean,
    [switch]$Publish,
    [switch]$CreateZip
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$PublishDir = Join-Path $ProjectRoot "publish"
$DistDir = Join-Path $ProjectRoot "dist"

Write-Host "=== MineTray Build Script ===" -ForegroundColor Cyan

# Clean
if ($Clean) {
    Write-Host "Cleaning..." -ForegroundColor Yellow
    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
    if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
    dotnet clean MineTray/MineTray.csproj -c Release
}

# Build
Write-Host "Building..." -ForegroundColor Yellow
dotnet build MineTray/MineTray.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# Test
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test MineTray.Tests/MineTray.Tests.csproj
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

# Publish
if ($Publish) {
    Write-Host "Publishing self-contained executable..." -ForegroundColor Yellow
    dotnet publish MineTray/MineTray.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $PublishDir
    
    if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
    
    $exeSize = (Get-Item "$PublishDir\MineTray.exe").Length / 1MB
    Write-Host "Created: $PublishDir\MineTray.exe ($([math]::Round($exeSize, 2)) MB)" -ForegroundColor Green
}

# Create ZIP
if ($CreateZip) {
    if (-not (Test-Path $PublishDir)) {
        throw "Run with -Publish first"
    }
    
    Write-Host "Creating ZIP package..." -ForegroundColor Yellow
    
    if (-not (Test-Path $DistDir)) { New-Item $DistDir -ItemType Directory | Out-Null }
    
    $version = "1.0.0"
    $zipPath = Join-Path $DistDir "MineTray_v$version.zip"
    
    Compress-Archive -Path "$PublishDir\*" -DestinationPath $zipPath -Force
    
    $zipSize = (Get-Item $zipPath).Length / 1MB
    Write-Host "Created: $zipPath ($([math]::Round($zipSize, 2)) MB)" -ForegroundColor Green
}

Write-Host "=== Build Complete ===" -ForegroundColor Cyan
