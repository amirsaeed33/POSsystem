## Frontend Deploy Script (PowerShell)
# Builds the Angular app and copies the output to an IIS site.
# Assumes IIS site folder: C:\inetpub\wwwroot\SmartPos\frontend

# Step 1: Set working directory to the Angular project
$angularRoot = "E:\\Github\\POSsystem\\angular"
Set-Location $angularRoot

# Step 2: Install dependencies (if needed)
# npm ci   # Uncomment if a clean install is required

# Step 3: Build the Angular app for production
npm run build -- --prod

# The compiled files are placed in the "dist" folder (exact name may vary)
$distFolder = Get-ChildItem -Path "$angularRoot\\dist" -Directory | Select-Object -First 1
if (-not $distFolder) {
    Write-Error "❌ Build output not found. Ensure the Angular project builds successfully."
    exit 1
}

# Step 4: Define IIS target folder for the frontend
$frontendIisFolder = "C:\\inetpub\\wwwroot\\SmartPos\\frontend"
if (-not (Test-Path $frontendIisFolder)) {
    New-Item -ItemType Directory -Path $frontendIisFolder | Out-Null
}

# Step 5: Copy the built files to IIS
Copy-Item -Path "$($distFolder.FullName)\\*" -Destination $frontendIisFolder -Recurse -Force

Write-Host "✅ Frontend deployed to $frontendIisFolder"
