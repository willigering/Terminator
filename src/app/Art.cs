using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

// 3.2: Das Original-Design (1538x1023) ist die Grundlage. Es wird nur gleichmäßig skaliert (x und y gleicher Faktor)
// und alle veränderlichen Teile (Ziffern, Knopf-Zustände, Texte) kommen als Ausschnitte aus demselben Bild darüber.
static class Art
{
    static Image design, atlas;
    static readonly ImageAttributes clamp = MakeClamp();
    static ImageAttributes MakeClamp() { var a = new ImageAttributes(); a.SetWrapMode(WrapMode.TileFlipXY); return a; }
    public static Image Load(string n) { try { var s = typeof(Art).Assembly.GetManifestResourceStream(n); return s != null ? Image.FromStream(s) : null; } catch { return null; } }
    public static Image Design { get { if (design == null) design = Load("design.png"); return design; } }
    static Image Atlas { get { if (atlas == null) atlas = Load("sprites.png"); return atlas; } }
    public const float LogicalW = 960f;
    public static readonly float ArtH = Sprites.SrcH / Sprites.K;      // 638.5 logische Pixel
    public static readonly int LogicalH = (int)Math.Ceiling(ArtH);      // Fensterhöhe 639

    public static RectangleF Dest(string n, float dx = 0, float dy = 0)
    {
        var a = Sprites.Atlas[n]; var p = Sprites.Pos[n];
        return new RectangleF((p.X + dx) / Sprites.K, (p.Y + dy) / Sprites.K, a.Width / Sprites.K, a.Height / Sprites.K);
    }
    public static void Draw(Graphics g, string n, float dx = 0, float dy = 0)
    {
        if (Atlas == null || !Sprites.Atlas.ContainsKey(n)) return;
        var a = Sprites.Atlas[n]; var d = Dest(n, dx, dy);
        var st = g.Save();
        g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(Atlas, new[] { d.Location, new PointF(d.Right, d.Top), new PointF(d.Left, d.Bottom) }, new RectangleF(a.X, a.Y, a.Width, a.Height), GraphicsUnit.Pixel, clamp);
        g.Restore(st);
    }
    public static void DrawBackground(Graphics g)
    {
        var img = Design; if (img == null) { g.Clear(Color.Black); return; }
        var st = g.Save();
        g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(img, new[] { new PointF(0, 0), new PointF(LogicalW, 0), new PointF(0, ArtH) }, new RectangleF(0, 0, img.Width, img.Height), GraphicsUnit.Pixel, clamp);
        // letzter halber Pixel (Fensterhöhe ganzzahlig): unterste Bildzeile fortsetzen, nichts wird gestreckt
        g.DrawImage(img, new[] { new PointF(0, ArtH - 0.5f), new PointF(LogicalW, ArtH - 0.5f), new PointF(0, LogicalH + 1) }, new RectangleF(0, img.Height - 1, img.Width, 1), GraphicsUnit.Pixel, clamp);
        g.Restore(st);
    }

    // ---- 3.3: Ziffern prozedural (saubere 7-Segment-Geometrie, abgeschrägte Enden, gleichmäßige Strichstärke) ----
    // Maße aus dem Design: Ziffernhöhe 20.6, Breite 13.1 (inkl. Neigung), Abstand 15.6 logische Pixel.
    static readonly string[] Map = { "abcdef", "bc", "abged", "abgcd", "fgbc", "afgcd", "afgedc", "abc", "abcdefg", "abcdfg" };
    public static readonly RectangleF PanelRect = Dest("panel");
    static readonly float[] PairCX = { 418.2f, 477.05f, 536.65f }, ColonCX = { 447.8f, 506.5f };
    const float DigitCY = 360.1f, CellH = 18.6f, CellW = 10.2f, Stroke = 2.6f, Pitch = 15.6f, Slant = 0.12f, Gap = 0.4f;
    static PointF[] SegPoly(char s, float x, float y)
    {
        float w = CellW, h = CellH, t = Stroke, m = y + h / 2;
        switch (s)
        {
            case 'a': return Hz(x + Gap, x + w - Gap, y, t);
            case 'g': return Hz(x + Gap, x + w - Gap, m, t);
            case 'd': return Hz(x + Gap, x + w - Gap, y + h, t);
            case 'f': return Vt(x, y + Gap, m - Gap, t);
            case 'e': return Vt(x, m + Gap, y + h - Gap, t);
            case 'b': return Vt(x + w, y + Gap, m - Gap, t);
            default: return Vt(x + w, m + Gap, y + h - Gap, t);
        }
    }
    static PointF[] Hz(float x1, float x2, float cy, float t)
    { return new[] { new PointF(x1, cy), new PointF(x1 + t / 2, cy - t / 2), new PointF(x2 - t / 2, cy - t / 2), new PointF(x2, cy), new PointF(x2 - t / 2, cy + t / 2), new PointF(x1 + t / 2, cy + t / 2) }; }
    static PointF[] Vt(float cx, float y1, float y2, float t)
    { return new[] { new PointF(cx, y1), new PointF(cx + t / 2, y1 + t / 2), new PointF(cx + t / 2, y2 - t / 2), new PointF(cx, y2), new PointF(cx - t / 2, y2 - t / 2), new PointF(cx - t / 2, y1 + t / 2) }; }
    public static readonly Color DigitOn = Tone.C(255, 88, 62), DigitCore = Tone.C(255, 150, 120);
    // Immer gleich gezeichnet – auch im Leerlauf (00:30:00): saubere Platten aus dem Bild, darauf die Ziffern
    public static void DrawDigits(Graphics g, int sec)
    {
        sec = Math.Max(0, Math.Min(sec, 99 * 3600 + 59 * 60 + 59));
        Draw(g, "panel");
        string t = (sec / 3600).ToString("00") + ((sec / 60) % 60).ToString("00") + (sec % 60).ToString("00");
        var st = g.Save(); var baseM = g.Transform;
        g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        using (var glow1 = new Pen(Tone.C(38, 255, 60, 30), 5.5f) { LineJoin = LineJoin.Round })
        using (var glow2 = new Pen(Tone.C(70, 255, 70, 40), 3f) { LineJoin = LineJoin.Round })
        using (var on = new SolidBrush(DigitOn))
        using (var core = new SolidBrush(Tone.C(170, DigitCore)))
        {
            for (int pass = 0; pass < 3; pass++)
                for (int i = 0; i < 6; i++)
                {
                    float cx = PairCX[i / 2] + (i % 2 == 0 ? -Pitch / 2 : Pitch / 2);
                    float x = cx - CellW / 2, y = DigitCY - CellH / 2;
                    // Neigung um die Ziffernmitte (wie im Design leicht kursiv)
                    using (var m = baseM.Clone()) { m.Multiply(new Matrix(1, 0, -Slant, 1, Slant * (y + CellH / 2), 0), MatrixOrder.Prepend); g.Transform = m; }
                    foreach (char s in Map[t[i] - '0'])
                    {
                        var p = SegPoly(s, x, y);
                        if (pass == 0) g.DrawPolygon(glow1, p);
                        else if (pass == 1) { g.DrawPolygon(glow2, p); g.FillPolygon(on, p); }
                        else
                        {
                            // heller Kern: dieselbe Form, schmaler
                            var c = Shrink(p);
                            g.FillPolygon(core, c);
                        }
                    }
                }
            g.Transform = baseM;
            using (var cg = new SolidBrush(Tone.C(60, 255, 60, 30)))
                foreach (float cx in ColonCX)
                    foreach (float dy in new[] { -4.2f, 4.2f })
                    {
                        g.FillEllipse(cg, cx - 2.6f, DigitCY + dy - 2.6f, 5.2f, 5.2f);
                        g.FillEllipse(on, cx - 1.35f, DigitCY + dy - 1.35f, 2.7f, 2.7f);
                    }
        }
        g.Restore(st);
    }
    static PointF[] Shrink(PointF[] p)
    {
        float cx = 0, cy = 0; foreach (var q in p) { cx += q.X; cy += q.Y; } cx /= p.Length; cy /= p.Length;
        var r = new PointF[p.Length];
        for (int i = 0; i < p.Length; i++) r[i] = new PointF(cx + (p[i].X - cx) * 0.55f, cy + (p[i].Y - cy) * 0.45f);
        return r;
    }

    // ---- 3.3: eine Schrift für die ganze Leiste, immer live gezeichnet (Pfad, DPI-scharf) ----
    public static void Text(Graphics g, string s, float px, Color c, float x, float cy, int align)
    {
        LoadFonts(); var fam = medium ?? regular;
        if (fam == null) { using (var f = new Font("Bahnschrift", px, GraphicsUnit.Pixel)) using (var b = new SolidBrush(c)) using (var sf = new StringFormat { Alignment = align < 0 ? StringAlignment.Near : align > 0 ? StringAlignment.Far : StringAlignment.Center, LineAlignment = StringAlignment.Center }) g.DrawString(s, f, b, new RectangleF(align < 0 ? x : align > 0 ? x - 400 : x - 200, cy - px, 400, px * 2), sf); return; }
        using (var path = new GraphicsPath())
        {
            path.AddString(s, fam, 0, px, new PointF(0, 0), StringFormat.GenericTypographic);
            float asc = px * fam.GetCellAscent(FontStyle.Regular) / fam.GetEmHeight(FontStyle.Regular);
            float cap = px * 0.70f; // Versalhöhe von Barlow
            float w = TextWidth(s, px);
            float ox = align < 0 ? x : align > 0 ? x - w : x - w / 2;
            using (var m = new Matrix()) { m.Translate(ox, cy + cap / 2 - asc); path.Transform(m); }
            var st = g.Save(); g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (var b = new SolidBrush(c)) g.FillPath(b, path);
            g.Restore(st);
        }
    }
    public static float TextWidth(string s, float px)
    {
        LoadFonts(); var fam = medium ?? regular; if (fam == null) return s.Length * px * 0.5f;
        using (var f = new Font(fam, px, FontStyle.Regular, GraphicsUnit.Pixel))
        using (var bmp = new Bitmap(1, 1)) using (var g = Graphics.FromImage(bmp))
            return g.MeasureString(s, f, 4000, StringFormat.GenericTypographic).Width;
    }

    // ---- Schrift: Barlow Semi Condensed (SIL OFL, eingebettet) – auf jedem PC gleich ----
    static PrivateFontCollection pfc;
    static FontFamily regular, medium;
    static void LoadFonts()
    {
        if (pfc != null) return;
        pfc = new PrivateFontCollection();
        foreach (var n in new[] { "BarlowSemiCondensed-Regular.ttf", "BarlowSemiCondensed-Medium.ttf" })
            try
            {
                using (var s = typeof(Art).Assembly.GetManifestResourceStream(n))
                {
                    if (s == null) continue;
                    var data = new byte[s.Length]; s.Read(data, 0, data.Length);
                    IntPtr p = Marshal.AllocCoTaskMem(data.Length); Marshal.Copy(data, 0, p, data.Length);
                    pfc.AddMemoryFont(p, data.Length); // Speicher bleibt bewusst reserviert, solange das Programm läuft
                }
            }
            catch { }
        foreach (var f in pfc.Families) { if (f.Name.EndsWith("Medium")) medium = f; else regular = f; }
        if (medium == null) medium = regular;
    }
    // Größe in logischen Pixeln (nicht Punkt) – skaliert mit der Fenster-Transformation, kein doppeltes DPI
    public static Font Font(float px, bool med)
    {
        LoadFonts();
        var fam = med ? medium : regular;
        if (fam != null) try { return new Font(fam, px, FontStyle.Regular, GraphicsUnit.Pixel); } catch { }
        return new Font("Bahnschrift", px * 0.95f, FontStyle.Regular, GraphicsUnit.Pixel);
    }
    public static readonly Color Ink = Tone.C(236, 236, 238), StatusRed = Tone.C(238, 72, 62), StatusGrey = Tone.C(226, 226, 228);
}
