$ErrorActionPreference = "Stop"

Write-Host "--- Provisioning Clean Build Environment (VS Build Tools) ---" -ForegroundColor Cyan

# 1. Uninstall .NET 9 / Modern SDKs
Write-Host "Removing Modern .NET SDKs (Cleaning Environment)..."
$sdks = Get-ChildItem -Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" |
Get-ItemProperty | 
Where-Object { $_.DisplayName -like "*Microsoft .NET*SDK*" -or $_.DisplayName -like "*Microsoft .NET*Runtime*" }

if ($sdks) {
    foreach ($sdk in $sdks) {
        Write-Host "Uninstalling: $($sdk.DisplayName)..."
        $args = "/x $($sdk.PSChildName) /quiet /norestart"
        Start-Process "msiexec.exe" -ArgumentList $args -Wait -NoNewWindow
    }
}
else {
    Write-Host "No Modern .NET SDKs found."
}

# 2. Install Visual Studio Build Tools 2022
# Components: 
# - Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools (MSBuild, Roslyn, etc.)
# - Microsoft.Net.Component.4.8.1.TargetingPack (The target)
# - Microsoft.Net.Component.4.8.1.SDK (The SDK reference assemblies)
$Url = "https://aka.ms/vs/17/release/vs_BuildTools.exe"
$Installer = "$env:TEMP\vs_BuildTools.exe"

Write-Host "Downloading VS Build Tools 2022..."
Invoke-WebRequest -Uri $Url -OutFile $Installer

Write-Host "Installing VS Build Tools (This may take 10-15 minutes)..." -ForegroundColor Cyan
$ArgsList = "--quiet --wait --norestart --nocache `
    --add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools `
    --add Microsoft.Net.Component.4.8.1.TargetingPack `
    --add Microsoft.Net.Component.4.8.1.SDK `
    --includeRecommended"

Start-Process -FilePath $Installer -ArgumentList $ArgsList -Wait -NoNewWindow

# 3. Add MSBuild and NuGet to PATH
# Typical Path: C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin
$MSBuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin"
$CurrentPath = [Environment]::GetEnvironmentVariable("Path", "Machine")

if ($CurrentPath -notlike "*$MSBuildPath*") {
    Write-Host "Adding MSBuild to PATH..."
    [Environment]::SetEnvironmentVariable("Path", "$CurrentPath;$MSBuildPath", "Machine")
}

# 4. Download NuGet.exe (Required for restore since we deleted 'dotnet')
$NuGetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$NuGetDir = "C:\ProgramData\chocolatey\bin" # Or just a tools dir
if (!(Test-Path $NuGetDir)) { New-Item -ItemType Directory -Force -Path $NuGetDir | Out-Null }
Invoke-WebRequest -Uri $NuGetUrl -OutFile "$NuGetDir\nuget.exe"

if ($CurrentPath -notlike "*$NuGetDir*") {
    Write-Host "Adding NuGet to PATH..."
    $CurrentPath = [Environment]::GetEnvironmentVariable("Path", "Machine") 
    [Environment]::SetEnvironmentVariable("Path", "$CurrentPath;$NuGetDir", "Machine")
}

Write-Host "Clean Environment Provisioned. MSBuild & NuGet Ready." -ForegroundColor Green
Write-Host "Please RESTART the Shell/VM to refresh PATH."
