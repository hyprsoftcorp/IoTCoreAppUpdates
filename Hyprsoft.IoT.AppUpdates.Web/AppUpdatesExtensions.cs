using Hyprsoft.Logging.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Hyprsoft.IoT.AppUpdates.Web
{
    public static class AppUpdatesExtensions
    {
        #region Methods

        public static IWebHostBuilder UseAppUpdates(this IWebHostBuilder hostBuilder)
        {
            return UseAppUpdates(hostBuilder, options => new AppUpdatesOptions());
        }

        public static IWebHostBuilder UseAppUpdates(this IWebHostBuilder hostBuilder, Action<AppUpdatesOptions> appUpdatesOptions)
        {
            var o = new AppUpdatesOptions();
            appUpdatesOptions.Invoke(o);
            hostBuilder.UseKestrel(options => options.Limits.MaxRequestBodySize = o.MaxFileUploadSizeBytes);
            return hostBuilder;
        }

        public static IServiceCollection AddAppUpdates(this IServiceCollection services, Action<AppUpdatesOptions> appUpdatesOptions)
        {
            var o = new AppUpdatesOptions();
            appUpdatesOptions.Invoke(o);

            var logger = new SimpleLogManager();
            logger.AddLogger(new SimpleFileLogger(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "app-updates-log.log"))
            {
                MaxFileSizeBytes = 524288
            });

            services.AddLogging(options => options.AddDebug());
            services.AddAuthentication(AuthenticationSettings.CookieAuthenticationScheme).AddCookie(AuthenticationSettings.CookieAuthenticationScheme, options =>
            {
                options.Cookie.Name = AuthenticationSettings.CookieName;
                options.LoginPath = "/appupdates/account/login";
                options.LogoutPath = "/appupdates/account/logout";
            });
            services.AddAuthentication(AuthenticationSettings.AuthenticationScheme).AddJwtBearer(AuthenticationSettings.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = AuthenticationSettings.DefaultBearerIssuer,
                    ValidAudience = AuthenticationSettings.DefaultBearerAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthenticationSettings.DefaultBearerSecurityKey))
                };
            });
            services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = o.MaxFileUploadSizeBytes);
            services.AddSingleton(logger);
            services.AddSingleton<UpdateManager>(s => new UpdateManager(o.ManifestUri, o.ClientCredentials, logger));
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
