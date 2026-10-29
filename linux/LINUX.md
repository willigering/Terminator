# Terminator unter Linux – ehrlicher Stand (3.16)

**Es gibt keinen nativen Linux-Build.** Terminator ist eine .NET-Framework-4.8-WinForms-App mit
starker GDI+-Nutzung und Windows-APIs (Per-Monitor-DPI `GetDpiForWindow`, Tray-Icon, `shutdown.exe`).

## Optionen
| Weg | Aufwand | Funktioniert Herunterfahren? |
|---|---|---|
| **Wine** + .NET 4.8 im Prefix | gering, aber **ungetestet** | Nein – Shutdown-Befehle betreffen Linux nicht |
| Mono + WinForms | gering–mittel, unsicher (Mono-WinForms ist eingefroren, GDI+-Darstellung abweichend) | Nein, ohne Codeänderung |
| **Echter Port**: .NET 8 + Avalonia (Zeichenlogik von GDI+ auf SkiaSharp), Linux-Aktionen über `systemctl poweroff/reboot/suspend` bzw. `loginctl`, Tray über Avalonia TrayIcon | erheblich (UI-Schicht ≈ Neuschreiben von Program.cs/Art.cs/Finale.cs/SettingsUI.cs) | Ja |

## Wine-Versuch (auf eigene Gefahr)
```sh
sudo apt install wine winetricks      # Debian/Ubuntu
WINEPREFIX=~/.wine-terminator winetricks -q dotnet48
./terminator-wine.sh
```
Hinweis: Anzeige/Countdown können laufen, die Aktionen (Herunterfahren usw.) wirken unter Wine nicht auf das Linux-System.
