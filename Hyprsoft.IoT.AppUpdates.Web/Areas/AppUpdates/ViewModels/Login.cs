using System.ComponentModel.DataAnnotations;

namespace Hyprsoft.IoT.AppUpdates.Web.Areas.AppUpdates.ViewModels
{
    public class Login
    {
        #region Properties

        [Required, MinLength(AuthenticationHelper.UsernameMinLength)]
        public string Username { get; set; }

        [Required, DataType(DataType.Password), MinLength(AuthenticationHelper.PasswordMinLength), MaxLength(AuthenticationHelper.PasswordMaxLength)]
        public string Password { get; set; }

        #endregion
    }
}
