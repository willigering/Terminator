# Manueller Testplan Auto-Update (echter Windows-PC)

Ziel: 3.16 → 3.17 (einmalig von Hand) → 3.18 (automatisch), ohne Datenverlust. Für 3.18 genügt ein **Test-Build**:
Version in `src/Directory.Build.props` auf `3.18.0`, `tools/make-release.ps1`, als Release in einem **Test-Repo
oder Fork** veröffentlichen (oder als echtes Release erst nach Freigabe). Hinweis: Die Update-Quelle ist fest
`willigering/Terminator`; für einen Fork-Test die Konstante `Repo` in `src/app/UpdateCore.cs` nur im Test-Build ändern.

## A. 3.16 → 3.17 (manuell, einmalig)
1. 3.16 aus `ShutdownTimer-v3.16-Windows.zip` nach z. B. `C:\Users\<Name>\Terminator` entpacken, starten.
   Im Zahnrad Finale AUS, „Nach Start minimieren“ AN stellen; Countdown 30 min starten und wieder abbrechen.
2. Terminator beenden. 3.17-ZIP in **denselben** Ordner entpacken (überschreiben).
3. `Terminator.exe` starten → Einstellungen müssen erhalten sein (liegen in `%LOCALAPPDATA%\Terminator\settings.ini`).
4. Alte Verknüpfungen auf `Terminator-3.16.exe` löschen, `Verknüpfung erstellen.bat` erneut ausführen (einmalig).
5. Zahnrad: Zeile „Automatisch nach Updates suchen“ = AN, „JETZT PRÜFEN“ → „Aktuell – 3.17 …“.

## B. 3.17 → 3.18 (automatisch)
| # | Schritt | Erwartet |
|---|---|---|
| 1 | WLAN/LAN trennen, Terminator starten, JETZT PRÜFEN | „Keine Verbindung …“, App läuft normal, Countdown startbar |
| 2 | Netz an, 3.18 als **Pre-Release** veröffentlichen, JETZT PRÜFEN | „Aktuell“ – Pre-Releases werden ignoriert |
| 3 | 3.18 als normales Release veröffentlichen, Countdown starten, JETZT PRÜFEN | Kein Hinweis während des Countdowns |
| 4 | Countdown abbrechen | Panel „SYSTEM UPDATE VERFÜGBAR“, Installiert 3.17 / Verfügbar 3.18, Release Notes |
| 5 | SPÄTER | Panel schließt, App funktioniert weiter; nächster Hinweis erst nach Neustart/JETZT PRÜFEN |
| 6 | JETZT PRÜFEN → JETZT AKTUALISIEREN, während des Downloads ABBRECHEN | „Download abgebrochen“, nichts geändert |
| 7 | JETZT AKTUALISIEREN, Netz während des Downloads trennen | „Download unterbrochen“, alte Version läuft weiter |
| 8 | JETZT AKTUALISIEREN normal | Fortschritt in %, App beendet sich, startet nach ~2 s als 3.18 neu |
| 9 | Prüfen | Einstellungen unverändert, eigene Dateien im Ordner noch da, `%TEMP%\Terminator-update\3.18\update.log` = „Update installiert“ |
| 10 | Skript-Version: 3.17 über `Terminator starten.vbs` starten, Schritte 4+8 | Neustart wieder über die VBS, `Daten\TimerUI.dll` = 3.18 |
| 11 | Ordner nach `C:\Program Files\Terminator` kopieren (ohne Admin starten), aktualisieren | Release-Seite öffnet sich, nichts überschrieben |
| 12 | Einzelne `Terminator.exe` auf den Desktop kopieren und starten, aktualisieren | Release-Seite öffnet sich (kein vollständiger Ordner) |
| 13 | Zahnrad: Auto-Update AUS, `update_last` in settings.ini auf 0 setzen, neu starten | keine automatische Prüfung |
| 14 | Manipulation: in einem Test-Release `SHA256SUMS` ändern | „Prüfsummen … widersprechen sich“, keine Installation |
| 15 | Rollback: `Terminator.exe` im Ordner nach Download in einem Editor/Explorer-Vorschau gesperrt halten | Helfer versucht 5 s, stellt alte Dateien wieder her, startet alte Version, Log „wiederhergestellt“ |
