using System.Drawing;
using System.Drawing.Drawing2D;

namespace Slapon.Core.Models;
using Slapon.Core.Interfaces;

public class TextAnnotation : BaseAnnotation
{
    private string _text;
    private readonly Font _font;

    public TextAnnotation(Point location, Color color, string text)
        : base(GetInitialBounds(location, text), color, 1.0f)
    {
        _text = text;
        _font = new Font("Arial", 12);
        UpdateBounds(location);
    }

    private static RectangleF GetInitialBounds(Point location, string text)
    {
        // Create initial bounds - will be updated when text is set
        return new RectangleF(location, new SizeF(100, 20));
    }

    public void SetText(string text, Point location)
    {
        _text = text;
        UpdateBounds(location);
    }

    private void UpdateBounds(Point location)
    {
        // Update bounds based on text content
        var size = TextRenderer.MeasureText(_text, _font);
        Bounds = new RectangleF(location, size);
    }

    public override void Draw(Graphics g)
    {
        if (!string.IsNullOrEmpty(_text))
        {
            using var brush = new SolidBrush(GetTransparentColor());
            g.DrawString(_text, _font, brush, Bounds.Location);
        }
    }

    public override bool Contains(PointF point)
    {
        var inflatedBounds = Bounds;
        inflatedBounds.Inflate(2, 2);
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