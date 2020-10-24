using Newtonsoft.Json;

namespace w3c_update_service.Models.GithubModels
{
    public class GithubRelease
    {
        public string Name { get; set; }

        public string Body { get; set; }

        [JsonProperty("prerelease")]
        public bool IsPreRelease { get; set; }

        [JsonProperty("draft")]
        public bool IsDraft { get; set; }

        public GithubReleaseAsset[] Assets { get; set; }

        public bool IsProdRelease
        {
            get
            {
                return !IsDraft && !IsPreRelease;
            }
        }
    }
}
