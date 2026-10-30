using System;
using System.Globalization;
using System.IO;

public enum PowerAction { Shutdown, Restart, Sleep, Hibernate }

public static class Core
{
    public static DateTime TargetFromMinutes(DateTime now, int minutes) { return now.AddMinutes(minutes); }

    // Time of day; if already passed (or now), the next day.
    public static DateTime TargetFromClock(DateTime now, int hour, int minute)
    {
        var t = now.Date.AddHours(hour).AddMinutes(minute);
        if (t <= now) t = t.AddDays(1);
        return t;
    }

    public static int SecondsUntil(DateTime now, DateTime target)
    {
        double s = Math.Ceiling((target - now).TotalSeconds);
        return (int)Math.Max(0, Math.Min(s, 315359999));
    }

    public static string Countdown(int seconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return ((int)ts.TotalHours).ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00");
    }

    public static bool UsesShutdownExe(PowerAction a) { return a == PowerAction.Shutdown || a == PowerAction.Restart; }

    // Sofort-Fallback, falls ExitWindowsEx scheitert: /t 0 ohne Kommentar -> keine Windows-Ankündigung
    public static string ImmediateArgs(PowerAction a)
    {
        if (!UsesShutdownExe(a)) throw new ArgumentException("only shutdown/restart");
        return (a == PowerAction.Shutdown ? "/s" : "/r") + " /t 0 /f";
    }

    public static string ActionName(PowerAction a)
    {
        switch (a)
        {
            case PowerAction.Restart: return "Neu starten";
            case PowerAction.Sleep: return "Energiesparen";
            case PowerAction.Hibernate: return "Ruhezustand";
            default: return "Herunterfahren";
        }
    }

    public static bool ShouldWarn(int secondsLeft, bool alreadyWarned) { return !alreadyWarned && secondsLeft <= 60 && secondsLeft > 0; }

    // Persist a scheduled shutdown/restart, so a reopened window can show and cancel it.
    public static string Serialize(PowerAction a, DateTime target) { return a + "|" + target.ToString("o", CultureInfo.InvariantCulture); }

    public static string Serialize(PowerAction a, DateTime target, DateTime start) { return Serialize(a, target) + "|" + start.ToString("o", CultureInfo.InvariantCulture); }

    public static bool TryParse(string s, DateTime now, out PowerAction a, out DateTime target)
    { DateTime st; return TryParse(s, now, out a, out target, out st); }

    // start = Startzeitpunkt des Countdowns (alte Dateien ohne Start: DateTime.MinValue)
    public static bool TryParse(string s, DateTime now, out PowerAction a, out DateTime target, out DateTime start)
    {
        a = PowerAction.Shutdown; target = DateTime.MinValue; start = DateTime.MinValue;
        if (string.IsNullOrEmpty(s)) return false;
        var p = s.Trim().Split('|');
        if (p.Length != 2 && p.Length != 3) return false;
        if (p.Length == 3 && !DateTime.TryParse(p[2], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out start)) start = DateTime.MinValue;
        if (!Enum.TryParse(p[0], out a)) return false;
        if (!DateTime.TryParse(p[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out target)) return false;
        return target > now;
    }

    // Glut-Ring rein aus Start/Ziel: Alter (s) des letzten Aufleuchtens von Block i, NaN = noch nie.
    // total = angezeigte Sekunden beim Start, rem = exakte Restzeit jetzt.
    public static double LitAge(int i, int n, int total, double rem)
    {
        int shown = (int)Math.Ceiling(Math.Max(0, rem));
        int d = (((-i - shown) % n) + n) % n;
        int s = shown + d;
        if (s > total) return double.NaN;
        return Math.Max(0, s - rem);
    }

    // Finale in der letzten Minute. rem = exakte Restzeit (s).
    // tint: Rot-Deckkraft 0..1 (Phase 1), h/w: Anteil der Höhe/Breite des roten Bereichs, glow: Punkt-Glühen 0..1.
    // phase: 0 = aus, 1 = röter werden, 2 = rot + vertikal zum Streifen, 3 = Streifen fährt zur Mitte, 4 = schwarz
    public static int Finale(double rem, out double tint, out double h, out double w, out double glow)
    {
        tint = 0; h = 1; w = 1; glow = 0;
        if (rem > 60) return 0;
        if (rem > 3) { double p = (60 - rem) / 57.0; tint = p * p * (3 - 2 * p) * 0.35 + Math.Pow(p, 2.2) * 0.65; return 1; }
        tint = 1;
        if (rem > 1)
        {
            double p = Math.Max(0, (2.6 - rem) / 1.6); // 3..2.6 s komplett rot, dann zusammenschieben
            double e = p * p * p;
            h = Math.Max(0.004, 1 - e * 0.996); return 2;
        }
        h = 0.004;
        if (rem > 0) { double p = 1 - rem; w = Math.Max(0, 1 - p * p); return 3; } // Streifen fährt von links/rechts zur Mitte (Breite -> 0)
        w = 0; glow = 0; tint = 0; return 4;
    }
}

// Einstellungen (Zahnrad): Finale + Texte oben/unten, gespeichert als key=value
public class Settings
{
    public const string DefaultTop = "HASTA LA VISTA, WINDOWS", DefaultBottom = "(Keine Sorge. Es kommt wieder.)";
    public bool Finale = true, TopOn = true, BottomOn = true, AutoMinimize = false; // 3.15: Standard AUS
    // 3.17: Auto-Update (Standard AN; letzte erfolgreiche Prüfung in UTC-Ticks + ETag für bedingte GitHub-Anfragen)
    public bool UpdateCheck = true; public long UpdateLastTicks; public string UpdateEtag = "";
    public string Top = DefaultTop, Bottom = DefaultBottom;
    public bool TopIsDefault { get { return Top == DefaultTop; } }
    public bool BottomIsDefault { get { return Bottom == DefaultBottom; } }
    public void Reset() { Finale = true; AutoMinimize = false; UpdateCheck = true; TopOn = true; BottomOn = true; Top = DefaultTop; Bottom = DefaultBottom; }
    static string Esc(string v) { return (v ?? "").Replace("\\", "\\\\").Replace("\r", "").Replace("\n", "\\n"); }
    static string Unesc(string v) { var sb = new System.Text.StringBuilder(); for (int i = 0; i < v.Length; i++) { if (v[i] == '\\' && i + 1 < v.Length) { i++; sb.Append(v[i] == 'n' ? ' ' : v[i]); } else sb.Append(v[i]); } return sb.ToString(); }
    public string Serialize()
    {
        return "finale=" + (Finale ? 1 : 0) + "\n" + "top_on=" + (TopOn ? 1 : 0) + "\n" + "top=" + Esc(Top) + "\n" + "bottom_on=" + (BottomOn ? 1 : 0) + "\n" + "bottom=" + Esc(Bottom) + "\n" + "min_after_start=" + (AutoMinimize ? 1 : 0) + "\n"
             + "update_check=" + (UpdateCheck ? 1 : 0) + "\n" + "update_last=" + UpdateLastTicks + "\n" + "update_etag=" + Esc(UpdateEtag) + "\n";
    }
    public static Settings Parse(string text)
    {
        var st = new Settings();
        if (string.IsNullOrEmpty(text)) return st;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r'); int k = line.IndexOf('='); if (k <= 0) continue;
            string key = line.Substring(0, k).Trim(), v = line.Substring(k + 1);
            switch (key)
            {
                case "finale": st.Finale = v.Trim() != "0"; break;
                case "min_after_start": st.AutoMinimize = v.Trim() != "0"; break; // 3.15: neuer Schlüssel, alter "auto_min" (3.11–3.14, Standard an) wird ignoriert
                case "update_check": st.UpdateCheck = v.Trim() != "0"; break;
                case "update_last": long t; if (long.TryParse(v.Trim(), out t) && t >= 0 && t <= DateTime.MaxValue.Ticks) st.UpdateLastTicks = t; break;
                case "update_etag": st.UpdateEtag = Clip(Unesc(v), 200); break;
                case "top_on": st.TopOn = v.Trim() != "0"; break;
                case "bottom_on": st.BottomOn = v.Trim() != "0"; break;
                case "top": st.Top = Clip(Unesc(v)); break;
                case "bottom": st.Bottom = Clip(Unesc(v)); break;
            }
        }
        return st;
    }
    public const int MaxLen = 60;
    public static string Clip(string v) { return Clip(v, MaxLen); }
    static string Clip(string v, int max) { v = (v ?? "").Replace("\r", " ").Replace("\n", " "); return v.Length > max ? v.Substring(0, max) : v; }
}
