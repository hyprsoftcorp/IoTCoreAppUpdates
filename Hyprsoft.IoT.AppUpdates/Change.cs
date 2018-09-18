using Newtonsoft.Json;

namespace Hyprsoft.IoT.AppUpdates
{
    public class Change
    {
        #region Properties

        [JsonIgnore]
        public Package Package { get; internal set; }

        [JsonProperty]
        public string Title { get; internal set; }

        [JsonProperty]
        public string Notes { get; internal set; }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"TItle: {Title}";
        }

        #endregion
    }
}
