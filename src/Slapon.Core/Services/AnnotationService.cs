using System.Drawing;
using Slapon.Core.Models;
using Slapon.Core.Interfaces;
using Slapon.Core.Commands;
namespace Slapon.Core.Services;

public class AnnotationService : IAnnotationService
{
    // Private fields
    internal readonly List<IAnnotation> _annotations = new();
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    // Properties
    public IReadOnlyList<IAnnotation> Annotations => _annotations.AsReadOnly();
    public IAnnotation? SelectedAnnotation => _annotations.FirstOrDefault(a => a.IsSelected);
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    // Events
    public event EventHandler<EventArgs>? AnnotationsChanged;

    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // Clear redo stack when new command is executed
        OnAnnotationsChanged();
    }


    internal void InternalAddAnnotation(IAnnotation annotation)
    {
        _annotations.Add(annotation);
    }

    internal void InternalRemoveAnnotation(IAnnotation annotation)
    {
        _annotations.Remove(annotation);
    }

    public void Undo()
    {
        if (!CanUndo) return;

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        OnAnnotationsChanged();
    }

    public void Redo()
    {
        if (!CanRedo) return;

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        OnAnnotationsChanged();
    }

    public void AddAnnotation(IAnnotation annotation)
    {
        var command = new AddAnnotationCommand(this, annotation);
        ExecuteCommand(command);
    }

    public void RemoveAnnotation(IAnnotation annotation)
    {
        var command = new RemoveAnnotationCommand(this, annotation);
        ExecuteCommand(command);
    }

    public void ClearAnnotations()
    {
        _annotations.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
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
            var oldLocation = selected.Bounds.Location;
            ExecuteCommand(new MoveAnnotationCommand(selected, oldLocation, newLocation));
        }
    }

    // Protected Event Methods
    protected virtual void OnAnnotationsChanged()
    {
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
    }

    

  
   
}