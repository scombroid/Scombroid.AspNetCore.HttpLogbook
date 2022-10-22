namespace Microsoft.Extensions.DependencyInjection
{
    public interface IHttpLogbookBuilder
    {
        IServiceCollection Services { get; }
    }
}