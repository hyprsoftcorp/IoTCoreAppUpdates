using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.IoT.AppUpdates.Service
{
    public class UpdateService : IHostedService
    {
        #region Fields

        private bool _isFirstCheck = true;
        private Task _updateCheckTask;
        private readonly object _lockObject = new object();
        private UpdateManager _manager;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<UpdateService> _logger;
        private CancellationTokenSource _cts;

        #endregion

        #region Constructors

        public UpdateService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<UpdateService>();
        }

        #endregion

        #region Properties

        public UpdateServiceSettings Settings { get; private set; } = new UpdateServiceSettings();

        #endregion

        #region Methods

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var version = (((AssemblyInformationalVersionAttribute)GetType().Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion);
            _logger.LogInformation($"Starting service v{version}.");
            LoadConfiguration();

            if (Settings.ManifestUri == null)
                _logger.LogWarning($"The configuration is missing the required manifest URI.  Please update the '{Settings.ConfigurationFilename}' configuration file and restart the service.");
            if (Settings.InstalledApps.Count <= 0)
                _logger.LogWarning($"The configuration does not have any apps listed to update.  Please update the '{Settings.ConfigurationFilename}' configuration file and restart the service.");

            if (Settings.ManifestUri != null && Settings.InstalledApps.Count > 0)
            {
                _logger.LogInformation($"Using manifest '{Settings.ManifestUri.ToString().ToLower()}' to check '{Settings.InstalledApps.Count}' app(s) for updates.");
                // If our packages are hosted using the Hyprsoft.IoT.Updates.Web NuGet then authentication is required; otherwise it depends on where the sources in the manifest reside.
                _manager = new UpdateManager(Settings.ManifestUri, Settings.ClientCredentials, _loggerFactory.CreateLogger<UpdateManager>());
                // Let's use our own cancellation token to make sure we shutdown cleanlyfor both console and service runs.
                _cts?.Dispose();
                _cts = new CancellationTokenSource();
                _updateCheckTask = Update(_cts.Token);
            }   // manifest URI valid and installed app count > 0?

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping service.");
            if (_updateCheckTask == null)
                return;

            _cts.Cancel();
            await _updateCheckTask;
        }

        private async Task Update(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_isFirstCheck || DateTime.Now >= Settings.NextCheckDate)
                    {
                        try
                        {
                            _isFirstCheck = false;
                            _logger.LogInformation("Checking for updates.");
                            // Always reload our manifest in case it has been altered since our last check.
                            await _manager.Load();
                            foreach (var install in Settings.InstalledApps)
                            {
                                var app = _manager.Applications.FirstOrDefault(a => a.Id == install.ApplicationId);
                                if (app != null)
                                {
                                    var package = app.GetLatestPackage();
                                    if (package != null)
                                        try
                                        {
                                            await _manager.Update(package, install.InstallUri, cancellationToken);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, $"Unable to update application '{package.Application.Name}'.");
                                        }
                                    else
                                        _logger.LogWarning($"Unable to get the latest package for app '{app.Name}'.  No update will be applied.");
                                }
                                else
                                    _logger.LogWarning($"The app with id '{install.ApplicationId}' doesn't exist in the manifest.  No update will be applied.");
                            }
                            UpdateNextCheckDate();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Update check failed.");
                        }
                    }   // time to run check?

                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }   // cancellation requested?
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void LoadConfiguration()
        {
            if (File.Exists(Settings.ConfigurationFilename))
            {
                try
                {
                    _logger.LogInformation($"Loading service configuration from '{Settings.ConfigurationFilename}'.");
                    Settings = JsonConvert.DeserializeObject<UpdateServiceSettings>(File.ReadAllText(Settings.ConfigurationFilename));
                    return;
                }
                catch (Exception ex)
                {
                    // Ignore any errors reading our configuration file.  Just use the defaults.
                    _logger.LogError(ex, "Unable to load the configuration file.");
                }
            }   // file exists?

            _logger.LogInformation("Using default service configuration.");
            UpdateNextCheckDate();
        }

        private void SaveConfiguration()
        {
            _logger.LogInformation($"Saving service configuration to '{Settings.ConfigurationFilename}'.");
            lock (_lockObject)
                File.WriteAllText(Settings.ConfigurationFilename, JsonConvert.SerializeObject(Settings, Formatting.Indented));
        }

        private void UpdateNextCheckDate()
        {
            var now = DateTime.Now;
            Settings.NextCheckDate = new DateTime(now.Year, now.Month, now.Day, Settings.CheckTime.Hours, Settings.CheckTime.Minutes, Settings.CheckTime.Seconds).AddDays(1);
            _logger.LogInformation($"Next check will be at '{Settings.NextCheckDate.ToString("g")}'.");
            SaveConfiguration();
        }

        #endregion
    }
}
