using Microsoft.AspNetCore.Http;
using Scombroid.AspNetCore.HttpLogbook.Filters;
namespace Scombroid.AspNetCore.HttpLogbook
{
    public interface IHttpLogbookFilter
    {
        HttpLogbookMethodFilter FindByAction(string actionName, string method);
        HttpLogbookMethodFilter FindByPath(PathString? pathString, string method);
    }
}

