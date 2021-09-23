using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scombroid.AspNetCore.HttpLogbook.Filters
{
    public class HttpLogbookMethodFilter
    {
        public bool Enabled { get; set; } = false;
        public HttpLogbookMessageFilter Request { get; set; }
            = new HttpLogbookMessageFilter();
        public HttpLogbookMessageFilter Response { get; set; }
            = new HttpLogbookMessageFilter();

        public bool IsRequestBodyEnabled()
        {
            if (Request == null)
            {
                return false;
            }
            return Request.Body;
        }

        public bool IsResponseBodyEnabled()
        {
            if (Response == null)
            {
                return false;
            }
            return Response.Body;
        }
    }
}
