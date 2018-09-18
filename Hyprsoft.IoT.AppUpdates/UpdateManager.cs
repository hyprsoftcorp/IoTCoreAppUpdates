using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Net.Http;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Hyprsoft.IoT.AppUpdates.Tests")]
namespace Hyprsoft.IoT.AppUpdates
{
    public class UpdateManager
    {
        #region Fields

        private readonly object _lockObject = new object();

        #endregion

        #region Constructors

        public UpdateManager(Uri manifestUri, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            ManifestUri = manifestUri ?? throw new ArgumentNullException(nameof(manifestUri));
            Logger = loggerFactory.CreateLogger<UpdateManager>();
        }

        #endregion

        #region Properties

        public const string DefaultAppUpdateManifestFilename = "app-update-manifest.json";

        /// <summary>
        /// Returns true if the app update manifest has been loaded; otherwise false.
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// Gets or sets whether or not the update manager can update applications that don't exists.
        /// </summary>
        /// <remarks>WARNING! Allowing applications to be updated that don't exist is a security risk.</remarks>
        public bool AllowInstalls { get; set; }

        /// <summary>
        /// Gets the app update manifest URI.
        /// </summary>
        public Uri ManifestUri { get; private set; }

        /// <summary>
        /// Get's the appliction logger.
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Gets the applications the update manager can update.
        /// </summary>
        public List<Application> Applications { get; private set; } = new List<Application>();

        #endregion

        #region Methods

        /// <summary>
        /// Loads the app update manifest.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown when the <see cref="ManifestUri" is null./></exception>
        /// <exception cref="FileNotFoundException">Thrown when the <see cref="ManifestUri" is a file URI and the file is not found./></exception>
        /// <exception cref="HttpRequestException">Thrown when the <see cref="ManifestUri" is a http or https URI and the resource is not found./></exception>
        public async Task Load()
        {
            if (ManifestUri == null)
                throw new NullReferenceException($"The '{nameof(ManifestUri)}' cannot be null.");

            Logger.LogInformation($"Loading manifest from '{ManifestUri.ToString().ToLower()}'.");
            if (ManifestUri.IsFile)
                Applications = JsonConvert.DeserializeObject<List<Application>>(File.ReadAllText(ManifestUri.LocalPath));
            else
            {
                using (var client = new HttpClient())
                    Applications = JsonConvert.DeserializeObject<List<Application>>(await client.GetStringAsync(ManifestUri));
            }

            // Hook up our heiarchy.
            foreach (var app in Applications)
            {
                app.UpdateManager = this;
                foreach (var package in app.Packages)
                {
                    package.Application = app;
                    foreach (var change in package.Changes)
                        change.Package = package;
                }   // for each package.
            }   // for each app.

            IsLoaded = true;
        }

        /// <summary>
        /// Saves the app update manifest.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown when the <see cref="ManifestUri" is null./></exception>
        /// <exception cref="InvalidOperationException">Thrown when the <see cref="ManifestUri" is not a file URI/></exception>
        public Task Save()
        {
            if (ManifestUri == null)
                throw new NullReferenceException($"The '{nameof(ManifestUri)}' cannot be null.");

            if (!ManifestUri.IsFile)
                throw new InvalidOperationException("The update manifest cannot be saved because it's not a file URI.");

            Logger.LogInformation($"Saving manifest to '{ManifestUri.ToString().ToLower()}'.");
            lock (_lockObject)
                File.WriteAllText(ManifestUri.LocalPath, JsonConvert.SerializeObject(Applications, Formatting.Indented));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates an app.
        /// </summary>
        /// <param name="package">The package used to update the app.</param>
        /// <param name="installUri">The install URI of the app.  Ex. c:\testapp.</param>
        /// <param name="token">Cancecllation token.</param>
        /// <exception cref="ArgumentNullException">Thrown when package is null./></exception>
        /// <exception cref="ArgumentNullException">Thrown when installUri is null./></exception>
        public async Task Update(Package package, Uri installUri, CancellationToken token)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));

            if (installUri == null)
                throw new ArgumentNullException(nameof(installUri));

            try
            {
                if (token.IsCancellationRequested) return;

                // STEP 1 - Check to see if our app needs to be updated.
                var versionFilename = Path.Combine(installUri.LocalPath, package.Application.VersionFilename).ToLower();
                Logger.LogInformation($"Checking '{package.Application.Name}' version using '{versionFilename}'.");
                if (File.Exists(versionFilename))
                {
                    var fileVersion = FileVersionInfo.GetVersionInfo(versionFilename).FileVersion;
                    Logger.LogInformation($"Version '{fileVersion}' found.");
                    if (package.Version.ToString() == fileVersion)
                    {
                        Logger.LogInformation($"Application is up to date.");
                        return;
                    }   // file versions the same?
                }
                else
                {
                    Logger.LogInformation($"'{package.Application.Name}' does not exist.");
                    if (!AllowInstalls)
                        return;
                }

                // STEP 2 - Download package.
                string packageFilename = (package.SourceUri.IsFile ? package.SourceUri.LocalPath : Path.GetTempFileName()).ToLower();
                Logger.LogInformation($"Updating '{package.Application.Name}' using package '{package.SourceUri}'.");
                if (!package.SourceUri.IsFile)
                {
                    Logger.LogInformation($"Downloading package to '{packageFilename}'.");
                    using (var client = new HttpClient())
                        await File.WriteAllBytesAsync(packageFilename, await client.GetByteArrayAsync(package.SourceUri));
                }

                // STEP 3 - Check package integrity.
                Logger.LogInformation($"Checking package checksum for '{packageFilename}'.");
                if (package.Checksum != CalculateMD5Checksum(new Uri(packageFilename)))
                {
                    if (!package.SourceUri.IsFile)
                        File.Delete(packageFilename);
                    Logger.LogWarning($"Checksum mismatch detected.");
                    return;
                }

                // STEP 4 = Kill application process.
                await KillProcess(package.Application.ExeFilename, Logger);

                // STEP 5 - Unzip our package to the install uri.
                Logger.LogInformation($"Extracting package '{packageFilename}' to '{installUri.LocalPath.ToLower()}'.");
                ZipFile.ExtractToDirectory(packageFilename, installUri.LocalPath, true);
                if (!package.SourceUri.IsFile)
                    File.Delete(packageFilename);

                // STEP 6 - Restart our process.
                var installFolder = installUri.LocalPath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? installUri.LocalPath : installUri.LocalPath + Path.DirectorySeparatorChar;
                var processFilename = Path.Combine(installFolder, package.Application.ExeFilename);
                Logger.LogInformation($"Starting process '{processFilename}'.");
                Process.Start(new ProcessStartInfo
                {
                    Arguments = package.Application.CommandLine,
                    FileName = processFilename,
                    WorkingDirectory = installFolder
                });
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, $"Unable to update application '{package.Application.Name}'.");
            }
        }

        /// <summary>
        /// Kills the specidfied process.
        /// </summary>
        /// <param name="exeFilename">Full exe filename of the process to kill.</param>
        /// <param name="logger">The application logger.  <see cref="Logger"./></param>
        public static async Task KillProcess(string exeFilename, ILogger logger)
        {
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeFilename)))
            {
                logger.LogInformation($"Killing process '{process.ProcessName}' with id '{process.Id}'.");
                process.Kill();
            }   // for each process
            await Task.Delay(1000);
        }

        /// <summary>
        /// Calculates a MD5 hash for the specified file.
        /// </summary>
        /// <param name="fileUri">The file URI of the file to checksum.</param>
        /// <returns>A MD5 hash for the specified file.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the fileUri is not a file URI/></exception>
        public static string CalculateMD5Checksum(Uri fileUri)
        {
            if (!fileUri.IsFile)
                throw new InvalidOperationException("Unable to calculate checksum because the URI is not a file URI.");

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileUri.LocalPath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }   // using stream
            }   // using MD5
        }

        public override string ToString()
        {
            return $"Manifest: {ManifestUri.ToString().ToLower()} Applications: {Applications.Count}";
        }

        #endregion
    }
}