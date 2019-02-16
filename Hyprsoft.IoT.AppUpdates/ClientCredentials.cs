using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Hyprsoft.IoT.AppUpdates
{
    public class ClientCredentials
    {
        #region Properties

        [Required, JsonProperty]
        public string ClientId { get; set; }

        [Required, JsonProperty]
        public string ClientSecret { get; set; }

        [Required, JsonProperty]
        public string Scope { get; set; }

        #endregion
    }
}
