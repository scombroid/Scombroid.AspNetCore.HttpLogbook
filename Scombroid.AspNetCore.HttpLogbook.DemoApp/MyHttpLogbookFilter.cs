﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Scombroid.AspNetCore.HttpLogbook.Filters;

namespace Scombroid.AspNetCore.HttpLogbook.DemoApp
{
    public class MyHttpLogbookFilter : IHttpLogbookFilter
    {
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

        public HttpLogbookMethodFilter FindByAction(string actionName, string method) 
        {
            return MyRule;
        }


        public HttpLogbookMethodFilter FindByPath(PathString? pathString, string method)
        {
            return MyRule;
        }
    }
}
