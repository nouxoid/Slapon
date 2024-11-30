using Slapon.Core.Interfaces;
using Slapon.Core.Services;

namespace Slapon.Core.Commands
{
    public class RemoveAnnotationCommand : ICommand
    {
        private readonly AnnotationService _service;
        private readonly IAnnotation _annotation;

        public RemoveAnnotationCommand(AnnotationService service, IAnnotation annotation)
        {
            _service = service;
            _annotation = annotation;
        }

        public void Execute()
        {
            _service.InternalRemoveAnnotation(_annotation);
        }

        public void Undo()
        {
            _service.InternalAddAnnotation(_annotation);
        }
    }
}