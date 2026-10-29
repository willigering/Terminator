using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

// Zahnrad-Panel: Finale ein/aus, (3.5: Texte immer sichtbar, nicht änderbar), Standard, Schließen. Änderungen wirken sofort.
class SettingsPanel : Control
{
    readonly Settings st; readonly Action changed; readonly Font font = Art.Font(14f, false), head = Art.Font(18f, true);
    readonly Widget wFin, wMin, wReset, wClose; readonly Widget[] ws;
    Widget hover, pressed;

    static Widget W(string id, WType t, int x, int y, int w, string text) { return new Widget { Id = id, Type = t, R = new Rectangle(x, y, w, 28), Text = text }; }

    public SettingsPanel(Settings settings, Action onChanged)
    {
        st = settings; changed = onChanged;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(340, 196); Visible = false; TabStop = false;
        wFin = W("fin", WType.Toggle, 235, 58, 80, "");
        wMin = W("min", WType.Toggle, 235, 94, 80, "");
        wReset = W("reset", WType.Button, 25, 146, 140, "STANDARD");
        wClose = W("close", WType.Primary, 195, 146, 120, "SCHLIESSEN");
        ws = new[] { wFin, wMin, wReset, wClose };
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
        Invalidate();
    }

    float K = 1f;
    public void ApplyScale(float k, Point loc)
    {
        K = k; Location = loc; Size = new Size((int)Math.Round(340 * k), (int)Math.Round(196 * k));
        Invalidate();
    }
    Widget Hit(Point p) { p = new Point((int)(p.X / K), (int)(p.Y / K)); foreach (var w in ws) if (w.R.Contains(p)) return w; return null; }

    void Activate(Widget w)
    {
        switch (w.Id)
        {
            case "fin": st.Finale = !st.Finale; break;
            case "min": st.AutoMinimize = !st.AutoMinimize; break;
            case "reset": st.Reset(); break;
            case "close": Visible = false; return;
        }
        LoadFrom(); changed();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.ScaleTransform(K, K);
        var r = new Rectangle(0, 0, 339, 195);
        using (var b = new LinearGradientBrush(r, Color.FromArgb(20, 23, 26), Color.FromArgb(11, 13, 15), 90f)) g.FillRectangle(b, r);
        using (var p = Hud.Round(Rectangle.Inflate(r, -3, -3), 8)) using (var pen = new Pen(Hud.RedBorder, 1.2f)) g.DrawPath(pen, p);
        using (var b = new SolidBrush(Color.FromArgb(255, 70, 60))) g.DrawString("⚙  EINSTELLUNGEN", head, b, 22, 14);
        using (var b = new SolidBrush(Hud.Ink))
        {
            g.DrawString("Finale (letzte Minute)", font, b, 25, 63);
            g.DrawString("Nach Start minimieren", font, b, 25, 99);
        }
        foreach (var w in ws) Hud.DrawWidget(g, w, w == hover, w == pressed && w == hover, false, font);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys k)
    {
        if (k == Keys.Escape) { Visible = false; return true; }
        return base.ProcessCmdKey(ref msg, k);
    }
}
