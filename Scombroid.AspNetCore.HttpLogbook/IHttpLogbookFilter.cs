using Microsoft.AspNetCore.Http;
using Scombroid.AspNetCore.HttpLogbook.Filters;
namespace Scombroid.AspNetCore.HttpLogbook
{
    public interface IHttpLogbookFilter
    {
        Microsoft.Extensions.Logging.LogLevel LogLevel { get; }
        HttpLogbookMethodFilter Find(PathString? pathString, string method);
    }
}

