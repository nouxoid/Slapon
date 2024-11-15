using System.Drawing;

namespace Slapon.Core.Models
{
    public class SerializableAnnotation
    {
        public string Type { get; set; }
        public RectangleF Bounds { get; set; }
        public Color Color { get; set; }
        public float Opacity { get; set; }
        public string Id { get; set; }  // Add this property

        public SerializableAnnotation()
        {
            Id = Guid.NewGuid().ToString(); // Generate a unique ID for each annotation
        }
    }


    public class SaveData
    {
        public byte[] Screenshot { get; set; }
        public List<SerializableAnnotation> Annotations { get; set; }
    }
}