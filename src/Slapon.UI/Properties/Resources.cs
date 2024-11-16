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
        public static System.Drawing.Image? clearall => LoadImage(Path.Combine(IconPath, "clearall.png"));
        public static System.Drawing.Image? newcapture => LoadImage(Path.Combine(IconPath, "newcapture.png"));
        public static System.Drawing.Image? select => LoadImage(Path.Combine(IconPath, "select.png"));
    }
}