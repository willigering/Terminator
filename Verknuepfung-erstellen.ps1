# Legt auf dem Desktop eine Verknüpfung "Terminator" mit dem Programmsymbol an.
# Eine alte Verknüpfung wird vorher gelöscht und der Symbol-Zwischenspeicher
# von Windows aufgefrischt, damit sicher das neue Symbol erscheint.
$dir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$vbs  = Join-Path $dir 'Terminator starten.vbs'
$ico  = Join-Path $dir 'Terminator-v2.ico'
$desk = [Environment]::GetFolderPath('Desktop')
$lnk  = Join-Path $desk 'Terminator.lnk'
foreach ($old in @('Terminator.lnk', 'Herunterfahr-Timer.lnk', 'Herunterfahr-Timer starten.lnk', 'Herunterfahr-Timer starten.vbs - Verknüpfung.lnk')) {
    $p = Join-Path $desk $old
    if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Force -ErrorAction SilentlyContinue }
}
$ws   = New-Object -ComObject WScript.Shell
$sc   = $ws.CreateShortcut($lnk)
$sc.TargetPath       = Join-Path ([Environment]::GetFolderPath('System')) 'wscript.exe'
$sc.Arguments        = '"' + $vbs + '"'
$sc.WorkingDirectory = $dir
$sc.IconLocation     = $ico + ',0'
$sc.Description      = 'Terminator – Herunterfahr-Timer'
$sc.Save()
# Symbol-Zwischenspeicher auffrischen (Windows 10/11)
$ie4 = Join-Path $env:WINDIR 'System32\ie4uinit.exe'
if (Test-Path -LiteralPath $ie4) {
    try { Start-Process -FilePath $ie4 -ArgumentList '-show' -WindowStyle Hidden -Wait -ErrorAction Stop } catch { }
}
Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.MessageBox]::Show("Verknüpfung auf dem Desktop erstellt:`n$lnk`n`nWichtig: Den Ordner nicht mehr verschieben, sonst funktioniert die Verknüpfung nicht.", 'Terminator') | Out-Null
