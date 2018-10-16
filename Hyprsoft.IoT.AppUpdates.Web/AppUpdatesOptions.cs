using System;

namespace Hyprsoft.IoT.AppUpdates.Web
{
    public class AppUpdatesOptions
    {
        public Uri ManifestUri { get; set; }

        public long MaxFileUploadSizeBytes { get; set; } = 52428800;    // 50MB
    }
}
