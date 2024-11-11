using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Slapon.Core.Services;

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
        int screenWidth = GetSystemMetrics(SystemMetric.ScreenWidth);
        int screenHeight = GetSystemMetrics(SystemMetric.ScreenHeight);

        var screenshot = new Bitmap(screenWidth, screenHeight);

        using (var graphics = Graphics.FromImage(screenshot))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, new Size(screenWidth, screenHeight));
        }

        return screenshot;
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