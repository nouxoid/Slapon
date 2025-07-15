using System.Drawing;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Models;

public class HighlightAnnotation : BaseAnnotation
{
    private readonly RectangleF _originalBounds;

    public HighlightAnnotation(RectangleF bounds, Color color, float opacity = 0.4f, float thickness = 1.0f)
        : base(bounds, color, opacity, thickness)
    {
        _originalBounds = bounds;
    }

    public override void Draw(Graphics g)
    {
        using (var highlightBrush = new SolidBrush(GetTransparentColor()))
        {
            g.FillRectangle(highlightBrush, Bounds);
        }

        // Draw selection indicators if selected
        DrawSelectionIndicators(g);
    }

    public override bool Contains(PointF point)
    {
        return Bounds.Contains(point);
    }

    public override bool Contains(Point point)
    {
        return Bounds.Contains(point);
    }

    public override bool HitTest(Point point)
    {
        return Contains(point);
    }

    public override IAnnotation Clone()
    {
        return new HighlightAnnotation(Bounds, Color, Opacity, Thickness)
        {
            IsSelected = this.IsSelected
        };
    }

    public override void Resize(float scaleX, float scaleY)
    {
        Bounds = new RectangleF(
            _originalBounds.X * scaleX,
            _originalBounds.Y * scaleY,
            _originalBounds.Width * scaleX,
            _originalBounds.Height * scaleY
        );
    }
}