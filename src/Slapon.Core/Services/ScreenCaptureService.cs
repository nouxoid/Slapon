using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Slapon.Core.Services
{
    public class ScreenCaptureService
    {
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(SystemMetric metric);

        private enum SystemMetric
        {
            ScreenWidth = 0,
            ScreenHeight = 1
        }

        public Bitmap CaptureScreen()
        {
            // Get the total size of all screens
            Rectangle totalBounds = GetTotalScreenBounds();

            var screenshot = new Bitmap(totalBounds.Width, totalBounds.Height);

            using (var graphics = Graphics.FromImage(screenshot))
            {
                // Copy from each screen
                foreach (Screen screen in Screen.AllScreens)
                {
                    // Calculate relative position
                    var relativeBounds = new Rectangle(
                        screen.Bounds.X - totalBounds.X,
                        screen.Bounds.Y - totalBounds.Y,
                        screen.Bounds.Width,
                        screen.Bounds.Height
                    );

                    // Copy this screen's content
                    graphics.CopyFromScreen(
                        screen.Bounds.Location,  // Source position
                        relativeBounds.Location, // Destination position
                        screen.Bounds.Size       // Size to copy
                    );
                }
            }

            return screenshot;
        }

        private Rectangle GetTotalScreenBounds()
        {
            // Start with the primary screen
            Rectangle totalBounds = Screen.PrimaryScreen.Bounds;

            // Union with all other screens
            foreach (Screen screen in Screen.AllScreens)
            {
                totalBounds = Rectangle.Union(totalBounds, screen.Bounds);
            }

            return totalBounds;
        }

        public Bitmap CaptureRegion(Rectangle region)
        {
            using var fullScreenshot = CaptureScreen();
            var regionShot = new Bitmap(region.Width, region.Height);

            using (var graphics = Graphics.FromImage(regionShot))
            {
                graphics.DrawImage(fullScreenshot, new Rectangle(0, 0, region.Width, region.Height),
                                   region, GraphicsUnit.Pixel);
            }

            return regionShot;
        }
    }
}