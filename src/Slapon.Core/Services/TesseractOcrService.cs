using System;
using System.Drawing;
using System.Drawing.Imaging;  // Keep this for ImageFormat
using System.IO;
using System.Threading.Tasks;
using Tesseract;

namespace Slapon.Core.Services
{
    public class TesseractOcrService : IOcrService
    {
        private readonly string _tessdataPath;

        public TesseractOcrService(string tessdataPath)
        {
            _tessdataPath = tessdataPath;
        }

        public async Task<string> ExtractTextAsync(Bitmap image)
        {
            return await Task.Run(() =>
            {
                using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
                // Convert Bitmap to Pix using memory stream
                using var ms = new MemoryStream();
                // Use fully qualified name for ImageFormat
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                using var pix = Pix.LoadFromMemory(ms.ToArray());
                using var page = engine.Process(pix);
                return page.GetText();
            });
        }
    }
}