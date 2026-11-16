# setup_flutter.ps1
$zipPath = "E:\flutter_windows.zip"
$destDir = "E:\"

Write-Host "Preparing destination E:\flutter..." -ForegroundColor Cyan
if (Test-Path "E:\flutter") {
    Remove-Item -Path "E:\flutter" -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Extracting Flutter SDK to E:\flutter (this takes about 1-2 minutes)..." -ForegroundColor Cyan
tar.exe -xf $zipPath -C $destDir

Write-Host "Verifying flutter installation..." -ForegroundColor Cyan
& "E:\flutter\bin\flutter.bat" doctor

Write-Host "Running flutter pub get for flutter_app..." -ForegroundColor Cyan
Set-Location "E:\Github\POSsystem\flutter_app"
& "E:\flutter\bin\flutter.bat" pub get

Write-Host "Flutter & Dart setup is complete! Open flutter_app in VS Code and press F5." -ForegroundColor Green


