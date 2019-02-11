using System.ComponentModel.DataAnnotations;

namespace Hyprsoft.IoT.AppUpdates
{
    public class ClientCredentials
    {
        #region Properties

        public const string DefaultClientId = "#jpMqwQT7ieEF7R";

        public const string DefaultClientSecret = "syfzDqGmGk3Qq01";

        [Required]
        public string ClientId { get; set; } = DefaultClientId;

        [Required]
        public string ClientSecret { get; set; } = DefaultClientSecret;

        #endregion
    }
}
