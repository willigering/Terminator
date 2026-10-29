# Baut die EXE (und ggf. die DLL für die Skript-Version). Benötigt .NET SDK (6 oder neuer).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
dotnet build "$root\src\app\app.csproj" -c Release -o "$root\bin"
dotnet build "$root\src\ui\ui.csproj" -c Release -o "$root\Daten"
Write-Host 'Fertig. EXE liegt in .\bin'
