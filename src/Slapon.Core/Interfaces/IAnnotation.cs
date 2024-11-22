using System.Drawing;

namespace Slapon.Core.Interfaces;

public interface IAnnotation
{
    Guid Id { get; }
    RectangleF Bounds { get; }
    float Opacity { get; }
    Color Color { get; }
    bool IsSelected { get; set; }
    void Draw(Graphics g);
    bool Contains(PointF point);
    void MoveTo(PointF location);
    IAnnotation Clone();
    bool Contains(Point point);
    bool HitTest(Point point);
    void Move(int deltaX, int deltaY);
    void Resize(float widthScale, float heightScale);
}