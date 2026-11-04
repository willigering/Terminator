using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

// 3.17: Netzwerk, Download, Prüfen und Austausch-Helfer für das Auto-Update (ohne Oberfläche).
public delegate string Fetcher(string url, string etag, out string newEtag, out int status);
public delegate Stream StreamOpener(string url, out long length);

public sealed class UpdateClient
{
    public Version Installed, Os = Environment.OSVersion.Version;
    public string CacheFile;
    public Fetcher Fetch = HttpFetch;
    public StreamOpener Open = HttpOpen;

    public UpdateClient(Version installed, string cacheFile) { Installed = installed; CacheFile = cacheFile; }

    // Wirft nie. Rückgabe: null = Prüfung erfolgreich (info == null: kein Update), sonst Fehlertext.
    public string Check(string etag, out string newEtag, out UpdateInfo info)
    {
        info = null; newEtag = etag;
        try
        {
            string cached = ReadCache(), ne; int st;
            string body = Fetch(UpdateCore.ApiLatest, cached != null ? etag : null, out ne, out st);
            if (st == 304 && cached != null) body = cached;
            else if (st == 404) { newEtag = null; return null; } // noch kein stabiles Release
            else if (st == 403 || st == 429) return "GitHub-Limit erreicht – bitte später erneut prüfen";
            else if (st != 200 || body == null) return "GitHub antwortet nicht wie erwartet (HTTP " + st + ")";
            else { WriteCache(body); newEtag = ne; }

            ReleaseCandidate c;
            string err = UpdateCore.EvaluateRelease(body, Installed, out c);
            if (err != null || c == null) return err;
            string meta = Fetch(c.MetaUrl, null, out ne, out st);
            if (st != 200 || meta == null) return UpdateCore.MetaName + " nicht abrufbar (HTTP " + st + ")";
            string sums = Fetch(c.SumsUrl, null, out ne, out st);
            if (st != 200 || sums == null) return UpdateCore.SumsName + " nicht abrufbar (HTTP " + st + ")";
            return UpdateCore.EvaluateMetadata(c, meta, sums, Installed, Os, out info);
        }
        catch (Exception ex) { return "Keine Verbindung zu GitHub (" + ex.Message + ")"; }
    }

    string ReadCache() { try { return CacheFile != null && File.Exists(CacheFile) ? File.ReadAllText(CacheFile, Encoding.UTF8) : null; } catch { return null; } }
    void WriteCache(string body) { try { if (CacheFile == null) return; Directory.CreateDirectory(Path.GetDirectoryName(CacheFile)); File.WriteAllText(CacheFile, body, new UTF8Encoding(false)); } catch { } }

    // Lädt das ZIP nach dest; prüft Größe + SHA-256. Wirft nie. null = OK.
    public string Download(UpdateInfo info, string dest, Action<long, long> progress, Func<bool> cancelled)
    {
        string part = dest + ".part";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            if (File.Exists(part)) File.Delete(part);
            if (File.Exists(dest)) File.Delete(dest);
            long len;
            using (var src = Open(info.ZipUrl, out len))
            {
                if (len >= 0 && len != info.Size) return "Download hat unerwartete Größe (" + len + " statt " + info.Size + " Bytes)";
                using (var f = new FileStream(part, FileMode.CreateNew, FileAccess.Write))
                {
                    var buf = new byte[81920]; long total = 0; int n;
                    while ((n = src.Read(buf, 0, buf.Length)) > 0)
                    {
                        if (cancelled != null && cancelled()) { f.Close(); TryDelete(part); return "Download abgebrochen"; }
                        total += n;
                        if (total > info.Size) { f.Close(); TryDelete(part); return "Download ist größer als angekündigt – abgebrochen"; }
                        f.Write(buf, 0, n);
                        if (progress != null) progress(total, info.Size);
                    }
                }
            }
            string err = UpdateCore.VerifyFile(part, info.Size, info.Sha256);
            if (err != null) { TryDelete(part); return err; }
            File.Move(part, dest);
            return null;
        }
        catch (Exception ex) { TryDelete(part); return "Download unterbrochen (" + ex.Message + ")"; }
    }

    // Entpacken + Inhalt prüfen. Wirft nie. null = OK, root = Ordner mit den neuen Dateien.
    public static string Stage(UpdateInfo info, string zip, string extractDir, out string root)
    {
        root = null;
        try
        {
            if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
            string err = UpdateCore.VerifyFile(zip, info.Size, info.Sha256); // erneut, direkt vor der Verwendung
            if (err != null) return err;
            err = UpdateCore.ExtractZipSafe(zip, extractDir);
            if (err != null) return err;
            root = UpdateCore.FindPackageRoot(extractDir);
            return UpdateCore.VerifyPackage(root, info.Version);
        }
        catch (Exception ex) { return "Paket konnte nicht geprüft werden (" + ex.Message + ")"; }
    }

    static void TryDelete(string p) { try { if (File.Exists(p)) File.Delete(p); } catch { } }

    // ---------- HTTP (HTTPS only, TLS 1.2) ----------
    const string UA = "Terminator-Updater (+https://github.com/" + UpdateCore.Repo + ")";
    static HttpWebRequest Req(string url)
    {
        if (!url.StartsWith("https://", StringComparison.Ordinal)) throw new InvalidOperationException("nur HTTPS erlaubt");
        try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
#pragma warning disable SYSLIB0014
        var r = (HttpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014
        r.UserAgent = UA; r.Timeout = 15000; r.ReadWriteTimeout = 20000; r.AllowAutoRedirect = true; r.MaximumAutomaticRedirections = 5;
        return r;
    }
    static void EnsureHttps(WebResponse resp) { if (resp.ResponseUri == null || resp.ResponseUri.Scheme != "https") throw new InvalidOperationException("Umleitung auf unsichere Adresse"); }

    public static string HttpFetch(string url, string etag, out string newEtag, out int status)
    {
        newEtag = null; status = 0;
        var r = Req(url);
        if (url.StartsWith("https://api.github.com/", StringComparison.Ordinal)) { r.Accept = "application/vnd.github+json"; r.Headers["X-GitHub-Api-Version"] = "2022-11-28"; }
        if (!string.IsNullOrEmpty(etag)) r.Headers[HttpRequestHeader.IfNoneMatch] = etag;
        HttpWebResponse resp = null;
        try { resp = (HttpWebResponse)r.GetResponse(); }
        catch (WebException we) { resp = we.Response as HttpWebResponse; if (resp == null) throw; }
        using (resp)
        {
            status = (int)resp.StatusCode;
            if (status != 200) return null;
            EnsureHttps(resp);
            newEtag = resp.Headers[HttpResponseHeader.ETag];
            using (var s = resp.GetResponseStream())
            using (var ms = new MemoryStream())
            {
                var buf = new byte[16384]; int n;
                while ((n = s.Read(buf, 0, buf.Length)) > 0) { ms.Write(buf, 0, n); if (ms.Length > UpdateCore.MaxTextBytes) throw new InvalidOperationException("Antwort zu groß"); }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }

    public static Stream HttpOpen(string url, out long length)
    {
        var resp = (HttpWebResponse)Req(url).GetResponse();
        try { EnsureHttps(resp); } catch { resp.Dispose(); throw; }
        length = resp.ContentLength;
        return new RespStream(resp);
    }
    sealed class RespStream : Stream
    {
        readonly WebResponse resp; readonly Stream s;
        public RespStream(WebResponse r) { resp = r; s = r.GetResponseStream(); }
        public override int Read(byte[] b, int o, int c) { return s.Read(b, o, c); }
        protected override void Dispose(bool d) { if (d) { s.Dispose(); resp.Dispose(); } base.Dispose(d); }
        public override bool CanRead { get { return true; } } public override bool CanSeek { get { return false; } } public override bool CanWrite { get { return false; } }
        public override long Length { get { throw new NotSupportedException(); } } public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override void Flush() { } public override long Seek(long o, SeekOrigin so) { throw new NotSupportedException(); }
        public override void SetLength(long v) { throw new NotSupportedException(); } public override void Write(byte[] b, int o, int c) { throw new NotSupportedException(); }
    }

    // ---------- Austausch nach dem Beenden ----------
    // Das Hilfsskript wartet auf das Ende des Prozesses, sichert jede zu überschreibende Datei,
    // kopiert die neuen Dateien, rollt bei einem Fehler zurück und startet den Terminator neu.
    public const string SwapScript =
@"param([int]$WaitPid, [string]$Src, [string]$Dst, [string]$Bak, [string]$Restart, [string]$Log)
$ErrorActionPreference = 'Stop'
function L([string]$m) { try { Add-Content -LiteralPath $Log -Value ((Get-Date -Format s) + ' ' + $m) -Encoding UTF8 } catch { } }
L ('Start: ' + $Src + ' -> ' + $Dst)
try { $p = Get-Process -Id $WaitPid -ErrorAction SilentlyContinue; if ($p) { if (-not $p.WaitForExit(60000)) { L 'Programm wurde nicht beendet - Abbruch'; exit 2 } } } catch { }
Start-Sleep -Milliseconds 700
$srcRoot = (Resolve-Path -LiteralPath $Src).Path.TrimEnd('\', '/')
$done = New-Object System.Collections.ArrayList
$ok = $true
foreach ($f in (Get-ChildItem -LiteralPath $srcRoot -Recurse -File)) {
    $rel = $f.FullName.Substring($srcRoot.Length + 1)
    $t = Join-Path $Dst $rel; $b = Join-Path $Bak $rel
    try {
        $existed = Test-Path -LiteralPath $t
        if ($existed) { New-Item -ItemType Directory -Force -Path (Split-Path -Parent $b) | Out-Null; Copy-Item -LiteralPath $t -Destination $b -Force }
        [void]$done.Add(@($t, $b, $existed))
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $t) | Out-Null
        $n = 0
        while ($true) { try { Copy-Item -LiteralPath $f.FullName -Destination $t -Force; break } catch { $n++; if ($n -ge 10) { throw }; Start-Sleep -Milliseconds 500 } }
    } catch { L ('Fehler bei ' + $rel + ': ' + $_.Exception.Message); $ok = $false; break }
}
if (-not $ok) {
    foreach ($d in $done) { try { if ($d[2]) { Copy-Item -LiteralPath $d[1] -Destination $d[0] -Force } elseif (Test-Path -LiteralPath $d[0]) { Remove-Item -LiteralPath $d[0] -Force } } catch { L ('Rollback-Fehler: ' + $d[0]) } }
    L 'Update fehlgeschlagen - alte Version wiederhergestellt'
} else { L 'Update installiert' }
try { Set-Content -LiteralPath (Join-Path (Split-Path -Parent $Log) 'update-result.txt') -Value $(if ($ok) { 'ok' } else { 'rollback' }) } catch { }
if ($Restart) {
    try {
        if ($Restart.EndsWith('.vbs')) { Start-Process -FilePath (Join-Path $env:WINDIR 'System32\wscript.exe') -ArgumentList ('""' + $Restart + '""') -WorkingDirectory $Dst }
        else { Start-Process -FilePath $Restart -WorkingDirectory $Dst }
    } catch { L ('Neustart fehlgeschlagen: ' + $_.Exception.Message) }
}
if ($ok) { exit 0 } else { exit 1 }
";

    static string Q(string p) { return "\"" + p.TrimEnd('\\') + "\""; }
    public static string SwapArguments(string script, int pid, string src, string dst, string bak, string restart, string log)
    {
        return "-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File " + Q(script) + " -WaitPid " + pid +
               " -Src " + Q(src) + " -Dst " + Q(dst) + " -Bak " + Q(bak) + " -Restart " + Q(restart) + " -Log " + Q(log);
    }

    // Wirft nie. null = Helfer gestartet; danach muss sich die App beenden.
    public static string LaunchSwap(string workDir, string src, string dst, string restart)
    {
        try
        {
            string script = Path.Combine(workDir, "apply-update.ps1"), bak = Path.Combine(workDir, "backup"), log = Path.Combine(workDir, "update.log");
            if (Directory.Exists(bak)) Directory.Delete(bak, true);
            File.WriteAllText(script, SwapScript, new UTF8Encoding(true));
            string ps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            if (!File.Exists(ps)) return "Windows PowerShell nicht gefunden";
            var psi = new ProcessStartInfo(ps, SwapArguments(script, Process.GetCurrentProcess().Id, src, dst, bak, restart, log)) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = workDir };
            Process.Start(psi);
            return null;
        }
        catch (Exception ex) { return "Update-Helfer konnte nicht gestartet werden (" + ex.Message + ")"; }
    }
}
