namespace Yahtzee.WinForms;

/// <summary>Shared colours and fonts so every form has a consistent look.</summary>
internal static class UiTheme
{
    public static readonly Color Felt = Color.FromArgb(22, 94, 60);
    public static readonly Color FeltText = Color.WhiteSmoke;
    public static readonly Color DieFace = Color.White;
    public static readonly Color DieBorder = Color.FromArgb(60, 60, 60);
    public static readonly Color Pip = Color.FromArgb(30, 30, 30);
    public static readonly Color HeldFace = Color.FromArgb(255, 236, 160);
    public static readonly Color HeldBorder = Color.FromArgb(230, 160, 0);
    public static readonly Color Accent = Color.FromArgb(0, 102, 204);
    public static readonly Color CurrentPlayerHeader = Color.FromArgb(255, 220, 120);
    public static readonly Color HeaderBack = Color.FromArgb(235, 235, 235);
    public static readonly Color PreviewBack = Color.FromArgb(232, 243, 255);
    public static readonly Color PreviewZero = Color.FromArgb(150, 150, 150);
    public static readonly Color TotalBack = Color.FromArgb(245, 245, 245);

    public static readonly Font Body = new("Segoe UI", 10F);
    public static readonly Font Bold = new("Segoe UI", 10F, FontStyle.Bold);
    public static readonly Font Heading = new("Segoe UI Semibold", 16F);
    public static readonly Font Subheading = new("Segoe UI", 11F);
    public static readonly Font DieCaption = new("Segoe UI", 8F, FontStyle.Bold);
}
