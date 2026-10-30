# Terminator – Herunterfahr-Timer (Skript-Version)
# Fährt Windows zeitgesteuert herunter / startet neu / Energiesparen / Ruhezustand.
# Start am besten über "Terminator starten.vbs".
# Fenster und Logik stecken fertig übersetzt in Daten\TimerUI.dll
# (Quellcode im Ordner EXE-Version\Quellcode). Dadurch startet es schnell und läuft flüssig.
$ErrorActionPreference = 'Stop'
$Dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Dll = Join-Path $Dir 'Daten\TimerUI.dll'
try {
    Add-Type -AssemblyName System.Windows.Forms, System.Drawing
    # Laden aus den Bytes: funktioniert auch, wenn die Datei aus einem heruntergeladenen ZIP stammt
    [void][System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($Dll))
    [TimerHost]::Run($Dir)  # Ordner wird für das Auto-Update benötigt
} catch {
    Add-Type -AssemblyName System.Windows.Forms
    [void][System.Windows.Forms.MessageBox]::Show("Terminator konnte nicht gestartet werden:`n`n$($_.Exception.Message)`n`nBitte prüfen, ob der Ordner 'Daten' mit TimerUI.dll neben dem Skript liegt.", 'Terminator', 'OK', 'Error')
}
