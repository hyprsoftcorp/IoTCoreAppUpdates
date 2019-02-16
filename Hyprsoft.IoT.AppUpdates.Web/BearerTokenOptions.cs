using System;

namespace Hyprsoft.IoT.AppUpdates.Web
{
    public class BearerTokenOptions
    {
        #region Properties

        public const string DefaultIssuer = "http://www.hyprsoft.com/appupdates";

        public const string DefaultAudience = "appupdates";

        public string Issuer { get; set; } = DefaultIssuer;

        public string Audience { get; set; } = DefaultAudience;

        public TimeSpan Lifespan { get; set; } = TimeSpan.FromDays(1);

        public string SecurityKey { get; set; } = "[CHANGE ME - THIS KEY IS USED FOR JWT BEARER TOKEN SIGNING]";

        #endregion
    }
}
