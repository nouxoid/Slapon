using System.Drawing;
using Slapon.Core.Models;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Services;

public class AnnotationService : IAnnotationService
{
    // Private fields
    private readonly List<IAnnotation> _annotations = new();

    // Properties
    public IReadOnlyList<IAnnotation> Annotations => _annotations.AsReadOnly();
    public IAnnotation? SelectedAnnotation => _annotations.FirstOrDefault(a => a.IsSelected);

    // Events
    public event EventHandler<EventArgs>? AnnotationsChanged;

    // Annotation Management Methods
    public void AddAnnotation(IAnnotation annotation)
    {
        _annotations.Add(annotation);
        OnAnnotationsChanged();
    }

    public void RemoveAnnotation(IAnnotation annotation)
    {
        if (_annotations.Remove(annotation))
        {
            OnAnnotationsChanged();
        }
    }

    public void ClearAnnotations()
    {
        _annotations.Clear();
        OnAnnotationsChanged();
    }

    // Annotation Finding Methods
    public IAnnotation? GetAnnotationAt(PointF point)
    {
        // Search in reverse order (top to bottom)
        for (int i = _annotations.Count - 1; i >= 0; i--)
        {
            if (_annotations[i].Contains(point))
            {
                return _annotations[i];
            }
        }
        return null;
    }

    public IAnnotation? GetAnnotationAt(Point point)
    {
        // Convert Point to PointF and use the existing method
        return GetAnnotationAt(new PointF(point.X, point.Y));
    }

    // Selection and Movement Methods
    public void SelectAnnotation(IAnnotation? annotation)
    {
        foreach (var a in _annotations)
        {
            a.IsSelected = (a == annotation);
        }
        OnAnnotationsChanged();
    }

    public void MoveSelectedAnnotation(PointF newLocation)
    {
        var selected = SelectedAnnotation;
        if (selected != null)
        {
            selected.MoveTo(newLocation);
            OnAnnotationsChanged();
        }
    }

    // Protected Event Methods
    protected virtual void OnAnnotationsChanged()
    {
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
    }
}