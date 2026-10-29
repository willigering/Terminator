using System;
using System.Drawing;
// 3.7: "weniger rot" – kräftige Rottöne der live gezeichneten Elemente werden ruhiger (weniger gesättigt, etwas dunkler).
// Orange/Gelb, Grau und Weiß bleiben unverändert. Während des Finales aus (Finale bleibt voll rot).
static class Tone
{
    public static bool Off;
    public const float Sat = 0.62f, Val = 0.88f;
    public static Color C(int r, int g, int b) { return C(255, r, g, b); }
    public static Color C(int a, Color c) { return C(a, c.R, c.G, c.B); }
    public static Color C(int a, int r, int g, int b)
    {
        if (Off) return Color.FromArgb(a, r, g, b);
        int mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
        if (mx == 0 || mx != r) return Color.FromArgb(a, r, g, b);
        float s = (mx - mn) / (float)mx; if (s < 0.35f) return Color.FromArgb(a, r, g, b);
        float hue = 60f * (g - b) / Math.Max(1, mx - mn); if (hue < 0) hue += 360;
        if (hue > 28 && hue < 330) return Color.FromArgb(a, r, g, b);   // Orange/Gelb unverändert
        float w = hue <= 28 ? 1f - Math.Max(0, hue - 14) / 14f : 1f;        // weicher Übergang zu Orange
        float ns = s * (1 - (1 - Sat) * w), nv = mx * (1 - (1 - Val) * w) / 255f;
        // HSV -> RGB mit gleichem Farbton
        float h6 = hue / 60f; int i = (int)Math.Floor(h6) % 6; float fr = h6 - (float)Math.Floor(h6);
        float p = nv * (1 - ns), q = nv * (1 - ns * fr), t = nv * (1 - ns * (1 - fr));
        float R, G, B;
        switch (i) { case 0: R = nv; G = t; B = p; break; case 1: R = q; G = nv; B = p; break; case 5: R = nv; G = p; B = q; break; default: R = nv; G = p; B = p; break; }
        return Color.FromArgb(a, Clamp(R), Clamp(G), Clamp(B));
    }
    static int Clamp(float v) { return Math.Max(0, Math.Min(255, (int)Math.Round(v * 255))); }
}
