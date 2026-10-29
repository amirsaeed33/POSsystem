# Code First: apply pending EF Core migrations to the Default connection string.
# Usage (from aspnet-core folder):
#   .\update-database.ps1
#   .\update-database.ps1 -Migration Sync_CodeFirst_Model

param(
    [string]$Migration = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$project = "src\SmartPos.EntityFrameworkCore\SmartPos.EntityFrameworkCore.csproj"
$startup = "src\SmartPos.Web.Host\SmartPos.Web.Host.csproj"

Write-Host "Applying EF Core migrations (Code First)..." -ForegroundColor Cyan

if ([string]::IsNullOrWhiteSpace($Migration)) {
    dotnet ef database update --context SmartPosDbContext --project $project --startup-project $startup
} else {
    dotnet ef database update $Migration --context SmartPosDbContext --project $project --startup-project $startup
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "database update failed. Stop IIS Express / running API if DLLs are locked, then retry." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Database update completed." -ForegroundColor Green
Write-Host ""
Write-Host "New migration next time:" -ForegroundColor Yellow
Write-Host "  dotnet ef migrations add YourName --context SmartPosDbContext --project $project --startup-project $startup"
Write-Host "  .\update-database.ps1"
