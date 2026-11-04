using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

// 3.17: Auto-Update über GitHub Releases – reine Logik ohne Oberfläche (wird auch von den Tests benutzt).
public sealed class ReleaseCandidate
{
    public Version Version; public string Tag, Notes, HtmlUrl, ZipUrl, MetaUrl, SumsUrl;
}

public sealed class UpdateInfo
{
    public Version Version; public string Tag, Notes, HtmlUrl, ZipUrl, Sha256; public long Size;
}

public static class UpdateCore
{
    public const string Repo = "willigering/Terminator";
    public const string ApiLatest = "https://api.github.com/repos/" + Repo + "/releases/latest";
    public const string ReleasesPage = "https://github.com/" + Repo + "/releases/latest";
    public const string DownloadPrefix = "https://github.com/" + Repo + "/releases/download/";
    public const string ZipName = "Terminator-Windows.zip", MetaName = "terminator-update.json", SumsName = "SHA256SUMS";
    public const string ExeName = "Terminator.exe", DllRel = "Daten\\TimerUI.dll";
    public const long MaxZipBytes = 200L * 1024 * 1024, MaxTextBytes = 1024 * 1024;
    public static readonly TimeSpan AutoInterval = TimeSpan.FromHours(24);

    // ---------- Versionen ----------
    public static bool TryParseVersion(string s, out Version v)
    {
        v = null; if (string.IsNullOrEmpty(s)) return false;
        s = s.Trim(); if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase)) s = s.Substring(1);
        var parts = s.Split('.'); if (parts.Length < 1 || parts.Length > 4) return false;
        var n = new int[3];
        for (int i = 0; i < parts.Length; i++)
        {
            int x; if (parts[i].Length == 0 || parts[i].Length > 6 || !int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out x)) return false;
            if (i < 3) n[i] = x;
        }
        v = new Version(n[0], n[1], n[2]); return true;
    }
    public static Version Norm(Version v) { return v == null ? new Version(0, 0, 0) : new Version(v.Major, v.Minor, Math.Max(0, v.Build)); }
    public static bool IsNewer(Version candidate, Version installed) { return Norm(candidate).CompareTo(Norm(installed)) > 0; }
    public static string Show(Version v) { v = Norm(v); return v.Major + "." + v.Minor + (v.Build > 0 ? "." + v.Build : ""); }

    // höchstens eine automatische Prüfung je 24 h; Uhr zurückgestellt -> wieder fällig
    public static bool DueForAutoCheck(DateTime nowUtc, DateTime lastUtc)
    {
        if (lastUtc == DateTime.MinValue) return true;
        if (lastUtc > nowUtc.AddHours(1)) return true;
        return nowUtc - lastUtc >= AutoInterval;
    }

    // ---------- GitHub-Antwort ----------
    static bool SafeUrl(string u) { return u != null && u.StartsWith(DownloadPrefix, StringComparison.Ordinal) && u.IndexOf("..", StringComparison.Ordinal) < 0 && u.IndexOfAny(new[] { ' ', '\r', '\n', '"' }) < 0; }

    // null = OK; sonst Grund. candidate == null und Rückgabe null = kein Update.
    public static string EvaluateRelease(string json, Version installed, out ReleaseCandidate c)
    {
        c = null;
        Dictionary<string, object> o;
        try { o = Json.Parse(json) as Dictionary<string, object>; } catch (Exception ex) { return "Ungültige Antwort von GitHub (" + ex.Message + ")"; }
        if (o == null) return "Ungültige Antwort von GitHub";
        if (Json.Bool(o, "draft") || Json.Bool(o, "prerelease")) return null; // nie Entwürfe/Pre-Releases
        string tag = Json.Str(o, "tag_name"); Version v;
        if (!TryParseVersion(tag, out v)) return "Release-Version nicht lesbar: " + Clip(tag, 30);
        if (!IsNewer(v, installed)) return null; // gleich oder älter: kein Update, nie Downgrade
        var cand = new ReleaseCandidate { Version = v, Tag = tag, Notes = Json.Str(o, "body") ?? "", HtmlUrl = Json.Str(o, "html_url") };
        if (cand.HtmlUrl == null || !cand.HtmlUrl.StartsWith("https://github.com/" + Repo + "/", StringComparison.Ordinal)) cand.HtmlUrl = ReleasesPage;
        var assets = o.ContainsKey("assets") ? o["assets"] as List<object> : null;
        if (assets != null)
            foreach (var a0 in assets)
            {
                var a = a0 as Dictionary<string, object>; if (a == null) continue;
                string name = Json.Str(a, "name"), url = Json.Str(a, "browser_download_url");
                if (!SafeUrl(url)) continue;
                if (name == ZipName) cand.ZipUrl = url; else if (name == MetaName) cand.MetaUrl = url; else if (name == SumsName) cand.SumsUrl = url;
            }
        if (cand.ZipUrl == null || cand.MetaUrl == null || cand.SumsUrl == null)
            return "Release " + Clip(tag, 20) + " ist unvollständig (" + ZipName + ", " + MetaName + " und " + SumsName + " erforderlich)";
        c = cand; return null;
    }

    // Prüft terminator-update.json + SHA256SUMS gegen das Release und dieses System.
    public static string EvaluateMetadata(ReleaseCandidate c, string metaJson, string sums, Version installed, Version osVersion, out UpdateInfo info)
    {
        info = null;
        Dictionary<string, object> m;
        try { m = Json.Parse(metaJson) as Dictionary<string, object>; } catch { return MetaName + " ist beschädigt"; }
        if (m == null) return MetaName + " ist beschädigt";
        if (Json.Str(m, "app") != "Terminator") return MetaName + ": falsche App-Kennung";
        Version v;
        if (!TryParseVersion(Json.Str(m, "version"), out v)) return MetaName + ": Version nicht lesbar";
        if (Norm(v).CompareTo(Norm(c.Version)) != 0) return "Versionen passen nicht zusammen (Tag " + Show(c.Version) + ", Paket " + Show(v) + ")";
        if (!IsNewer(v, installed)) return "Kein Downgrade";
        if (Json.Str(m, "asset") != ZipName) return MetaName + ": falscher Dateiname";
        string sha = (Json.Str(m, "sha256") ?? "").ToLowerInvariant();
        if (!IsHex64(sha)) return MetaName + ": Prüfsumme fehlt";
        double size = Json.Num(m, "size");
        if (size <= 0 || size > MaxZipBytes) return MetaName + ": ungültige Größe";
        string fromSums = ShaFromSums(sums, ZipName);
        if (fromSums == null) return SumsName + " enthält " + ZipName + " nicht";
        if (fromSums != sha) return "Prüfsummen in " + MetaName + " und " + SumsName + " widersprechen sich";
        Version minOs;
        string mo = Json.Str(m, "minWindows");
        if (mo != null && TryParseVersion(mo, out minOs) && osVersion != null && new Version(osVersion.Major, osVersion.Minor, 0).CompareTo(Norm(minOs)) < 0)
            return "Diese Version benötigt Windows " + mo + " oder neuer";
        info = new UpdateInfo { Version = v, Tag = c.Tag, Notes = c.Notes, HtmlUrl = c.HtmlUrl, ZipUrl = c.ZipUrl, Sha256 = sha, Size = (long)size };
        return null;
    }

    public static bool IsHex64(string s) { if (s == null || s.Length != 64) return false; foreach (char ch in s) if (!((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f'))) return false; return true; }
    public static string ShaFromSums(string sums, string name)
    {
        if (sums == null) return null;
        foreach (var raw in sums.Split('\n'))
        {
            var line = raw.Trim(); if (line.Length < 66) continue;
            string h = line.Substring(0, 64).ToLowerInvariant(), rest = line.Substring(64).Trim().TrimStart('*');
            if (rest == name && IsHex64(h)) return h;
        }
        return null;
    }

    public static string Sha256Hex(Stream s)
    {
        using (var sha = SHA256.Create())
        {
            var h = sha.ComputeHash(s); var sb = new StringBuilder(64);
            foreach (var b in h) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
    public static string Sha256File(string path) { using (var f = File.OpenRead(path)) return Sha256Hex(f); }

    // Datei gegen Größe und Prüfsumme prüfen (unvollständige/manipulierte Downloads)
    public static string VerifyFile(string path, long size, string sha)
    {
        if (!File.Exists(path)) return "Download fehlt";
        long len = new FileInfo(path).Length;
        if (len != size) return "Download unvollständig (" + len + " von " + size + " Bytes)";
        if (Sha256File(path) != sha) return "Prüfsumme stimmt nicht – Datei beschädigt oder manipuliert";
        return null;
    }

    // ---------- Paket ----------
    // Entpacken mit Schutz gegen "../"-Pfade (ZIP-Slip) und absolute Pfade
    public static string ExtractZipSafe(string zip, string dest)
    {
        try
        {
            string root = Path.GetFullPath(dest).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(root);
            long total = 0;
            using (var za = ZipFile.OpenRead(zip))
            {
                if (za.Entries.Count == 0 || za.Entries.Count > 2000) return "Paket hat eine ungültige Anzahl Dateien";
                foreach (var e in za.Entries)
                {
                    string name = e.FullName.Replace('\\', '/');
                    if (name.StartsWith("/") || name.Contains(":") || Array.IndexOf(name.Split('/'), "..") >= 0) return "Paket enthält unzulässigen Pfad: " + Clip(e.FullName, 60);
                    string full = Path.GetFullPath(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar)));
                    if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return "Paket enthält unzulässigen Pfad: " + Clip(e.FullName, 60);
                    total += e.Length; if (total > 4 * MaxZipBytes) return "Paket ist zu groß";
                    if (name.EndsWith("/")) { Directory.CreateDirectory(full); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(full));
                    e.ExtractToFile(full, true);
                }
            }
            return null;
        }
        catch (InvalidDataException) { return "Paket ist beschädigt (kein gültiges ZIP)"; }
        catch (Exception ex) { return "Entpacken fehlgeschlagen: " + ex.Message; }
    }

    // Ordner im entpackten Paket, der Terminator.exe und Daten\TimerUI.dll enthält (Wurzel oder ein Unterordner)
    public static string FindPackageRoot(string extracted)
    {
        Func<string, bool> ok = d => File.Exists(Path.Combine(d, ExeName)) && File.Exists(Path.Combine(d, "Daten", "TimerUI.dll"));
        if (ok(extracted)) return extracted;
        var subs = Directory.GetDirectories(extracted);
        foreach (var s in subs) if (ok(s)) return s;
        return null;
    }

    // Neue EXE/DLL müssen echte .NET-Assemblies mit der erwarteten Version und dem erwarteten Namen sein
    public static string VerifyPackage(string root, Version expected)
    {
        if (root == null) return "Paket enthält kein " + ExeName + " mit Daten\\TimerUI.dll";
        var checks = new[] { new[] { Path.Combine(root, ExeName), "Terminator" }, new[] { Path.Combine(root, "Daten", "TimerUI.dll"), "TimerUI" } };
        foreach (var c in checks)
        {
            AssemblyName an;
            try { an = AssemblyName.GetAssemblyName(c[0]); } catch { return Path.GetFileName(c[0]) + " ist keine gültige Programmdatei"; }
            if (!string.Equals(an.Name, c[1], StringComparison.Ordinal)) return Path.GetFileName(c[0]) + ": falsche Programmkennung (" + an.Name + ")";
            if (Norm(an.Version).CompareTo(Norm(expected)) != 0) return Path.GetFileName(c[0]) + ": Version " + Show(an.Version) + " statt " + Show(expected);
        }
        return null;
    }

    // Kann in den Programmordner geschrieben werden? (sonst: Release-Seite öffnen)
    public static bool IsWritable(string dir)
    {
        try
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
            string t = Path.Combine(dir, ".update-test-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(t, "x"); File.Delete(t); return true;
        }
        catch { return false; }
    }
    public static bool LooksLikeInstallDir(string dir)
    {
        return !string.IsNullOrEmpty(dir) && (File.Exists(Path.Combine(dir, ExeName)) || File.Exists(Path.Combine(dir, "Daten", "TimerUI.dll")));
    }

    // ---------- Release Notes sicher als Klartext ----------
    public static List<string> NotesLines(string md, int maxLines, int maxLen)
    {
        var res = new List<string>();
        if (string.IsNullOrEmpty(md)) return res;
        foreach (var raw in md.Replace("\r", "").Split('\n'))
        {
            var sb = new StringBuilder();
            foreach (char ch in raw) if (!char.IsControl(ch) && ch != '\u202E' && ch != '\u202D') sb.Append(ch);
            string l = sb.ToString().Trim();
            while (l.StartsWith("#")) l = l.Substring(1).Trim();
            l = l.Replace("**", "").Replace("__", "").Replace("`", "");
            if (l.StartsWith("- ") || l.StartsWith("* ")) l = "• " + l.Substring(2).Trim();
            if (l.Length == 0 || l.StartsWith("<!--")) continue;
            res.Add(Clip(l, maxLen));
            if (res.Count >= maxLines) break;
        }
        return res;
    }

    public static string Clip(string s, int n) { s = s ?? ""; return s.Length > n ? s.Substring(0, Math.Max(0, n - 1)) + "…" : s; }
}

// Minimaler JSON-Leser (keine externen Abhängigkeiten)
public static class Json
{
    public static object Parse(string s)
    {
        if (s == null) throw new FormatException("leer");
        int i = 0; var v = Val(s, ref i); Ws(s, ref i);
        if (i != s.Length) throw new FormatException("Zeichen nach Ende");
        return v;
    }
    public static string Str(Dictionary<string, object> o, string k) { object v; return o != null && o.TryGetValue(k, out v) ? v as string : null; }
    public static bool Bool(Dictionary<string, object> o, string k) { object v; return o != null && o.TryGetValue(k, out v) && v is bool && (bool)v; }
    public static double Num(Dictionary<string, object> o, string k) { object v; return o != null && o.TryGetValue(k, out v) && v is double ? (double)v : -1; }

    static void Ws(string s, ref int i) { while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n' || s[i] == '\uFEFF')) i++; }
    static object Val(string s, ref int i, int depth = 0)
    {
        if (depth > 64) throw new FormatException("zu tief");
        Ws(s, ref i); if (i >= s.Length) throw new FormatException("unerwartetes Ende");
        char c = s[i];
        if (c == '{')
        {
            var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (true)
            {
                Ws(s, ref i); if (i >= s.Length || s[i] != '"') throw new FormatException("Schlüssel erwartet");
                string k = Strg(s, ref i); Ws(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException(": erwartet"); i++;
                d[k] = Val(s, ref i, depth + 1); Ws(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return d; }
                throw new FormatException(", oder } erwartet");
            }
        }
        if (c == '[')
        {
            var l = new List<object>(); i++; Ws(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(Val(s, ref i, depth + 1)); Ws(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return l; }
                throw new FormatException(", oder ] erwartet");
            }
        }
        if (c == '"') return Strg(s, ref i);
        if (Lit(s, ref i, "true")) return true;
        if (Lit(s, ref i, "false")) return false;
        if (Lit(s, ref i, "null")) return null;
        int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
        double dv; if (i > st && double.TryParse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture, out dv)) return dv;
        throw new FormatException("unerwartetes Zeichen");
    }
    static bool Lit(string s, ref int i, string w) { if (string.CompareOrdinal(s, i, w, 0, w.Length) == 0) { i += w.Length; return true; } return false; }
    static string Strg(string s, ref int i)
    {
        var sb = new StringBuilder(); i++;
        while (i < s.Length)
        {
            char c = s[i++];
            if (c == '"') return sb.ToString();
            if (c != '\\') { sb.Append(c); continue; }
            if (i >= s.Length) break;
            char e = s[i++];
            switch (e)
            {
                case 'n': sb.Append('\n'); break; case 'r': sb.Append('\r'); break; case 't': sb.Append('\t'); break;
                case 'b': sb.Append('\b'); break; case 'f': sb.Append('\f'); break;
                case 'u': if (i + 4 > s.Length) throw new FormatException("\\u"); sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 4; break;
                default: sb.Append(e); break;
            }
        }
        throw new FormatException("String nicht beendet");
    }
}
