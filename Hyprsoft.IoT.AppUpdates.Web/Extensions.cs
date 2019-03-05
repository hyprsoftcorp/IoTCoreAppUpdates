using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Reflection;

namespace Hyprsoft.IoT.AppUpdates.Web
{
    public static class Extensions
    {
        #region Methods

        public static string GetAssemblyInformationalVersion(this Type startupType)
        {
            return (((AssemblyInformationalVersionAttribute)startupType.GetTypeInfo().Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion);
        }

        public static DateTime GetAssemblyBuildDateTime(this IHtmlHelper helper)
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<BuildDateAttribute>();
            return attribute != null ? attribute.DateTimeUtc : default(DateTime);
        }

        #endregion
    }
}
