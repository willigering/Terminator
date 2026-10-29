# Release v3.16

**Tag:** `v3.16` · **Titel:** Terminator 3.16

## Assets
| Datei | Für | Inhalt |
|---|---|---|
| `Terminator-Windows.zip` | **Windows 10/11** (.NET Framework 4.8, vorinstalliert) | `Terminator-3.16.exe`, `Daten\TimerUI.dll`, `Terminator.ps1`, `Terminator starten.vbs`, `Verknüpfung erstellen.bat`, `LIESMICH.txt` |

Windows: ZIP entpacken, `Terminator-3.16.exe` starten (EXE und Ordner `Daten` müssen zusammenbleiben).

## Schritte
1. Quellcode pushen (`git push`), Tag setzen: `git tag v3.16 && git push origin v3.16`
2. GitHub → Releases → *Draft a new release* → Tag `v3.16`, Text aus „Neu in 3.16“ (README).
3. Beide ZIPs als Assets hochladen, veröffentlichen.
4. Vorher in `LICENSE` „DEIN NAME“ ersetzen.

Selbst bauen (Windows): `./build.ps1`
