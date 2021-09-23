using Microsoft.AspNetCore.Http;
using Scombroid.AspNetCore.HttpLogbook.Filters;
namespace Scombroid.AspNetCore.HttpLogbook
{
    public interface IHttpLogbookFilter
    {
        HttpLogbookMethodFilter Find(PathString? pathString, string method);
    }
}

