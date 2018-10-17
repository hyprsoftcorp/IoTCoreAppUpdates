using System;

namespace Hyprsoft.IoT.AppUpdates.Web
{
    public class AppUpdatesOptions
    {
        public const long DefaultMaxFileUploadSizeBytes = 52428800;    // 50MB

        public Uri ManifestUri { get; set; }

        public long MaxFileUploadSizeBytes { get; set; } = DefaultMaxFileUploadSizeBytes;
    }
}
