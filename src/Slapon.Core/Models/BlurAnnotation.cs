using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Models;

public class BlurAnnotation : BaseAnnotation
{
    private readonly RectangleF _originalBounds;

    public BlurAnnotation(RectangleF bounds)
        : base(bounds, Color.Gray, 1.0f)
    {
        _originalBounds = bounds;
    }

    public override void Draw(Graphics g)
    {
        DrawBlurEffect(g);

        // Draw selection indicators and resize handles
        if (IsSelected)
        {
            DrawSelectionIndicators(g);
        }
    }

    private void DrawBlurEffect(Graphics g)
    {
        var blurRect = Rectangle.Round(Bounds);
        
        // Apply a frosted glass effect
        using var brush = new SolidBrush(Color.FromArgb(180, 240, 240, 240));
        g.FillRectangle(brush, blurRect);
        
        // Add noise pattern for blur effect
        var random = new Random(42); // Fixed seed for consistent pattern
        for (int i = 0; i < 50; i++)
        {
            var x = random.Next(blurRect.Left, blurRect.Right);
            var y = random.Next(blurRect.Top, blurRect.Bottom);
            var size = random.Next(2, 6);
            var alpha = random.Next(30, 100);
            
            using var noiseBrush = new SolidBrush(Color.FromArgb(alpha, 200, 200, 200));
            g.FillEllipse(noiseBrush, x, y, size, size);
        }
        
        // Add border to indicate blur area
        using var borderPen = new Pen(Color.FromArgb(120, 150, 150, 150), 1f) { DashStyle = DashStyle.Dot };
        g.DrawRectangle(borderPen, blurRect);
    }

    private void DrawSelectionIndicators(Graphics g)
    {
        // Selection border
        using var selectionPen = new Pen(Color.FromArgb(100, 181, 246), 1f) { DashStyle = DashStyle.Dash };
        var selectionRect = Rectangle.Round(Bounds);
        selectionRect.Inflate(4, 4);
        g.DrawRectangle(selectionPen, selectionRect);

        // Resize handles
        DrawResizeHandles(g, selectionRect);
    }

    private void DrawResizeHandles(Graphics g, Rectangle bounds)
    {
        const int handleSize = 8;
        var handleColor = Color.FromArgb(100, 181, 246);
        
        using var brush = new SolidBrush(handleColor);
        using var pen = new Pen(Color.White, 1);

        var handles = new[]
        {
            new Point(bounds.Left - handleSize/2, bounds.Top - handleSize/2),      // TopLeft
            new Point(bounds.Right - handleSize/2, bounds.Top - handleSize/2),     // TopRight
            new Point(bounds.Right - handleSize/2, bounds.Bottom - handleSize/2),  // BottomRight
            new Point(bounds.Left - handleSize/2, bounds.Bottom - handleSize/2)    // BottomLeft
        };

        foreach (var handle in handles)
        {
            var handleRect = new Rectangle(handle, new Size(handleSize, handleSize));
            g.FillRectangle(brush, handleRect);
            g.DrawRectangle(pen, handleRect);
        }
    }

    public ResizeHandle GetResizeHandle(Point point)
    {
        if (!IsSelected) return ResizeHandle.None;

        const int handleSize = 8;
        var bounds = Rectangle.Round(Bounds);
        bounds.Inflate(4, 4);

        var handles = new[]
        {
            (ResizeHandle.TopLeft, new Rectangle(bounds.Left - handleSize/2, bounds.Top - handleSize/2, handleSize, handleSize)),
            (ResizeHandle.TopRight, new Rectangle(bounds.Right - handleSize/2, bounds.Top - handleSize/2, handleSize, handleSize)),
            (ResizeHandle.BottomRight, new Rectangle(bounds.Right - handleSize/2, bounds.Bottom - handleSize/2, handleSize, handleSize)),
            (ResizeHandle.BottomLeft, new Rectangle(bounds.Left - handleSize/2, bounds.Bottom - handleSize/2, handleSize, handleSize))
        };

        foreach (var (handle, rect) in handles)
        {
            if (rect.Contains(point))
                return handle;
        }

        return ResizeHandle.None;
    }

    public void ResizeToHandle(ResizeHandle handle, Point newPosition)
    {
        var currentBounds = Bounds;
        
        switch (handle)
        {
            case ResizeHandle.TopLeft:
                Bounds = new RectangleF(
                    newPosition.X,
                    newPosition.Y,
                    currentBounds.Right - newPosition.X,
                    currentBounds.Bottom - newPosition.Y);
                break;
            case ResizeHandle.TopRight:
                Bounds = new RectangleF(
                    currentBounds.X,
                    newPosition.Y,
                    newPosition.X - currentBounds.X,
                    currentBounds.Bottom - newPosition.Y);
                break;
            case ResizeHandle.BottomRight:
                Bounds = new RectangleF(
                    currentBounds.X,
                    currentBounds.Y,
                    newPosition.X - currentBounds.X,
                    newPosition.Y - currentBounds.Y);
                break;
            case ResizeHandle.BottomLeft:
                Bounds = new RectangleF(
                    newPosition.X,
                    currentBounds.Y,
                    currentBounds.Right - newPosition.X,
                    newPosition.Y - currentBounds.Y);
                break;
        }
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
        return Contains(point) || GetResizeHandle(point) != ResizeHandle.None;
    }

    public override IAnnotation Clone()
    {
        return new BlurAnnotation(Bounds)
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