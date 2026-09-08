## Backend Deploy Script (PowerShell)
# This script publishes the ASP.NET Core API and copies it to an IIS site.
# Assumes IIS site folder: C:\inetpub\wwwroot\SmartPos

# Step 1: Navigate to the ASP.NET Core project root
$projectRoot = "E:\\Github\\POSsystem\\aspnet-core"
Set-Location $projectRoot

# Step 2: Publish the project (Release configuration)
$publishDir = "E:\\Github\\POSsystem\\iis_deploy\\SmartPos"
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
dotnet publish "./src/SmartPos.Web" -c Release -o $publishDir

# Step 3: Ensure the IIS site folder exists
$iisFolder = "C:\\inetpub\\wwwroot\\SmartPos"
if (-not (Test-Path $iisFolder)) {
    New-Item -ItemType Directory -Path $iisFolder | Out-Null
}

# Step 4: Copy published files to IIS folder
Copy-Item -Path "$publishDir\\*" -Destination $iisFolder -Recurse -Force

Write-Host "✅ Backend deployed to $iisFolder"
