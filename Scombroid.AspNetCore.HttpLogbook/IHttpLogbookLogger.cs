using Microsoft.AspNetCore.Http;
using System;

namespace Scombroid.AspNetCore.HttpLogbook
{
    public interface IHttpLogbookLogger
    {
        void Log(LogContext context);
        void LogException(LogContext context, Exception ex);
    }
}
