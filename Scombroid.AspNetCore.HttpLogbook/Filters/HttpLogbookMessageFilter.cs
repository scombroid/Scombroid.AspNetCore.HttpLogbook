using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Text.RegularExpressions;
namespace Scombroid.AspNetCore.HttpLogbook.Filters
{
    public class HttpLogbookMessageFilter
    {
        public bool Body { get; set; } = false;
        public HttpLogbookMaskFilter[] BodyMasks { get; set; } = null;

        public void ApplyBodyMask(ref string body)
        {
            if (BodyMasks == null)
                return;
            foreach (var r in BodyMasks)
            {
                if (r.Pattern != null)
                {
                    string replacement = r.Replacement ?? string.Empty;
                    body = Regex.Replace(body, r.Pattern, replacement, RegexOptions.IgnoreCase);
                }
            }
        }
    }
}

