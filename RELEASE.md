# Release-Verfahren (ab 3.17, mit Auto-Update)

Die App prüft beim Start (höchstens alle 24 h) `https://api.github.com/repos/willigering/Terminator/releases/latest`.
Ein Update wird nur angeboten, wenn **alle** folgenden Punkte stimmen:

- Release ist **veröffentlicht**, kein Entwurf und **kein Pre-Release**.
- Tag `vX.Y` bzw. `vX.Y.Z` ist **höher** als die installierte Version (nie Downgrade).
- Es hat genau diese drei Assets:

| Datei | Inhalt |
|---|---|
| `Terminator-Windows.zip` | Ordner `Terminator\` mit `Terminator.exe`, `Daten\TimerUI.dll`, Skript-Version, LIESMICH, Quellcode |
| `terminator-update.json` | `app`, `version`, `tag`, `asset`, `size`, `sha256`, `minWindows` |
| `SHA256SUMS` | `<sha256>  Terminator-Windows.zip` |

Die drei Dateien erzeugt **`tools/make-release.ps1`** – nie von Hand bearbeiten.

## Schritte

1. Version erhöhen: **nur** in `src/Directory.Build.props` (`<TerminatorVersion>3.18.0</TerminatorVersion>`).
2. „NEU in …“ oben in `LIESMICH.txt` ergänzen.
3. Tests: `dotnet test tests/Terminator.Tests`
4. Paket bauen (Windows, .NET SDK 6+):
   `powershell -ExecutionPolicy Bypass -File tools\make-release.ps1`
   → `dist\Terminator-Windows.zip`, `dist\terminator-update.json`, `dist\SHA256SUMS`
5. Committen, pushen, Tag setzen: `git tag v3.18 && git push origin v3.18`
   (Tag muss zur Version passen: 3.18.0 → `v3.18`, 3.18.1 → `v3.18.1`; das Skript zeigt den Tag an.)
6. GitHub → Releases → *Draft a new release* → Tag wählen, Text = „NEU in …“ (wird im Update-Hinweis angezeigt,
   die ersten 4 Zeilen; Markdown wird als Klartext dargestellt).
7. **Alle drei Dateien** aus `dist` hochladen. Release-Label „None“ (kein Pre-Release), „Set as latest release“ an.
8. Vor dem Veröffentlichen: ZIP einmal herunterladen und `Get-FileHash -Algorithm SHA256` mit `SHA256SUMS` vergleichen.
9. *Publish release*.

Ein Release ohne `terminator-update.json`/`SHA256SUMS` wird von der App **nicht** installiert
(sie meldet dann „Release … ist unvollständig“). Ein fehlerhaftes Release lässt sich also nicht versehentlich verteilen,
und ein Pre-Release eignet sich zum Testen, ohne dass Nutzer es angeboten bekommen.

## Hinweise
- **Code-Signing:** Die EXE ist nicht signiert (SmartScreen-Warnung bleibt). Schutz gegen beschädigte/manipulierte
  Downloads: nur HTTPS, nur Download-URLs aus `github.com/willigering/Terminator/releases/download/`,
  Größe + SHA-256, Programmkennung und Version der neuen Dateien. Wer das GitHub-Konto kontrolliert, kann ein Update
  ausliefern – deshalb 2FA für das Konto aktiv halten.
- Der Dateiname `Terminator-Windows.zip` und die EXE `Terminator.exe` dürfen sich nicht mehr ändern.
