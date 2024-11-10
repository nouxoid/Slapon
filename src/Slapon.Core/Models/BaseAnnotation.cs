using System.Drawing;
using System.Drawing.Drawing2D;
using Slapon.Core.Interfaces;
using Slapon.Core.Models;
using Slapon.Core.Services;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Slapon.Core.Models;

public abstract class BaseAnnotation : IAnnotation
{
    protected BaseAnnotation(RectangleF bounds, Color color, float opacity)
    {
        Id = Guid.NewGuid();
        Bounds = bounds;
        Color = color;
        Opacity = opacity;
        IsSelected = false;
    }

    public Guid Id { get; }
    public RectangleF Bounds { get; protected set; }
    public Color Color { get; protected set; }
    public float Opacity { get; protected set; }
    public bool IsSelected { get; set; }

    public abstract bool Contains(Point point);

    public abstract void Draw(Graphics g);
    public abstract bool Contains(PointF point);
    public abstract IAnnotation Clone();

    public virtual void MoveTo(PointF location)
    {
        Bounds = new RectangleF(location, Bounds.Size);
    }

    protected Color GetTransparentColor()
    {
        return Color.FromArgb((int)(Opacity * 255), Color);
    }
}