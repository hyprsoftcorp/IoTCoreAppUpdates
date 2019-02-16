using Hyprsoft.Logging.Core;
using Microsoft.Extensions.Hosting;
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
        private readonly SimpleLogManager _logger;

        #endregion

        #region Constructors

        public UpdateService(SimpleLogManager logger)
        {
            _logger = logger;
        }

        #endregion

        #region Properties

        public UpdateServiceSettings Settings { get; private set; } = new UpdateServiceSettings();

        #endregion

        #region Methods

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var version = (((AssemblyInformationalVersionAttribute)GetType().Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion);
            await _logger.LogAsync<UpdateService>(LogLevel.Info, $"Starting service v{version}.");
            await LoadConfiguration();

            if (Settings.ManifestUri == null)
                await _logger.LogAsync<UpdateService>(LogLevel.Warn, $"The configuration is missing the required manifest URI.  Please update the '{Settings.ConfigurationFilename}' configuration file and restart the service.");
            if (Settings.InstalledApps.Count <= 0)
                await _logger.LogAsync<UpdateService>(LogLevel.Warn, $"The configuration does not have any apps listed to update.  Please update the '{Settings.ConfigurationFilename}' configuration file and restart the service.");

            if (Settings.ManifestUri != null && Settings.InstalledApps.Count > 0)
            {
                await _logger.LogAsync<UpdateService>(LogLevel.Info, $"Using manifest '{Settings.ManifestUri.ToString().ToLower()}' to check '{Settings.InstalledApps.Count}' app(s) for updates.");
                // If our packages are hosted using the Hyprsoft.IoT.Updates.Web NuGet then authentication is required; otherwise it depends on where the sources in the manifest reside.
                _manager = new UpdateManager(Settings.ManifestUri, Settings.ClientCredentials, _logger);
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
                if (_isFirstCheck || DateTime.Now >= Settings.NextCheckDate)
                {
                    try
                    {
                        _isFirstCheck = false;
                        await _logger.LogAsync<UpdateService>(LogLevel.Info, "Checking for updates.");
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

        private async Task LoadConfiguration()
        {
            if (File.Exists(Settings.ConfigurationFilename))
            {
                try
                {
                    await _logger.LogAsync<UpdateService>(LogLevel.Info, $"Loading service configuration from '{Settings.ConfigurationFilename}'.");
                    Settings = JsonConvert.DeserializeObject<UpdateServiceSettings>(File.ReadAllText(Settings.ConfigurationFilename));
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
            await _logger.LogAsync<UpdateService>(LogLevel.Info, $"Saving service configuration to '{Settings.ConfigurationFilename}'.");
            lock (_lockObject)
                File.WriteAllText(Settings.ConfigurationFilename, JsonConvert.SerializeObject(Settings, Formatting.Indented));
        }

        private async Task UpdateNextCheckDate()
        {
            var now = DateTime.Now;
            Settings.NextCheckDate = new DateTime(now.Year, now.Month, now.Day, Settings.CheckTime.Hours, Settings.CheckTime.Minutes, Settings.CheckTime.Seconds).Add(TimeSpan.FromDays(1));
            await SaveConfiguration();
        }

        #endregion
    }
}
