using Microsoft.AspNetCore.Http;
using System;

namespace Scombroid.AspNetCore.HttpLogbook
{
    public interface IHttpLogbookLogger
    {
        void LogRequest(RequestLogContext context);
        void LogResponse(ResponseLogContext context);
    }
}
