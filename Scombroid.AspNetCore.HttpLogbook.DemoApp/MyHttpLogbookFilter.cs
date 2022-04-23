using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Scombroid.AspNetCore.HttpLogbook.Filters;

namespace Scombroid.AspNetCore.HttpLogbook.DemoApp
{
    public class MyHttpLogbookFilter : IHttpLogbookFilter
    {
        public bool IsEnabled() { return true; }

        private HttpLogbookMethodFilter MyRule { get; set; } = new HttpLogbookMethodFilter()
        {
            Request = new HttpLogbookMessageFilter( )
            {
                LogLevel = LogLevel.Trace
            },
            Response = new HttpLogbookMessageFilter()
            {
                LogLevel = LogLevel.Trace
            }
        };

        public HttpLogbookMethodFilter Find(PathString? pathString, string method)
        {
            return MyRule;
        }
    }
}
