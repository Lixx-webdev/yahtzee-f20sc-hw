using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Yahtzee.WinForms.Controls;

/// <summary>
/// A clickable, custom-drawn die. Held dice are drawn in gold with a "HELD" caption; dice that
/// will be re-rolled show "WILL ROLL", so it is always clear which dice the next roll affects.
/// </summary>
internal sealed class DieControl : Control
{
    /// <summary>
    /// Pip positions for each face on a 3x3 grid (column, row), indexed by face value.
    /// Index 0 is unused.
    /// </summary>
    private static readonly (int Col, int Row)[][] PipLayout =
    [
        [],
        [(1, 1)],
        [(0, 0), (2, 2)],
        [(0, 0), (1, 1), (2, 2)],
        [(0, 0), (2, 0), (0, 2), (2, 2)],
        [(0, 0), (2, 0), (1, 1), (0, 2), (2, 2)],
        [(0, 0), (2, 0), (0, 1), (2, 1), (0, 2), (2, 2)],
    ];

    private int _value;
    private bool _held;
    private bool _showRollCaption;

    public DieControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = true;
        Size = new Size(76, 96);
        Margin = new Padding(6);
    }

    /// <summary>The face shown, 1–6, or 0 before the first roll of a turn.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set { if (_value != value) { _value = value; Invalidate(); } }
    }

    /// <summary>Whether the die is set aside and will not be re-rolled.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Held
    {
        get => _held;
        set { if (_held != value) { _held = value; Invalidate(); } }
    }

    /// <summary>Whether another roll is available, so non-held dice should be labelled "WILL ROLL".</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowRollCaption
    {
        get => _showRollCaption;
        set { if (_showRollCaption != value) { _showRollCaption = value; Invalidate(); } }
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }

    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    /// <summary>Space or Enter toggles the die, like clicking it.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int captionHeight = LogicalToDeviceUnits(20);
        int side = Math.Min(Width, Height - captionHeight) - 4;
        var faceRect = new Rectangle((Width - side) / 2, 2, side, side);

        DrawFace(g, faceRect);
        DrawCaption(g, new Rectangle(0, faceRect.Bottom + 2, Width, captionHeight));
    }

    private void DrawFace(Graphics g, Rectangle rect)
    {
        using var path = RoundedRectangle(rect, rect.Width / 6);
        Color face = _held ? UiTheme.HeldFace : UiTheme.DieFace;
        if (!Enabled && _value != 0)
            face = ControlPaint.Light(face, 0.1f);

        using (var fill = new SolidBrush(face))
            g.FillPath(fill, path);
        using (var border = new Pen(_held ? UiTheme.HeldBorder : UiTheme.DieBorder, _held ? 4f : 2f))
            g.DrawPath(border, path);

        if (Focused)
        {
            var focusRect = Rectangle.Inflate(rect, -6, -6);
            ControlPaint.DrawFocusRectangle(g, focusRect);
        }

        if (_value == 0)
        {
            TextRenderer.DrawText(g, "?", UiTheme.Heading, rect, Color.Gray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        float cell = rect.Width / 4f;
        float pipRadius = rect.Width / 11f;
        using var pipBrush = new SolidBrush(UiTheme.Pip);
        foreach (var (col, row) in PipLayout[_value])
        {
            float cx = rect.Left + cell * (col + 1);
            float cy = rect.Top + cell * (row + 1);
            g.FillEllipse(pipBrush, cx - pipRadius, cy - pipRadius, pipRadius * 2, pipRadius * 2);
        }
    }

    private void DrawCaption(Graphics g, Rectangle rect)
    {
        (string text, Color color) = (_value, _held, _showRollCaption) switch
        {
            (0, _, _) => ("", Color.Empty),
            (_, true, _) => ("HELD", UiTheme.HeldFace),
            (_, false, true) => ("WILL ROLL", UiTheme.FeltText),
            _ => ("", Color.Empty),
        };
        if (text.Length > 0)
            TextRenderer.DrawText(g, text, UiTheme.DieCaption, rect, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static GraphicsPath RoundedRectangle(Rectangle rect, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
