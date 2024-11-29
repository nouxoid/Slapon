// src/Slapon.Core/Commands/MoveAnnotationCommand.cs
using System.Drawing;
using Slapon.Core.Interfaces;

namespace Slapon.Core.Commands
{
    public class MoveAnnotationCommand : ICommand
    {
        private readonly IAnnotation _annotation;
        private readonly PointF _oldLocation;
        private readonly PointF _newLocation;

        public MoveAnnotationCommand(IAnnotation annotation, PointF oldLocation, PointF newLocation)
        {
            _annotation = annotation;
            _oldLocation = oldLocation;
            _newLocation = newLocation;
        }

        public void Execute()
        {
            _annotation.MoveTo(_newLocation);
        }

        public void Undo()
        {
            _annotation.MoveTo(_oldLocation);
        }
    }
}