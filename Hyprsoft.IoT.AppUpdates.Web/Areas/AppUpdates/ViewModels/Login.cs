using System.ComponentModel.DataAnnotations;

namespace Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.ViewModels
{
    public class Login
    {
        #region Properties

        [Required, MinLength(AuthenticationSettings.UsernameMinLength)]
        public string Username { get; set; }

        [Required, DataType(DataType.Password), MinLength(AuthenticationSettings.PasswordMinLength), MaxLength(AuthenticationSettings.PasswordMaxLength)]
        public string Password { get; set; }

        #endregion
    }
}
