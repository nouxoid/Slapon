using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Models;

public class CircleAnnotation : BaseAnnotation
{
    private readonly RectangleF _originalBounds;

    public CircleAnnotation(RectangleF bounds, Color color, float opacity, float thickness = 2f)
        : base(bounds, color, opacity, thickness)
    {
        _originalBounds = bounds;
    }

    public override void Draw(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        
        using var pen = new Pen(GetTransparentColor(), Thickness);

        // Check for valid bounds
        if (Bounds.Width > 0 && Bounds.Height > 0 &&
            !float.IsNaN(Bounds.X) && !float.IsNaN(Bounds.Y) &&
            !float.IsInfinity(Bounds.X) && !float.IsInfinity(Bounds.Y) &&
            !float.IsNaN(Bounds.Width) && !float.IsNaN(Bounds.Height) &&
            !float.IsInfinity(Bounds.Width) && !float.IsInfinity(Bounds.Height))
        {
            g.DrawEllipse(pen, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
        }

        // Draw selection indicators if selected (using base class method for consistency)
        DrawSelectionIndicators(g);
    }

    public override bool Contains(PointF point)
    {
        // For a circle, we need to check if the point is inside the ellipse
        // Calculate the center and radii
        var centerX = Bounds.X + Bounds.Width / 2;
        var centerY = Bounds.Y + Bounds.Height / 2;
        var radiusX = Bounds.Width / 2;
        var radiusY = Bounds.Height / 2;

        // Check if point is inside the ellipse using the standard ellipse equation
        // ((x-h)/a)^2 + ((y-k)/b)^2 <= 1, where (h,k) is center and a,b are radii
        if (radiusX == 0 || radiusY == 0) return false;
        
        var dx = point.X - centerX;
        var dy = point.Y - centerY;
        var ellipseValue = (dx * dx) / (radiusX * radiusX) + (dy * dy) / (radiusY * radiusY);

        // Add some tolerance for border thickness
        var tolerance = Math.Max(Thickness / radiusX, Thickness / radiusY);
        return ellipseValue <= (1 + tolerance);
    }

    public override bool Contains(Point point)
    {
        return Contains(new PointF(point.X, point.Y));
    }

    // Override HitTest to use the ellipse-specific hit detection
    public override bool HitTest(Point point)
    {
        return Contains(point);
    }

    public override IAnnotation Clone()
    {
        return new CircleAnnotation(Bounds, Color, Opacity, Thickness)
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