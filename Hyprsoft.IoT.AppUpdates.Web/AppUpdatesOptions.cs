using System;

namespace Hyprsoft.IoT.AppUpdates.Web
{
    public class AppUpdatesOptions
    {
        #region Properties

        public const long DefaultMaxFileUploadSizeBytes = 52428800;    // 50MB

        public Uri ManifestUri { get; set; }

        public ClientCredentials ClientCredentials { get; set; } = new ClientCredentials();

        public BearerTokenOptions TokenOptions { get; set; } = new BearerTokenOptions();

        public long MaxFileUploadSizeBytes { get; set; } = DefaultMaxFileUploadSizeBytes;

        #endregion
    }
}