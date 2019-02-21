using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.IoT.AppUpdates.Tests
{
    [TestClass]
    public class UpdateManagerTests
    {
        #region Fields

        private string _testDataFolder;
        private string _testInstallFolder;
        private Uri _manifestUri;
        private UpdateManager _manager;

        #endregion

        #region Methods

        [TestInitialize]
        public void Initialize()
        {
            _testDataFolder = Path.Combine(Path.GetDirectoryName(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName), "..\\Testing\\Data");
            _testInstallFolder = Path.Combine(Path.GetDirectoryName(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName), "..\\Testing\\Install");
            _manifestUri = new Uri(Path.Combine(_testDataFolder, UpdateManager.DefaultManifestFilename));
            _manager = CreateManager(_manifestUri);
            CreateUnitTestAppUpdateManifest();
        }

        [TestMethod]
        public void Defaults()
        {
            var manager = CreateManager(_manifestUri);
            Assert.IsFalse(manager.IsLoaded);
            Assert.AreEqual(_manifestUri, manager.ManifestUri);
            Assert.AreEqual(0, manager.Applications.Count);
        }

        [TestMethod]
        public async Task BadManifest()
        {
            var manager = CreateManager(new Uri("https://www.google.com/" + UpdateManager.DefaultManifestFilename));
            await Assert.ThrowsExceptionAsync<HttpRequestException>(async () => await manager.Load());
        }

        [TestMethod]
        public async Task Loaded()
        {
            await _manager.Load();

            Assert.IsTrue(_manager.IsLoaded);
            Assert.AreEqual(2, _manager.Applications.Count);

            Assert.AreEqual(2, _manager.Applications[0].Packages.Count);
            Assert.AreEqual(3, _manager.Applications[1].Packages.Count);
        }

        [TestMethod]
        public async Task Saved()
        {
            await _manager.Load();
            _manager.Applications.Clear();
            _manager.Save();

            var manager = CreateManager(_manifestUri);
            Assert.IsFalse(manager.IsLoaded);
            await manager.Load();

            Assert.IsTrue(manager.IsLoaded);
            Assert.AreEqual(_manifestUri, manager.ManifestUri);
            Assert.AreEqual(0, manager.Applications.Count);
        }

        [TestMethod]
        public async Task Update()
        {
            var installUri = new Uri(Path.Combine(_testInstallFolder, "Test App 01"));

            await _manager.Load();

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _manager.Update(null, installUri, CancellationToken.None));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _manager.Update(_manager.Applications.First().Packages.First(), null, CancellationToken.None));

            await ValidateUpdate(_manager, Guid.Parse("02902554-4D3D-4159-B106-41E0AC158733"), installUri);
            await ValidateUpdate(_manager, Guid.Parse("61038014-97C6-418A-9262-94D78DB167E8"), installUri);
        }

        [TestMethod]
        public async Task MultipleApps()
        {
            await _manager.Load();

            await ValidateUpdate(_manager, Guid.Parse("61038014-97C6-418A-9262-94D78DB167E8"), new Uri(Path.Combine(_testInstallFolder, "Test App 01")));
            await ValidateUpdate(_manager, Guid.Parse("941D4BF3-4F6B-4D16-B254-0C2A6BA3808B"), new Uri(Path.Combine(_testInstallFolder, "Test App 02")));
        }

        [TestMethod]
        public async Task LatestPackage()
        {
            await _manager.Load();

            // IsAvailable
            Assert.AreEqual(_manager.Applications[0].GetLatestPackage(), _manager.Applications.SelectMany(a => a.Packages).First(p => p.Id == Guid.Parse("61038014-97C6-418A-9262-94D78DB167E8")));
            _manager.Applications[0].Packages[1].IsAvailable = false;
            Assert.AreEqual(_manager.Applications[0].GetLatestPackage(), _manager.Applications.SelectMany(a => a.Packages).First(p => p.Id == Guid.Parse("02902554-4D3D-4159-B106-41E0AC158733")));

            // ReleasedDateUtc
            Assert.AreEqual(_manager.Applications[1].GetLatestPackage(), _manager.Applications.SelectMany(a => a.Packages).First(p => p.Id == Guid.Parse("941D4BF3-4F6B-4D16-B254-0C2A6BA3808B")));
            _manager.Applications[1].Packages[0].ReleaseDateUtc = DateTime.UtcNow;
            Assert.AreEqual(_manager.Applications[1].GetLatestPackage(), _manager.Applications.SelectMany(a => a.Packages).First(p => p.Id == Guid.Parse("97630359-BCDC-401F-917E-F940483B5A71")));
        }

        private UpdateManager CreateManager(Uri manifestUri)
        {
            var loggerFactory = new LoggerFactory();
            return new UpdateManager(manifestUri, null, loggerFactory.CreateLogger<UpdateManager>());
        }

        private async Task ValidateUpdate(UpdateManager manager, Guid packageId, Uri installUri)
        {
            var package = manager.Applications.SelectMany(a => a.Packages).First(p => p.Id == packageId);
            await manager.Update(package, installUri, CancellationToken.None);
            using (var zip = ZipFile.OpenRead(package.SourceUri.LocalPath))
                Assert.AreEqual(zip.Entries.Count, CountFiles(installUri.LocalPath));
            Assert.AreEqual(package.FileVersion.ToString(), FileVersionInfo.GetVersionInfo(Path.Combine(installUri.LocalPath, package.Application.VersionFilename)).FileVersion);
        }

        private int CountFiles(string folder)
        {
            var count = Directory.GetFiles(folder).Length;
            foreach (var f in Directory.GetDirectories(folder))
                count += CountFiles(f);
            return count;
        }

        private void CreateUnitTestAppUpdateManifest()
        {
            _manager.Applications.Add(new Application
            {
                Id = Guid.Parse("04FC007E-DB18-430F-B4FA-F5B54DE1E142"),
                Name = "Test App 01",
                Description = "Test App 01",
                ExeFilename = "hyprsoft.iot.appupdates.testapp.exe",
                VersionFilename = "hyprsoft.iot.appupdates.testapp.dll",
                CommandLine = "param1",
                Packages = new List<Package>
                {
                    new Package
                    {
                        Id = Guid.Parse("02902554-4D3D-4159-B106-41E0AC158733"),
                        ReleaseDateUtc = DateTime.Parse("10/01/2018"),
                        FileVersion = "1.0.0.0",
                        SourceUri = new Uri($"{Path.Combine(_testDataFolder, "testapp01_1000.zip")}"),
                        Checksum = UpdateManager.CalculateMD5Checksum(new Uri($"{Path.Combine(_testDataFolder, "testapp01_1000.zip")}"))
                    },
                    new Package
                    {
                        Id = Guid.Parse("61038014-97C6-418A-9262-94D78DB167E8"),
                        ReleaseDateUtc = DateTime.Parse("10/02/2018"),
                        FileVersion ="1.0.1.0",
                        SourceUri = new Uri($"{Path.Combine(_testDataFolder, "testapp01_1010.zip")}"),
                        Checksum = UpdateManager.CalculateMD5Checksum(new Uri($"{Path.Combine(_testDataFolder, "testapp01_1010.zip")}"))
                    }
                }
            });
            _manager.Applications.Add(new Application
            {
                Id = Guid.Parse("48CF6DAC-F378-4D8B-870C-3D331D98404E"),
                Name = "Test App 02",
                Description = "Test App 02",
                ExeFilename = "hyprsoft.iot.appupdates.testapp.exe",
                VersionFilename = "hyprsoft.iot.appupdates.testapp.dll",
                CommandLine = "param1 param2",
                Packages = new List<Package>
                {
                    new Package
                    {
                        Id = Guid.Parse("97630359-BCDC-401F-917E-F940483B5A71"),
                        ReleaseDateUtc = DateTime.Parse("10/01/2018"),
                        FileVersion = "1.0.0.0",
                        SourceUri = new Uri($"{Path.Combine(_testDataFolder, "testapp02_1000.zip")}"),
                        Checksum = UpdateManager.CalculateMD5Checksum(new Uri($"{Path.Combine(_testDataFolder, "testapp02_1000.zip")}")),
                    },
                    new Package
                    {
                        Id = Guid.Parse("1783071C-AE5A-431C-BD84-3845D822F0BC"),
                        ReleaseDateUtc = DateTime.Parse("10/02/2018"),
                        FileVersion = "1.0.1.0",
                        SourceUri = new Uri($"{Path.Combine(_testDataFolder, "testapp02_1010.zip")}"),
                        Checksum = UpdateManager.CalculateMD5Checksum(new Uri($"{Path.Combine(_testDataFolder, "testapp02_1010.zip")}"))
                    },
                    new Package
                    {
                        Id = Guid.Parse("941D4BF3-4F6B-4D16-B254-0C2A6BA3808B"),
                        ReleaseDateUtc = DateTime.Parse("10/03/2018"),
                        FileVersion = "1.0.2.0",
                        SourceUri = new Uri($"{Path.Combine(_testDataFolder, "testapp02_1020.zip")}"),
                        Checksum = UpdateManager.CalculateMD5Checksum(new Uri($"{Path.Combine(_testDataFolder, "testapp02_1020.zip")}"))
                    }
                }
            });
            _manager.Save();
        }

        #endregion
    }
}