$ErrorActionPreference = "Stop"
$root = "e:\Github\POSsystem"
$hostProject = Join-Path $root "aspnet-core\src\SmartPos.Web.Host\SmartPos.Web.Host.csproj"
$deployDir = Join-Path $root "publish\webhost"
$angularDist = Join-Path $root "angular\dist\verona-ng"
$backupDir = Join-Path $root ("publish\_backup_" + (Get-Date -Format "yyyyMMdd_HHmmss"))

New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

if (Test-Path (Join-Path $deployDir "appsettings.json")) {
    Copy-Item (Join-Path $deployDir "appsettings.json") (Join-Path $backupDir "appsettings.json") -Force
    Write-Host "Backed up appsettings.json"
}

$imageFolders = @(
    "CompanyImages","ProductImages","UserImages","BranchImages",
    "CustomerImages","OnlineOrderHeroImages","CategoryImages","DeveloperImages"
)
foreach ($folder in $imageFolders) {
    $src = Join-Path $deployDir ("wwwroot\" + $folder)
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $backupDir $folder) -Recurse -Force
        Write-Host ("Backed up wwwroot\" + $folder)
    }
}

Write-Host "==> Publishing ASP.NET Host..."
if (Test-Path $deployDir) {
    Remove-Item $deployDir -Recurse -Force
}
dotnet publish $hostProject -c Release -o $deployDir
if ($LASTEXITCODE -ne 0) {
    throw ("dotnet publish failed: " + $LASTEXITCODE)
}

$appsettingsPath = Join-Path $deployDir "appsettings.json"
$backupSettings = Join-Path $backupDir "appsettings.json"
if (Test-Path $backupSettings) {
    Copy-Item $backupSettings $appsettingsPath -Force
    Write-Host "Restored production appsettings.json"
}

# Prefer siteasp SQL for site105439; keep Auth/Email secrets from prior production backup when present.
$prodSettings = @{
  ConnectionStrings = @{
    Default = "Server=db78327.public.databaseasp.net; Database=db78327; User Id=db78327; Password=9Rn-e3+DF@i5; Encrypt=False; MultipleActiveResultSets=True;"
  }
  App = @{
    ServerRootAddress = "https://aljannatapp.com/"
    ClientRootAddress = "https://aljannatapp.com/"
    RunDbSeed = $false
    SkipDbMigration = $false
    CorsOrigins = "http://localhost:4200,http://localhost:4201,http://localhost:8080,http://localhost:8081,http://localhost:3000,https://aljannatapp.com,http://aljannatapp.com,https://www.aljannatapp.com,http://www.aljannatapp.com"
    BranchActivation = @{ ExpirationHours = 72 }
  }
  Authentication = @{
    JwtBearer = @{
      IsEnabled = "true"
      SecurityKey = "SmartPos_A9411127C2254434BDF2BAE9DEF9E25A"
      Issuer = "SmartPos"
      Audience = "SmartPos"
    }
    Google = @{
      IsEnabled = "true"
      ClientId = "364022858643-t59a2gq3hl1mligror79vbtavfaf5fsh.apps.googleusercontent.com"
      ClientSecret = "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"
    }
    EmailLogin = @{
      IsEnabled = "true"
      CodeLength = 6
      ExpirationMinutes = 5
      ResendCooldownSeconds = 60
    }
  }
  Email = @{
    SmtpHost = "mail2576.mailasp.net"
    SmtpPort = 587
    EnableSsl = $true
    UserName = "postmaster@aljannatapp.com"
    Password = "qT=6@y7Q9Ep#"
    FromAddress = "postmaster@aljannatapp.com"
    FromDisplayName = "Al Jannat"
  }
  Swagger = @{ ShowSummaries = $false }
}
if (Test-Path $backupSettings) {
    $prev = Get-Content $backupSettings -Raw | ConvertFrom-Json
    if ($prev.Authentication -and $prev.Authentication.JwtBearer -and $prev.Authentication.JwtBearer.SecurityKey) {
        $prodSettings.Authentication.JwtBearer.SecurityKey = [string]$prev.Authentication.JwtBearer.SecurityKey
    }
    if ($prev.Authentication -and $prev.Authentication.Google) {
        if ($prev.Authentication.Google.ClientId) { $prodSettings.Authentication.Google.ClientId = [string]$prev.Authentication.Google.ClientId }
        if ($prev.Authentication.Google.ClientSecret) { $prodSettings.Authentication.Google.ClientSecret = [string]$prev.Authentication.Google.ClientSecret }
        if ($prev.Authentication.Google.IsEnabled) { $prodSettings.Authentication.Google.IsEnabled = [string]$prev.Authentication.Google.IsEnabled }
    }
    if ($prev.Email) {
        foreach ($k in @("SmtpHost","UserName","Password","FromAddress","FromDisplayName")) {
            if ($prev.Email.$k) { $prodSettings.Email[$k] = [string]$prev.Email.$k }
        }
        if ($null -ne $prev.Email.SmtpPort) { $prodSettings.Email.SmtpPort = [int]$prev.Email.SmtpPort }
        if ($null -ne $prev.Email.EnableSsl) { $prodSettings.Email.EnableSsl = [bool]$prev.Email.EnableSsl }
    }
}
$prodSettings | ConvertTo-Json -Depth 20 | Set-Content $appsettingsPath -Encoding UTF8
Write-Host "Wrote production appsettings.json for aljannatapp.com (databaseasp)"

$webConfigPath = Join-Path $deployDir "web.config"
if (Test-Path $webConfigPath) {
    $wc = Get-Content $webConfigPath -Raw
    $wc = $wc -replace 'hostingModel="InProcess"', 'hostingModel="OutOfProcess"'
    $wc = $wc -replace 'stdoutLogEnabled="false"', 'stdoutLogEnabled="true"'
    Set-Content $webConfigPath -Value $wc -Encoding UTF8
    Write-Host "web.config: OutOfProcess + stdout logging"
}
New-Item -ItemType Directory -Path (Join-Path $deployDir "logs") -Force | Out-Null
Set-Content -Path (Join-Path $deployDir "logs\.keep") -Value ""

$wwwroot = Join-Path $deployDir "wwwroot"
if (-not (Test-Path $wwwroot)) {
    New-Item -ItemType Directory -Path $wwwroot | Out-Null
}

foreach ($folder in $imageFolders) {
    $src = Join-Path $backupDir $folder
    if (Test-Path $src) {
        $dest = Join-Path $wwwroot $folder
        if (-not (Test-Path $dest)) {
            New-Item -ItemType Directory -Path $dest | Out-Null
        }
        Copy-Item (Join-Path $src "*") $dest -Recurse -Force
    }
}

Write-Host "==> Copying Angular SPA..."
Copy-Item -Path (Join-Path $angularDist "*") -Destination $wwwroot -Recurse -Force
if (-not (Test-Path (Join-Path $wwwroot "index.html"))) {
    throw "index.html missing after Angular copy"
}

Write-Host ("DONE package: " + $deployDir)
Write-Host ("index.html: " + (Get-Item (Join-Path $wwwroot "index.html")).LastWriteTime)
Write-Host ("Host dll: " + (Get-Item (Join-Path $deployDir "SmartPos.Web.Host.dll")).LastWriteTime)
