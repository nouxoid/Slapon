using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using Slapon.Core.Services;
using System.Drawing.Imaging;

namespace Slapon.Core.Models;

public abstract class BaseAnnotation : IAnnotation
{
    protected BaseAnnotation(RectangleF bounds, Color color, float opacity, float thickness = 2.0f)
    {
        Id = Guid.NewGuid();
        Bounds = bounds;
        Color = color;
        Opacity = opacity;
        Thickness = thickness;
        IsSelected = false;
    }

    public Guid Id { get; }
    public RectangleF Bounds { get; protected set; }
    public Color Color { get; protected set; }
    public float Opacity { get; protected set; }
    public float Thickness { get; set; }
    public bool IsSelected { get; set; }

    public abstract bool Contains(Point point);

    public abstract void Draw(Graphics g);
    
    /// <summary>
    /// Draws the annotation with optional selection indicators
    /// </summary>
    /// <param name="g">Graphics context</param>
    /// <param name="showSelection">Whether to show selection indicators</param>
    public virtual void Draw(Graphics g, bool showSelection)
    {
        // Temporarily store the selection state
        bool originalSelectionState = IsSelected;
        
        // Set selection state based on parameter
        if (!showSelection)
        {
            IsSelected = false;
        }
        
        try
        {
            // Call the regular Draw method
            Draw(g);
        }
        finally
        {
            // Restore original selection state
            IsSelected = originalSelectionState;
        }
    }
    public abstract bool Contains(PointF point);
    public abstract IAnnotation Clone();

    public virtual void MoveTo(PointF location)
    {
        Bounds = new RectangleF(location, Bounds.Size);
    }

    public virtual void Move(int deltaX, int deltaY)
    {
        Bounds = new RectangleF(
            Bounds.X + deltaX,
            Bounds.Y + deltaY,
            Bounds.Width,
            Bounds.Height
        );
    }

    protected Color GetTransparentColor()
    {
        return Color.FromArgb((int)(Opacity * 255), Color);
    }

    public virtual void Resize(float scaleX, float scaleY)
    {
        Bounds = new RectangleF(
            Bounds.X * scaleX,
            Bounds.Y * scaleY,
            Bounds.Width * scaleX,
            Bounds.Height * scaleY
        );
    }

    /// <summary>
    /// Updates the color of the annotation
    /// </summary>
    /// <param name="color">The new color for the annotation</param>
    public virtual void UpdateColor(Color color)
    {
        Color = color;
    }

    /// <summary>
    /// Draws selection indicators around the annotation when it's selected
    /// </summary>
    protected virtual void DrawSelectionIndicators(Graphics g)
    {
        if (!IsSelected) return;

        // Selection border - make it more visually distinct by inflating more and using a thicker line
        using var selectionPen = new Pen(Color.FromArgb(100, 181, 246), 2f) { DashStyle = DashStyle.Dash };
        var selectionRect = Rectangle.Round(Bounds);
        selectionRect.Inflate(6, 6); // Increased from 4 to 6 for better visibility
        g.DrawRectangle(selectionPen, selectionRect);

        // Resize handles
        DrawResizeHandles(g, selectionRect);
    }

    /// <summary>
    /// Draws resize handles at the corners of the selection rectangle
    /// </summary>
    protected virtual void DrawResizeHandles(Graphics g, Rectangle bounds)
    {
        const int handleSize = 8; // Increased from 6 to 8 for better visibility
        var handleColor = Color.FromArgb(100, 181, 246);
        
        using var brush = new SolidBrush(handleColor);
        using var pen = new Pen(Color.White, 1);

        var handles = new[]
        {
            new Point(bounds.Left - handleSize/2, bounds.Top - handleSize/2),
            new Point(bounds.Right - handleSize/2, bounds.Top - handleSize/2),
            new Point(bounds.Right - handleSize/2, bounds.Bottom - handleSize/2),
            new Point(bounds.Left - handleSize/2, bounds.Bottom - handleSize/2)
        };

        foreach (var handle in handles)
        {
            var handleRect = new Rectangle(handle, new Size(handleSize, handleSize));
            g.FillEllipse(brush, handleRect);
            g.DrawEllipse(pen, handleRect);
        }
    }

    /// <summary>
    /// Draws selection indicators for line-based annotations (lines and arrows)
    /// </summary>
    protected virtual void DrawLineSelectionIndicators(Graphics g, Point start, Point end)
    {
        if (!IsSelected) return;

        // Draw a wider selection outline around the line for better visibility
        using var selectionPen = new Pen(Color.FromArgb(100, 181, 246), 5f) { DashStyle = DashStyle.Dash };
        g.DrawLine(selectionPen, start, end);

        // End point handles - make them larger and more visible
        DrawLineEndHandles(g, start, end);
    }

    /// <summary>
    /// Draws handles at the start and end points of a line
    /// </summary>
    protected virtual void DrawLineEndHandles(Graphics g, Point start, Point end)
    {
        const int handleSize = 10; // Increased from 8 to 10 for better visibility
        var handleColor = Color.FromArgb(100, 181, 246);
        
        using var brush = new SolidBrush(handleColor);
        using var pen = new Pen(Color.White, 1);

        var handles = new[] { start, end };

        foreach (var handle in handles)
        {
            var handleRect = new Rectangle(
                handle.X - handleSize/2, 
                handle.Y - handleSize/2, 
                handleSize, 
                handleSize);
            g.FillEllipse(brush, handleRect);
            g.DrawEllipse(pen, handleRect);
        }
    }

    /// <summary>
    /// Gets the resize handle at the specified point
    /// Default implementation for rectangular annotations
    /// </summary>
    public virtual ResizeHandle GetResizeHandle(Point point)
    {
        if (!IsSelected) return ResizeHandle.None;

        const int handleSize = 8;
        var bounds = Rectangle.Round(Bounds);
        bounds.Inflate(6, 6); // Match the inflation used in DrawSelectionIndicators

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

    /// <summary>
    /// Resizes the annotation to the specified handle position
    /// Default implementation for rectangular annotations
    /// </summary>
    public virtual void ResizeToHandle(ResizeHandle handle, Point newPosition)
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

        // Ensure minimum size
        if (Bounds.Width < 5)
        {
            if (handle == ResizeHandle.TopLeft || handle == ResizeHandle.BottomLeft)
                Bounds = new RectangleF(currentBounds.Right - 5, Bounds.Y, 5, Bounds.Height);
            else
                Bounds = new RectangleF(Bounds.X, Bounds.Y, 5, Bounds.Height);
        }
        if (Bounds.Height < 5)
        {
            if (handle == ResizeHandle.TopLeft || handle == ResizeHandle.TopRight)
                Bounds = new RectangleF(Bounds.X, currentBounds.Bottom - 5, Bounds.Width, 5);
            else
                Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, 5);
        }
    }

    /// <summary>
    /// Enhanced HitTest that includes resize handles
    /// </summary>
    public override bool HitTest(Point point)
    {
        return Contains(point) || GetResizeHandle(point) != ResizeHandle.None;
    }
}