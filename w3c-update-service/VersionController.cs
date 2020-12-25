using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using w3c_update_service.Cache;
using w3c_update_service.Models.GithubModels;

namespace w3c_update_service
{
    [ApiController]
    [Route("api")]
    public class VersionController : ControllerBase
    {
        private const int GithubReleaseCacheMunutes = 5;
        private const int CurrentVersion = 14;
        private static readonly string _launcherFolder = "Launchers";

        private readonly IHttpClientFactory _clientFactory;

        private static CachedData<Task<GithubReleaseResponse>> LauncherReleaseResponse;

        private static CachedData<Task<GithubReleaseResponse>> UpdateServiceReleaseResponse;


        public VersionController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;

            LauncherReleaseResponse ??= new CachedData<Task<GithubReleaseResponse>>(GetLatestLauncherReleaseFromGithub,
                TimeSpan.FromMinutes(GithubReleaseCacheMunutes));

            UpdateServiceReleaseResponse ??= new CachedData<Task<GithubReleaseResponse>>(GetLatestUpdateServiceReleaseFromGithub,
                TimeSpan.FromMinutes(GithubReleaseCacheMunutes));
        }

        [HttpGet("client-version")]
        public async Task<IActionResult> GetVersion()
        {
            //var latestRelease = await UpdateServiceReleaseResponse.GetCachedData();

            //if (latestRelease == null)
            //{
            //    return BadRequest("There was a problem getting data from github");
            //}

            return Ok(new { version = CurrentVersion });
        }

        [HttpGet("maps")]
        public async Task<IActionResult> GetMaps()
        {
            //var latestRelease = await UpdateServiceReleaseResponse.GetCachedData();

            //if (latestRelease == null)
            //{
            //    return BadRequest("There was a problem getting data from github");
            //}

            //var url = GetLinkToReleaseAssetByFileName(latestRelease, "maps");

            return Redirect($"https://github.com/w3champions/w3champions-update-service/releases/download/v{CurrentVersion}/maps_v{CurrentVersion}.zip");
        }

        [HttpGet("webui")]
        public async Task<IActionResult> GetWebUi(bool ptr)
        {
            //var latestRelease = await UpdateServiceReleaseResponse.GetCachedData();

            //if (latestRelease == null)
            //{
            //    return BadRequest("There was a problem getting data from github");
            //}

            //var url = GetLinkToReleaseAssetByFileName(latestRelease, ptr ? "ptr-webui" : "webui");

            if (ptr)
            {
              return Redirect($"https://github.com/w3champions/w3champions-update-service/releases/download/v{CurrentVersion}/ptr-webui.zip");
            }

            return Redirect($"https://github.com/w3champions/w3champions-update-service/releases/download/v{CurrentVersion}/webui.zip");
        }


        [HttpGet("launcher/{type}")]
        public async Task<IActionResult> GetInstaller(SupportedOs type, bool localFile)
        {
            // local file is used so we can distribute beta launchers on the test site
            if (localFile)
            {
                switch (type)
                {
                    case SupportedOs.mac : return DownloadLauncherFor("dmg");
                    case SupportedOs.win : return DownloadLauncherFor("exe");
                    default: return BadRequest("Unsupported OS Version");
                }
            }

            var latestRelease = await LauncherReleaseResponse.GetCachedData();

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
            var latestRelease = await LauncherReleaseResponse.GetCachedData();

            if (latestRelease == null)
            {
                return BadRequest("There was a problem getting data from github");
            }

            return Ok(new { version = latestRelease.Name });
        }

        private static IActionResult DownloadLauncherFor(string fileEnding)
        {
            var strings = Directory.GetFiles(_launcherFolder).Where(f => f.EndsWith(fileEnding)).ToList();
            var ordered = strings.OrderByDescending(s => s);
            var filePath = ordered.First();
            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(dataBytes, $"application/{fileEnding}")
            {
                FileDownloadName = filePath.Split("/").Last()
            };
        }

        private static string GetLinkToReleaseAssetByFileExtension(GithubReleaseResponse response, string fileExtension)
        {
            var releaseAsset = response.Assets.FirstOrDefault(x => x.Name.EndsWith(fileExtension));

            return releaseAsset?.BrowserDownloadUrl;
        }

        private static string GetLinkToReleaseAssetByFileName(GithubReleaseResponse response, string startsWithFileName)
        {
            var releaseAsset = response.Assets.FirstOrDefault(x => x.Name.StartsWith(startsWithFileName));

            return releaseAsset?.BrowserDownloadUrl;
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