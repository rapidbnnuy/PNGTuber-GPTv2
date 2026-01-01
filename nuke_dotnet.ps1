$ErrorActionPreference = "Stop"
Write-Host "--- ORBITAL LASER: Destroying .NET SDK Folders ---" -ForegroundColor Red

$Paths = @(
    "C:\Program Files\dotnet",
    "C:\Program Files (x86)\dotnet",
    "$env:USERPROFILE\.dotnet"
)

foreach ($p in $Paths) {
    if (Test-Path $p) {
        Write-Host "Deleting $p ..." -ForegroundColor Yellow
        Remove-Item -Path $p -Recurse -Force -ErrorAction Continue
    }
    else {
        Write-Host "$p not found."
    }
}

# Locate VSTest because we need it for CI
$VSTestSearch = Get-ChildItem -Path "C:\Program Files (x86)\Microsoft Visual Studio" -Recurse -Filter "vstest.console.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($VSTestSearch) {
    $Dir = $VSTestSearch.DirectoryName
    Write-Host "Found VSTest at: $Dir" -ForegroundColor Green
    
    $CurrentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")
    if ($CurrentPath -notlike "*$Dir*") {
        Write-Host "Adding VSTest to PATH..."
        [Environment]::SetEnvironmentVariable("Path", "$CurrentPath;$Dir", "Machine")
    }
}
else {
    Write-Host "WARNING: VSTest.console.exe not found!" -ForegroundColor Red
}

Write-Host "Cleanup Complete."
