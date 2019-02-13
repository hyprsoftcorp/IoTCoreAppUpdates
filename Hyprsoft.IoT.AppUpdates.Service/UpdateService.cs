using Hyprsoft.Logging.Core;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.IoT.AppUpdates.Service
{
    public class UpdateService : IHostedService
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

        #region Fields

        private bool _isFirstCheck = true;
        private Task _updateCheckTask;
        private readonly object _lockObject = new object();
        private UpdateManager _manager;
        private readonly SimpleLogManager _logger;

        #endregion

        #region Constructors

        public UpdateService(SimpleLogManager logger)
        {
            _logger = logger;
        }

        #endregion

        #region Properties

        public const string DefaultAppUpdatesConfigFilename = "app-updates-config.json";

        [JsonIgnore]
        public string ConfigurationFilename => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DefaultAppUpdatesConfigFilename).ToLower();

        [JsonProperty]
        public string ClientId { get; set; }

        [JsonProperty]
        public string ClientSecret { get; set; }

        [JsonProperty]
        public TimeSpan CheckTime { get; set; } = new TimeSpan(3, 0, 0);

        [JsonProperty]
        public DateTime NextCheckDate { get; set; }

        [JsonProperty]
        public Uri ManifestUri { get; set; } = new Uri($"https://hyprsoftweb.azurewebsites.net/{UpdateManager.DefaultAppUpdatesManifestFilename}");

        [JsonProperty]
        public List<InstalledApp> InstalledApps { get; set; } = new List<InstalledApp>();

        #endregion

        #region Methods

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _logger.LogAsync<UpdateService>(LogLevel.Info, "Starting service.");
            await LoadConfiguration();

            if (ManifestUri == null)
                await _logger.LogAsync<UpdateService>(LogLevel.Warn, $"The configuration is missing the required manifest URI.  Please update the '{ConfigurationFilename}' configuration file and restart the service.");
            if (InstalledApps.Count <= 0)
                await _logger.LogAsync<UpdateService>(LogLevel.Warn, $"The configuration does not have any apps listed to update.  Please update the '{ConfigurationFilename}' configuration file and restart the service.");

            if (ManifestUri != null && InstalledApps.Count > 0)
            {
                await _logger.LogAsync<UpdateService>(LogLevel.Info, $"Using manifest '{ManifestUri.ToString().ToLower()}' to check '{InstalledApps.Count}' app(s) for updates.");
                // If our packages are hosted using the Hyprsoft.IoT.Updates.Web NuGet then authentication is required; otherwise it depends on where the sources in the manifest reside.
                _manager = new UpdateManager(ManifestUri,
                    !String.IsNullOrWhiteSpace(ClientId) && !String.IsNullOrWhiteSpace(ClientSecret) ? new ClientCredentials { ClientId = ClientId, ClientSecret = ClientSecret } : null, 
                    _logger);
                _updateCheckTask = Update(cancellationToken);
            }   // manifest URI valid and installed app count > 0?
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _logger.LogAsync<UpdateService>(LogLevel.Info, "Stopping service.");
            if (_updateCheckTask != null)
                await _updateCheckTask;
        }

        private async Task Update(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_isFirstCheck || DateTime.Now >= NextCheckDate)
                {
                    try
                    {
                        _isFirstCheck = false;
                        await _logger.LogAsync<UpdateService>(LogLevel.Info, "Checking for updates.");
                        // Always re-load our manifest in case it has been altered since our last check.
                        await _manager.Load();
                        foreach (var install in InstalledApps)
                        {
                            var app = _manager.Applications.FirstOrDefault(a => a.Id == install.ApplicationId);
                            if (app != null)
                            {
                                var package = app.GetLatestPackage();
                                if (package != null)
                                    try
                                    {
                                        await _manager.Update(package, install.InstallUri, token);
                                    }
                                    catch (Exception ex)
                                    {
                                        await _logger.LogAsync<UpdateService>(ex, $"Unable to update application '{package.Application.Name}'.");
                                    }
                                else
                                    await _logger.LogAsync<UpdateService>(LogLevel.Warn, $"Unable to get the latest package for app '{app.Name}'.  No update will be applied.");
                            }
                            else
                                await _logger.LogAsync<UpdateService>(LogLevel.Warn, $"The app with id '{install.ApplicationId}' doesn't exist in the manifest.  No update will be applied.");
                        }
                        await UpdateNextCheckDate();
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogAsync<UpdateService>(ex, "Update check failed.");
                    }
                }   // time to run check?

                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }   // cancellation requested?
        }

        private async Task UpdateNextCheckDate()
        {
            var now = DateTime.Now;
            NextCheckDate = new DateTime(now.Year, now.Month, now.Day, CheckTime.Hours, CheckTime.Minutes, CheckTime.Seconds).Add(TimeSpan.FromDays(1));
            await SaveConfiguration();
        }

        private async Task LoadConfiguration()
        {
            if (File.Exists(ConfigurationFilename))
            {
                try
                {
                    await _logger.LogAsync<UpdateService>(LogLevel.Info, $"Loading service configuration from '{ConfigurationFilename}'.");
                    var instance = JsonConvert.DeserializeObject<UpdateService>(File.ReadAllText(ConfigurationFilename));
                    var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
                    foreach (var prop in instance.GetType().GetProperties(bindingFlags))
                    {
                        if (Attribute.IsDefined(prop, typeof(JsonIgnoreAttribute)))
                            continue;

                        var setter = GetType().GetProperty(prop.Name, bindingFlags);
                        if (setter.CanWrite)
                            setter.SetValue(this, prop.GetValue(instance));
                    }
                    return;
                }
                catch (Exception ex)
                {
                    // Ignore any errors reading our configuration file.  Just use the defaults.
                    await _logger.LogAsync<UpdateService>(ex, "Unable to load the configuration file.");
                }
            }   // file exists?

            await _logger.LogAsync<UpdateService>(LogLevel.Info, "Using default service configuration.");
            await UpdateNextCheckDate();
        }

        private async Task SaveConfiguration()
        {
            await _logger.LogAsync<UpdateService>(LogLevel.Info, $"Saving service configuration to '{ConfigurationFilename}'.");
            lock (_lockObject)
                File.WriteAllText(ConfigurationFilename, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        #endregion
    }
}
