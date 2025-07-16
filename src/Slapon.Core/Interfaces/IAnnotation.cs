using System.Drawing;
using Slapon.Core.Models;

namespace Slapon.Core.Interfaces;

public interface IAnnotation
{
    Guid Id { get; }
    RectangleF Bounds { get; }
    float Opacity { get; }
    Color Color { get; }
    bool IsSelected { get; set; }
    float Thickness { get; set; }
    void Draw(Graphics g);
    void Draw(Graphics g, bool showSelection);
    bool Contains(PointF point);
    void MoveTo(PointF location);
    IAnnotation Clone();
    bool Contains(Point point);
    bool HitTest(Point point);
    void Move(int deltaX, int deltaY);
    void Resize(float widthScale, float heightScale);
    void UpdateColor(Color color);
    
    // Resize handle methods for interactive resizing
    ResizeHandle GetResizeHandle(Point point);
    void ResizeToHandle(ResizeHandle handle, Point newPosition);
}