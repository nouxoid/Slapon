using Slapon.Core.Interfaces;
using Slapon.Core.Services;

namespace Slapon.Core.Commands
{
    public class AddAnnotationCommand : ICommand
    {
        private readonly AnnotationService _service;
        private readonly IAnnotation _annotation;

        public AddAnnotationCommand(AnnotationService service, IAnnotation annotation)
        {
            _service = service;
            _annotation = annotation;
        }

        public void Execute()
        {
            _service.InternalAddAnnotation(_annotation);
        }

        public void Undo()
        {
            _service.InternalRemoveAnnotation(_annotation);
        }
    }
}