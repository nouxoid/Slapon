using System.Drawing;
using Slapon.Core.Models;
using Slapon.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Slapon.Core.Interfaces;

public interface IAnnotationService
{
    IReadOnlyList<IAnnotation> Annotations { get; }
    IAnnotation? SelectedAnnotation { get; }
    void AddAnnotation(IAnnotation annotation);
    void RemoveAnnotation(IAnnotation annotation);
    void ClearAnnotations();
    IAnnotation? GetAnnotationAt(PointF point);
    void SelectAnnotation(IAnnotation? annotation);
    void MoveSelectedAnnotation(PointF newLocation);
    event EventHandler<EventArgs>? AnnotationsChanged;
    IAnnotation? GetAnnotationAt(Point point);

    
}
