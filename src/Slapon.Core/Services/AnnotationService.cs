using Slapon.Core.Commands;
using Slapon.Core.Interfaces;

public class AnnotationService : IAnnotationService
{
    // Private fields
    private readonly List<IAnnotation> _annotations = new();
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    // Properties
    public IReadOnlyList<IAnnotation> Annotations => _annotations.AsReadOnly();
    public IAnnotation? SelectedAnnotation => _annotations.FirstOrDefault(a => a.IsSelected);
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    // Events
    public event EventHandler<EventArgs>? AnnotationsChanged;
    public event EventHandler<IAnnotation>? AnnotationAdded;

    public void ExecuteCommand(ICommand command)
    {
        System.Diagnostics.Debug.WriteLine($"ExecuteCommand called with command type: {command.GetType().Name}");
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();
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
        LogStackState("Before Undo");
        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        OnAnnotationsChanged();
        LogStackState("After Undo");
    }

    public void Redo()
    {
        if (!CanRedo) return;
        LogStackState("Before Redo");
        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        OnAnnotationsChanged();
        LogStackState("After Redo");
    }

    public void AddAnnotation(IAnnotation annotation)
    {
        System.Diagnostics.Debug.WriteLine($"AddAnnotation called for annotation {annotation.Id}");
        var command = new AddAnnotationCommand(this, annotation);
        ExecuteCommand(command);

        // Fire the AnnotationAdded event after adding the annotation
        OnAnnotationAdded(annotation);
    }

    public void RemoveAnnotation(IAnnotation annotation)
    {
        ExecuteCommand(new RemoveAnnotationCommand(this, annotation));
    }

    public void AddPreviewAnnotation(IAnnotation annotation)
    {
        InternalAddAnnotation(annotation);
        OnAnnotationsChanged();
    }

    public void RemovePreviewAnnotation(IAnnotation annotation)
    {
        InternalRemoveAnnotation(annotation);
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
        return GetAnnotationAt(new PointF(point.X, point.Y));
    }

    public void SelectAnnotation(IAnnotation? annotation)
    {
        foreach (var a in _annotations)
        {
            a.IsSelected = (a == annotation);
        }
        OnAnnotationsChanged();
    }

    public void MoveAnnotation(IAnnotation annotation, PointF fromLocation, PointF toLocation)
    {
        // Only create a command if the annotation actually moved
        if (fromLocation != toLocation)
        {
            System.Diagnostics.Debug.WriteLine($"Creating move command from {fromLocation} to {toLocation}");
            ExecuteCommand(new MoveAnnotationCommand(annotation, fromLocation, toLocation));
        }
    }

    protected virtual void OnAnnotationsChanged()
    {
        AnnotationsChanged?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnAnnotationAdded(IAnnotation annotation)
    {
        AnnotationAdded?.Invoke(this, annotation);
    }

    private void LogStackState(string operation)
    {
        System.Diagnostics.Debug.WriteLine($"{operation}:");
        System.Diagnostics.Debug.WriteLine($"Undo Stack Count: {_undoStack.Count}");
        System.Diagnostics.Debug.WriteLine($"Redo Stack Count: {_redoStack.Count}");
        System.Diagnostics.Debug.WriteLine($"Annotations Count: {_annotations.Count}");
        System.Diagnostics.Debug.WriteLine("------------------------");
    }
}