using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                using var page = engine.Process(image);
                return page.GetText();
            });
        }
    }
}
