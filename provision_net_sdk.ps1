$ErrorActionPreference = "Stop"

Write-Host "--- Provisioning .NET Framework 4.8.1 Environment ---" -ForegroundColor Cyan

# 1. Install .NET Framework 4.8.1 Developer Pack (Targeting Pack)
# Required to build apps targeting 'net481'
$DevPackUrl = "https://go.microsoft.com/fwlink/?linkid=2203304"
$DevPackInstaller = "$env:TEMP\ndp481-devpack-enu.exe"

Write-Host "Downloading .NET Framework 4.8.1 Developer Pack..."
Invoke-WebRequest -Uri $DevPackUrl -OutFile $DevPackInstaller

Write-Host "Installing .NET Framework 4.8.1 Developer Pack..." -ForegroundColor Cyan
# /quiet /norestart
$args = "/quiet /norestart"
Start-Process -FilePath $DevPackInstaller -ArgumentList $args -Wait -NoNewWindow

# 2. Verify Install
$RegPath = "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
$Release = Get-ItemProperty -Path $RegPath -Name Release -ErrorAction SilentlyContinue
if ($Release -and $Release.Release -ge 533320) {
    Write-Host ".NET Framework 4.8.1 detected (Release: $($Release.Release))." -ForegroundColor Green
}
else {
    Write-Host "WARNING: .NET Framework 4.8.1 might not be fully installed. Release: $($Release.Release)" -ForegroundColor Yellow
}

# 3. Ensure .NET SDK (CLI) is present to run the build command
# We use the existing one or install 9.0 if missing, as CLI is needed for SDK-style projects.
dotnet --info
Write-Host "Provisioning Complete." -ForegroundColor Green
