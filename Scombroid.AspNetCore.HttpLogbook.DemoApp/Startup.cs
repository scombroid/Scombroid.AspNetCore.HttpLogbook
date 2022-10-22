using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Scombroid.AspNetCore.HttpLogbook.DemoApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Example 1 - Using config for filter and default logger
            services
                .AddHttpLogbook(Configuration.GetSection("HttpLogbook"));

            // Example 2 - Using config for filter and custom logger
            //services
            //    .AddHttpLogbook(Configuration.GetSection("HttpLogbook"))
            //    .AddLogger<MyHttpLogbookLogger>();

            // Example 3 - Using custom filter and custom logger
            //services.AddHttpLogbook()
            //    .AddFilter<MyHttpLogbookFilter>()
            //    .AddLogger<MyHttpLogbookLogger>();

            // Example 4 - Using custom filter and default logger
            //services.AddHttpLogbook()
            //    .AddFilter<MyHttpLogbookFilter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseLogbook();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }



        protected T BindConfig<T>(string key)
            where T : class, new()
        {
            var configSection = Configuration.GetSection(key);
            T config = new T();
            configSection.Bind(config);
            return config;
        }
    }
}
