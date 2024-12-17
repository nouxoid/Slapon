using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slapon.Core.Services
{
    public interface IOcrService
    {
        Task<string> ExtractTextAsync(Bitmap image);
    }
}
