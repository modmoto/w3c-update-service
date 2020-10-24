﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using w3c_update_service.Cache;
using w3c_update_service.Models;
using w3c_update_service.Models.GithubModels;

namespace w3c_update_service
{
    [ApiController]
    [Route("api")]
    public class VersionController : ControllerBase
    {
        private const int GithubReleaseCacheMunutes = 5;

        private readonly IHttpClientFactory _clientFactory;

        private static CachedData<Task<GithubRelease>> LauncherLatestRelease;
        private static CachedData<Task<GithubRelease[]>> LauncherReleases;
        private static CachedData<Task<GithubRelease>> UpdateServiceReleaseReponse;


        public VersionController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;

            if (LauncherLatestRelease == null)
            {
                LauncherLatestRelease = new CachedData<Task<GithubRelease>>(GetLatestLauncherReleaseFromGithub, TimeSpan.FromMinutes(GithubReleaseCacheMunutes));
            }

            if (LauncherReleases == null)
            {
                LauncherReleases = new CachedData<Task<GithubRelease[]>>(GetLauncherReleasesFromGithub, TimeSpan.FromMinutes(GithubReleaseCacheMunutes));
            }

            if (UpdateServiceReleaseReponse == null)
            {
                UpdateServiceReleaseReponse = new CachedData<Task<GithubRelease>>(GetLatestUpdateServiceReleaseFromGithub, TimeSpan.FromMinutes(GithubReleaseCacheMunutes));
            }
        }

        [HttpGet("client-version")]
        public async Task<IActionResult> GetVersion()
        {
            //var latestRelease = await UpdateServiceReleaseReponse.GetCachedData();

            //if (latestRelease == null)
            //{
            //    return BadRequest("There was a problem getting data from github");
            //}

            return Ok(new { version = "12" });
        }

        [HttpGet("maps")]
        public async Task<IActionResult> GetMaps()
        {
            //var latestRelease = await UpdateServiceReleaseReponse.GetCachedData();

            //if (latestRelease == null)
            //{
            //    return BadRequest("There was a problem getting data from github");
            //}

            //var url = GetLinkToReleaseAssetByFileName(latestRelease, "maps");

            return Redirect("https://github.com/w3champions/w3champions-update-service/releases/download/v12/maps_v12.zip");
        }

        [HttpGet("webui")]
        public async Task<IActionResult> GetWebUi(bool ptr)
        {
            //var latestRelease = await UpdateServiceReleaseReponse.GetCachedData();

            //if (latestRelease == null)
            //{
            //    return BadRequest("There was a problem getting data from github");
            //}

            //var url = GetLinkToReleaseAssetByFileName(latestRelease, ptr ? "ptr-webui" : "webui");

            return Redirect("https://github.com/w3champions/w3champions-update-service/releases/download/v12/webui.zip");
        }


        [HttpGet("launcher/{type}")]
        public async Task<IActionResult> GetInstaller(SupportedOs type)
        {
            var latestRelease = await LauncherLatestRelease.GetCachedData();

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
            var latestRelease = await LauncherLatestRelease.GetCachedData();

            if (latestRelease == null)
            {
                return BadRequest("There was a problem getting data from github");
            }

            return Ok(new { version = latestRelease.Name });
        }

        [HttpGet("launcher-release-notes")]
        public async Task<IActionResult> GetLauncherReleaseNotes(bool ptr)
        {
            var releases = await LauncherReleases.GetCachedData();

            if (releases == null)
            {
                return BadRequest("There was a problem getting data from github");
            }

            if (!ptr)
            {
                releases = releases.Where(x => x.IsProdRelease).ToArray();
            }

            var releaseNotes = releases
                .Select(x => new ReleaseNotes() { Version = x.Name, Notes = x.Body })
                .ToArray();

            return Ok(releaseNotes);
        }

        private static string GetLinkToReleaseAssetByFileExtension(GithubRelease response, string fileExtension)
        {
            var releaseAsset = response.Assets.FirstOrDefault(x => x.Name.EndsWith(fileExtension));

            return releaseAsset?.BrowserDownloadUrl;
        }

        private static string GetLinkToReleaseAssetByFileName(GithubRelease response, string startsWithFileName)
        {
            var releaseAsset = response.Assets.FirstOrDefault(x => x.Name.StartsWith(startsWithFileName));

            return releaseAsset?.BrowserDownloadUrl;
        }

        private async Task<GithubRelease> GetLatestLauncherReleaseFromGithub()
        {
            return await GetLatestReleaseFromRepo("w3champions/w3champions-launcher");
        }

        private async Task<GithubRelease[]> GetLauncherReleasesFromGithub()
        {
            return await GetReleasesFromRepo("w3champions/w3champions-launcher");
        }

        private async Task<GithubRelease> GetLatestUpdateServiceReleaseFromGithub()
        {
            return await GetLatestReleaseFromRepo("w3champions/w3champions-update-service");
        }

        private async Task<GithubRelease> GetLatestReleaseFromRepo(string repoName)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repoName}/releases/latest");
            request.Headers.Add("Accept", "application/vnd.github.v3+json");
            request.Headers.Add("User-Agent", "Asp.WebApi");

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var reponseString = await response.Content.ReadAsStringAsync();
                var releaseResposne = JsonConvert.DeserializeObject<GithubRelease>(reponseString);
                return releaseResposne;
            }

            return null;
        }

        private async Task<GithubRelease[]> GetReleasesFromRepo(string repoName)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repoName}/releases");
            request.Headers.Add("Accept", "application/vnd.github.v3+json");
            request.Headers.Add("User-Agent", "Asp.WebApi");

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var reponseString = await response.Content.ReadAsStringAsync();
                var releaseResposne = JsonConvert.DeserializeObject<GithubRelease[]>(reponseString);
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