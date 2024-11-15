using System.Drawing;

namespace Slapon.Core.Models
{
    public class SerializableAnnotation
    {
        public string Type { get; set; }
        public RectangleF Bounds { get; set; }
        public Color Color { get; set; }
        public float Opacity { get; set; }
    }

    public class SaveData
    {
        public byte[] Screenshot { get; set; }
        public List<SerializableAnnotation> Annotations { get; set; }
    }
}