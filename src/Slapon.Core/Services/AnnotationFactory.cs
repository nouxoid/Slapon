using System.Drawing;
using Slapon.Core.Models;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Services;

public class AnnotationFactory : IAnnotationFactory
{
    public IAnnotation CreateAnnotation(AnnotationType type, RectangleF bounds, Color color, float opacity = 0.8f, float thickness = 2.0f)
    {
        return type switch
        {
            AnnotationType.Rectangle => new RectangleAnnotation(bounds, color, opacity, thickness),
            AnnotationType.Circle => new CircleAnnotation(bounds, color, opacity, thickness),
            // We'll add other types later
            _ => throw new ArgumentException($"Unknown annotation type: {type}", nameof(type))
        };
    }
}