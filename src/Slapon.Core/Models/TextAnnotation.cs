using System.Drawing;
using System.Drawing.Drawing2D;

namespace Slapon.Core.Models;
using Slapon.Core.Interfaces;

public class TextAnnotation : BaseAnnotation
{
    private readonly string _text;
    private readonly Font _font;
    private readonly Size _textSize;

    public TextAnnotation(Point location, Color color, string text)
        : base(GetInitialBounds(location, text), color, 1.0f)
    {
        _text = text;
        _font = new Font("Arial", 12, FontStyle.Regular);
        _textSize = TextRenderer.MeasureText(text, _font);
        UpdateBounds(location);
    }

    private static RectangleF GetInitialBounds(Point location, string text)
    {
        using var tempFont = new Font("Arial", 12, FontStyle.Regular);
        var size = TextRenderer.MeasureText(text, tempFont);
        return new RectangleF(location, size);
    }

    private void UpdateBounds(Point location)
    {
        Bounds = new RectangleF(location, _textSize);
    }

    public override void Draw(Graphics g)
    {
        if (!string.IsNullOrEmpty(_text))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var brush = new SolidBrush(GetTransparentColor());
            TextRenderer.DrawText(g, _text, _font, Point.Round(Bounds.Location), Color);
        }

        if (IsSelected)
        {
            using var pen = new Pen(Color.Blue, 1f) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(pen, Rectangle.Round(Bounds));
        }
    }

    public override bool Contains(PointF point)
    {
        var inflatedBounds = Bounds;
        inflatedBounds.Inflate(4, 4); // Slightly larger hit area for easier selection
        return inflatedBounds.Contains(point);
    }

    public override bool Contains(Point point)
    {
        return Contains(new PointF(point.X, point.Y));
    }

    public override IAnnotation Clone()
    {
        return new TextAnnotation(Point.Round(Bounds.Location), Color, _text)
        {
            IsSelected = IsSelected
        };
    }
}