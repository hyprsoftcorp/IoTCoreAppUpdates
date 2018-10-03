using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Reflection;
using System.Text;

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
            services.AddAuthentication(AuthenticationSettings.CookieAuthenticationScheme).AddCookie(AuthenticationSettings.CookieAuthenticationScheme, options =>
            {
                options.Cookie.Name = AuthenticationSettings.CookieName;
                options.LoginPath = "/appupdates/account/login";
                options.LogoutPath = "/appupdates/account/logout";
            });
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
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
