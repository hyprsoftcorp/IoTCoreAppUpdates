using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Hyprsoft.IoT.AppUpdates
{
    public class Package
    {
        #region Properties

        [JsonIgnore]
        public Application Application { get; internal set; }

        [JsonProperty]
        public Guid Id { get; internal set; }

        [JsonProperty]
        public Version Version { get; internal set; }

        [JsonProperty]
        public DateTime ReleaseDateUtc { get; internal set; }

        [JsonProperty]
        public Uri SourceUri { get; internal set; }

        [JsonProperty]
        public string Checksum { get; internal set; }

        [JsonProperty]
        public List<Change> Changes { get; internal set; } = new List<Change>();

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"Id: {Id} Version: {Version} Released: {ReleaseDateUtc} Changes: {Changes.Count}";
        }

        #endregion
    }
}