# 💀 SHUTDOWN TIMER
**Weil selbst dein PC irgendwann die Schnauze voll hat.**

Windows herunterfahren kann jeder. Aber warum einfach, wenn es auch völlig übertrieben geht?

Ich präsentiere **SHUTDOWN TIMER** – die vermutlich unnötig dramatischste Art, einen Windows-PC auszuschalten.

Andere Programme schließen Fenster.
Mein Programm schließt mit deinem Computer ab.

Und mittendrin sitzt eine graue Britisch-Kurzhaar-Katze mit Sonnenbrille, die aussieht, als hätte sie gerade drei Rechenzentren lahmgelegt und würde sich jetzt erst mal ein Nickerchen gönnen.

Sie bereut nichts.

## 🐈 MEET THE BOSS
Du dachtest, du kontrollierst deinen Computer?

**SÜSS.**

Ich dachte das auch mal.
Bis die Katze kam.

Sie trägt Sonnenbrille.
Sie stellt keine Fragen.
Sie hat Administratorrechte.\*

Und wenn der Countdown bei null ankommt, gibt es keine Diskussionen mehr.

**Hasta la Vista, Windows.**

<sub>\*Administratorrechte nicht im Lieferumfang der Katze enthalten.</sub>

## 🚨 WAS KANN DIESES TECHNOLOGISCHE MEISTERWERK?
- ✅ Einen Countdown starten. Revolutionär!
- ✅ Windows zeitgesteuert herunterfahren. Die Zukunft ist jetzt!
- ✅ Einen laufenden Shutdown abbrechen. Für Menschen mit Bindungsängsten.
- ✅ Dich daran erinnern, dass auch dein PC irgendwann Feierabend verdient.
- ✅ Dabei verdammt gut aussehen. Denn:

**SCHEISSEGAL, OB'S FUNKTIONIERT!**
**ES SOLL GUT AUSSEHEN!**

<sub>(Okay, funktionieren sollte es schon, sonst wäre das irgendwie peinlich. Es gibt schließlich nur eine Funktion.)</sub>

## ☢️ WIE FUNKTIONIERT DIESE HÖCHSTKOMPLEXE TECHNOLOGIE?
- **SCHRITT 1:** Ich stelle den Timer ein.
- **SCHRITT 2:** Ich drücke auf Start.
- **SCHRITT 3:** Die Katze übernimmt.
- **SCHRITT 4:** Existenzielle Zweifel.
- **SCHRITT 5:** Windows verabschiedet sich.
- **SCHRITT 6:** Endlich Ruhe. Außer die Katze hat Hunger.

## ⚠️ WICHTIGE SICHERHEITSHINWEISE
Ich übernehme keinerlei Verantwortung für:

- Ungespeicherte Excel-Tabellen, die plötzlich sehr endgültig geschlossen werden.
- Downloads, die bei 99 % standen.
- Panikattacken bei weniger als zehn Sekunden Restzeit.
- Emotionale Schäden durch das Herunterfahren von Windows.
- Katzen, die sich durch dieses Programm zu höheren Lebensformen entwickeln.

**ACHTUNG:** Dieses Programm macht exakt das, was es verspricht.
Ich weiß selbst nicht, wie das passieren konnte.

## 🧠 WARUM EXISTIERT DIESES PROGRAMM?
Eine berechtigte Frage.

Windows kann sich bereits selbst herunterfahren.
Ich könnte auch einfach auf „Herunterfahren" klicken.
Oder einen Befehl eingeben.

Aber wo bleibt denn da der Spaß?
Wo bleibt die Dramatik?
**WO BLEIBT DIE VERDAMMTE KATZE?!**

Eben.

## 🏆 FAZIT
Ist es notwendig? Nein.
Ist es übertrieben? Absolut.
Hat jemand danach gefragt? Vermutlich nicht.

Würde ich es wieder entwickeln?
**SOFORT. MIT NOCH MEHR KATZE.**


## 🔄 AUTO-UPDATE (ab 3.17)
**Funktionsweise:** Kurz nach dem Start fragt der Terminator im Hintergrund das neueste *stabile* GitHub Release ab (`releases/latest`, höchstens einmal in 24 h, mit ETag; Entwürfe und Pre-Releases werden ignoriert). Offline oder bei GitHub-Fehlern passiert nichts, die App läuft normal. Ist die Version höher als die installierte, erscheint ein Hinweis im Zahnrad-Stil mit den Release Notes – aber nie während eines Countdowns.
Nach **JETZT AKTUALISIEREN** wird `Terminator-Windows.zip` über HTTPS nach `%TEMP%\Terminator-update` geladen und geprüft (Größe + SHA-256 aus `terminator-update.json` und `SHA256SUMS`, Schutz gegen `../`-Pfade, Programmkennung und Version von `Terminator.exe` und `Daten\TimerUI.dll`). Dann beendet sich der Terminator; ein kleines PowerShell-Skript ersetzt die Dateien, sichert die alten, rollt bei Fehlern zurück und startet neu (EXE oder Skript-Version). Ist der Ordner schreibgeschützt (z. B. `C:\Program Files`) oder kein vollständiger Terminator-Ordner, öffnet sich stattdessen die Release-Seite.

**Einstellungen:** Zahnrad → „Automatisch nach Updates suchen“ (AN/AUS) und „JETZT PRÜFEN“. Gespeichert in `%LOCALAPPDATA%\Terminator\settings.ini` (`update_check`, `update_last`, `update_etag`); Einstellungen und Timer-Zustand werden vom Update nicht angefasst.

**Version:** einzige Quelle ist `src/Directory.Build.props`. Release-Tag `v3.17` / `v3.17.1`. Die EXE heißt fest `Terminator.exe`.

**Release erstellen:** siehe [RELEASE.md](RELEASE.md) – kurz: Version erhöhen, `tools\make-release.ps1` ausführen, die drei Dateien aus `dist` an ein (nicht Pre-)Release hängen. Es sind **keine GitHub Secrets** nötig, da nichts signiert wird.

**Signierung:** Keine (kein Code-Signing-Zertifikat). Windows SmartScreen kann weiterhin warnen.

**Tests:** `dotnet test tests/Terminator.Tests` (läuft auch unter Linux/macOS). Manueller Ablauf: [docs/TESTPLAN-UPDATE.md](docs/TESTPLAN-UPDATE.md).

**Fehlerbehebung**
- *„Release … ist unvollständig“* – `terminator-update.json` oder `SHA256SUMS` fehlt am Release.
- *„GitHub-Limit erreicht“* – 60 Anfragen/Stunde pro IP ohne Anmeldung; später erneut prüfen.
- *„Prüfsumme stimmt nicht“* – Download beschädigt; erneut versuchen, sonst Release-Assets neu erzeugen.
- *Update startet nicht neu* – Log: `%TEMP%\Terminator-update\<Version>\update.log`. Bei „wiederhergestellt“ wurde die alte Version zurückgespielt.
- *Von 3.16 kommend* – 3.17 einmal von Hand über den alten Ordner entpacken und Verknüpfungen neu erstellen (die EXE heißt jetzt `Terminator.exe`).

---

**💀 SHUTDOWN TIMER**
*One timer. One cat. Zero mercy.*
*Powered by questionable decisions.*

Made by Gering Digital. 🐈‍⬛
