using Slapon.Core.Interfaces;
using Slapon.Core.Models;

public class RectangleAnnotation : BaseAnnotation
{
    private readonly RectangleF _originalBounds;

    public RectangleAnnotation(RectangleF bounds, Color color, float opacity, float thickness = 2f)
        : base(bounds, color, opacity, thickness)
    {
        _originalBounds = bounds;
    }

    public override void Draw(Graphics g)
    {
        using var pen = new Pen(GetTransparentColor(), Thickness);

        // Check for valid bounds
        if (Bounds.Width > 0 && Bounds.Height > 0 &&
            !float.IsNaN(Bounds.X) && !float.IsNaN(Bounds.Y) &&
            !float.IsInfinity(Bounds.X) && !float.IsInfinity(Bounds.Y) &&
            !float.IsNaN(Bounds.Width) && !float.IsNaN(Bounds.Height) &&
            !float.IsInfinity(Bounds.Width) && !float.IsInfinity(Bounds.Height))
        {
            g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
        }

        // Draw selection indicators if selected (using base class method for consistency)
        DrawSelectionIndicators(g);
    }

    public override bool Contains(PointF point)
    {
        var inflatedBounds = Bounds;
        inflatedBounds.Inflate(Thickness, Thickness);
        return inflatedBounds.Contains(point);
    }

    public override bool Contains(Point point)
    {
        return Contains(new PointF(point.X, point.Y));
    }

    // Override HitTest to use the inflated bounds for better hit detection
    public override bool HitTest(Point point)
    {
        return Contains(point);
    }

    public override IAnnotation Clone()
    {
        return new RectangleAnnotation(Bounds, Color, Opacity, Thickness)
        {
            IsSelected = IsSelected
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
        Thickness *= Math.Min(scaleX, scaleY);
    }
}