using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Vollbild-Finale über allen Monitoren: oberste Ebene, klick-durchlässig, ohne Rahmen/Taskleiste.
// Layered Window (LWA_ALPHA) + doppelt gepuffertes GDI+-Zeichnen, ~60 fps.
class FinaleOverlay : Form
{
    const int WS_EX_LAYERED = 0x80000, WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_TOPMOST = 0x8, WS_EX_NOACTIVATE = 0x8000000;
    const int LWA_ALPHA = 2;
    [DllImport("user32.dll")] static extern bool SetLayeredWindowAttributes(IntPtr h, int key, byte alpha, int flags);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint f);
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    int phase; double tint, hF = 1, wF = 1, glow; byte lastAlpha = 255;

    public FinaleOverlay(Rectangle bounds)
    {
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
        StartPosition = FormStartPosition.Manual; Bounds = bounds; BackColor = Color.Black;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
    }
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE; return cp; }
    }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); SetLayeredWindowAttributes(Handle, 0, 0, LWA_ALPHA); lastAlpha = 0; }

    public void Set(int ph, double t, double h, double w, double g)
    {
        phase = ph; tint = t; hF = h; wF = w; glow = g;
        byte a = (byte)Math.Round(255 * (ph == 1 ? Math.Max(0, Math.Min(1, t)) : 1.0));
        if (a != lastAlpha) { SetLayeredWindowAttributes(Handle, 0, a, LWA_ALPHA); lastAlpha = a; }
        SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010); // oben halten, ohne Aktivierung
        Invalidate(); Update();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; int W = ClientSize.Width, H = ClientSize.Height;
        if (phase <= 1) { using (var b = new SolidBrush(Color.FromArgb(200, 0, 0))) g.FillRectangle(b, 0, 0, W, H); return; }
        g.Clear(Color.Black);
        if (phase >= 4) return;
        float cx = W / 2f, cy = H / 2f;
        float rh = Math.Max(2f, (float)(H * hF)), rw = (float)(W * wF);
        // reines Rot – kein weißer Kern, kein Glühen, kein Punkt
        if (rw >= 1) using (var b = new SolidBrush(Color.FromArgb(200, 0, 0))) g.FillRectangle(b, cx - rw / 2, cy - rh / 2, rw, rh);
    }
}

class Finale : IDisposable
{
    [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint p);
    [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint p);
    readonly List<FinaleOverlay> forms = new List<FinaleOverlay>();
    readonly Timer timer = new Timer { Interval = 15 };
    Func<double> remaining; Action atZero; bool fired;
    public bool Active { get { return forms.Count > 0; } }

    public Finale() { timer.Tick += (s, e) => Frame(); }

    public void Start(Func<double> rem, Action zero)
    {
        if (Active) return;
        remaining = rem; atZero = zero; fired = false;
        foreach (var sc in Screen.AllScreens) { var f = new FinaleOverlay(sc.Bounds); forms.Add(f); f.Show(); }
        timeBeginPeriod(1); timer.Start(); Frame();
    }

    void Frame()
    {
        double t, h, w, g; double rem = remaining();
        int ph = Core.Finale(rem, out t, out h, out w, out g);
        if (ph == 0) { Stop(); return; }
        foreach (var f in forms) f.Set(ph, t, h, w, g);
        if (ph == 4 && !fired) { fired = true; atZero(); }
    }

    public void Stop()
    {
        if (!Active) return;
        timer.Stop(); timeEndPeriod(1);
        foreach (var f in forms) { f.Hide(); f.Dispose(); }
        forms.Clear();
    }
    public void Dispose() { Stop(); timer.Dispose(); }
}
