namespace Slapon.Core.Commands
{
    public class AddAnnotationCommand : ICommand
    {
        private readonly IAnnotationService _service;
        private readonly IAnnotation _annotation;

        public AddAnnotationCommand(IAnnotationService service, IAnnotation annotation)
        {
            _service = service;
            _annotation = annotation;
        }

        public void Execute()
        {
            _service._annotations.Add(_annotation);
        }

        public void Undo()
        {
            _service._annotations.Remove(_annotation);
        }
    }
}