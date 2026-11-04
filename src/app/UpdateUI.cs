using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

// 3.17: Update-Hinweis im Stil des Zahnrad-Panels. Installiert wird nur nach Klick auf JETZT AKTUALISIEREN.
class UpdatePanel : Control
{
    public const int W0 = 440, H0 = 262;
    readonly Font font = Art.Font(14f, false), small = Art.Font(12f, false), head = Art.Font(18f, true);
    readonly Widget wGo, wLater; readonly Widget[] ws;
    readonly Action onGo, onLater;
    Widget hover, pressed;
    string installed = "", available = "", message = ""; bool messageIsError;
    List<string> notes = new List<string>();

    static Widget W(string id, WType t, int x, int y, int w, string text) { return new Widget { Id = id, Type = t, R = new Rectangle(x, y, w, 28), Text = text }; }

    public UpdatePanel(Action go, Action later)
    {
        onGo = go; onLater = later;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(W0, H0); Visible = false; TabStop = false;
        wGo = W("go", WType.Primary, 25, 216, 230, "JETZT AKTUALISIEREN");
        wLater = W("later", WType.Button, 275, 216, 140, "SPÄTER");
        ws = new[] { wGo, wLater };
        MouseMove += (s, e) => { var h = Hit(e.Location); if (h != hover) { hover = h; Invalidate(); } Cursor = h != null ? Cursors.Hand : Cursors.Default; };
        MouseLeave += (s, e) => { hover = null; Invalidate(); };
        MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { pressed = Hit(e.Location); Invalidate(); } };
        MouseUp += (s, e) => { var p = pressed; pressed = null; if (p != null && p == Hit(e.Location) && p.Enabled) { if (p == wGo) onGo(); else onLater(); } Invalidate(); };
    }

    public void ShowOffer(UpdateInfo info, Version inst)
    {
        installed = UpdateCore.Show(inst); available = UpdateCore.Show(info.Version);
        notes = UpdateCore.NotesLines(info.Notes, 4, 70);
        SetState(true, "JETZT AKTUALISIEREN", "SPÄTER", "", false);
    }
    public void SetState(bool goEnabled, string goText, string laterText, string msg, bool error)
    {
        wGo.Enabled = goEnabled; wGo.Text = goText; wLater.Text = laterText; message = msg ?? ""; messageIsError = error; Invalidate();
    }

    float K = 1f;
    public void ApplyScale(float k, Point loc) { K = k; Location = loc; Size = new Size((int)Math.Round(W0 * k), (int)Math.Round(H0 * k)); Invalidate(); }
    Widget Hit(Point p) { p = new Point((int)(p.X / K), (int)(p.Y / K)); foreach (var w in ws) if (w.R.Contains(p)) return w; return null; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.ScaleTransform(K, K);
        var r = new Rectangle(0, 0, W0 - 1, H0 - 1);
        using (var b = new LinearGradientBrush(r, Color.FromArgb(20, 23, 26), Color.FromArgb(11, 13, 15), 90f)) g.FillRectangle(b, r);
        using (var p = Hud.Round(Rectangle.Inflate(r, -3, -3), 8)) using (var pen = new Pen(Hud.RedBorder, 1.2f)) g.DrawPath(pen, p);
        using (var b = new SolidBrush(Color.FromArgb(255, 70, 60))) g.DrawString("SYSTEM UPDATE VERFÜGBAR", head, b, 22, 14);
        var clip = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        using (var ink = new SolidBrush(Hud.Ink))
        using (var grey = new SolidBrush(Color.FromArgb(150, 156, 162)))
        {
            g.DrawString("Installiert: " + installed + "     Verfügbar: " + available, font, ink, 25, 52);
            if (notes.Count > 0)
            {
                g.DrawString("Was ist neu?", font, ink, 25, 82);
                for (int i = 0; i < notes.Count; i++) g.DrawString(notes[i], small, grey, new RectangleF(25, 106 + i * 19, W0 - 50, 19), clip);
            }
            if (message.Length > 0)
                using (var mb = new SolidBrush(messageIsError ? Color.FromArgb(255, 110, 96) : Hud.Ink))
                    g.DrawString(message, small, mb, new RectangleF(25, 186, W0 - 50, 22), clip);
        }
        foreach (var w in ws) Hud.DrawWidget(g, w, w == hover, w == pressed && w == hover, false, font);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys k)
    {
        if (k == Keys.Escape && wLater.Enabled) { onLater(); return true; }
        return base.ProcessCmdKey(ref msg, k);
    }
}
