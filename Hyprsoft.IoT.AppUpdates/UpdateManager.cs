﻿using Newtonsoft.Json;
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
using System.Net.Http.Headers;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Hyprsoft.IoT.AppUpdates.Tests")]
namespace Hyprsoft.IoT.AppUpdates
{
    public class UpdateManager
    {
        #region Fields

        private readonly object _lockObject = new object();
        private readonly ILogger<UpdateManager> _logger;
        private readonly ClientCredentials _clientCredentials;

        #endregion

        #region Constructors

        public UpdateManager(Uri manifestUri, ClientCredentials credentials, ILogger<UpdateManager> logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            ManifestUri = manifestUri ?? throw new ArgumentNullException(nameof(manifestUri));
            _clientCredentials = credentials;
            _logger = logger;
        }

        #endregion

        #region Properties

        public const string DefaultManifestFilename = "app-updates-manifest.json";

        /// <summary>
        /// Returns true if the app update manifest has been loaded; otherwise false.
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// Gets the app update manifest URI.
        /// </summary>
        public Uri ManifestUri { get; private set; }

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

            var version = (((AssemblyInformationalVersionAttribute)GetType().Assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))).InformationalVersion);
            _logger.LogInformation($"Update Manager v{version} loading manifest from '{ManifestUri.ToString().ToLower()}'.");
            if (ManifestUri.IsFile)
            {
                if (File.Exists(ManifestUri.LocalPath))
                    Applications = JsonConvert.DeserializeObject<List<Application>>(File.ReadAllText(ManifestUri.LocalPath));
            }
            else
            {
                using (var client = new HttpClient())
                    Applications = JsonConvert.DeserializeObject<List<Application>>(await client.GetStringAsync(ManifestUri));
            }

            // Hook up our hierarchy.
            foreach (var app in Applications)
            {
                app.UpdateManager = this;
                foreach (var package in app.Packages)
                    package.Application = app;
            }   // for each app.

            IsLoaded = true;
        }

        /// <summary>
        /// Saves the app update manifest.
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown when the <see cref="ManifestUri" is null./></exception>
        /// <exception cref="InvalidOperationException">Thrown when the <see cref="ManifestUri" is not a file URI/></exception>
        public void Save()
        {
            if (ManifestUri == null)
                throw new NullReferenceException($"The '{nameof(ManifestUri)}' cannot be null.");

            if (!ManifestUri.IsFile)
                throw new InvalidOperationException("The update manifest cannot be saved because it's not a file URI.");

            _logger.LogInformation($"Saving manifest to '{ManifestUri.ToString().ToLower()}'.");
            lock (_lockObject)
                File.WriteAllText(ManifestUri.LocalPath, JsonConvert.SerializeObject(Applications, Formatting.Indented));
        }

        /// <summary>
        /// Updates an app.
        /// </summary>
        /// <param name="package">The package used to update the app.</param>
        /// <param name="installUri">The install URI of the app.  Ex. c:\testapp.</param>
        /// <param name="token">Cancellation token.</param>
        /// <exception cref="ArgumentNullException">Thrown when package is null./></exception>
        /// <exception cref="ArgumentNullException">Thrown when installUri is null./></exception>
        public async Task Update(Package package, Uri installUri, CancellationToken token)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));

            if (installUri == null)
                throw new ArgumentNullException(nameof(installUri));

            if (token.IsCancellationRequested) return;

            // STEP 1 - Make sure our install folder exists
            if (!Directory.Exists(installUri.LocalPath))
                Directory.CreateDirectory(installUri.LocalPath);

            // STEP 2 - Check to see if our app needs to be updated.
            var versionFilename = Path.Combine(installUri.LocalPath, package.Application.VersionFilename).ToLower();
            _logger.LogInformation($"Checking '{package.Application.Name}' version using '{versionFilename}'.");
            if (File.Exists(versionFilename))
            {
                var fileVersion = FileVersionInfo.GetVersionInfo(versionFilename).FileVersion;
                _logger.LogInformation($"Version '{fileVersion}' found.");
                if (package.FileVersion == fileVersion)
                {
                    _logger.LogInformation($"Application is up to date.");
                    return;
                }   // file versions the same?
            }   // version file exists?
            else
                _logger.LogInformation($"'{package.Application.Name}' does not exist and will be installed.");


            string packageFilename = (package.SourceUri.IsFile ? package.SourceUri.LocalPath : Path.GetTempFileName()).ToLower();
            try
            {
                // STEP 3 - Download package.
                _logger.LogInformation($"Updating '{package.Application.Name}' using package '{package.SourceUri}'.");
                if (!package.SourceUri.IsFile)
                {
                    _logger.LogInformation($"Downloading package to '{packageFilename}'.");
                    using (var client = new HttpClient())
                    {
                        if (_clientCredentials != null && !String.IsNullOrWhiteSpace(_clientCredentials.ClientId) && !String.IsNullOrWhiteSpace(_clientCredentials.ClientSecret) &&
                            !String.IsNullOrWhiteSpace(_clientCredentials.Scope))
                        {
                            var response = await client.PostAsync($"{ManifestUri.Scheme}://{ManifestUri.Host}:{ManifestUri.Port}/appupdates/account/token",
                                new StringContent(JsonConvert.SerializeObject(_clientCredentials), Encoding.UTF8, "application/json"));
                            if (response.IsSuccessStatusCode)
                            {
                                var payload = JsonConvert.DeserializeAnonymousType(await response.Content.ReadAsStringAsync(), new { Token = String.Empty });
                                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Token);
                            }
                            else
                            {
                                _logger.LogWarning($"Package download failed.  Unable to authenticate. Status: {response.StatusCode}");
                                return;
                            }
                        }   // username and password not null?
                        File.WriteAllBytes(packageFilename, await client.GetByteArrayAsync(package.SourceUri));
                    }
                }   // file URI?

                // STEP 4 - Check package integrity.
                _logger.LogInformation($"Checking package checksum for '{packageFilename}'.");
                var checksum = CalculateMD5Checksum(new Uri(packageFilename));
                if (package.Checksum != checksum)
                {
                    _logger.LogWarning($"Checksum mismatch detected.  Expected '{package.Checksum}' and calculated '{checksum}'.");
                    return;
                }   // checksums match?

                // STEP 5 - Kill application process.
                await KillProcessIfRunning(package.Application.ExeFilename, _logger);

                // STEP 6 - Unzip our package to the install URI.
                _logger.LogInformation($"Extracting package '{packageFilename}' to '{installUri.LocalPath.ToLower()}'.");
                using (var zip = ZipFile.Open(packageFilename, ZipArchiveMode.Read))
                {
                    foreach (var entry in zip.Entries)
                    {
                        var filename = Path.Combine(installUri.LocalPath, entry.FullName);
                        var folder = Path.GetDirectoryName(filename);
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);
                        entry.ExtractToFile(filename, true);
                    }   // for each zip entry
                }   // using zip file.

                // STEP 7 - Restart our process.
                var installFolder = installUri.LocalPath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? installUri.LocalPath : installUri.LocalPath + Path.DirectorySeparatorChar;
                var processFilename = Path.Combine(installFolder, package.Application.ExeFilename);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _logger.LogInformation($"Running chmod 744 on '{processFilename}'.");
                    var process = Process.Start(new ProcessStartInfo
                    {
                        Arguments = $"744 {package.Application.ExeFilename}",
                        FileName = "chmod",
                        WorkingDirectory = installFolder
                    });
                    process.WaitForExit();
                }   // Linux?
                _logger.LogInformation($"Starting process '{processFilename}'.");
                Process.Start(new ProcessStartInfo
                {
                    Arguments = package.Application.CommandLine,
                    FileName = processFilename,
                    WorkingDirectory = installFolder
                });

                _logger.LogInformation($"'{package.Application.Name}' successfully updated to version '{package.FileVersion}'.");
            }
            finally
            {
                // If we downloaded our source package, make sure we cleanup our temp package file.
                if (!package.SourceUri.IsFile && File.Exists(packageFilename))
                {
                    _logger.LogInformation($"Deleting temporary package '{packageFilename}'.");
                    File.Delete(packageFilename);
                }
            }
        }

        /// <summary>
        /// Kills the specified process if running.
        /// </summary>
        /// <param name="exeFilename">Exe filename of the process to kill.</param>
        /// <param name="logger">The application logger.  <see cref="SimpleLogManager"./></param>
        public static async Task KillProcessIfRunning(string exeFilename, ILogger logger)
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

        public override string ToString() => $"Manifest: {ManifestUri.ToString().ToLower()} Applications: {Applications.Count}";

        #endregion
    }
}