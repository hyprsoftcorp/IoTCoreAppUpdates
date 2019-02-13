using System;

namespace Hyprsoft.IoT.AppUpdates.Web
{
    public class AppUpdatesOptions
    {
        #region Properties

        public const long DefaultMaxFileUploadSizeBytes = 52428800;    // 50MB

        public Uri ManifestUri { get; set; }

        public ClientCredentials ClientCredentials { get; set; }

        public long MaxFileUploadSizeBytes { get; set; } = DefaultMaxFileUploadSizeBytes;

        #endregion
    }
}