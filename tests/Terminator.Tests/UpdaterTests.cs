using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using Xunit;

public class UpdaterTests : IDisposable
{
    static readonly Version V317 = new Version(3, 17, 0);
    const string Base = "https://github.com/willigering/Terminator/releases/download/v3.18/";
    readonly string tmp = Path.Combine(Path.GetTempPath(), "term-tests-" + Guid.NewGuid().ToString("N"));
    public UpdaterTests() { Directory.CreateDirectory(tmp); }
    public void Dispose() { try { Directory.Delete(tmp, true); } catch { } }

    // ---------- Hilfen ----------
    static string Release(string tag = "v3.18", bool draft = false, bool pre = false, string host = Base, string body = "- Neue Katze\\n- Fehler behoben")
    {
        return "{\"tag_name\":\"" + tag + "\",\"draft\":" + (draft ? "true" : "false") + ",\"prerelease\":" + (pre ? "true" : "false") +
               ",\"html_url\":\"https://github.com/willigering/Terminator/releases/tag/" + tag + "\",\"body\":\"" + body + "\",\"assets\":[" +
               "{\"name\":\"Terminator-Windows.zip\",\"browser_download_url\":\"" + host + "Terminator-Windows.zip\"}," +
               "{\"name\":\"terminator-update.json\",\"browser_download_url\":\"" + host + "terminator-update.json\"}," +
               "{\"name\":\"SHA256SUMS\",\"browser_download_url\":\"" + host + "SHA256SUMS\"}]}";
    }
    static string Meta(string version, long size, string sha, string app = "Terminator", string minWin = "6.1")
    {
        return "{\"app\":\"" + app + "\",\"version\":\"" + version + "\",\"tag\":\"v" + version + "\",\"asset\":\"Terminator-Windows.zip\",\"size\":" + size + ",\"sha256\":\"" + sha + "\",\"minWindows\":\"" + minWin + "\"}";
    }
    static string Sha(byte[] b) { using (var ms = new MemoryStream(b)) return UpdateCore.Sha256Hex(ms); }

    sealed class FakeNet
    {
        public readonly Dictionary<string, KeyValuePair<int, string>> Map = new Dictionary<string, KeyValuePair<int, string>>();
        public Exception Throw; public string LastEtagSent; public int Calls;
        public string Fetch(string url, string etag, out string newEtag, out int status)
        {
            Calls++;
            if (Throw != null) throw Throw;
            if (url == UpdateCore.ApiLatest) LastEtagSent = etag;
            newEtag = "\"etag-1\"";
            KeyValuePair<int, string> r;
            if (!Map.TryGetValue(url, out r)) { status = 404; return null; }
            status = r.Key; if (status == 304 && etag == null) status = 200;
            return r.Key == 200 || (r.Key == 304 && etag == null) ? r.Value : null;
        }
    }
    UpdateClient Client(FakeNet n, Version installed = null)
    {
        var c = new UpdateClient(installed ?? V317, Path.Combine(tmp, "cache.json")); c.Fetch = n.Fetch; return c;
    }
    FakeNet NetFor(string release, string meta, string sums)
    {
        var n = new FakeNet();
        n.Map[UpdateCore.ApiLatest] = new KeyValuePair<int, string>(200, release);
        if (meta != null) n.Map[Base + "terminator-update.json"] = new KeyValuePair<int, string>(200, meta);
        if (sums != null) n.Map[Base + "SHA256SUMS"] = new KeyValuePair<int, string>(200, sums);
        return n;
    }
    // ---------- Updateprüfung ----------
    [Fact] public void NoInternet_NoCrash_ReturnsError()
    {
        var n = new FakeNet { Throw = new WebException("Der Remotename konnte nicht aufgelöst werden", WebExceptionStatus.NameResolutionFailure) };
        string etag; UpdateInfo info;
        var err = Client(n).Check(null, out etag, out info);
        Assert.NotNull(err); Assert.Null(info); Assert.Contains("Keine Verbindung", err);
    }
    [Fact] public void GitHubUnreachable_ServerError_NoCrash()
    {
        var n = new FakeNet(); n.Map[UpdateCore.ApiLatest] = new KeyValuePair<int, string>(502, null);
        string etag; UpdateInfo info;
        Assert.Contains("502", Client(n).Check(null, out etag, out info)); Assert.Null(info);
    }
    [Fact] public void GitHubTimeout_NoCrash()
    {
        var n = new FakeNet { Throw = new WebException("timeout", WebExceptionStatus.Timeout) };
        string etag; UpdateInfo info; Assert.NotNull(Client(n).Check(null, out etag, out info));
    }
    [Fact] public void BrokenGitHubJson_NoCrash()
    {
        foreach (var body in new[] { "", "{", "[]", "{\"tag_name\":12}", "<html>rate limited</html>", "{\"tag_name\":\"v../../x\"}" })
        {
            var n = NetFor(body, null, null); string etag; UpdateInfo info;
            Assert.NotNull(Client(n).Check(null, out etag, out info)); Assert.Null(info);
        }
    }
    [Fact] public void RateLimit_ReportsLimit()
    {
        var n = new FakeNet(); n.Map[UpdateCore.ApiLatest] = new KeyValuePair<int, string>(403, null);
        string etag; UpdateInfo info; Assert.Contains("Limit", Client(n).Check(null, out etag, out info));
    }
    [Fact] public void NoReleaseYet_NoUpdate_NoError()
    {
        string etag; UpdateInfo info; Assert.Null(Client(new FakeNet()).Check(null, out etag, out info)); Assert.Null(info);
    }
    [Fact] public void SameVersion_NoUpdate()
    {
        var n = NetFor(Release("v3.17"), null, null); string etag; UpdateInfo info;
        Assert.Null(Client(n).Check(null, out etag, out info)); Assert.Null(info);
        Assert.Equal(1, n.Calls); // keine weiteren Anfragen
    }
    [Fact] public void OlderVersion_NoDowngrade()
    {
        var n = NetFor(Release("v3.16"), null, null); string etag; UpdateInfo info;
        Assert.Null(Client(n).Check(null, out etag, out info)); Assert.Null(info);
    }
    [Fact] public void DraftAndPrerelease_Ignored()
    {
        foreach (var rel in new[] { Release(draft: true), Release(pre: true) })
        {
            string etag; UpdateInfo info; Assert.Null(Client(NetFor(rel, null, null)).Check(null, out etag, out info)); Assert.Null(info);
        }
    }
    [Fact] public void NewVersion_UpdateOffered_WithNotes()
    {
        var zip = new byte[] { 1, 2, 3, 4 }; string sha = Sha(zip);
        var n = NetFor(Release(), Meta("3.18.0", 4, sha), sha + "  Terminator-Windows.zip\n");
        string etag; UpdateInfo info;
        Assert.Null(Client(n).Check(null, out etag, out info));
        Assert.NotNull(info); Assert.Equal(new Version(3, 18, 0), info.Version); Assert.Equal(sha, info.Sha256); Assert.Equal(4, info.Size);
        Assert.Equal("\"etag-1\"", etag);
        Assert.Equal(new[] { "• Neue Katze", "• Fehler behoben" }, UpdateCore.NotesLines(info.Notes, 5, 80).ToArray());
    }
    [Fact] public void ETag_SentOnlyWithCache_And304UsesCache()
    {
        var zip = new byte[] { 9 }; string sha = Sha(zip);
        var n = NetFor(Release(), Meta("3.18.0", 1, sha), sha + "  Terminator-Windows.zip\n");
        var c = Client(n); string etag; UpdateInfo info;
        c.Check("\"old\"", out etag, out info);
        Assert.Null(n.LastEtagSent); // noch kein Cache -> keine bedingte Anfrage
        n.Map[UpdateCore.ApiLatest] = new KeyValuePair<int, string>(304, null);
        Assert.Null(c.Check(etag, out etag, out info));
        Assert.Equal("\"etag-1\"", n.LastEtagSent); Assert.NotNull(info);
    }
    [Fact] public void ForeignDownloadHost_Rejected()
    {
        var n = NetFor(Release(host: "https://evil.example.com/"), null, null); string etag; UpdateInfo info;
        Assert.Contains("unvollständig", Client(n).Check(null, out etag, out info)); Assert.Null(info);
    }
    [Fact] public void HttpDownloadUrl_Rejected()
    {
        var n = NetFor(Release(host: "http://github.com/willigering/Terminator/releases/download/v3.18/"), null, null); string etag; UpdateInfo info;
        Assert.NotNull(Client(n).Check(null, out etag, out info)); Assert.Null(info);
    }
    [Fact] public void ManipulatedMetadata_Rejected()
    {
        string sha = Sha(new byte[] { 1 }), sums = sha + "  Terminator-Windows.zip\n";
        var bad = new[]
        {
            Meta("3.19.0", 1, sha),                        // Version passt nicht zum Tag
            Meta("3.18.0", 1, sha, app: "Other"),         // falsche App-Kennung
            Meta("3.18.0", 1, new string('a', 64)),        // widerspricht SHA256SUMS
            Meta("3.18.0", 0, sha),                        // Größe ungültig
            Meta("3.18.0", 1, "xyz"),                      // keine Prüfsumme
            "{kaputt",
        };
        foreach (var m in bad) { string e; UpdateInfo i; Assert.NotNull(Client(NetFor(Release(), m, sums)).Check(null, out e, out i)); Assert.Null(i); }
        string e2; UpdateInfo i2; Assert.NotNull(Client(NetFor(Release(), Meta("3.18.0", 1, sha), "")).Check(null, out e2, out i2)); Assert.Null(i2);
    }
    [Fact] public void UnsuitableWindowsVersion_Rejected()
    {
        string sha = Sha(new byte[] { 1 });
        var c = Client(NetFor(Release(), Meta("3.18.0", 1, sha, minWin: "10.0"), sha + "  Terminator-Windows.zip"));
        c.Os = new Version(6, 1, 7601); string e; UpdateInfo i;
        Assert.Contains("Windows", c.Check(null, out e, out i)); Assert.Null(i);
    }

    [Theory]
    [InlineData("v3.17", "3.17.0")] [InlineData("3.17.1", "3.17.1")] [InlineData("V4", "4.0.0")] [InlineData("v3.16", "3.16.0")]
    public void VersionParsing(string s, string expect) { Version v; Assert.True(UpdateCore.TryParseVersion(s, out v)); Assert.Equal(Version.Parse(expect), v); }
    [Theory] [InlineData("")] [InlineData("latest")] [InlineData("v3.17-beta")] [InlineData("v-1.2")] [InlineData("1.2.3.4.5")]
    public void VersionParsing_Invalid(string s) { Version v; Assert.False(UpdateCore.TryParseVersion(s, out v)); }
    [Fact] public void VersionCompare_Numeric_NotLexical()
    {
        Assert.True(UpdateCore.IsNewer(new Version(3, 100, 0), new Version(3, 17, 0)));
        Assert.False(UpdateCore.IsNewer(new Version(3, 17), new Version(3, 17, 0, 0)));
    }
    [Fact] public void AutoCheck_AtMostEvery24h()
    {
        var now = new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);
        Assert.True(UpdateCore.DueForAutoCheck(now, DateTime.MinValue));
        Assert.False(UpdateCore.DueForAutoCheck(now, now.AddHours(-23)));
        Assert.True(UpdateCore.DueForAutoCheck(now, now.AddHours(-24)));
        Assert.True(UpdateCore.DueForAutoCheck(now, now.AddDays(3))); // Uhr verstellt
    }
    [Fact] public void Notes_SanitizedAndLimited()
    {
        var l = UpdateCore.NotesLines("## Neu\r\n\r\n* **fett** `code`\n<!-- x -->\n\u0007Steuer\u202Ezeichen\n" + new string('x', 200) + "\na\nb\nc", 4, 50);
        Assert.Equal(4, l.Count); Assert.Equal("Neu", l[0]); Assert.Equal("• fett code", l[1]); Assert.Equal("Steuerzeichen", l[2]); Assert.True(l[3].Length <= 50);
    }

    // ---------- Download ----------
    sealed class BreakingStream : MemoryStream
    {
        readonly int breakAt; public BreakingStream(byte[] b, int at) : base(b) { breakAt = at; }
        public override int Read(byte[] buf, int o, int c) { if (Position >= breakAt) throw new IOException("Verbindung getrennt"); return base.Read(buf, o, Math.Min(c, breakAt - (int)Position)); }
    }
    UpdateInfo Info(byte[] data) { return new UpdateInfo { Version = new Version(3, 18, 0), ZipUrl = Base + "Terminator-Windows.zip", Size = data.Length, Sha256 = Sha(data) }; }

    [Fact] public void Download_Ok_VerifiesHash()
    {
        var data = Encoding.UTF8.GetBytes(new string('z', 300000)); var c = Client(new FakeNet());
        c.Open = (string u, out long l) => { l = data.Length; return new MemoryStream(data); };
        string dest = Path.Combine(tmp, "dl", "a.zip"); long last = 0;
        Assert.Null(c.Download(Info(data), dest, (n, t) => last = n, null));
        Assert.Equal(data.Length, last); Assert.True(File.Exists(dest));
    }
    [Fact] public void Download_Interrupted_CleanError_NoLeftovers()
    {
        var data = new byte[200000]; new Random(1).NextBytes(data); var c = Client(new FakeNet());
        c.Open = (string u, out long l) => { l = -1; return new BreakingStream(data, 100000); };
        string dest = Path.Combine(tmp, "dl", "b.zip");
        Assert.Contains("unterbrochen", c.Download(Info(data), dest, null, null));
        Assert.False(File.Exists(dest)); Assert.False(File.Exists(dest + ".part"));
    }
    [Fact] public void Download_Truncated_Rejected()
    {
        var data = new byte[1000]; var c = Client(new FakeNet());
        c.Open = (string u, out long l) => { l = -1; return new MemoryStream(data, 0, 600); };
        string dest = Path.Combine(tmp, "dl", "c.zip");
        Assert.Contains("unvollständig", c.Download(Info(data), dest, null, null)); Assert.False(File.Exists(dest));
    }
    [Fact] public void Download_Corrupted_Rejected()
    {
        var data = new byte[1000]; var bad = (byte[])data.Clone(); bad[500] = 1; var c = Client(new FakeNet());
        c.Open = (string u, out long l) => { l = bad.Length; return new MemoryStream(bad); };
        string dest = Path.Combine(tmp, "dl", "d.zip");
        Assert.Contains("Prüfsumme", c.Download(Info(data), dest, null, null)); Assert.False(File.Exists(dest));
    }
    [Fact] public void Download_Oversized_Rejected()
    {
        var data = new byte[1000]; var c = Client(new FakeNet());
        c.Open = (string u, out long l) => { l = -1; return new MemoryStream(new byte[5000]); };
        Assert.NotNull(c.Download(Info(data), Path.Combine(tmp, "dl", "e.zip"), null, null));
    }
    [Fact] public void Download_Cancelled()
    {
        var data = new byte[500000]; var c = Client(new FakeNet());
        c.Open = (string u, out long l) => { l = data.Length; return new MemoryStream(data); };
        string dest = Path.Combine(tmp, "dl", "f.zip");
        Assert.Contains("abgebrochen", c.Download(Info(data), dest, null, () => true)); Assert.False(File.Exists(dest + ".part"));
    }
    [Fact] public void Download_HttpOnlyUrl_Refused()
    {
        var c = new UpdateClient(V317, null); var i = new UpdateInfo { ZipUrl = "http://example.com/x.zip", Size = 1, Sha256 = new string('0', 64) };
        Assert.NotNull(c.Download(i, Path.Combine(tmp, "dl", "g.zip"), null, null));
    }

    // ---------- Paket ----------
    string FixtureDir() { return Path.GetDirectoryName(typeof(UpdaterTests).Assembly.Location); }
    byte[] BuildZip(Action<ZipArchive> fill)
    {
        using (var ms = new MemoryStream())
        {
            using (var za = new ZipArchive(ms, ZipArchiveMode.Create, true)) fill(za);
            return ms.ToArray();
        }
    }
    static void Add(ZipArchive za, string name, byte[] data) { using (var s = za.CreateEntry(name).Open()) s.Write(data, 0, data.Length); }
    byte[] GoodPackage(string exeAssembly = "Terminator.dll")
    {
        var exe = File.ReadAllBytes(Path.Combine(FixtureDir(), exeAssembly)); var dll = File.ReadAllBytes(Path.Combine(FixtureDir(), "TimerUI.dll"));
        return BuildZip(za => { Add(za, "Terminator/Terminator.exe", exe); Add(za, "Terminator/Daten/TimerUI.dll", dll); Add(za, "Terminator/Terminator.ps1", Encoding.UTF8.GetBytes("# neu")); });
    }
    string Stage(byte[] zip, Version v, out string root)
    {
        string z = Path.Combine(tmp, "pkg.zip"); File.WriteAllBytes(z, zip);
        var info = new UpdateInfo { Version = v, Size = zip.Length, Sha256 = Sha(zip) };
        return UpdateClient.Stage(info, z, Path.Combine(tmp, "neu"), out root);
    }

    [Fact] public void Package_Valid_Accepted()
    {
        string root; Assert.Null(Stage(GoodPackage(), new Version(9, 9, 0), out root));
        Assert.True(File.Exists(Path.Combine(root, "Terminator.exe")));
    }
    [Fact] public void Package_WrongVersionInside_Rejected()
    {
        string root; Assert.Contains("Version", Stage(GoodPackage(), new Version(3, 18, 0), out root));
    }
    [Fact] public void Package_WrongIdentity_Rejected()
    {
        // fremde Assembly als Terminator.exe
        string root; Assert.Contains("falsche Programmkennung", Stage(GoodPackage("TimerUI.dll"), new Version(9, 9, 0), out root));
    }
    [Fact] public void Package_NotAnAssembly_Rejected()
    {
        var zip = BuildZip(za => { Add(za, "Terminator/Terminator.exe", Encoding.UTF8.GetBytes("MZ kaputt")); Add(za, "Terminator/Daten/TimerUI.dll", new byte[] { 1 }); });
        string root; Assert.Contains("keine gültige Programmdatei", Stage(zip, new Version(9, 9, 0), out root));
    }
    [Fact] public void Package_CorruptZip_Rejected()
    {
        string root; Assert.Contains("beschädigt", Stage(Encoding.UTF8.GetBytes("PK das ist kein zip"), new Version(9, 9, 0), out root));
    }
    [Fact] public void Package_ZipSlip_Rejected()
    {
        var zip = BuildZip(za => { Add(za, "Terminator/../../boese.txt", new byte[] { 1 }); });
        string root; Assert.Contains("unzulässigen Pfad", Stage(zip, new Version(9, 9, 0), out root));
        Assert.False(File.Exists(Path.Combine(tmp, "..", "boese.txt")));
    }
    [Fact] public void Package_Incomplete_Rejected()
    {
        var zip = BuildZip(za => Add(za, "Terminator/LIESMICH.txt", new byte[] { 1 }));
        string root; Assert.NotNull(Stage(zip, new Version(9, 9, 0), out root));
    }
    [Fact] public void Package_HashChangedAfterDownload_Rejected()
    {
        var zip = GoodPackage(); string z = Path.Combine(tmp, "pkg2.zip"); File.WriteAllBytes(z, zip);
        var info = new UpdateInfo { Version = new Version(9, 9, 0), Size = zip.Length, Sha256 = new string('0', 64) };
        string root; Assert.Contains("Prüfsumme", UpdateClient.Stage(info, z, Path.Combine(tmp, "neu2"), out root));
    }

    // ---------- Einstellungen / Benutzerdaten ----------
    [Fact] public void Settings_RoundTrip_KeepsOldAndNewKeys()
    {
        var old = Settings.Parse("finale=0\ntop_on=1\ntop=MEIN TEXT\nbottom_on=0\nbottom=x\nmin_after_start=1\n");
        Assert.True(old.UpdateCheck); // Standard AN für bestehende Nutzer
        old.UpdateCheck = false; old.UpdateLastTicks = 638000000000000000; old.UpdateEtag = "W/\"abc\"";
        var back = Settings.Parse(old.Serialize());
        Assert.False(back.Finale); Assert.Equal("MEIN TEXT", back.Top); Assert.True(back.AutoMinimize);
        Assert.False(back.UpdateCheck); Assert.Equal(638000000000000000, back.UpdateLastTicks); Assert.Equal("W/\"abc\"", back.UpdateEtag);
        Assert.Equal(0, Settings.Parse("update_last=-5\n").UpdateLastTicks);
    }

    // ---------- Austausch-Skript (braucht PowerShell 7 "pwsh" oder Windows PowerShell) ----------
    static string FindPwsh()
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            foreach (var n in new[] { "pwsh", "pwsh.exe", "powershell.exe" })
                if (dir.Length > 0 && File.Exists(Path.Combine(dir, n))) return Path.Combine(dir, n);
        return null;
    }
    int RunSwap(string src, string dst)
    {
        string ps = FindPwsh(); if (ps == null) return -100;
        string script = Path.Combine(tmp, "apply-update.ps1"); File.WriteAllText(script, UpdateClient.SwapScript, new UTF8Encoding(true));
        var psi = new ProcessStartInfo(ps) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var a in new[] { "-NoProfile", "-NonInteractive", "-File", script, "-WaitPid", "999999", "-Src", src, "-Dst", dst, "-Bak", Path.Combine(tmp, "backup"), "-Restart", "", "-Log", Path.Combine(tmp, "update.log") })
            psi.ArgumentList.Add(a);
        using (var p = Process.Start(psi)) { p.StandardOutput.ReadToEnd(); p.StandardError.ReadToEnd(); p.WaitForExit(); return p.ExitCode; }
    }
    [Fact] public void Swap_ReplacesFiles_KeepsUserFiles()
    {
        string src = Path.Combine(tmp, "src"), dst = Path.Combine(tmp, "dst");
        Directory.CreateDirectory(Path.Combine(src, "Daten")); Directory.CreateDirectory(Path.Combine(dst, "Daten"));
        File.WriteAllText(Path.Combine(src, "Terminator.exe"), "neu"); File.WriteAllText(Path.Combine(src, "Daten", "TimerUI.dll"), "neu-dll");
        File.WriteAllText(Path.Combine(dst, "Terminator.exe"), "alt"); File.WriteAllText(Path.Combine(dst, "Daten", "TimerUI.dll"), "alt-dll");
        File.WriteAllText(Path.Combine(dst, "meine-notiz.txt"), "bleibt");
        int rc = RunSwap(src, dst); if (rc == -100) return; // kein PowerShell vorhanden
        Assert.Equal(0, rc);
        Assert.Equal("neu", File.ReadAllText(Path.Combine(dst, "Terminator.exe")));
        Assert.Equal("neu-dll", File.ReadAllText(Path.Combine(dst, "Daten", "TimerUI.dll")));
        Assert.Equal("bleibt", File.ReadAllText(Path.Combine(dst, "meine-notiz.txt")));
        Assert.Equal("alt", File.ReadAllText(Path.Combine(tmp, "backup", "Terminator.exe")));
        Assert.Equal("ok", File.ReadAllText(Path.Combine(tmp, "update-result.txt")).Trim());
    }
    [Fact] public void Swap_FailureMidway_RollsBack()
    {
        string src = Path.Combine(tmp, "src"), dst = Path.Combine(tmp, "dst");
        Directory.CreateDirectory(Path.Combine(src, "Daten")); Directory.CreateDirectory(dst);
        File.WriteAllText(Path.Combine(src, "A.txt"), "neu"); File.WriteAllText(Path.Combine(src, "Daten", "TimerUI.dll"), "neu-dll");
        File.WriteAllText(Path.Combine(dst, "A.txt"), "alt");
        File.WriteAllText(Path.Combine(dst, "Daten"), "Datei statt Ordner -> Kopieren schlägt fehl");
        int rc = RunSwap(src, dst); if (rc == -100) return;
        Assert.Equal(1, rc);
        Assert.Equal("alt", File.ReadAllText(Path.Combine(dst, "A.txt")));
        Assert.Equal("rollback", File.ReadAllText(Path.Combine(tmp, "update-result.txt")).Trim());
    }
    [Fact] public void SwapArguments_QuotePaths()
    {
        var a = UpdateClient.SwapArguments(@"C:\T\a.ps1", 5, @"C:\T\neu\Terminator", @"C:\Users\Max Mustermann\Terminator\", @"C:\T\b", @"C:\x\Terminator starten.vbs", @"C:\T\l.log");
        Assert.Contains("-Dst \"C:\\Users\\Max Mustermann\\Terminator\"", a); Assert.Contains("-Restart \"C:\\x\\Terminator starten.vbs\"", a);
    }
}
