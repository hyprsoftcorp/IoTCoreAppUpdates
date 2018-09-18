using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hyprsoft.IoT.AppUpdates
{
    public class Application
    {
        #region Properties

        [JsonIgnore]
        public UpdateManager UpdateManager { get; internal set; }

        [JsonProperty]
        public Guid Id { get; internal set; }

        [JsonProperty]
        public string Name { get; internal set; }

        [JsonProperty]
        public string Description { get; internal set; }

        [JsonProperty]
        public string ExeFilename { get; internal set; }

        [JsonProperty]
        public string VersionFilename { get; internal set; }

        [JsonProperty]
        public string CommandLine { get; internal set; }

        [JsonProperty]
        public List<Package> Packages { get; internal set; } = new List<Package>();

        #endregion

        #region Methods

        public Package GetLatestPackage()
        {
            return Packages.OrderByDescending(p => p.ReleaseDateUtc).FirstOrDefault();
        }

        public override string ToString()
        {
            return $"Id: {Id} Name: {Name} Exe: {ExeFilename} Packages: {Packages.Count}";
        }

        #endregion
    }
}
