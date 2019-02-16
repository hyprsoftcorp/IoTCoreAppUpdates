using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Hyprsoft.IoT.AppUpdates.Service
{
    public class UpdateServiceSettings
    {
        #region InstalledApp Helper Class

        public class InstalledApp
        {
            [JsonProperty]
            public Guid ApplicationId { get; set; }

            [JsonProperty]
            public Uri InstallUri { get; set; }

            public override string ToString() => $"{ApplicationId} {InstallUri.ToString().ToLower()}";
        }

        #endregion

        #region Properties

        public const string DefaultConfigFilename = "app-updates-config.json";

        [JsonIgnore]
        public string ConfigurationFilename => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DefaultConfigFilename).ToLower();

        [JsonProperty]
        public ClientCredentials ClientCredentials { get; set; } = new ClientCredentials();

        [JsonProperty]
        public TimeSpan CheckTime { get; set; } = new TimeSpan(3, 0, 0);

        [JsonProperty]
        public DateTime NextCheckDate { get; set; }

        [JsonProperty]
        public Uri ManifestUri { get; set; } = new Uri($"https://hyprsoftweb.azurewebsites.net/{UpdateManager.DefaultManifestFilename}");

        [JsonProperty]
        public List<InstalledApp> InstalledApps { get; set; } = new List<InstalledApp>();

        #endregion
    }
}
