using System.ComponentModel.DataAnnotations;

namespace Hyprsoft.IoT.AppUpdates
{
    public class ClientCredentials
    {
        #region Properties

        [Required]
        public string ClientId { get; set; }

        [Required]
        public string ClientSecret { get; set; }

        #endregion
    }
}
