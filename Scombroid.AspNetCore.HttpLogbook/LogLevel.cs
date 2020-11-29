using System;
using System.Collections.Generic;
using System.Text;

namespace Scombroid.AspNetCore.HttpLogbook
{
    public enum LogLevel
    {
        None = Microsoft.Extensions.Logging.LogLevel.None,
        Trace = Microsoft.Extensions.Logging.LogLevel.Trace,
        Information = Microsoft.Extensions.Logging.LogLevel.Information
    }
}
