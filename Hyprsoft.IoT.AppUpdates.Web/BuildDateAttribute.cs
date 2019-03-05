using System;
using System.Globalization;

namespace Hyprsoft.IoT.AppUpdates.Web
{
    [AttributeUsage(AttributeTargets.Assembly)]
    internal class BuildDateAttribute : Attribute
    {
        public BuildDateAttribute(string value)
        {
            /* .csproj addition
             <ItemGroup>
                <AssemblyAttribute Include="Hyprsoft.Enp.Web.BuildDateAttribute">
                    <_Parameter1>$([System.DateTime]::UtcNow.ToString("yyyyMMddHHmmss"))</_Parameter1>
                </AssemblyAttribute>
            </ItemGroup>
            */
            DateTimeUtc = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        public DateTime DateTimeUtc { get; }
    }
}
