using System.Drawing;
using Slapon.Core.Models;
using Slapon.Core.Interfaces;
using Slapon.Core.Services;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace Slapon.Core.Interfaces;

public interface IAnnotationFactory
{
    IAnnotation CreateAnnotation(AnnotationType type, RectangleF bounds, Color color, float opacity = 0.8f);
}