// automatisch erzeugt von gen36.py aus dem finalen Design (1537x1023). Nicht von Hand ändern.
using System.Drawing;
using System.Collections.Generic;
static class Sprites
{
    public const float K = 1.6010416666666667f; // Quellpixel je logischem Pixel
    public const int SrcW = 1537, SrcH = 1023;
    public static readonly Dictionary<string, Rectangle> Atlas = new Dictionary<string, Rectangle> {
        { "act_clean", new Rectangle(0, 0, 429, 70) },
        { "cancel_clean", new Rectangle(431, 0, 400, 70) },
        { "start_clean", new Rectangle(833, 0, 336, 70) },
        { "in_bg_on", new Rectangle(1171, 0, 97, 63) },
        { "in_bg_off", new Rectangle(1270, 0, 97, 63) },
        { "unit_bg_on", new Rectangle(1369, 0, 106, 63) },
        { "unit_bg_off", new Rectangle(1477, 0, 106, 63) },
        { "q15_bg_on", new Rectangle(0, 72, 110, 63) },
        { "q15_bg_off", new Rectangle(112, 72, 110, 63) },
        { "q30_bg_on", new Rectangle(224, 72, 122, 63) },
        { "q30_bg_off", new Rectangle(348, 72, 122, 63) },
        { "q60_bg_on", new Rectangle(472, 72, 112, 63) },
        { "q60_bg_off", new Rectangle(586, 72, 112, 63) },
        { "q120_bg_on", new Rectangle(700, 72, 111, 63) },
        { "q120_bg_off", new Rectangle(813, 72, 111, 63) },
        { "at_bg_on", new Rectangle(926, 72, 108, 63) },
        { "at_bg_off", new Rectangle(1036, 72, 108, 63) },
        { "num_clean", new Rectangle(1146, 72, 119, 63) },
        { "time_clean", new Rectangle(1267, 72, 140, 63) },
        { "panel", new Rectangle(0, 137, 270, 54) },
        { "status_clean", new Rectangle(272, 137, 420, 28) },
    };
    // Zielposition im Design in Quellpixeln
    public static readonly Dictionary<string, Point> Pos = new Dictionary<string, Point> {
        { "act_clean", new Point(185, 922) },
        { "cancel_clean", new Point(942, 922) },
        { "start_clean", new Point(610, 922) },
        { "in_bg_on", new Point(185, 867) },
        { "in_bg_off", new Point(185, 867) },
        { "unit_bg_on", new Point(404, 867) },
        { "unit_bg_off", new Point(404, 867) },
        { "q15_bg_on", new Point(505, 867) },
        { "q15_bg_off", new Point(505, 867) },
        { "q30_bg_on", new Point(610, 867) },
        { "q30_bg_off", new Point(610, 867) },
        { "q60_bg_on", new Point(726, 867) },
        { "q60_bg_off", new Point(726, 867) },
        { "q120_bg_on", new Point(834, 867) },
        { "q120_bg_off", new Point(834, 867) },
        { "at_bg_on", new Point(942, 867) },
        { "at_bg_off", new Point(942, 867) },
        { "num_clean", new Point(289, 867) },
        { "time_clean", new Point(1045, 867) },
        { "panel", new Point(628, 548) },
        { "status_clean", new Point(552, 836) },
    };
    public static readonly int[] DigitX = { 0, 0, 0, 0, 0, 0 }; public const int DigitY = 0; // Ursprung je Ziffer (Quellpixel)
    public const int DigitX0 = 0; // Ursprung der Vorlagen-Ziffer
    public static readonly int[] ColonX = { 0, 0 };
    // Knopf-Rechtecke (logisch, 960er Raster)
    public static readonly RectangleF R_in = new RectangleF(121.80f, 547.77f, 48.09f, 26.86f);
    public static readonly RectangleF R_unit = new RectangleF(258.58f, 547.77f, 53.72f, 26.86f);
    public static readonly RectangleF R_q15 = new RectangleF(321.67f, 547.77f, 56.21f, 26.86f);
    public static readonly RectangleF R_q30 = new RectangleF(387.25f, 547.77f, 63.71f, 26.86f);
    public static readonly RectangleF R_q60 = new RectangleF(459.70f, 547.77f, 57.46f, 26.86f);
    public static readonly RectangleF R_q120 = new RectangleF(527.16f, 547.77f, 56.84f, 26.86f);
    public static readonly RectangleF R_at = new RectangleF(594.61f, 547.77f, 54.96f, 26.86f);
    public static readonly RectangleF R_num = new RectangleF(186.75f, 547.77f, 61.83f, 26.86f);
    public static readonly RectangleF R_time = new RectangleF(658.95f, 547.77f, 74.95f, 26.86f);
    public static readonly RectangleF R_empty = new RectangleF(742.64f, 547.77f, 89.32f, 26.86f);
    public static readonly RectangleF R_act = new RectangleF(121.80f, 582.12f, 255.46f, 31.23f);
    public static readonly RectangleF R_start = new RectangleF(387.25f, 582.12f, 197.37f, 31.23f);
    public static readonly RectangleF R_cancel = new RectangleF(594.61f, 582.12f, 237.35f, 31.23f);
}
