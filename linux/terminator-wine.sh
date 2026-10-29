#!/bin/sh
# Terminator unter Linux über Wine starten (UNGETESTET – nur Anleitung/Hilfsskript).
# Voraussetzung: wine + .NET Framework 4.8 im Wine-Prefix (z.B. "winetricks -q dotnet48").
# Herunterfahren/Neustart/Ruhezustand wirken unter Wine NICHT auf Linux (Windows-Befehle laufen ins Leere).
cd "$(dirname "$0")/Terminator" || exit 1
export WINEPREFIX="${WINEPREFIX:-$HOME/.wine-terminator}"
exec wine "$(ls Terminator-*.exe | sort -V | tail -1)" "$@"
