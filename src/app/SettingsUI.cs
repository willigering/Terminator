using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

// Zahnrad-Panel: Finale ein/aus, (3.5: Texte immer sichtbar, nicht änderbar), Standard, Schließen. Änderungen wirken sofort.
class SettingsPanel : Control
{
    readonly Settings st; readonly Action changed, checkNow; string updStatus = ""; readonly Font font = Art.Font(14f, false), head = Art.Font(18f, true);
    readonly Widget wFin, wMin, wUpd, wCheck, wReset, wClose; readonly Widget[] ws;
    Widget hover, pressed;

    static Widget W(string id, WType t, int x, int y, int w, string text) { return new Widget { Id = id, Type = t, R = new Rectangle(x, y, w, 28), Text = text }; }

    public const int W0 = 360, H0 = 270;
    public SettingsPanel(Settings settings, Action onChanged, Action onCheckNow)
    {
        st = settings; changed = onChanged; checkNow = onCheckNow;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(W0, H0); Visible = false; TabStop = false;
        wFin = W("fin", WType.Toggle, 265, 58, 70, "");
        wMin = W("min", WType.Toggle, 265, 94, 70, "");
        wUpd = W("upd", WType.Toggle, 265, 130, 70, "");          // 3.17
        wCheck = W("check", WType.Button, 25, 166, 140, "JETZT PRÜFEN");
        wReset = W("reset", WType.Button, 25, 220, 140, "STANDARD");
        wClose = W("close", WType.Primary, 215, 220, 120, "SCHLIESSEN");
        ws = new[] { wFin, wMin, wUpd, wCheck, wReset, wClose };
        MouseMove += (s, e) => { var h = Hit(e.Location); if (h != hover) { hover = h; Invalidate(); } Cursor = h != null ? Cursors.Hand : Cursors.Default; };
        MouseLeave += (s, e) => { hover = null; Invalidate(); };
        MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { pressed = Hit(e.Location); Invalidate(); } };
        MouseUp += (s, e) => { var p = pressed; pressed = null; if (p != null && p == Hit(e.Location)) Activate(p); Invalidate(); };
        LoadFrom();
    }

    public void LoadFrom()
    {
        wFin.On = st.Finale;
        wFin.Text = st.Finale ? "AN" : "AUS";
        wMin.On = st.AutoMinimize; wMin.Text = st.AutoMinimize ? "AN" : "AUS";
        wUpd.On = st.UpdateCheck; wUpd.Text = st.UpdateCheck ? "AN" : "AUS";
        Invalidate();
    }

    public void SetUpdateStatus(string text) { updStatus = text ?? ""; Invalidate(); }

    float K = 1f;
    public void ApplyScale(float k, Point loc)
    {
        K = k; Location = loc; Size = new Size((int)Math.Round(W0 * k), (int)Math.Round(H0 * k));
        Invalidate();
    }
    Widget Hit(Point p) { p = new Point((int)(p.X / K), (int)(p.Y / K)); foreach (var w in ws) if (w.R.Contains(p)) return w; return null; }

    void Activate(Widget w)
    {
        switch (w.Id)
        {
            case "fin": st.Finale = !st.Finale; break;
            case "min": st.AutoMinimize = !st.AutoMinimize; break;
            case "upd": st.UpdateCheck = !st.UpdateCheck; break;
            case "check": checkNow(); return;
            case "reset": st.Reset(); break;
            case "close": Visible = false; return;
        }
        LoadFrom(); changed();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.ScaleTransform(K, K);
        var r = new Rectangle(0, 0, W0 - 1, H0 - 1);
        using (var b = new LinearGradientBrush(r, Color.FromArgb(20, 23, 26), Color.FromArgb(11, 13, 15), 90f)) g.FillRectangle(b, r);
        using (var p = Hud.Round(Rectangle.Inflate(r, -3, -3), 8)) using (var pen = new Pen(Hud.RedBorder, 1.2f)) g.DrawPath(pen, p);
        using (var b = new SolidBrush(Color.FromArgb(255, 70, 60))) g.DrawString("⚙  EINSTELLUNGEN", head, b, 22, 14);
        using (var b = new SolidBrush(Hud.Ink))
        {
            g.DrawString("Finale (letzte Minute)", font, b, 25, 63);
            g.DrawString("Nach Start minimieren", font, b, 25, 99);
            g.DrawString("Automatisch nach Updates suchen", font, b, 25, 135);
            if (updStatus.Length > 0)
                using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
                using (var sm = Art.Font(11f, false))
                    g.DrawString(updStatus, sm, b, new RectangleF(175, 166, W0 - 195, 28), sf);
        }
        foreach (var w in ws) Hud.DrawWidget(g, w, w == hover, w == pressed && w == hover, false, font);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys k)
    {
        if (k == Keys.Escape) { Visible = false; return true; }
        return base.ProcessCmdKey(ref msg, k);
    }
}
