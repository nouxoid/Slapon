using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Models;

public class ArrowAnnotation : BaseAnnotation
{
    private readonly Point _originalStart;
    private readonly Point _originalEnd;
    private Point _start;
    private Point _end;
    public float ArrowHeadSize { get; set; } = 12f;

    public ArrowAnnotation(Point start, Point end, Color color, float thickness = 3f)
        : base(GetBounds(start, end), color, 1.0f, thickness)
    {
        _originalStart = start;
        _originalEnd = end;
        _start = start;
        _end = end;
    }

    private static RectangleF GetBounds(Point start, Point end)
    {
        // Add some padding for the arrow head
        var padding = 15;
        return new RectangleF(
            Math.Min(start.X, end.X) - padding,
            Math.Min(start.Y, end.Y) - padding,
            Math.Abs(end.X - start.X) + padding * 2,
            Math.Abs(end.Y - start.Y) + padding * 2
        );
    }

    public override void Draw(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        
        using var pen = new Pen(GetTransparentColor(), Thickness);
        pen.EndCap = LineCap.Custom;
        
        // Create arrow head
        var arrowHead = new AdjustableArrowCap(ArrowHeadSize / 3, ArrowHeadSize / 2);
        pen.CustomEndCap = arrowHead;
        
        // Draw the arrow line
        g.DrawLine(pen, _start, _end);
        
        arrowHead.Dispose();

        // Draw selection indicators if selected (using base class method for consistency)
        DrawLineSelectionIndicators(g, _start, _end);
    }

    public override bool Contains(PointF point)
    {
        // Create a slightly wider hit area for the arrow
        var inflatedBounds = Bounds;
        inflatedBounds.Inflate(Thickness * 2, Thickness * 2);
        return inflatedBounds.Contains(point);
    }

    public override bool Contains(Point point)
    {
        return Contains(new PointF(point.X, point.Y));
    }

    public override IAnnotation Clone()
    {
        return new ArrowAnnotation(_start, _end, Color, Thickness)
        {
            ArrowHeadSize = ArrowHeadSize,
            IsSelected = IsSelected
        };
    }

    public override void Move(int deltaX, int deltaY)
    {
        _start = new Point(_start.X + deltaX, _start.Y + deltaY);
        _end = new Point(_end.X + deltaX, _end.Y + deltaY);
        Bounds = GetBounds(_start, _end);
    }

    public override bool HitTest(Point point)
    {
        // Create a path for the arrow line with wider hit area
        using var path = new GraphicsPath();
        path.AddLine(_start, _end);
        using var pen = new Pen(Color.Black, Thickness * 3); // Wider hit area for easier selection
        return path.IsOutlineVisible(point, pen);
    }

    public override void Resize(float scaleX, float scaleY)
    {
        _start = new Point((int)(_originalStart.X * scaleX), (int)(_originalStart.Y * scaleY));
        _end = new Point((int)(_originalEnd.X * scaleX), (int)(_originalEnd.Y * scaleY));
        Bounds = GetBounds(_start, _end);
        Thickness *= Math.Min(scaleX, scaleY);
        ArrowHeadSize *= Math.Min(scaleX, scaleY);
    }
}