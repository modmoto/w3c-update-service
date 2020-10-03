using Newtonsoft.Json;

namespace w3c_update_service.Models.GithubModels
{
    public class GithubReleaseAsset
    {
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrownserDownloadUrl { get; set; }
    }
}
