public class ScreenCaptureService
{

    public Bitmap CaptureScreen()
    {
        // Get virtual screen bounds
        Rectangle virtualScreen = SystemInformation.VirtualScreen;
        var screenshot = new Bitmap(virtualScreen.Width, virtualScreen.Height);

        using (var graphics = Graphics.FromImage(screenshot))
        {
            graphics.CopyFromScreen(
                virtualScreen.Left,
                virtualScreen.Top,
                0,
                0,
                virtualScreen.Size,
                CopyPixelOperation.SourceCopy
            );
        }

        return screenshot;
    }

    private Rectangle GetVirtualScreenBounds()
    {
        int left = int.MaxValue;
        int top = int.MaxValue;
        int right = int.MinValue;
        int bottom = int.MinValue;

        foreach (Screen screen in Screen.AllScreens)
        {
            left = Math.Min(left, screen.Bounds.Left);
            top = Math.Min(top, screen.Bounds.Top);
            right = Math.Max(right, screen.Bounds.Right);
            bottom = Math.Max(bottom, screen.Bounds.Bottom);
        }

        return new Rectangle(left, top, right - left, bottom - top);
    }

    public Bitmap CaptureRegion(Rectangle region)
    {
        try
        {
            // Debug the incoming coordinates
            System.Diagnostics.Debug.WriteLine($"Attempting to capture: X={region.X}, Y={region.Y}, Width={region.Width}, Height={region.Height}");

            // Convert coordinates from virtual space to actual screen space
            int actualX = region.X;
            if (actualX > 1920) // If on second monitor and coordinates are wrong
            {
                actualX = actualX - 1920; // Adjust to correct screen coordinates
            }

            Rectangle adjustedRegion = new Rectangle(
                actualX,
                region.Y,
                region.Width,
                region.Height
            );

            System.Diagnostics.Debug.WriteLine($"Adjusted capture region: X={adjustedRegion.X}, Y={adjustedRegion.Y}, Width={adjustedRegion.Width}, Height={adjustedRegion.Height}");

            // Create the bitmap for capture
            var regionShot = new Bitmap(region.Width, region.Height);

            using (var graphics = Graphics.FromImage(regionShot))
            {
                graphics.CopyFromScreen(
                    adjustedRegion.X,
                    adjustedRegion.Y,
                    0,
                    0,
                    adjustedRegion.Size,
                    CopyPixelOperation.SourceCopy
                );
            }

            return regionShot;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Capture error: {ex.Message}");
            throw;
        }
    }

    private Screen GetScreenFromRegion(Rectangle region)
    {
        // Find the screen that contains the largest area of the selection
        Screen bestScreen = Screen.PrimaryScreen;
        int largestArea = 0;

        foreach (Screen screen in Screen.AllScreens)
        {
            Rectangle intersection = Rectangle.Intersect(screen.Bounds, region);
            int area = intersection.Width * intersection.Height;

            if (area > largestArea)
            {
                largestArea = area;
                bestScreen = screen;
            }
        }

        return bestScreen;
    }

    private IEnumerable<Screen> GetIntersectingScreens(Rectangle region)
    {
        // Return all screens that intersect with our selection region
        return Screen.AllScreens.Where(screen =>
            Rectangle.Intersect(screen.Bounds, region).IsEmpty == false);
    }
}