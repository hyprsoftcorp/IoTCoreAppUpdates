using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        #region AppInstall Helper Class

        public class AppInstall
        {
            [JsonProperty]
            public Guid ApplicationId { get; internal set; }

            [JsonProperty]
            public Uri InstallUri { get; internal set; }

            public override string ToString()
            {
                return $"{ApplicationId} {InstallUri.ToString().ToLower()}";
            }
        }

        #endregion

        #region Fields

        private bool _isFirstCheck = true;
        private Task _updateCheckTask;
        private readonly object _lockObject = new object();
        private UpdateManager _manager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        #endregion

        #region Constructors

        public UpdateService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory?.CreateLogger<UpdateService>();
        }

        #endregion

        #region Properties

        [JsonIgnore]
        public string ConfigurationFilename => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "app-update-config.json").ToLower();

        [JsonProperty]
        public TimeSpan CheckTime { get; set; } = new TimeSpan(3, 0, 0);

        [JsonProperty]
        public DateTime NextCheckDate { get; internal set; }

        [JsonProperty]
        public Uri ManifestUri { get; set; } = new Uri("http://appupd.azurewebsites.net/app-update-manifest.json");

        [JsonProperty]
        public List<AppInstall> InstalledApps { get; internal set; } = new List<AppInstall>();

        #endregion

        #region Methods

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting service.");
            LoadConfiguration();

            if (ManifestUri == null)
                _logger.LogWarning($"The configuration is missing the required manifest URI.  Please update the '{ConfigurationFilename}' configuration file and restart the service.");
            if (InstalledApps.Count <= 0)
                _logger.LogWarning($"The configuration does not have any apps listed to update.  Please update the '{ConfigurationFilename}' configuration file and restart the service.");

            if (ManifestUri != null && InstalledApps.Count > 0)
            {
                _logger.LogInformation($"Using manifest '{ManifestUri.ToString().ToLower()}' to check '{InstalledApps.Count}' app(s) for updates.");
                _manager = new UpdateManager(ManifestUri, _loggerFactory);
                _updateCheckTask = Update(cancellationToken);
            }   // manifest URI valid and installed app count > 0?

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping service.");
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
                        _logger.LogInformation("Checking for updates.");
                        // Always re-load our manifest in case it has been altered since our last check.
                        await _manager.Load();
                        foreach (var appInstall in InstalledApps)
                        {
                            var app = _manager.Applications.FirstOrDefault(a => a.Id == appInstall.ApplicationId);
                            if (app != null)
                            {
                                var package = app.GetLatestPackage();
                                if (package != null)
                                    try
                                    {
                                        await _manager.Update(package, appInstall.InstallUri, token);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogCritical(ex, $"Unable to update application '{package.Application.Name}'.");
                                    }
                                else
                                    _logger.LogWarning($"Unable to get the latest package for app '{app.Name}'.  No update will be applied.");
                            }
                            else
                                _logger.LogWarning($"The app with id '{appInstall.ApplicationId}' doesn't exist in the manifest.  No update will be applied.");
                        }
                        UpdateNextCheckDate();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Update check failed.");
                    }
                }   // time to run check?

                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }   // cancellation requested?
        }

        private void UpdateNextCheckDate()
        {
            var now = DateTime.Now;
            NextCheckDate = new DateTime(now.Year, now.Month, now.Day, CheckTime.Hours, CheckTime.Minutes, CheckTime.Seconds).Add(TimeSpan.FromDays(1));
            SaveConfiguration();
        }

        private void LoadConfiguration()
        {
            if (File.Exists(ConfigurationFilename))
            {
                try
                {
                    _logger.LogInformation($"Loading service configuration from '{ConfigurationFilename}'.");
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
                    _logger.LogCritical(ex, "Unable to load the configuration file.");
                }
            }   // file exists?

            _logger.LogInformation("Using default service configuration.");
            UpdateNextCheckDate();
        }

        private void SaveConfiguration()
        {
            _logger.LogInformation($"Saving configuration to '{ConfigurationFilename}'.");
            lock (_lockObject)
                File.WriteAllText(ConfigurationFilename, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        #endregion
    }
}
