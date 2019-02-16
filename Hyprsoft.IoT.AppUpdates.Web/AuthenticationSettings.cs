
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

        #endregion
    }
}
