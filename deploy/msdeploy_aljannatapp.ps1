$ErrorActionPreference = "Stop"

$msdeploy = "C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe"
if (-not (Test-Path $msdeploy)) {
    throw "msdeploy.exe not found"
}

$publishSettings = Join-Path $env:USERPROFILE "Downloads\aljannatapp.com-WebDeploy.publishSettings"
if (-not (Test-Path $publishSettings)) {
    throw ("Publish settings not found: " + $publishSettings)
}

[xml]$xml = Get-Content $publishSettings
$profile = $xml.publishData.publishProfile
$userName = $profile.userName
$password = $profile.userPWD
$site = $profile.msdeploySite
$serviceUrl = $profile.publishUrl

$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $root "publish"))) {
    $root = "d:\POSsystem"
}
$source = Join-Path $root "publish\webhost"
if (-not (Test-Path (Join-Path $source "SmartPos.Web.Host.dll"))) {
    $alt = Join-Path $root "publish\webhost_seo"
    if (Test-Path (Join-Path $alt "SmartPos.Web.Host.dll")) {
        $source = $alt
    }
}
if (-not (Test-Path (Join-Path $source "SmartPos.Web.Host.dll"))) {
    throw "Deploy package missing. Run assemble_webhost.ps1 first."
}
if (-not (Test-Path (Join-Path $source "wwwroot\index.html"))) {
    throw "Angular index.html missing in package wwwroot."
}

# ComputerName for WMSVC — siteasp requires ?site= on the axd URL
$computerName = "https://" + $serviceUrl + ":8172/msdeploy.axd?site=" + $site

# Leave uploaded pictures on the server. sync would otherwise delete any file
# the new package does not contain, and those folders only exist on the server.
$imageFolders = @(
    "CompanyImages","ProductImages","UserImages","BranchImages",
    "CustomerImages","OnlineOrderHeroImages","CategoryImages","DeveloperImages"
)
$skipArgs = @()
foreach ($folder in $imageFolders) {
    $pattern = "wwwroot[/\\]" + $folder
    $skipArgs += ("-skip:objectName=dirPath,absolutePath=" + $pattern)
    $skipArgs += ("-skip:objectName=filePath,absolutePath=" + $pattern + "[/\\]")
}

Write-Host ("==> Deploying to " + $site + " via " + $computerName)
Write-Host ("Source: " + $source)
Write-Host "Keeping server image folders: $($imageFolders -join ', ')"

& $msdeploy `
  -verb:sync `
  ("-source:contentPath=" + $source) `
  ("-dest:contentPath=" + $site + ",ComputerName='" + $computerName + "',UserName='" + $userName + "',Password='" + $password + "',AuthType='Basic'") `
  -allowUntrusted `
  -enableRule:AppOffline `
  -retryAttempts:3 `
  -retryInterval:3000 `
  @skipArgs

if ($LASTEXITCODE -ne 0) {
    throw ("msdeploy failed with exit code " + $LASTEXITCODE)
}

Write-Host "Deploy completed successfully."
