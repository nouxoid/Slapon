using Slapon.Core.Interfaces;
using Slapon.Core.Models;

public class RectangleAnnotation : BaseAnnotation
{
    public float BorderThickness { get; set; } = 2f;

    public RectangleAnnotation(RectangleF bounds, Color color, float opacity)
        : base(bounds, color, opacity)
    {
    }

    public override void Draw(Graphics g)
    {
        using var pen = new Pen(GetTransparentColor(), BorderThickness);

        // Check for valid bounds
        if (Bounds.Width > 0 && Bounds.Height > 0 &&
            !float.IsNaN(Bounds.X) && !float.IsNaN(Bounds.Y) &&
            !float.IsInfinity(Bounds.X) && !float.IsInfinity(Bounds.Y) &&
            !float.IsNaN(Bounds.Width) && !float.IsNaN(Bounds.Height) &&
            !float.IsInfinity(Bounds.Width) && !float.IsInfinity(Bounds.Height))
        {
            g.DrawRectangle(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
        }
    }

    public override bool Contains(PointF point)
    {
        var inflatedBounds = Bounds;
        inflatedBounds.Inflate(BorderThickness, BorderThickness);
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
        return new RectangleAnnotation(Bounds, Color, Opacity)
        {
            BorderThickness = BorderThickness,
            IsSelected = IsSelected
        };
    }

    public override void Resize(float scaleX, float scaleY)
    {
        base.Resize(scaleX, scaleY);
        BorderThickness *= Math.Min(scaleX, scaleY);
    }
}