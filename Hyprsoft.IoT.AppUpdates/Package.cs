using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Hyprsoft.IoT.AppUpdates
{
    public class Package
    {
        #region Properties

        [JsonIgnore]
        public Application Application { get; set; }

        [Required, JsonProperty]
        public Guid Id { get; set; }

        [Display(Name = "Available?")]
        public bool IsAvailable { get; set; } = true;

        [Required, JsonProperty, Display(Name = "Version")]
        public string FileVersion { get; set; }

        [Required, JsonProperty, Display(Name = "Released"), DataType(DataType.Date), DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime ReleaseDateUtc { get; set; }

        [Required, JsonProperty, Display(Name = "Source")]
        public Uri SourceUri { get; set; }

        [Required, JsonProperty]
        public string Checksum { get; set; }

        #endregion

        #region Methods

        public override string ToString() => $"Id: {Id} Version: {FileVersion} Released: {ReleaseDateUtc}";

        #endregion
    }
}