using Hyprsoft.IoT.AppUpdates.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Hyprsoft.IoT.AppUpdates.TestWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        public IConfiguration Configuration { get; }

        public IHostingEnvironment HostingEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAppUpdates(options => options.ManifestUri = new Uri(Path.Combine(HostingEnvironment.WebRootPath, UpdateManager.DefaultAppUpdatesManifestFilename)));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public void Configure(IApplicationBuilder app)
        {
            if (HostingEnvironment.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");

            app.UseAppUpdates();
            app.UseMvc(routes => routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}"));
        }
    }
}
