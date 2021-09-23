using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Scombroid.AspNetCore.HttpLogbook.Filters;

namespace Scombroid.AspNetCore.HttpLogbook.DemoApp
{
    public class MyHttpLogbookFilter : IHttpLogbookFilter
    {
        public LogLevel LogLevel => LogLevel.Information;

        private HttpLogbookMethodFilter MyRule { get; set; } = new HttpLogbookMethodFilter()
        {
            Request = new HttpLogbookMessageFilter( )
            {
                Body = true
            },
            Response = new HttpLogbookMessageFilter()
            {
                Body = true
            }
        };

        public HttpLogbookMethodFilter Find(PathString? pathString, string method)
        {
            return MyRule;
        }
    }
}
