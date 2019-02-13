
namespace Hyprsoft.IoT.AppUpdates.Web
{
    public static class AuthenticationSettings
    {
        #region Cookie Authentication Related

        public const string CookieAuthenticationScheme = "CookieAppUpdates";

        public const int CookieExpirationDays = 30;

        public const string CookieName = ".appupdates.cookies";

        #endregion

        #region Default Usernames and Passwords Related

        public const string DefaultUsername = "appupdatesadmin";

        public const string DefaultPassword = "appupdatesadmin!";

        public const int UsernameMinLength = 8;

        public const int PasswordMinLength = 8;

        public const int PasswordMaxLength = 30;

        #endregion

        #region Bearer Authentication Related

        public const string AuthenticationScheme = "BearerAppUpdates";

        public const string DefaultBearerIssuer = "hyprsoft.com";

        public const string DefaultBearerAudience = DefaultBearerIssuer;

        public const string DefaultBearerSecurityKey = "3B8CEE84-FDD1-421E-B58B-3C213474DCE6-4752EF35-83E8-4EFB-AD94-39584EF87376-9CE2C146-6112-460B-9FEF-C4A69F2E4907";

        public const int BearerTokenExpirationDays = 1;

        #endregion
    }
}
