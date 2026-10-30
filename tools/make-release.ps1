# Baut Terminator und erzeugt alle Release-Assets für das Auto-Update in .\dist:
#   Terminator-Windows.zip   (Ordner "Terminator" mit Terminator.exe, Daten\TimerUI.dll, Skript-Version ...)
#   terminator-update.json   (Version, Dateiname, Größe, SHA-256, Mindest-Windows)
#   SHA256SUMS               (Prüfsumme im sha256sum-Format)
# Veröffentlicht NICHTS. Hochladen siehe RELEASE.md.
# Aufruf (Windows):  powershell -ExecutionPolicy Bypass -File tools\make-release.ps1
param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

[xml]$props = Get-Content -LiteralPath (Join-Path $root 'src/Directory.Build.props') -Raw
$version = ([string]$props.Project.PropertyGroup.TerminatorVersion).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Version in src/Directory.Build.props ungültig: '$version'" }
$p = $version.Split('.'); $tag = if ($p[2] -eq '0') { "v$($p[0]).$($p[1])" } else { "v$version" }
Write-Host "Terminator $version (Tag $tag)"

$bin = Join-Path $root 'bin'; $daten = Join-Path $root 'Daten'
if (-not $SkipBuild) {
    dotnet build (Join-Path $root 'src/app/app.csproj') -c Release -o $bin
    if ($LASTEXITCODE -ne 0) { throw 'Build der EXE fehlgeschlagen' }
    dotnet build (Join-Path $root 'src/ui/ui.csproj') -c Release -o $daten
    if ($LASTEXITCODE -ne 0) { throw 'Build der TimerUI.dll fehlgeschlagen' }
}
$exe = Join-Path $bin 'Terminator.exe'; $dll = Join-Path $daten 'TimerUI.dll'
foreach ($f in @($exe, $dll)) {
    if (-not (Test-Path -LiteralPath $f)) { throw "$f fehlt" }
    $v = [System.Reflection.AssemblyName]::GetAssemblyName($f).Version
    if ("$($v.Major).$($v.Minor).$($v.Build)" -ne $version) { throw "$f hat Version $v statt $version" }
}

$dist = Join-Path $root 'dist'; $stage = Join-Path $dist 'stage'; $pkg = Join-Path $stage 'Terminator'
if (Test-Path -LiteralPath $dist) { Remove-Item -LiteralPath $dist -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $pkg 'Daten'), (Join-Path $pkg 'EXE-Version/Quellcode') | Out-Null
Copy-Item -LiteralPath $exe -Destination $pkg
Copy-Item -LiteralPath $dll -Destination (Join-Path $pkg 'Daten')
foreach ($f in @('Terminator.ps1', 'Terminator starten.vbs', 'Verknüpfung erstellen.bat', 'Verknuepfung-erstellen.ps1', 'Terminator-v2.ico', 'LIESMICH.txt')) {
    Copy-Item -LiteralPath (Join-Path $root $f) -Destination $pkg
}
Get-ChildItem -LiteralPath (Join-Path $root 'src/app') -File | Where-Object { $_.Extension -in '.cs', '.png', '.ttf', '.ico', '.manifest', '.txt', '.csproj' } |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $pkg 'EXE-Version/Quellcode') }
Copy-Item -LiteralPath (Join-Path $root 'src/ui/ui.csproj') -Destination (Join-Path $pkg 'EXE-Version/Quellcode')
Copy-Item -LiteralPath (Join-Path $root 'src/Directory.Build.props') -Destination (Join-Path $pkg 'EXE-Version/Quellcode')

$zip = Join-Path $dist 'Terminator-Windows.zip'
# Einträge immer mit "/" (unabhängig von der .NET-Version)
$za = [System.IO.Compression.ZipFile]::Open($zip, 'Create')
try {
    Get-ChildItem -LiteralPath $stage -Recurse -File | Sort-Object FullName | ForEach-Object {
        $rel = $_.FullName.Substring($stage.Length + 1).Replace('\', '/')
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($za, $_.FullName, $rel, 'Optimal')
    }
} finally { $za.Dispose() }
Remove-Item -LiteralPath $stage -Recurse -Force

$sha = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$size = (Get-Item -LiteralPath $zip).Length
$meta = [ordered]@{ app = 'Terminator'; version = $version; tag = $tag; asset = 'Terminator-Windows.zip'; size = $size; sha256 = $sha; minWindows = '6.1' }
$enc = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Join-Path $dist 'terminator-update.json'), ($meta | ConvertTo-Json) + "`n", $enc)
[System.IO.File]::WriteAllText((Join-Path $dist 'SHA256SUMS'), "$sha  Terminator-Windows.zip`n", $enc)
Write-Host "Fertig: $dist"
Write-Host "  Terminator-Windows.zip  $size Bytes  sha256 $sha"
Write-Host "Nächster Schritt: Release $tag als Entwurf anlegen und die drei Dateien aus dist hochladen (siehe RELEASE.md)."
