using System.Drawing;
using Slapon.Core.Models;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Services;

public class AnnotationFactory : IAnnotationFactory
{
    public IAnnotation CreateAnnotation(AnnotationType type, RectangleF bounds, Color color, float opacity = 0.8f)
    {
        return type switch
        {
            AnnotationType.Rectangle => new RectangleAnnotation(bounds, color, opacity),
            // We'll add other types later
            _ => throw new ArgumentException($"Unknown annotation type: {type}", nameof(type))
        };
    }
}