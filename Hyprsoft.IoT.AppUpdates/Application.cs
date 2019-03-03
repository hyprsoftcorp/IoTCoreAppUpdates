using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Hyprsoft.IoT.AppUpdates
{
    public class Application
    {
        #region Properties

        [JsonIgnore]
        public UpdateManager UpdateManager { get; set; }

        [Required, JsonProperty]
        public Guid Id { get; set; }

        [Required, JsonProperty]
        public string Name { get; set; }

        [Required, JsonProperty]
        public string Description { get; set; }

        [Required, JsonProperty, Display(Name = "Launch Filename")]
        public string ExeFilename { get; set; }

        [Required, JsonProperty, Display(Name = "Version Filename")]
        public string VersionFilename { get; set; }

        [JsonProperty, Display(Name = "Arguments")]
        public string CommandLine { get; set; }

        [JsonProperty, Display(Name = "Before Install Command")]
        public string BeforeInstallCommand { get; set; }

        [JsonProperty, Display(Name = "After Install Command")]
        public string AfterInstallCommand { get; set; }

        [JsonProperty]
        public List<Package> Packages { get; set; } = new List<Package>();

        #endregion

        #region Methods

        public Package GetLatestPackage()
        {
            return Packages.Where(p => p.IsAvailable).OrderByDescending(p => p.ReleaseDateUtc).FirstOrDefault();
        }

        public override string ToString() => $"Id: {Id} Name: {Name} Exe: {ExeFilename} Packages: {Packages.Count}";

        #endregion
    }
}
