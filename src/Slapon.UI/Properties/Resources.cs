namespace Slapon.UI.Properties
{
    internal class Resources
    {
        private static System.Drawing.Image? LoadImage(string path)
        {
            try
            {
                return System.Drawing.Image.FromFile(path);
            }
            catch
            {
                return null;
            }
        }

        private static readonly string IconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");

        public static System.Drawing.Image? rectangle => LoadImage(Path.Combine(IconPath, "rectangle.png"));
        public static System.Drawing.Image? highlighter => LoadImage(Path.Combine(IconPath, "highlighter.png"));
        public static System.Drawing.Image? line => LoadImage(Path.Combine(IconPath, "line.png"));
        public static System.Drawing.Image? text => LoadImage(Path.Combine(IconPath, "text.png"));
        public static System.Drawing.Image? circle => LoadImage(Path.Combine(IconPath, "circle.png"));
        public static System.Drawing.Image? clearall => LoadImage(Path.Combine(IconPath, "clearall.png"));
        public static System.Drawing.Image? newcapture => LoadImage(Path.Combine(IconPath, "newcapture.png"));
        public static System.Drawing.Image? select => LoadImage(Path.Combine(IconPath, "select.png"));
        public static System.Drawing.Image? colorIcon => LoadImage(Path.Combine(IconPath, "colorpicker.png"));

        public static System.Drawing.Image? arrow => LoadImage(Path.Combine(IconPath, "arrow.png"));
        public static System.Drawing.Image? copy => LoadImage(Path.Combine(IconPath, "copy.png"));
        public static System.Drawing.Image? save => LoadImage(Path.Combine(IconPath, "save.png"));
        public static System.Drawing.Image? blur => LoadImage(Path.Combine(IconPath, "blur.png"));
        public static System.Drawing.Image? rotate => LoadImage(Path.Combine(IconPath, "rotation.png"));
        public static System.Drawing.Image? undo => LoadImage(Path.Combine(IconPath, "undo.png"));
        public static System.Drawing.Image? redo => LoadImage(Path.Combine(IconPath, "redo.png"));
        public static System.Drawing.Image? ocr => LoadImage(Path.Combine(IconPath, "ocr.png"));
    }
}