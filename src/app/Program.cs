using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

static class Seg
{
    static readonly string[] Map = { "abcdef", "bc", "abged", "abgcd", "fgbc", "afgcd", "afgedc", "abc", "abcdefg", "abcdfg" };
    public static readonly Point[] Boxes = { new Point(399, 356), new Point(459, 356), new Point(517, 356) };
    public const int BoxW = 43, BoxH = 29;

    static PointF[] Hz(float x1, float x2, float cy, float t)
    { return new[] { new PointF(x1, cy), new PointF(x1 + t / 2, cy - t / 2), new PointF(x2 - t / 2, cy - t / 2), new PointF(x2, cy), new PointF(x2 - t / 2, cy + t / 2), new PointF(x1 + t / 2, cy + t / 2) }; }
    static PointF[] Vt(float cx, float y1, float y2, float t)
    { return new[] { new PointF(cx, y1), new PointF(cx + t / 2, y1 + t / 2), new PointF(cx + t / 2, y2 - t / 2), new PointF(cx, y2), new PointF(cx - t / 2, y2 - t / 2), new PointF(cx - t / 2, y1 + t / 2) }; }
    static PointF[] Poly(char s, float x, float y, float w, float h, float t)
    {
        float g = 1.2f, m = y + h / 2;
        switch (s)
        {
            case 'a': return Hz(x + g, x + w - g, y, t);
            case 'g': return Hz(x + g, x + w - g, m, t);
            case 'd': return Hz(x + g, x + w - g, y + h, t);
            case 'f': return Vt(x, y + g, m - g, t);
            case 'e': return Vt(x, m + g, y + h - g, t);
            case 'b': return Vt(x + w, y + g, m - g, t);
            default: return Vt(x + w, m + g, y + h - g, t);
        }
    }

    public static void DrawText(Graphics gr, string txt, float right, float y, float w, float h, float t, float pitch, bool dim)
    {
        gr.SmoothingMode = SmoothingMode.AntiAlias;
        int a = dim ? 110 : 255;
        using (var glow = new Pen(Tone.C(dim ? 12 : 35, 255, 20, 20), t + 1.5f) { LineJoin = LineJoin.Round })
        using (var on = new SolidBrush(Tone.C(a, 255, 40, 40)))
        using (var off = new SolidBrush(Tone.C(30, 6, 6)))
        {
            float x = right - txt.Length * pitch + (pitch - w);
            foreach (char ch in txt)
            {
                string segs = Map[ch - '0'];
                foreach (char s in "abcdefg")
                {
                    var p = Poly(s, x, y, w, h, t);
                    if (segs.IndexOf(s) >= 0) { gr.DrawPolygon(glow, p); gr.FillPolygon(on, p); } else gr.FillPolygon(off, p);
                }
                x += pitch;
            }
        }
    }

    public static void Blank(Graphics gr)
    {
        using (var bg = new SolidBrush(Tone.C(16, 3, 3)))
            foreach (var b in Boxes) gr.FillRectangle(bg, b.X + 2, b.Y + 2, BoxW - 4, BoxH - 4);
    }

    public static void Draw(Graphics gr, int sec)
    {
        sec = Math.Min(sec, 99 * 3600 + 59 * 60 + 59);
        int[] parts = { sec / 3600, (sec / 60) % 60, sec % 60 };
        gr.SmoothingMode = SmoothingMode.AntiAlias;
        var st = gr.Save();
        using (var off = new SolidBrush(Tone.C(40, 10, 9)))
        using (var glow = new Pen(Tone.C(70, 255, 60, 30), 3.5f) { LineJoin = LineJoin.Round })
        using (var on = new SolidBrush(Tone.C(238, 92, 66)))
        using (var core = new SolidBrush(Tone.C(255, 155, 122)))
        {
            gr.ResetTransform();
            foreach (float cx in new[] { 450f, 509f })
                foreach (float cy in new[] { 365f, 375f }) { gr.FillEllipse(on, cx - 1.8f, cy - 1.8f, 3.6f, 3.6f); }
            for (int i = 0; i < 3; i++)
            {
                var b = Boxes[i];
                string txt = parts[i].ToString("00");
                for (int k = 0; k < 2; k++)
                {
                    string segs = Map[txt[k] - '0'];
                    float x = b.X + 8.5f + k * 15, y = b.Y + 5.5f;
                    // leichte Kursivstellung wie im Design
                    gr.Transform = new Matrix(1, 0, -0.08f, 1, 0.08f * (y + 8.5f), 0);
                    foreach (char s in "abcdefg")
                    {
                        var p = Poly(s, x, y, 9.5f, 17, 3f);
                        if (segs.IndexOf(s) >= 0) { gr.DrawPolygon(glow, p); gr.FillPolygon(on, p); gr.FillPolygon(core, Poly(s, x, y, 9.5f, 17, 1f)); }
                        else gr.FillPolygon(off, p);
                    }
                }
            }
        }
        gr.Restore(st);
    }
}

// Leuchtring um den Power-Knopf: 60 Segmente, eins pro Sekunde, mit verglühender Leuchtspur
static class Ring
{
    public const float CX = 476.1f, CY = 290.2f, R1 = 105f, R2 = 122.9f; // Mitte des Zifferblatts im Original (gleichmäßig skaliert)
    public const int N = 60;
    public static readonly Rectangle Bounds = Rectangle.Round(new RectangleF(CX - R2 - 30, CY - R2 - 30, (R2 + 30) * 2, (R2 + 30) * 2));
    static readonly PointF[][] Polys = Build(R1, R2, 0.62f), Cores = Build(R1 + 4, R2 - 4, 0.36f);
    // Die Digitalanzeige liegt im Design über dem unteren Ring: dort laufen die Segmente "dahinter"
    public static readonly RectangleF Display = new RectangleF(384, 339, 176, 50);
    public static readonly bool[] Hidden = Polys.Select(q => q.Any(p => Display.Contains(p))).ToArray();

    static PointF[][] Build(float r1, float r2, float fill)
    {
        var res = new PointF[N][];
        double step = 2 * Math.PI / N, half = step * fill / 2;
        for (int i = 0; i < N; i++)
        {
            double a = i * step + step / 2, a0 = a - half, a1 = a + half;
            res[i] = new[] { P(r1, a0), P(r2, a0), P(r2, a1), P(r1, a1) };
        }
        return res;
    }
    static PointF P(double r, double a) { return new PointF((float)(CX + r * Math.Sin(a)), (float)(CY - r * Math.Cos(a))); }

    // Einmalig in den Hintergrund-Cache: Ring abdunkeln, damit die gemalten Segmente die Leuchtsegmente des Bildes ersetzen
    public static void BakeBase(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var p = new GraphicsPath())
        {
            p.AddEllipse(CX - R2 - 3, CY - R2 - 3, (R2 + 3) * 2, (R2 + 3) * 2);
            p.AddEllipse(CX - R1 + 3, CY - R1 + 3, (R1 - 3) * 2, (R1 - 3) * 2);
            using (var b = new SolidBrush(Tone.C(225, 14, 3, 4))) g.FillPath(b, p);
        }
        using (var off = new SolidBrush(Tone.C(55, 9, 8))) for (int i = 0; i < N; i++) g.FillPolygon(off, Polys[i]);
    }

    // Anzeige-Tafel aus dem Originalbild: weicher Rand (keine Kante), alte Ziffern nur dort entfernt, wo sie leuchten.
    // Wird jedes Bild über den Ring gelegt, damit die Blöcke "hinter" der Tafel verschwinden.
    public static readonly Rectangle PanelRect = new Rectangle(378, 342, 204, 66);
    public static Bitmap MakePanel(Bitmap original)
    {
        var bmp = original.Clone(PanelRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        int w = bmp.Width, h = bmp.Height; var px = new int[w * h];
        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, px, 0, px.Length);
        foreach (var b in Seg.Boxes)
        {
            int x0 = b.X + 2 - PanelRect.X, y0 = b.Y + 2 - PanelRect.Y, x1 = x0 + Seg.BoxW - 4, y1 = y0 + Seg.BoxH - 4;
            // Farbe der dunklen Platte: Mittel der dunkelsten Pixel im Kasten
            var lum = new System.Collections.Generic.List<int>();
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) lum.Add(px[y * w + x]);
            lum.Sort((p, q) => R(p).CompareTo(R(q)));
            int n = Math.Max(1, lum.Count / 4); long sr = 0, sg = 0, sb = 0;
            for (int i = 0; i < n; i++) { sr += R(lum[i]); sg += G(lum[i]); sb += B(lum[i]); }
            int pr = (int)(sr / n), pg = (int)(sg / n), pb = (int)(sb / n);
            for (int y = y0 - 3; y < y1 + 3; y++)
                for (int x = x0 - 3; x < x1 + 3; x++)
                {
                    float edge = Math.Min(Math.Min(x - (x0 - 3), (x1 + 2) - x), Math.Min(y - (y0 - 3), (y1 + 2) - y)) / 4f;
                    int c = px[y * w + x];
                    float k = Math.Max(0, Math.Min(1, (R(c) - pr) / 50f)) * Math.Max(0, Math.Min(1, edge));
                    px[y * w + x] = Argb(255, Mix(R(c), pr, k), Mix(G(c), pg, k), Mix(B(c), pb, k));
                }
        }
        const float feather = 6f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = Math.Min(Math.Min(x + 0.5f, w - x - 0.5f), Math.Min(y + 0.5f, h - y - 0.5f));
                float a = Math.Max(0, Math.Min(1, d / feather)); a = a * a * (3 - 2 * a);
                int c = px[y * w + x]; px[y * w + x] = Argb((int)(255 * a), R(c), G(c), B(c));
            }
        System.Runtime.InteropServices.Marshal.Copy(px, 0, data.Scan0, px.Length);
        bmp.UnlockBits(data);
        return bmp;
    }
    static int R(int c) { return (c >> 16) & 255; } static int G(int c) { return (c >> 8) & 255; } static int B(int c) { return c & 255; }
    static int Argb(int a, int r, int g, int b) { return (a << 24) | (r << 16) | (g << 8) | b; }
    static int Mix(int a, int b, float k) { return (int)(a + (b - a) * k + 0.5f); }

    // level[i] 0..1 = Helligkeit je Segment, pulse 0..1 = Pulsieren des Leuchtkreises
    public static void Draw(Graphics g, float[] level, float[] flash, float pulse)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        // Leuchtkreis dahinter (pulsierend) – nur wenn der Ring läuft, im Leerlauf bleibt das Bild unverändert
        bool any = false; for (int i = 0; i < N; i++) if (level[i] > 0.015f || (flash != null && flash[i] > 0)) { any = true; break; }
        if (!any) return;
        for (int k = 0; k < 5; k++)
        {
            float r = R2 + 4 + k * 4, a = (14 + 26 * pulse) * (1 - k / 5f);
            using (var pen = new Pen(Tone.C((int)a, 255, 25, 20), 6)) g.DrawEllipse(pen, CX - r, CY - r, r * 2, r * 2);
        }
        for (int k = 0; k < 3; k++)
        {
            float r = R1 - 6 - k * 4, a = (14 + 26 * pulse) * (1 - k / 3f);
            using (var pen = new Pen(Tone.C((int)a, 255, 25, 20), 5)) g.DrawEllipse(pen, CX - r, CY - r, r * 2, r * 2);
        }
        for (int i = 0; i < N; i++)
        {
            float v = Math.Max(0, Math.Min(1, level[i])), f = flash == null ? 0 : Math.Max(0, Math.Min(1, flash[i]));
            if (v < 0.015f && f <= 0) continue;
            var q = Polys[i]; float hx = (q[0].X + q[2].X) / 2, hy = (q[0].Y + q[2].Y) / 2;
            // Glut-Halo: schrumpft beim Abkühlen
            float hr = 6 + 16 * v + 16 * f;
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(hx - hr, hy - hr, hr * 2, hr * 2);
                using (var pg = new PathGradientBrush(path) { CenterColor = Tone.C(Clamp255(120 * v + 130 * f), 255, (int)(40 + 70 * v + 120 * f), 20), SurroundColors = new[] { Tone.C(0, 255, 0, 0) } })
                    g.FillPath(pg, path);
            }
            // Farbe: dunkelrot (kalt) -> orangerot (heiß) -> weißgelb (Blitz)
            float sv = (float)Math.Sqrt(v);
            int r = (int)(55 + 200 * sv), gg = (int)(9 + 80 * v * v), bb = (int)(8 + 20 * v * v);
            r = Lerp(r, 255, f); gg = Lerp(gg, 240, f); bb = Lerp(bb, 190, f);
            PointF[] poly = f > 0 ? Scale(q, hx, hy, 1 + 0.22f * f) : q;
            using (var b = new SolidBrush(Tone.C(r, gg, bb))) g.FillPolygon(b, poly);
            float core = Math.Max(f, (v - 0.45f) / 0.55f);
            if (core > 0) using (var c = new SolidBrush(Tone.C(Clamp255(255 * core), 255, Lerp(170, 250, f), Lerp(110, 220, f)))) g.FillPolygon(c, f > 0 ? Scale(Cores[i], hx, hy, 1 + 0.22f * f) : Cores[i]);
        }
    }
    static int Clamp255(float x) { return Math.Max(0, Math.Min(255, (int)x)); }
    static int Lerp(int a, int b, float t) { return Clamp255(a + (b - a) * t); }
    static PointF[] Scale(PointF[] p, float cx, float cy, float k) { var r = new PointF[p.Length]; for (int i = 0; i < p.Length; i++) r[i] = new PointF(cx + (p[i].X - cx) * k, cy + (p[i].Y - cy) * k); return r; }

    // ---- Glut-Modell: jeder Block hat seine eigene Zeit, wann er aufgeblitzt ist ----
    public const double FlashTime = 0.2;
    public const float Ember = 0.3f;
    public static float Heat(double age, double tau) { return age < 0 ? 0 : (float)Math.Exp(-age / tau); }
    public static float Flash(double age) { return age < 0 || age >= FlashTime ? 0 : (float)(age < 0.03 ? age / 0.03 : 1 - (age - 0.03) / (FlashTime - 0.03)); }
    // Helligkeit aller Blöcke aus ihren Zündzeitpunkten (litAt; NaN = aus), mit leichtem Glut-Flackern
    public static void Embers(double now, double[] litAt, float[] tau, float[] phase, float fade, float[] level, float[] flash)
    {
        for (int i = 0; i < N; i++)
        {
            if (double.IsNaN(litAt[i])) { level[i] = 0; flash[i] = 0; continue; }
            double age = now - litAt[i];
            // kühlt in ~20-30 s auf eine bleibende Glut (~30 %) ab und bleibt an
            float h = Ember + (1 - Ember) * Heat(age, tau[i]);
            float flick = 1 + 0.045f * (float)Math.Sin(now * 7.3 + phase[i]) + 0.025f * (float)Math.Sin(now * 19.7 + phase[i] * 2.3);
            level[i] = Math.Min(1, h * flick) * fade;
            flash[i] = Flash(age) * fade;
        }
    }

}

enum WType { Button, Primary, Toggle, Num, Dropdown }

class Widget
{
    public string Id, Text; public WType Type; public Rectangle R;
    public bool Enabled = true, On, NoSpin;
    public int Value, Min, Max;
    public Rectangle UpR { get { return NoSpin ? Rectangle.Empty : new Rectangle(R.Right - 17, R.Y, 17, R.Height / 2); } }
    public Rectangle DownR { get { return NoSpin ? Rectangle.Empty : new Rectangle(R.Right - 17, R.Y + R.Height / 2, 17, R.Height - R.Height / 2); } }
}

static class Hud
{
    public static readonly Color Neon = Tone.C(255, 40, 40);
    public static Font UiFont(float size)
    {
        var f = new Font("Bahnschrift SemiBold Condensed", size, FontStyle.Regular, GraphicsUnit.Point);
        if (f.Name == "Bahnschrift SemiBold Condensed") return f;
        f.Dispose();
        f = new Font("Bahnschrift", size, FontStyle.Bold, GraphicsUnit.Point);
        if (f.Name == "Bahnschrift") return f;
        f.Dispose();
        return new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Point);
    }

    public static GraphicsPath Round(RectangleF r, float rad)
    {
        var p = new GraphicsPath(); float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure(); return p;
    }

    // Fenster-Knopf (min/close) im Neon-/Chrom-Stil
    public static void WinBtn(Graphics g, Rectangle r, bool close, bool hot, bool down)
    {
        var rr = r; if (down) rr.Offset(1, 1);
        using (var p = Round(rr, 7))
        {
            Color fill = hot && close ? Tone.C(230, 150, 30, 26) : hot ? Tone.C(220, 84, 24, 23) : Tone.C(150, 12, 14, 17);
            using (var b = new SolidBrush(fill)) g.FillPath(b, p);
            using (var pen = new Pen(hot ? RedBorder : Tone.C(170, BtnBorder), 1f)) g.DrawPath(pen, p);
        }
        float cx = rr.X + rr.Width / 2f, cy = rr.Y + rr.Height / 2f, k = 4.5f;
        Color ink = hot ? Color.White : Ink;
        foreach (var w in new[] { 1.6f })
            using (var pen = new Pen(ink, w) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (close) { g.DrawLine(pen, cx - k, cy - k, cx + k, cy + k); g.DrawLine(pen, cx + k, cy - k, cx - k, cy + k); }
                else g.DrawLine(pen, cx - k, cy + 3, cx + k, cy + 3);
            }
    }

    public static void PaintedBtn(Graphics g, Rectangle r, bool hot, bool down, bool disabled)
    {
        if (!hot && !down && !disabled) return;
        var st = g.Save(); g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var p = Round(Rectangle.Inflate(r, -1, -1), 4))
        {
            Color c = disabled ? Tone.C(120, 0, 0, 0) : down ? Tone.C(70, 0, 0, 0) : Tone.C(70, 255, 70, 55);
            using (var b = new SolidBrush(c)) g.FillPath(b, p);
            if (hot) using (var pen = new Pen(Tone.C(160, 255, 90, 80), 1f)) g.DrawPath(pen, p);
        }
        g.Restore(st);
    }

    public static void GearBtn(Graphics g, Rectangle r, bool hot, bool down)
    {
        var rr = r; if (down) rr.Offset(1, 1);
        using (var p = Round(rr, 7))
        {
            using (var b = new SolidBrush(hot ? Tone.C(220, 84, 24, 23) : Tone.C(150, 12, 14, 17))) g.FillPath(b, p);
            using (var pen = new Pen(hot ? RedBorder : Tone.C(170, BtnBorder), 1f)) g.DrawPath(pen, p);
        }
        float cx = rr.X + rr.Width / 2f, cy = rr.Y + rr.Height / 2f;
        Color ink = hot ? Color.White : Ink;
        using (var gp = new GraphicsPath())
        {
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI / 4;
                var c = (float)Math.Cos(a); var s = (float)Math.Sin(a);
                var ox = -s; var oy = c;
                PointF P(float rad, float w) { return new PointF(cx + c * rad + ox * w, cy + s * rad + oy * w); }
                gp.AddPolygon(new[] { P(4.5f, -1.8f), P(8.2f, -1.4f), P(8.2f, 1.4f), P(4.5f, 1.8f) });
            }
            gp.AddEllipse(cx - 5.8f, cy - 5.8f, 11.6f, 11.6f);
            using (var b = new SolidBrush(ink)) g.FillPath(b, gp);
            gp.Dispose();
        }
        using (var hole = new SolidBrush(hot ? Tone.C(84, 24, 23) : Tone.C(14, 16, 19))) g.FillEllipse(hole, cx - 2.6f, cy - 2.6f, 5.2f, 5.2f);
    }

    public static void NeonBorder(Graphics g, GraphicsPath p, int alpha, bool strong)
    {
        float[] widths = strong ? new[] { 9f, 5f, 2.5f, 1.2f } : new[] { 5f, 2.5f, 1.2f };
        int[] alphas = strong ? new[] { 30, 70, 160, 255 } : new[] { 35, 110, 255 };
        for (int i = 0; i < widths.Length; i++)
        {
            Color c = i == widths.Length - 1 ? Tone.C(alpha * alphas[i] / 255, 255, 120, 110) : Tone.C(alpha * alphas[i] / 255, 255, 30, 30);
            using (var pen = new Pen(c, widths[i]) { LineJoin = LineJoin.Round }) g.DrawPath(pen, p);
        }
    }

    public static void Chevron(Graphics g, float cx, float cy, float s, bool up, Color c)
    {
        var pts = up ? new[] { new PointF(cx - s, cy + s / 2), new PointF(cx, cy - s / 2), new PointF(cx + s, cy + s / 2) }
                     : new[] { new PointF(cx - s, cy - s / 2), new PointF(cx, cy + s / 2), new PointF(cx + s, cy - s / 2) };
        using (var pen = new Pen(c, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }) g.DrawLines(pen, pts);
    }

    static void Metal(Graphics g, GraphicsPath p, RectangleF r, bool hover, bool pressed, bool red)
    {
        Color top, bottom;
        if (red) { top = pressed ? Tone.C(120, 0, 0) : hover ? Tone.C(230, 30, 30) : Tone.C(190, 15, 15); bottom = pressed ? Tone.C(160, 10, 10) : Tone.C(90, 0, 0); }
        else { top = pressed ? Tone.C(20, 18, 20) : hover ? Tone.C(78, 62, 66) : Tone.C(58, 52, 56); bottom = pressed ? Tone.C(50, 40, 44) : Tone.C(14, 12, 14); }
        using (var b = new LinearGradientBrush(new RectangleF(r.X, r.Y - 1, r.Width, r.Height + 2), top, bottom, 90f)) g.FillPath(b, p);
        // metallic highlight line
        using (var hl = new Pen(Tone.C(pressed ? 20 : 70, 255, 255, 255), 1f)) g.DrawLine(hl, r.X + 6, r.Y + 1.5f, r.Right - 6, r.Y + 1.5f);
    }

    // Schrift der Bedienleiste wie im Design (Bahnschrift, gut lesbar)
    public static Font BarFont(float size, bool semibold)
    {
        foreach (var n in semibold ? new[] { "Bahnschrift SemiBold", "Bahnschrift" } : new[] { "Bahnschrift", "Segoe UI" })
        {
            var f = new Font(n, size, n == "Bahnschrift" && semibold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
            if (f.Name == n) return f; f.Dispose();
        }
        return new Font("Segoe UI", size, semibold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
    }

    // Farben aus dem Design
    public static readonly Color BtnTop = Tone.C(33, 37, 41), BtnBot = Tone.C(22, 25, 28), BtnBorder = Tone.C(64, 68, 74),
        FieldFill = Tone.C(10, 13, 15), FieldBorder = Tone.C(52, 56, 61), Ink = Tone.C(214, 216, 220),
        RedBorder = Tone.C(228, 88, 72), RedTop = Tone.C(84, 24, 23), RedBot = Tone.C(52, 16, 15);

    static void Glow(Graphics g, RectangleF r, int alpha)
    {
        for (int i = 3; i >= 1; i--)
            using (var p = Round(RectangleF.Inflate(r, i, i), 4 + i))
            using (var pen = new Pen(Tone.C(alpha * (4 - i) * 22 / 255, 255, 50, 35), 1.6f)) g.DrawPath(pen, p);
    }

    static void Box(Graphics g, RectangleF r, Color top, Color bot, Color border, int alpha)
    {
        using (var p = Round(r, 4))
        {
            using (var b = new LinearGradientBrush(new RectangleF(r.X, r.Y - 1, r.Width, r.Height + 2), Tone.C(alpha, top), Tone.C(alpha, bot), 90f)) g.FillPath(b, p);
            using (var pen = new Pen(Tone.C(alpha, border), 1f)) g.DrawPath(pen, p);
        }
    }
    static Color Lift(Color c, int d) { return Tone.C(Math.Min(255, c.R + d), Math.Min(255, c.G + d), Math.Min(255, c.B + d)); }

    public static void DrawWidget(Graphics g, Widget w, bool hover, bool pressed, bool focused, Font font)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        var r = new RectangleF(w.R.X + 0.5f, w.R.Y + 0.5f, w.R.Width - 1, w.R.Height - 1);
        int alpha = w.Enabled ? 255 : 110;
        if (!w.Enabled) { hover = false; pressed = false; }
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        var ink = Tone.C(w.Enabled ? 255 : 120, Ink);
        bool red = w.Type == WType.Primary || w.On;
        switch (w.Type)
        {
            case WType.Primary:
                {
                    Glow(g, r, alpha);
                    Box(g, r, pressed ? Tone.C(105, 30, 29) : hover ? Tone.C(158, 46, 43) : Tone.C(140, 40, 38), pressed ? Tone.C(120, 34, 33) : Tone.C(100, 28, 27), RedBorder, alpha);
                    var tr = pressed ? new RectangleF(r.X, r.Y + 1, r.Width, r.Height) : r;
                    using (var br = new SolidBrush(Tone.C(alpha, 255, 255, 255)))
                    {
                        float tw = g.MeasureString(w.Text, font).Width, tx = tr.X + tr.Width / 2 - tw / 2 + 9, cy = tr.Y + tr.Height / 2;
                        g.FillPolygon(br, new[] { new PointF(tx - 18, cy - 6), new PointF(tx - 18, cy + 6), new PointF(tx - 8, cy) });
                        g.DrawString(w.Text, font, br, new RectangleF(tx - 4, tr.Y, tw + 8, tr.Height), sf);
                    }
                    break;
                }
            case WType.Button:
            case WType.Toggle:
                {
                    bool led = w.Type == WType.Toggle && w.Id == "in";
                    if (red)
                    {
                        Glow(g, r, alpha);
                        if (led) Box(g, r, hover ? Tone.C(178, 50, 48) : Tone.C(162, 45, 44), Tone.C(118, 32, 31), Tone.C(236, 96, 86), alpha);
                        else Box(g, r, hover ? Lift(RedTop, 14) : RedTop, RedBot, RedBorder, alpha);
                    }
                    else Box(g, r, pressed ? BtnBot : hover ? Lift(BtnTop, 10) : BtnTop, pressed ? BtnTop : BtnBot, hover ? Lift(BtnBorder, 50) : BtnBorder, alpha);
                    var tr = pressed ? new RectangleF(r.X, r.Y + 1, r.Width, r.Height) : r;
                    if (led)
                    {
                        using (var lb = new SolidBrush(w.On ? Tone.C(alpha, 252, 104, 94) : Tone.C(alpha, 90, 36, 34))) g.FillEllipse(lb, r.X + 9, r.Y + r.Height / 2 - 3, 6, 6);
                        tr = new RectangleF(r.X + 10, tr.Y, r.Width - 10, tr.Height);
                    }
                    using (var br = new SolidBrush(red ? Tone.C(alpha, 255, 240, 236) : ink)) g.DrawString(w.Text, font, br, tr, sf);
                    break;
                }
            case WType.Num:
                {
                    Box(g, r, FieldFill, FieldFill, focused ? RedBorder : hover ? Lift(FieldBorder, 40) : FieldBorder, 255);
                    if (!w.NoSpin)
                    {
                        using (var sep = new Pen(Tone.C(alpha, FieldBorder), 1f)) g.DrawLine(sep, w.UpR.X + 0.5f, r.Y + 1, w.UpR.X + 0.5f, r.Bottom - 1);
                        Chevron(g, w.UpR.X + 8.5f, r.Y + r.Height * 0.32f, 2.6f, true, ink);
                        Chevron(g, w.UpR.X + 8.5f, r.Y + r.Height * 0.70f, 2.6f, false, ink);
                    }
                    if (w.Text != "custom")
                    {
                        var tr = new RectangleF(r.X, r.Y, (w.NoSpin ? r.Width : w.UpR.X - r.X), r.Height);
                        using (var br = new SolidBrush(focused ? Tone.C(255, 255, 120, 100) : ink)) g.DrawString(w.Value.ToString(w.Max >= 100 ? "0" : "00"), font, br, tr, sf);
                    }
                    break;
                }
            case WType.Dropdown:
                {
                    Box(g, r, FieldFill, FieldFill, focused ? RedBorder : hover ? Lift(FieldBorder, 40) : FieldBorder, 255);
                    using (var br = new SolidBrush(ink))
                    using (var lsf = new StringFormat { LineAlignment = StringAlignment.Center })
                        g.DrawString(w.Text, font, br, new RectangleF(r.X + 10, r.Y, r.Width - 36, r.Height), lsf);
                    Chevron(g, r.Right - 21, r.Y + r.Height / 2f, 4.5f, focused, ink);
                    break;
                }
        }
    }

    public static void DrawList(Graphics g, Rectangle r, string[] items, int selected, int hover, Font font)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var p = Round(r, 6))
        {
            using (var b = new SolidBrush(Tone.C(250, 12, 15, 18))) g.FillPath(b, p);
            using (var pen = new Pen(RedBorder, 1f)) g.DrawPath(pen, p);
        }
        int ih = (r.Height - 6) / items.Length;
        using (var lsf = new StringFormat { LineAlignment = StringAlignment.Center })
            for (int i = 0; i < items.Length; i++)
            {
                var ir = new Rectangle(r.X + 4, r.Y + 3 + i * ih, r.Width - 8, ih);
                if (i == hover) using (var hb = new LinearGradientBrush(ir, RedTop, RedBot, 90f)) g.FillRectangle(hb, ir);
                using (var br = new SolidBrush(i == selected ? Tone.C(255, 110, 95) : Ink))
                    g.DrawString((i == selected ? "▸ " : "   ") + items[i], font, br, new RectangleF(ir.X + 6, ir.Y, ir.Width - 6, ir.Height), lsf);
            }
    }
}

class MainForm : Form
{
    // ---- layout (960x640: Bild 960x540 + Bedienleiste darunter) ----
    // 3.2: alle Rechtecke kommen aus Sprites.cs (gemessen im Original-Design, gleichmäßig skaliert)
    static readonly Rectangle HudRect = new Rectangle(117, 540, 719, 85);
    static readonly RectangleF StatusRect = new RectangleF(117, 520, 719, 23);
    static readonly Rectangle TimeField = Rectangle.Round(Sprites.R_time);
    const string DefaultStatus = "BEREIT – Zeit und Aktion wählen, dann START";
    static readonly Rectangle CountRect = Ring.Bounds;
    static readonly string[] ActionNames = Enum.GetValues(typeof(PowerAction)).Cast<PowerAction>().Select(Core.ActionName).ToArray();

    Widget wIn, wNum, wUnit, wAt, wHour, wMin, wAction, wStart, wCancel;
    Widget[] quick, all;
    Widget hover, pressed, focusedNum;
    bool listOpen; int listHover = -1;
    Rectangle ListRect { get { return new Rectangle(wAction.R.X, wAction.R.Y - 4 - ActionNames.Length * 26 - 6, wAction.R.Width, ActionNames.Length * 26 + 6); } }
    Font uiFont = Art.Font(11.6f, false), statusFont = Art.Font(14f, false), startFont = Art.Font(13f, true);
    int actionIndex;

    NotifyIcon tray = new NotifyIcon();
    Timer tick = new Timer();
    bool running, startHidden, exiting;
    int shown;
    string status = DefaultStatus;
    DateTime target; PowerAction act;
    static readonly string StateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Herunterfahr-Timer", "geplant.txt");

    // 3.6: gemalte Fensterknöpfe im Design (oben rechts) sind die echten Klickflächen
    static readonly Rectangle MinBtn = new Rectangle(884, 8, 22, 22), MaxBtn = new Rectangle(908, 8, 21, 22), CloseBtn = new Rectangle(930, 8, 23, 22);
    string winHot, winPressed;
    static readonly Rectangle GearBtn = new Rectangle(858, 8, 22, 22);
    string WinHit(Point p) { return GearBtn.Contains(p) ? "gear" : MinBtn.Contains(p) ? "min" : CloseBtn.Contains(p) ? "close" : null; }
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool ReleaseCapture();
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.Style |= 0x00020000 | 0x00080000 | 0x02000000; // WS_MINIMIZEBOX | WS_SYSMENU | WS_CLIPCHILDREN (Einstellungs-Panel)
            cp.ClassStyle |= 0x00020000;          // CS_DROPSHADOW
            return cp;
        }
    }

    ToolStripMenuItem miAutoMin;
    Bitmap bgCache, farCache;
    // ---- 3.15 Parallax: nur die ferne Ebene (Himmel + Skyline, far.png mit weicher Maske) verschiebt sich um wenige Pixel ----
    // Titel, Ring, Katze, Boden und Bedienleiste bleiben fest. Maus -> Zielversatz, weich nachgeführt; ohne Maus langsame Eigenbewegung.
    const float ParMaxX = 3f, ParMaxY = 1.3f;  // 3.16: ein Drittel weniger   // logische Pixel
    float parX, parY, parTX, parTY; readonly Timer parTimer = new Timer(); readonly System.Diagnostics.Stopwatch parClock = System.Diagnostics.Stopwatch.StartNew();
    static Image farImg; static bool farTried;
    void EnsureFar()
    {
        if (!farTried) { farTried = true; farImg = Art.Load("far.png"); }
        if (farImg == null) return;
        int w = Math.Max(1, ClientSize.Width), h = Math.Max(1, (int)Math.Ceiling(farImg.Height * Art.LogicalW / farImg.Width * Scale));
        if (farCache != null && farCache.Width == w && farCache.Height == h) return;
        if (farCache != null) farCache.Dispose();
        farCache = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(farCache))
        {
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (var ia = new System.Drawing.Imaging.ImageAttributes()) { ia.SetWrapMode(WrapMode.TileFlipXY); g.DrawImage(farImg, new Rectangle(0, 0, w, h), 0, 0, farImg.Width, farImg.Height, GraphicsUnit.Pixel, ia); }
        }
    }
    void ParStep()
    {
        bool inside = Visible && ClientRectangle.Contains(PointToClient(Cursor.Position));
        if (inside)
        {
            var p = PointToClient(Cursor.Position);
            parTX = -((p.X / (float)Math.Max(1, ClientSize.Width)) * 2 - 1) * ParMaxX;
            parTY = -((p.Y / (float)Math.Max(1, ClientSize.Height)) * 2 - 1) * ParMaxY;
        }
        else
        {
            double t = parClock.Elapsed.TotalSeconds;
            parTX = (float)(Math.Sin(t * 0.31) * ParMaxX * 0.35); parTY = (float)(Math.Sin(t * 0.23 + 1.3) * ParMaxY * 0.35);
        }
        float nx = parX + (parTX - parX) * 0.08f, ny = parY + (parTY - parY) * 0.08f;
        if (Math.Abs(nx - parX) * Scale < 0.04f && Math.Abs(ny - parY) * Scale < 0.04f) return; // ruhig -> nichts neu zeichnen
        parX = nx; parY = ny;
        if (farCache != null) Invalidate(new Rectangle(0, 0, ClientSize.Width, farCache.Height + 8));
    }
    // Hintergrund einmal in echter Bildschirmauflösung (DPI) vorbereiten: Original-Design, gleichmäßig skaliert
    void EnsureBg()
    {
        if (bgCache == null || bgCache.Size != ClientSize)
        {
            if (bgCache != null) bgCache.Dispose();
            bgCache = new Bitmap(Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height), System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bgCache))
            {
                g.Clear(Color.Black);
                g.ScaleTransform(Scale, Scale);
                Art.DrawBackground(g);
            }
        }
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        EnsureBg();
        e.Graphics.CompositingMode = CompositingMode.SourceCopy;
        e.Graphics.DrawImageUnscaled(bgCache, 0, 0);
        e.Graphics.CompositingMode = CompositingMode.SourceOver;
        EnsureFar();
        if (farCache != null && e.ClipRectangle.Top < farCache.Height + 8)
        {
            var g = e.Graphics; var im = g.InterpolationMode; var po = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.Bilinear; g.PixelOffsetMode = PixelOffsetMode.Half; g.CompositingQuality = CompositingQuality.HighSpeed;
            g.DrawImage(farCache, new RectangleF(parX * Scale, parY * Scale, farCache.Width, farCache.Height));
            g.InterpolationMode = im; g.PixelOffsetMode = po;
        }
    }

    // ---- DPI: echtes Per-Monitor-DPI, alles gleichmäßig skaliert (logisch 960x639) ----
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr h);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    new float Scale = 1f;
    float QueryScale()
    {
        try { uint d = GetDpiForWindow(Handle); if (d >= 48) return d / 96f; } catch { }
        try { using (var g = CreateGraphics()) return g.DpiX / 96f; } catch { return 1f; }
    }
    void ApplyScale(float k, bool resize)
    {
        Scale = k;
        if (resize) ClientSize = new Size((int)Math.Round(Art.LogicalW * k), (int)Math.Round(Art.LogicalH * k));
        if (bgCache != null) { bgCache.Dispose(); bgCache = null; }
        if (farCache != null) { farCache.Dispose(); farCache = null; }
        if (settingsPanel != null) settingsPanel.ApplyScale(k, new Point((int)Math.Round(SetX * k), (int)Math.Round(SetY * k)));
        if (updatePanel != null) updatePanel.ApplyScale(k, new Point((int)Math.Round(UpdX * k), (int)Math.Round(UpdY * k)));
        Invalidate();
    }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); ApplyScale(QueryScale(), true); }
    Rectangle Phys(Rectangle r) { return Rectangle.FromLTRB((int)Math.Floor(r.Left * Scale) - 2, (int)Math.Floor(r.Top * Scale) - 2, (int)Math.Ceiling(r.Right * Scale) + 2, (int)Math.Ceiling(r.Bottom * Scale) + 2); }
    void Inv(Rectangle r) { var q = r; q.Inflate(28, 28); Invalidate(Phys(q)); }
    MouseEventArgs L(MouseEventArgs e) { return new MouseEventArgs(e.Button, e.Clicks, (int)Math.Floor(e.X / Scale), (int)Math.Floor(e.Y / Scale), e.Delta); }

    protected override void SetVisibleCore(bool v) { if (startHidden) { startHidden = false; v = false; if (!IsHandleCreated) CreateHandle(); } base.SetVisibleCore(v); }

    static Widget WS(string id, WType t, RectangleF r, string text) { return new Widget { Id = id, Type = t, R = Rectangle.Round(r), Text = text }; }
    static Widget W(string id, WType t, int x, int y, int w, string text) { return new Widget { Id = id, Type = t, R = new Rectangle(x, y, w, 28), Text = text }; }

    public MainForm()
    {
        Text = "Terminator – HASTA LA VISTA, WINDOWS";
        try { using (var st = typeof(MainForm).Assembly.GetManifestResourceStream("app.ico")) Icon = new Icon(st); } catch { }
        AutoScaleMode = AutoScaleMode.None; KeyPreview = true;
        FormBorderStyle = FormBorderStyle.None; MaximizeBox = false; MinimizeBox = true; StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size((int)Art.LogicalW, Art.LogicalH); BackColor = Color.Black; DoubleBuffered = true;

        // Bedienleiste = Bild aus dem Design; Zustände kommen als Ausschnitte aus demselben Bild
        wIn = WS("in", WType.Toggle, Sprites.R_in, "IN"); wIn.On = true;
        wNum = WS("num", WType.Num, Sprites.R_num, ""); wNum.Value = 30; wNum.Min = 1; wNum.Max = 999;
        wUnit = WS("unit", WType.Button, Sprites.R_unit, "MIN");
        quick = new[] { WS("q15", WType.Button, Sprites.R_q15, "15 MIN"), WS("q30", WType.Button, Sprites.R_q30, "30 MIN"),
                        WS("q60", WType.Button, Sprites.R_q60, "1 STD"), WS("q120", WType.Button, Sprites.R_q120, "2 STD") };
        wAt = WS("at", WType.Toggle, Sprites.R_at, "UM");
        var tf = Sprites.R_time; float colonX = tf.X + tf.Width * 0.34f;
        wHour = WS("hh", WType.Num, new RectangleF(tf.X, tf.Y, colonX - tf.X, tf.Height), ""); wHour.Value = 23; wHour.Min = 0; wHour.Max = 23; wHour.NoSpin = true;
        wMin = WS("mm", WType.Num, new RectangleF(colonX, tf.Y, tf.Right - colonX, tf.Height), ""); wMin.Value = 0; wMin.Min = 0; wMin.Max = 59;
        wAction = WS("action", WType.Dropdown, Sprites.R_act, ActionNames[0]);
        wStart = WS("start", WType.Primary, Sprites.R_start, "START");
        wCancel = WS("cancel", WType.Button, Sprites.R_cancel, "ABBRECHEN"); wCancel.Enabled = false;
        wFinale = new Widget { Id = "finale", Type = WType.Toggle, R = Rectangle.Empty, Text = "FINALE" }; wFinale.On = finaleOn; // nur im Zahnrad
        all = new[] { wIn, wNum, wUnit }.Concat(quick).Concat(new[] { wAt, wHour, wMin, wAction, wStart, wCancel, wFinale }).ToArray();

        Paint += OnPaintDesign;
        MouseMove += OnMove; MouseDown += OnDown; MouseUp += OnUp; MouseWheel += OnWheel;
        MouseLeave += (s, e) => { if (mouseIn && !ClientRectangle.Contains(PointToClient(Cursor.Position))) { mouseIn = false; InvalidateWin(); } if (winHot != null) { winHot = null; InvalidateWin(); } if (hover != null) { hover = null; Cursor = Cursors.Default; InvalidateHud(); } };
        KeyDown += OnKey;

        try { tray.Icon = Icon != null ? new Icon(Icon, SystemInformation.SmallIconSize) : SystemIcons.Application; } catch { tray.Icon = Icon ?? SystemIcons.Application; } tray.Text = "Terminator"; tray.Visible = true;
        tray.DoubleClick += (s, e) => ShowWindow();
        var menu = new ContextMenuStrip();
        menu.Items.Add("Fenster öffnen", null, (s, e) => ShowWindow());
        menu.Items.Add("Abbrechen", null, (s, e) => DoCancel());
        miFinale = new ToolStripMenuItem("Finale-Effekt (letzte Minute)") { Checked = finaleOn, CheckOnClick = false };
        miFinale.Click += (s, e) => SetFinale(!finaleOn);
        menu.Items.Add(miFinale);
        // 3.11: Fenster nach START automatisch in den Infobereich minimieren (3.15: Standard AUS, auch im Zahnrad, wird gespeichert)
        miAutoMin = new ToolStripMenuItem("Nach Start automatisch minimieren") { Checked = settings.AutoMinimize, CheckOnClick = true };
        miAutoMin.CheckedChanged += (s, e) => { if (settings.AutoMinimize == miAutoMin.Checked) return; settings.AutoMinimize = miAutoMin.Checked; SaveSettings(); if (settingsPanel != null) settingsPanel.LoadFrom(); };
        menu.Items.Add(miAutoMin);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (s, e) => DoExit());
        tray.ContextMenuStrip = menu;

        tick.Interval = 250; tick.Tick += (s, e) => OnTick();
        InitEmbers();
        anim.Interval = 20; anim.Tick += (s, e) => Inv(CountRect);
        parTimer.Interval = 33; parTimer.Tick += (s, e) => ParStep();
        VisibleChanged += (s, e) => UpdateAnim();
        FormClosing += OnClosing;
        Resize += (s, e) => { if (WindowState == FormWindowState.Minimized && running) Hide(); UpdateAnim(); };
        settingsPanel = new SettingsPanel(settings, OnSettingsChanged, () => StartUpdateCheck(true)) { Location = new Point(SetX, SetY) };
        settingsPanel.VisibleChanged += (s, e) => InvalidateWin();
        Controls.Add(settingsPanel);
        settingsPanel.ApplyScale(Scale, new Point((int)Math.Round(SetX * Scale), (int)Math.Round(SetY * Scale)));
        InitUpdater();
        RestoreState();
    }

    // ---------- painting ----------
    void OnPaintDesign(object sender, PaintEventArgs e)
    {
        DrawScene(e.Graphics, e.ClipRectangle);
    }
    void DrawScene(Graphics g, Rectangle clipRect)
    {
        var e = new { ClipRectangle = clipRect };
        g.ScaleTransform(Scale, Scale);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var clip = Rectangle.FromLTRB((int)(e.ClipRectangle.Left / Scale) - 1, (int)(e.ClipRectangle.Top / Scale) - 1, (int)(e.ClipRectangle.Right / Scale) + 2, (int)(e.ClipRectangle.Bottom / Scale) + 2);
        if (clip.IntersectsWith(Ring.Bounds))
        {
            float pulse;
            AnimLevels(out pulse);
            Ring.Draw(g, levels, flashes, pulse);   // im Leerlauf nichts: der Ring sieht aus wie im Bild
            Art.DrawDigits(g, running ? shown : IdleSeconds());
        }
        DrawHud(g);
        if (listOpen) Hud.DrawList(g, ListRect, ActionNames, actionIndex, listHover, uiFont);
        // gemalte Knöpfe: nur Hover/Druck-Schimmer; Maximieren ist bewusst gesperrt (feste Größe/Seitenverhältnis)
        Hud.PaintedBtn(g, MinBtn, winHot == "min", winPressed == "min" && winHot == "min", false);
        Hud.PaintedBtn(g, MaxBtn, false, false, true);
        Hud.PaintedBtn(g, CloseBtn, winHot == "close", winPressed == "close" && winHot == "close", false);
        if (mouseIn || settingsPanel.Visible || winHot != null)
            Hud.GearBtn(g, GearBtn, winHot == "gear" || settingsPanel.Visible, winPressed == "gear" && winHot == "gear");
    }
    bool mouseIn;

    // Bedienleiste: das Bild ist bereits der Grundzustand. Andere Zustände = Ausschnitte aus dem Design,
    // geänderte Werte = sauberer Feld-Ausschnitt + Text in Barlow (eingebettet).
    void Btn(Graphics g, Widget w, string id, bool on, float cx)
    {
        Art.Draw(g, id + (on ? "_bg_on" : "_bg_off"));
        Art.Text(g, w.Text, TextPx, on ? TxtRed : TxtGrey, cx > 0 ? cx : w.R.X + w.R.Width / 2, Row1Y, 0);
    }
    // 3.3: Alle Texte der Leiste werden IMMER live mit derselben Schrift (Barlow Semi Condensed Medium)
    // auf textfreie Hintergründe aus dem Design gezeichnet – Leerlauf und geänderte Werte sehen gleich aus.
    const float TextPx = 11.5f, StartPx = 15.2f, StatusPx = 13.0f, Row1Y = 561.2f, Row2Y = 596.8f;
    static readonly Color TxtGrey = Tone.C(225, 226, 229), TxtRed = Tone.C(252, 244, 240), TxtWhite = Tone.C(253, 254, 254);
    void DrawHud(Graphics g)
    {
        foreach (var q in quick) q.On = wIn.On && wUnit.Text == "MIN" && wNum.Value == int.Parse(q.Id.Substring(1));
        Btn(g, wIn, "in", wIn.On, 151.6f);
        foreach (var q in quick) Btn(g, q, q.Id, q.On, -1);
        Btn(g, wAt, "at", wAt.On, -1);
        Btn(g, wUnit, "unit", false, -1);
        var hi = Tone.C(255, 128, 110);
        // Zahl
        Art.Draw(g, "num_clean");
        Art.Text(g, wNum.Value.ToString(), TextPx, focusedNum == wNum ? hi : TxtWhite, 208.0f, Row1Y, 0);
        // Uhrzeit
        Art.Draw(g, "time_clean");
        {
            float cx = 683.9f, cw = Art.TextWidth(":", TextPx);
            Art.Text(g, wHour.Value.ToString("00"), TextPx, focusedNum == wHour ? hi : TxtWhite, cx - cw / 2 - 0.3f, Row1Y, 1);
            Art.Text(g, ":", TextPx, TxtWhite, cx, Row1Y - 0.3f, 0);
            Art.Text(g, wMin.Value.ToString("00"), TextPx, focusedNum == wMin ? hi : TxtWhite, cx + cw / 2 + 0.3f, Row1Y, -1);
        }
        // Aktion (Pfeil bleibt aus dem Design)
        Art.Draw(g, "act_clean");
        Art.Text(g, wAction.Text, TextPx, TxtWhite, 134.9f, Row2Y, -1);
        // START mit Dreieck
        Art.Draw(g, "start_clean");
        {
            float tw = Art.TextWidth(wStart.Text, StartPx), tri = 7.8f, gap = 14f, x0 = 484.5f - (tri + gap + tw) / 2;
            var st = g.Save(); g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(TxtWhite)) g.FillPolygon(b, new[] { new PointF(x0, Row2Y - 5f), new PointF(x0 + tri, Row2Y), new PointF(x0, Row2Y + 5f) });
            g.Restore(st);
            Art.Text(g, wStart.Text, StartPx, TxtWhite, x0 + tri + gap, Row2Y, -1);
        }
        // ABBRECHEN
        Art.Draw(g, "cancel_clean");
        Art.Text(g, wCancel.Text, TextPx, TxtGrey, 711.7f, Row2Y, 0);
        // Statuszeile zweifarbig
        Art.Draw(g, "status_clean");
        {
            int cut = status.IndexOf(" – "); string head = cut > 0 ? status.Substring(0, cut) : status, tail = cut > 0 ? status.Substring(cut) : "";
            float w1 = Art.TextWidth(head, StatusPx), w2 = tail.Length > 0 ? Art.TextWidth(tail, StatusPx) : 0, x = 475.0f - (w1 + w2) / 2;
            Art.Text(g, head, StatusPx, Art.StatusRed, x, 530.9f, -1);
            if (tail.Length > 0) Art.Text(g, tail, StatusPx, Art.StatusGrey, x + w1, 530.9f, -1);
        }
        // Rahmen/Overlays für Hover, Fokus, gesperrt
        foreach (var w in all)
        {
            if (w == wFinale || w == wHour || w == wMin) continue;
            Overlay(g, w.R, w.Enabled ? (w == pressed && w == hover ? 2 : w == hover ? 1 : 0) : (w == wCancel ? 0 : 3), w == focusedNum || (w == wAction && listOpen));
        }
        Overlay(g, TimeField, !wHour.Enabled ? 3 : (hover == wHour || hover == wMin) ? 1 : 0, focusedNum == wHour || focusedNum == wMin);
        if (wCancel.Enabled) RedFrame(g, wCancel.R, true);
    }
    static void Overlay(Graphics g, Rectangle r, int mode, bool focus)
    {
        if (mode == 0 && !focus) return;
        var rf = new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1);
        using (var p = Hud.Round(rf, 3.5f))
        {
            if (mode == 1) using (var b = new SolidBrush(Tone.C(22, 255, 255, 255))) g.FillPath(b, p);
            if (mode == 2) using (var b = new SolidBrush(Tone.C(60, 0, 0, 0))) g.FillPath(b, p);
            if (mode == 3) using (var b = new SolidBrush(Tone.C(125, 8, 9, 11))) g.FillPath(b, p);
        }
        if (focus) RedFrame(g, r, false);
    }
    static void RedFrame(Graphics g, Rectangle r, bool glow)
    {
        var rf = new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1);
        if (glow) for (int i = 3; i >= 1; i--) using (var p = Hud.Round(RectangleF.Inflate(rf, i, i), 3.5f + i)) using (var pen = new Pen(Tone.C((4 - i) * 22, 255, 50, 35), 1.6f)) g.DrawPath(pen, p);
        using (var p = Hud.Round(rf, 3.5f)) using (var pen = new Pen(Hud.RedBorder, 1.2f)) g.DrawPath(pen, p);
    }

    void InvalidateWin() { Inv(Rectangle.Inflate(Rectangle.Union(GearBtn, CloseBtn), 10, 10)); }

    void InvalidateHud() { Inv(new Rectangle(HudRect.X - 8, HudRect.Y - 24, HudRect.Width + 16, HudRect.Height + 30)); if (listOpen) Inv(Rectangle.Inflate(ListRect, 8, 8)); }
    void RedrawCountdown() { Inv(CountRect); }

    // ---------- Animation (~40 fps, nur Ring + Anzeige werden neu gezeichnet) ----------
    readonly float[] levels = new float[Ring.N];
    readonly Stopwatch clock = Stopwatch.StartNew();
    Timer anim = new Timer();
    readonly float[] flashes = new float[Ring.N], tau = new float[Ring.N], phase = new float[Ring.N];
    readonly double[] litAt = Enumerable.Repeat(double.NaN, Ring.N).ToArray();
    int lastHead = -1; double fadeStart = double.NaN;
    void InitEmbers() { var rnd = new Random(); for (int i = 0; i < Ring.N; i++) { tau[i] = 6.0f + (float)rnd.NextDouble() * 2.0f; phase[i] = (float)(rnd.NextDouble() * 6.283); } }
    void AnimLevels(out float pulse)
    {
        double t = clock.Elapsed.TotalSeconds;
        pulse = (float)(0.5 + 0.5 * Math.Sin(t * 2 * Math.PI / 2.4));
        float fade = 1f;
        if (running)
        {
            fadeStart = double.NaN;
            int cur = Core.SecondsUntil(DateTime.Now, target); if (cur < shown) shown = cur; // Anzeige und Ring exakt im Sekundentakt
            // jeder Block deterministisch aus Start- und Zielzeit (auch nach Tray/Neustart sofort korrekt)
            double rem = Math.Max(0, (target - DateTime.Now).TotalSeconds);
            for (int i = 0; i < Ring.N; i++) { double age = Core.LitAge(i, Ring.N, startShown, rem); litAt[i] = double.IsNaN(age) ? double.NaN : t - age; }
        }
        else
        {
            lastHead = -1;
            if (!double.IsNaN(fadeStart)) { fade = (float)Math.Max(0, 1 - (t - fadeStart) / 0.35); if (fade <= 0) { for (int i = 0; i < Ring.N; i++) litAt[i] = double.NaN; fadeStart = double.NaN; } }
            else for (int i = 0; i < Ring.N; i++) litAt[i] = double.NaN; // Leerlauf: Ring dunkel
        }
        Ring.Embers(t, litAt, tau, phase, fade, levels, flashes);
    }
    int startShown; DateTime startTime = DateTime.MinValue;
    int IdleSeconds()
    {
        try
        {
            DateTime now = DateTime.Now;
            var tg = wIn.On ? Core.TargetFromMinutes(now, Math.Max(wNum.Min, Math.Min(wNum.Max, wNum.Value)) * (wUnit.Text == "STD" ? 60 : 1))
                            : Core.TargetFromClock(now, wHour.Value, wMin.Value);
            return Core.SecondsUntil(now, tg);
        }
        catch { return 0; }
    }
    void UpdateAnim() { bool on = Visible && WindowState != FormWindowState.Minimized; if (on != anim.Enabled) anim.Enabled = on; if (on != parTimer.Enabled) parTimer.Enabled = on; }

    // ---------- input ----------
    Widget Hit(Point p) { return all.FirstOrDefault(w => w.Enabled && w.R.Contains(p)); }

    void OnMove(object s, MouseEventArgs e)
    {
        e = L(e);
        if (!mouseIn) { mouseIn = true; InvalidateWin(); }
        if (listOpen)
        {
            int lh = ListRect.Contains(e.Location) ? (e.Y - ListRect.Y - 3) / 26 : -1;
            if (lh >= ActionNames.Length) lh = -1;
            if (lh != listHover) { listHover = lh; Inv(Rectangle.Inflate(ListRect, 8, 8)); }
        }
        var h = Hit(e.Location);
        var wh = WinHit(e.Location);
        if (wh != winHot) { winHot = wh; InvalidateWin(); }
        if (h != hover) { hover = h; InvalidateHud(); }
        Cursor = h != null || wh != null || listHover >= 0 ? Cursors.Hand : Cursors.Default;
    }

    void OnDown(object s, MouseEventArgs e)
    {
        e = L(e);
        if (e.Button != MouseButtons.Left) return;
        if (listOpen)
        {
            if (ListRect.Contains(e.Location) && listHover >= 0) { actionIndex = listHover; wAction.Text = ActionNames[actionIndex]; }
            CloseList(); return;
        }
        winPressed = WinHit(e.Location);
        if (winPressed != null) { InvalidateWin(); return; }
        var focusedNum0 = focusedNum;
        pressed = Hit(e.Location);
        if (pressed == null && !HudRect.Contains(e.Location)) { focusedNum = null; InvalidateHud(); ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); return; }
        if (pressed != null && pressed.Type != WType.Num) focusedNum = null;
        if (pressed == null) focusedNum = null;
        if (pressed != null && pressed.Type == WType.Num)
        {
            focusedNum = pressed; typed = false;
            var tgt = pressed;
            if (pressed == wMin && (pressed.UpR.Contains(e.Location) || pressed.DownR.Contains(e.Location)) && focusedNum0 == wHour) { tgt = wHour; focusedNum = wHour; }
            if (pressed.UpR.Contains(e.Location)) Step(tgt, +1);
            else if (pressed.DownR.Contains(e.Location)) Step(tgt, -1);
        }
        InvalidateHud();
    }

    void OnUp(object s, MouseEventArgs e)
    {
        e = L(e);
        var wp = winPressed; winPressed = null;
        if (wp != null) { InvalidateWin(); if (wp == WinHit(e.Location)) { if (wp == "gear") ToggleSettings(); else if (wp == "min") WindowState = FormWindowState.Minimized; else Close(); } return; }
        var p = pressed; pressed = null;
        if (p != null && p == Hit(e.Location)) ActivateWidget(p);
        InvalidateHud();
    }

    void OnWheel(object s, MouseEventArgs e)
    {
        e = L(e);
        var h = Hit(e.Location);
        if (h != null && h.Type == WType.Num) { focusedNum = h; Step(h, e.Delta > 0 ? 1 : -1); }
    }

    bool typed;
    void OnKey(object s, KeyEventArgs e)
    {
        if (updatePanel != null && updatePanel.Visible) { if (e.KeyCode == Keys.Escape) { OnUpdateLater(); e.Handled = true; } return; } // 3.17
        if (settingsPanel.Visible) { if (e.KeyCode == Keys.Escape) { settingsPanel.Visible = false; InvalidateWin(); e.Handled = true; } return; }
        if (listOpen && e.KeyCode == Keys.Escape) { CloseList(); return; }
        if (e.KeyCode == Keys.Escape && focusedNum == null) { Close(); return; }
        if (focusedNum == null || !focusedNum.Enabled) { if (e.KeyCode == Keys.Enter && wStart.Enabled) DoStart(); return; }
        var w = focusedNum;
        int d = -1;
        if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) d = e.KeyCode - Keys.D0;
        if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9) d = e.KeyCode - Keys.NumPad0;
        if (d >= 0)
        {
            int v = typed ? w.Value * 10 + d : d;
            if (v > w.Max) v = d;
            w.Value = v; typed = true; Touched(w); InvalidateHud();
        }
        else if (e.KeyCode == Keys.Back) { w.Value /= 10; typed = true; InvalidateHud(); }
        else if (e.KeyCode == Keys.Up) Step(w, 1);
        else if (e.KeyCode == Keys.Down) Step(w, -1);
        else if (e.KeyCode == Keys.Enter) { Clamp(w); if (wStart.Enabled) DoStart(); }
        else if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape) { Clamp(w); focusedNum = null; InvalidateHud(); }
        e.Handled = true;
    }

    static void Clamp(Widget w) { w.Value = Math.Max(w.Min, Math.Min(w.Max, w.Value)); }

    void Step(Widget w, int dir)
    {
        int v = w.Value + dir;
        if (w.Max <= 59) { if (v > w.Max) v = w.Min; if (v < w.Min) v = w.Max; } // hh/mm wrap around
        w.Value = Math.Max(w.Min, Math.Min(w.Max, v));
        typed = false; Touched(w); InvalidateHud();
    }

    void Touched(Widget w)
    {
        if (w == wNum) SetMode(true);
        if (w == wHour || w == wMin) SetMode(false);
    }

    void SetMode(bool inMode) { wIn.On = inMode; wAt.On = !inMode; InvalidateHud(); }

    void CloseList() { listOpen = false; listHover = -1; Invalidate(); }

    void ActivateWidget(Widget w)
    {
        switch (w.Id)
        {
            case "in": SetMode(true); break;
            case "at": SetMode(false); break;
            case "unit": wUnit.Text = wUnit.Text == "MIN" ? "STD" : "MIN"; SetMode(true); break;
            case "q15": case "q30": case "q60": case "q120":
                wNum.Value = int.Parse(w.Id.Substring(1)); wUnit.Text = "MIN"; SetMode(true); break;
            case "action": listOpen = true; listHover = -1; Invalidate(); break;
            case "start": DoStart(); break;
            case "cancel": DoCancel(); break;
            case "finale": SetFinale(!finaleOn); break;
        }
    }

    void SetSetup(bool on)
    {
        foreach (var w in all) w.Enabled = on;
        wCancel.Enabled = !on; wFinale.Enabled = true; // Finale jederzeit umschaltbar
        if (!on) { focusedNum = null; listOpen = false; }
        Invalidate();
    }

    void ShowWindow() { Show(); WindowState = FormWindowState.Normal; Activate(); }

    void SetIdle()
    {
        SetThreadExecutionState(ES_CONTINUOUS);
        StopFinale();
        if (running) fadeStart = clock.Elapsed.TotalSeconds;
        running = false; tick.Stop(); SetSetup(true);
        shown = 0; status = "BEREIT – Zeit und Aktion wählen, dann START"; tray.Text = "Terminator"; Invalidate();
        MaybeShowUpdate(); // 3.17: während des Countdowns zurückgehaltenen Hinweis jetzt zeigen
    }

    // ---------- logic ----------
    static bool RunShutdown(string args, out string err)
    {
        err = null;
        try
        {
            string exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shutdown.exe");
            var p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true });
            string e = p.StandardError.ReadToEnd(); p.WaitForExit();
            if (p.ExitCode != 0) { err = e.Trim().Length > 0 ? e.Trim() : "Fehlercode " + p.ExitCode; return false; }
            return true;
        }
        catch (Exception ex) { err = ex.Message; return false; }
    }

    void BeginCountdown(bool announce)
    {
        running = true; SetSetup(false);
        if (startTime == DateTime.MinValue || startTime > DateTime.Now || startTime >= target) startTime = DateTime.Now;
        startShown = Core.SecondsUntil(startTime, target);
        DateTime now = DateTime.Now;
        string when = target.ToString("HH:mm") + (target.Date > now.Date ? " (" + target.ToString("dd.MM.") + ")" : "");
        status = "AKTIV – " + Core.ActionName(act) + " um " + when + " Uhr";
        tick.Start(); OnTick(); Invalidate();
        if (settings.AutoMinimize) Hide();
        // PC während des Countdowns nicht einschlafen lassen, damit der Timer sicher durchläuft
        if (Core.UsesShutdownExe(act)) SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
    }

    void DoStart()
    {
        DateTime now = DateTime.Now;
        Clamp(wNum); Clamp(wHour); Clamp(wMin);
        act = (PowerAction)actionIndex;
        target = wIn.On ? Core.TargetFromMinutes(now, wNum.Value * (wUnit.Text == "STD" ? 60 : 1))
                        : Core.TargetFromClock(now, wHour.Value, wMin.Value);
        startTime = now;
        // Kein Vorab-Planen bei Windows (keine "Sie werden abgemeldet"-Meldung): der Terminator zählt selbst
        SaveState();
        BeginCountdown(true);
    }

    void DoExit()
    {
        if (running)
        {
            if (MessageBox.Show("Beim Beenden wird " + Core.ActionName(act) + " NICHT ausgeführt.\n\nTrotzdem beenden?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        }
        exiting = true; tray.Visible = false; Application.Exit();
    }

    void DoCancel()
    {
        if (!running) return;
        ClearState(); SetIdle(); // still: kein shutdown /a, keine Meldung
        ShowWindow();
    }

    void OnTick()
    {
        if (!running) return;
        int left = Core.SecondsUntil(DateTime.Now, target);
        if (left != shown) { shown = left; RedrawCountdown(); }
        string tt = Core.ActionName(act) + " in " + Core.Countdown(left);
        tray.Text = tt.Length > 63 ? tt.Substring(0, 63) : tt;
        if (finaleOn && Core.UsesShutdownExe(act) && left <= 60 && left > 0 && !finale.Active) StartFinale();
        if (left <= 0)
        {
            tick.Stop();
            if (Core.UsesShutdownExe(act))
            {
                status = "HASTA LA VISTA – Windows wird beendet …"; Invalidate(); ClearState();
                if (!Power.Exit(act))
                {
                    string err;
                    if (!RunShutdown(Core.ImmediateArgs(act), out err))
                    { StopFinale(); var a0 = act; SetIdle(); MessageBox.Show(Core.ActionName(a0) + " hat nicht geklappt:\n" + err, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
            else
            {
                var a = act; SetIdle();
                if (!Application.SetSuspendState(a == PowerAction.Hibernate ? PowerState.Hibernate : PowerState.Suspend, false, false))
                    MessageBox.Show(Core.ActionName(a) + " ist auf diesem PC nicht verfügbar oder deaktiviert.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    void OnClosing(object s, FormClosingEventArgs e)
    {
        if (running && !exiting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
        tray.Visible = false;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint flags);
    const uint ES_CONTINUOUS = 0x80000000, ES_SYSTEM_REQUIRED = 0x00000001;

    // ---------- Finale (letzte Minute) ----------
    readonly Finale finale = new Finale();
    Widget wFinale; ToolStripMenuItem miFinale;
    static readonly string FinaleFile = Path.Combine(Path.GetDirectoryName(StateFile), "finale.txt");
    // ---------- Einstellungen (Zahnrad) ----------
    static readonly string SettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terminator", "settings.ini");
    readonly Settings settings = LoadSettings();
    SettingsPanel settingsPanel;
    bool finaleOn { get { return settings.Finale; } }
    static Settings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFile)) return Settings.Parse(File.ReadAllText(SettingsFile, System.Text.Encoding.UTF8));
            var st = new Settings(); // Übernahme aus 2.8/2.9
            if (File.Exists(FinaleFile)) st.Finale = File.ReadAllText(FinaleFile).Trim() != "0";
            return st;
        }
        catch { return new Settings(); }
    }
    void SaveSettings() { try { Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)); File.WriteAllText(SettingsFile, settings.Serialize(), new System.Text.UTF8Encoding(false)); } catch { } }
    void ToggleSettings() { settingsPanel.LoadFrom(); settingsPanel.Visible = !settingsPanel.Visible; if (settingsPanel.Visible) { listOpen = false; settingsPanel.BringToFront(); settingsPanel.Focus(); } InvalidateWin(); }
    void OnSettingsChanged()
    {
        SaveSettings();
        wFinale.On = settings.Finale; if (miFinale != null) miFinale.Checked = settings.Finale;
        if (miAutoMin != null && miAutoMin.Checked != settings.AutoMinimize) miAutoMin.Checked = settings.AutoMinimize;
        if (!settings.Finale) StopFinale(); else if (running) OnTick();
        if (bgCache != null) { bgCache.Dispose(); bgCache = null; } // Texte sofort neu einbacken
        Invalidate();
    }
    void SetFinale(bool on)
    {
        settings.Finale = on; if (settingsPanel != null) settingsPanel.LoadFrom();
        OnSettingsChanged(); InvalidateHud();
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
    bool hotkeys;
    void StartFinale()
    {
        Tone.Off = true;
        finale.Start(() => (target - DateTime.Now).TotalSeconds, () => OnTick());
        // Abbrechen trotz Overlay: Strg+Alt+A und Esc (nur während des Finales global belegt)
        if (!hotkeys) { RegisterHotKey(Handle, 0x7A01, 0x0001 | 0x0002 | 0x4000, (uint)Keys.A); RegisterHotKey(Handle, 0x7A02, 0x4000, (uint)Keys.Escape); hotkeys = true; }
    }
    void StopFinale()
    {
        finale.Stop(); Tone.Off = false;
        if (hotkeys) { UnregisterHotKey(Handle, 0x7A01); UnregisterHotKey(Handle, 0x7A02); hotkeys = false; }
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x02E0) // WM_DPICHANGED: auf neuen Monitor gleichmäßig neu skalieren
        {
            var rc = new int[4]; System.Runtime.InteropServices.Marshal.Copy(m.LParam, rc, 0, 4);
            SetWindowPos(Handle, IntPtr.Zero, rc[0], rc[1], rc[2] - rc[0], rc[3] - rc[1], 0x0014);
            ApplyScale(((int)(long)m.WParam & 0xFFFF) / 96f, true);
            return;
        }
        if (m.Msg == 0x0312 && (m.WParam.ToInt32() == 0x7A01 || m.WParam.ToInt32() == 0x7A02)) { if (running && left0() > 0) DoCancel(); return; }
        base.WndProc(ref m);
    }
    int left0() { return Core.SecondsUntil(DateTime.Now, target); }

    // ---------- 3.17: Auto-Update über GitHub Releases ----------
    const int SetX = 590, SetY = 176, UpdX = 260, UpdY = 140;
    static readonly string UpdateCache = Path.Combine(Path.GetDirectoryName(SettingsFile), "update-cache.json");
    static readonly string UpdateWork = Path.Combine(Path.GetTempPath(), "Terminator-update");
    static readonly Version Installed = UpdateCore.Norm(typeof(MainForm).Assembly.GetName().Version);
    UpdatePanel updatePanel; UpdateInfo pendingUpdate; bool updateDismissed, checking, downloading, cancelDownload;
    readonly Timer updateDelay = new Timer();

    void InitUpdater()
    {
        updatePanel = new UpdatePanel(OnUpdateGo, OnUpdateLater) { Location = new Point(UpdX, UpdY) };
        Controls.Add(updatePanel);
        updatePanel.ApplyScale(Scale, new Point((int)Math.Round(UpdX * Scale), (int)Math.Round(UpdY * Scale)));
        // Start nicht blockieren: automatische Prüfung kurz nach dem Start im Hintergrund, höchstens alle 24 h
        updateDelay.Interval = 4000;
        updateDelay.Tick += (s, e) => { updateDelay.Stop(); if (settings.UpdateCheck && UpdateCore.DueForAutoCheck(DateTime.UtcNow, LastCheckUtc())) StartUpdateCheck(false); };
        updateDelay.Start();
    }
    DateTime LastCheckUtc() { return settings.UpdateLastTicks > 0 ? new DateTime(settings.UpdateLastTicks, DateTimeKind.Utc) : DateTime.MinValue; }

    void StartUpdateCheck(bool manual)
    {
        if (checking || downloading) return;
        checking = true;
        if (manual) settingsPanel.SetUpdateStatus("Prüfe …");
        string etag = settings.UpdateEtag;
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            string err, newEtag; UpdateInfo info = null;
            try { err = new UpdateClient(Installed, UpdateCache).Check(etag, out newEtag, out info); }
            catch (Exception ex) { err = ex.Message; newEtag = etag; }
            try { if (!IsDisposed) BeginInvoke(new Action(() => OnUpdateChecked(manual, err, newEtag, info))); } catch { }
        });
    }

    void OnUpdateChecked(bool manual, string err, string etag, UpdateInfo info)
    {
        checking = false;
        if (err == null) { settings.UpdateLastTicks = DateTime.UtcNow.Ticks; settings.UpdateEtag = etag ?? ""; SaveSettings(); }
        if (manual) settingsPanel.SetUpdateStatus(err != null ? err : info != null ? "Version " + UpdateCore.Show(info.Version) + " verfügbar" : "Aktuell – " + UpdateCore.Show(Installed) + " ist die neueste Version");
        if (info == null) return;
        pendingUpdate = info;
        if (manual) updateDismissed = false;
        MaybeShowUpdate();
    }

    void MaybeShowUpdate()
    {
        if (pendingUpdate == null || updateDismissed || running || downloading || updatePanel == null || updatePanel.Visible) return;
        updatePanel.ShowOffer(pendingUpdate, Installed);
        settingsPanel.Visible = false; listOpen = false;
        updatePanel.Visible = true; updatePanel.BringToFront(); updatePanel.Focus();
        tray.ShowBalloonTip(5000, "Terminator", "Update " + UpdateCore.Show(pendingUpdate.Version) + " verfügbar", ToolTipIcon.Info);
    }

    void OnUpdateLater()
    {
        if (downloading) { cancelDownload = true; return; }
        updateDismissed = true; updatePanel.Visible = false; Invalidate();
    }

    // Programmordner + Neustart-Ziel; null, wenn es kein vollständiger Terminator-Ordner ist
    static string InstallDir(out string restart)
    {
        restart = null;
        try
        {
            if (Program.ScriptMode)
            {
                string d = Program.ScriptDir;
                if (string.IsNullOrEmpty(d) || !File.Exists(Path.Combine(d, "Terminator.ps1")) || !File.Exists(Path.Combine(d, "Daten", "TimerUI.dll"))) return null;
                restart = Path.Combine(d, "Terminator starten.vbs"); return File.Exists(restart) ? d : null;
            }
            string exe = Application.ExecutablePath, dir = Path.GetDirectoryName(exe);
            if (!string.Equals(Path.GetFileName(exe), UpdateCore.ExeName, StringComparison.OrdinalIgnoreCase) || !File.Exists(Path.Combine(dir, "Daten", "TimerUI.dll"))) return null;
            restart = Path.Combine(dir, UpdateCore.ExeName); return dir;
        }
        catch { return null; }
    }

    void OpenReleasePage(string why)
    {
        updatePanel.SetState(true, "JETZT AKTUALISIEREN", "SCHLIESSEN", why + " – Release-Seite wurde geöffnet.", true);
        try { Process.Start(new ProcessStartInfo(pendingUpdate != null ? pendingUpdate.HtmlUrl : UpdateCore.ReleasesPage) { UseShellExecute = true }); } catch { }
    }

    void OnUpdateGo()
    {
        var info = pendingUpdate; if (info == null || downloading) return;
        if (running) { updatePanel.SetState(false, "JETZT AKTUALISIEREN", "SPÄTER", "Während ein Countdown läuft, wird nicht aktualisiert.", true); return; }
        string restart, dir = InstallDir(out restart);
        if (dir == null) { OpenReleasePage("Kein vollständiger Terminator-Ordner"); return; }
        if (!UpdateCore.IsWritable(dir)) { OpenReleasePage("Ordner ist schreibgeschützt"); return; }
        downloading = true; cancelDownload = false;
        updatePanel.SetState(false, "LÄDT …", "ABBRECHEN", "Download über HTTPS …", false);
        string work = Path.Combine(UpdateWork, UpdateCore.Show(info.Version)), zip = Path.Combine(work, UpdateCore.ZipName), ext = Path.Combine(work, "neu");
        int lastPct = -1;
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            string root = null;
            string err = new UpdateClient(Installed, UpdateCache).Download(info, zip, (n, total) =>
            {
                int pct = (int)(n * 100 / Math.Max(1, total));
                if (pct != lastPct) { lastPct = pct; try { BeginInvoke(new Action(() => updatePanel.SetState(false, "LÄDT … " + pct + " %", "ABBRECHEN", "Download über HTTPS …", false))); } catch { } }
            }, () => cancelDownload);
            if (err == null) err = UpdateClient.Stage(info, zip, ext, out root);
            try { BeginInvoke(new Action(() => OnUpdateReady(err, work, root, dir, restart))); } catch { }
        });
    }

    void OnUpdateReady(string err, string work, string root, string dir, string restart)
    {
        downloading = false;
        if (err == null && running) err = "Während ein Countdown läuft, wird nicht aktualisiert.";
        if (err == null) err = UpdateClient.LaunchSwap(work, root, dir, restart);
        if (err != null) { updatePanel.SetState(true, "ERNEUT VERSUCHEN", "SPÄTER", err, true); return; }
        updatePanel.SetState(false, "INSTALLIERE …", "SPÄTER", "Terminator wird beendet, aktualisiert und neu gestartet.", false);
        exiting = true; tray.Visible = false; Application.Exit();
    }

    void SaveState() { try { Directory.CreateDirectory(Path.GetDirectoryName(StateFile)); File.WriteAllText(StateFile, Core.Serialize(act, target, startTime)); } catch { } }
    void ClearState() { try { File.Delete(StateFile); } catch { } }
    void RestoreState()
    {
        try
        {
            PowerAction a; DateTime t, st;
            if (File.Exists(StateFile) && Core.TryParse(File.ReadAllText(StateFile), DateTime.Now, out a, out t, out st))
            {
                startTime = st; act = a; target = t; actionIndex = (int)a; wAction.Text = ActionNames[actionIndex];
                BeginCountdown(false);
                startHidden = true;
            }
            else ClearState();
        }
        catch { }
    }
}

// Herunterfahren/Neustart direkt über ExitWindowsEx: sofort, ohne Windows-Ankündigung
static class Power
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
    struct TokPriv { public int Count; public long Luid; public int Attr; }
    [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)] static extern bool OpenProcessToken(IntPtr h, int acc, out IntPtr tok);
    [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)] static extern bool LookupPrivilegeValue(string host, string name, out long luid);
    [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)] static extern bool AdjustTokenPrivileges(IntPtr tok, bool dis, ref TokPriv n, int len, IntPtr prev, IntPtr rl);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)] static extern bool ExitWindowsEx(uint flags, uint reason);
    const uint EWX_SHUTDOWN = 0x1, EWX_REBOOT = 0x2, EWX_POWEROFF = 0x8, EWX_FORCEIFHUNG = 0x10;
    const uint SHTDN_REASON_FLAG_PLANNED = 0x80000000;

    public static bool Exit(PowerAction a)
    {
        try
        {
            IntPtr tok;
            if (OpenProcessToken(GetCurrentProcess(), 0x20 | 0x8, out tok)) // ADJUST_PRIVILEGES | QUERY
            {
                var tp = new TokPriv { Count = 1, Attr = 2 }; // SE_PRIVILEGE_ENABLED
                if (LookupPrivilegeValue(null, "SeShutdownPrivilege", out tp.Luid)) AdjustTokenPrivileges(tok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                CloseHandle(tok);
            }
            uint f = (a == PowerAction.Restart ? EWX_REBOOT : EWX_SHUTDOWN | EWX_POWEROFF) | EWX_FORCEIFHUNG;
            return ExitWindowsEx(f, SHTDN_REASON_FLAG_PLANNED);
        }
        catch { return false; }
    }
}

// Einstieg für die Skript-Version: powershell lädt die DLL und ruft Run() auf
// 3.17: Run(ordner) übergibt den Programmordner (für das Auto-Update); Run() ohne Ordner bleibt kompatibel.
public static class TimerHost
{
    public static void Run() { Run(null); }
    public static void Run(string scriptDir) { Program.ScriptMode = true; Program.ScriptDir = scriptDir; Program.Start(); }
}

static class Program
{
    public static bool ScriptMode; public static string ScriptDir;
    [STAThread]
    static void Main() { Start(); }
    public static void Start()
    {
        Application.EnableVisualStyles();
        try { Application.SetCompatibleTextRenderingDefault(false); } catch { }
        Application.Run(new MainForm());
    }
}
