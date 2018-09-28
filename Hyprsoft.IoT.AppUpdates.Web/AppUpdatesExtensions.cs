using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace Hyprsoft.IoT.AppUpdates.Web
{
    public static class AppUpdatesExtensions
    {

        #region Methods

        public static IServiceCollection AddAppUpdates(this IServiceCollection services, Action<AppUpdatesOptions> appUpdateOptions)
        {
            var o = new AppUpdatesOptions();
            appUpdateOptions.Invoke(o);

            services.AddLogging(options => options.AddDebug());
            services.AddAuthentication(AuthenticationHelper.CookieAuthenticationScheme).AddCookie(AuthenticationHelper.CookieAuthenticationScheme, options =>
            {
                options.Cookie.Name = AuthenticationHelper.CookieName;
                options.LoginPath = "/appupdates/account/login";
                options.LogoutPath = "/appupdates/account/logout";
            });

            services.AddSingleton<UpdateManager>(s => new UpdateManager(o.ManifestUri, new LoggerFactory()));
            return services;
        }

        public static IApplicationBuilder UseAppUpdates(this IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly(), "Hyprsoft.IoT.AppUpdates.Web.wwwroot"),
                RequestPath = new PathString("/appupdates")
            });
            app.UseAuthentication();
            app.UseMvc(routes => routes.MapRoute("appupdates", "{area}/{controller=Apps}/{action=List}/{id?}"));
            return app;
        }

        #endregion
    }
}
