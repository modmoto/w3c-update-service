using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using w3c_update_service.Models.GithubModels;

namespace w3c_update_service
{
    [ApiController]
    [Route("api")]
    public class VersionController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;

        public VersionController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet("client-version")]
        public async Task<IActionResult> GetVersion()
        {
            var latestRelease = await GetLatestUpdateServiceReleaseFromGithub();

            if (latestRelease == null)
            {
                return BadRequest("There was a problem getting data from github");
            }

            return Ok(new { version = latestRelease.Name });
        }

        [HttpGet("maps")]
        public async Task<IActionResult> GetMaps()
        {
            var latestRelease = await GetLatestUpdateServiceReleaseFromGithub();

            if (latestRelease == null)
            {
                return BadRequest("There was a problem getting data from github");
            }

            var url = GetLinkToReleaseAssetByFileName(latestRelease, "maps");

            return Redirect(url);
        }

        [HttpGet("webui")]
        public async Task<IActionResult> GetWebUi(bool ptr)
        {
            var latestRelease = await GetLatestUpdateServiceReleaseFromGithub();

            if (latestRelease == null)
            {
                return BadRequest("There was a problem getting data from github");
            }

            var url = GetLinkToReleaseAssetByFileName(latestRelease, ptr ? "ptr-webui" : "webui");

            return Redirect(url);
        }


        [HttpGet("launcher/{type}")]
        public async Task<IActionResult> GetInstaller(SupportedOs type)
        {
            var latestRelease = await GetLatestLauncherReleaseFromGithub();

            if (latestRelease == null)
            {
                return BadRequest("There was a problem getting data from github");
            }

            string fileExtension;
            switch (type)
            {
                case SupportedOs.mac: {
                        fileExtension = "dmg";
                        break;
                    }
                case SupportedOs.win:
                    {
                        fileExtension = "exe";
                        break;
                    }
                default:
                   return BadRequest("Unsupported OS Version");
            }

            var url = GetLinkToReleaseAssetByFileExtension(latestRelease, fileExtension);

            return Redirect(url);
        }

        [HttpGet("launcher-version")]
        public async Task<IActionResult> GetInstallerVersion()
        {
            var latestRelease = await GetLatestLauncherReleaseFromGithub();

            if (latestRelease == null)
            {
                return BadRequest("There was a problem getting data from github");
            }

            return Ok(new { version = latestRelease.Name });
        }

        private static string GetLinkToReleaseAssetByFileExtension(GithubReleaseResponse response, string fileExtension)
        {
            var releaseAsset = response.Assets.FirstOrDefault(x => x.Name.EndsWith(fileExtension));

            return releaseAsset?.BrowserDownloadUrl;
        }

        private static string GetLinkToReleaseAssetByFileName(GithubReleaseResponse response, string startsWithFileName)
        {
            var releaseAsset = response.Assets.FirstOrDefault(x => x.Name.StartsWith(startsWithFileName));

            return $"https://cors-anywhere.herokuapp.com/{releaseAsset?.BrowserDownloadUrl}";
        }

        private async Task<GithubReleaseResponse> GetLatestLauncherReleaseFromGithub()
        {
            return await GetLatestReleaseFromRepo("w3champions/w3champions-launcher");
        }

        private async Task<GithubReleaseResponse> GetLatestUpdateServiceReleaseFromGithub()
        {
            return await GetLatestReleaseFromRepo("w3champions/w3champions-update-service");
        }

        private async Task<GithubReleaseResponse> GetLatestReleaseFromRepo(string repoName)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repoName}/releases/latest");
            request.Headers.Add("Accept", "application/vnd.github.v3+json");
            request.Headers.Add("User-Agent", "Asp.WebApi");

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var reponseString = await response.Content.ReadAsStringAsync();
                var releaseResposne = JsonConvert.DeserializeObject<GithubReleaseResponse>(reponseString);
                return releaseResposne;
            }

            return null;
        }
    }

    public enum SupportedOs
    {
        mac, win
    }
}