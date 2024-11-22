using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Models;

public class TextAnnotation : BaseAnnotation
{
    private readonly string _text;
    private readonly Font _font;
    private Size _textSize;
    private PointF _originalLocation;
    private Size _originalSize;

    public TextAnnotation(PointF location, Color color, string text)
    : base(new RectangleF(location, TextRenderer.MeasureText(text, new Font("Arial", 12))), color, 1.0f)
    {
        _font = new Font("Arial", 12);
        _originalLocation = location;
        _originalSize = TextRenderer.MeasureText(text, _font);
        _textSize = _originalSize;
        UpdateBounds(location);
    }


    private static RectangleF GetInitialBounds(Point location, string text)
    {
        using var tempFont = new Font("Arial", 12, FontStyle.Regular);
        var size = TextRenderer.MeasureText(text, tempFont);
        return new RectangleF(location, size);
    }

    private void UpdateBounds(PointF location)
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

    public override bool HitTest(Point point)
    {
        return Contains(point);
    }

    public override IAnnotation Clone()
    {
        return new TextAnnotation(Point.Round(Bounds.Location), Color, _text)
        {
            IsSelected = IsSelected
        };
    }

    public override void Resize(float scaleX, float scaleY)
    {
        var location = new PointF(_originalLocation.X * scaleX, _originalLocation.Y * scaleY);
        _textSize = new Size((int)(_originalSize.Width * scaleX), (int)(_originalSize.Height * scaleY));
        UpdateBounds(location);
        Bounds = new RectangleF(location, _textSize);
    }
}