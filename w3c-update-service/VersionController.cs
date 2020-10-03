using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using w3c_update_service.Models;
using w3c_update_service.Models.GithubModels;

namespace w3c_update_service
{
    [ApiController]
    [Route("api")]
    public class VersionController : ControllerBase
    {
        private string _updateFileFolder = "UpdateFiles/";

        private readonly IHttpClientFactory _clientFactory;

        public VersionController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet("client-version")]
        public IActionResult GetVersion()
        {
            var latestMapData = GetLatestMapsData();
            return Ok(new { version = latestMapData.Version.ToString() });
        }

        [HttpGet("maps")]
        public IActionResult GetMaps()
        {
            var latestMapData = GetLatestMapsData();

            return LoadFileDirectly(latestMapData.FilePath, "maps");
        }

        [HttpGet("webui")]
        public IActionResult GetWebUi(bool ptr)
        {
            return LoadFile(_updateFileFolder, ptr ? "ptr-webui" : "webui");
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

            var url = GetLinkToReleaseAsset(latestRelease, fileExtension);

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

        private static string GetLinkToReleaseAsset(GithubReleaseResponse response, string fileExtension)
        {
            var releaseAsset = response.Assets.FirstOrDefault(x => x.Name.EndsWith(fileExtension));

            return releaseAsset?.BrowserDownloadUrl;
        }

        private static IActionResult LoadFile(string basePath, string fileNameStart)
        {
            var filePath = Directory.GetFiles(basePath)
                .Where(f => f.StartsWith(basePath + fileNameStart))
                .OrderByDescending(f => f)
                .First();
            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            var fileContentResult = new FileContentResult(dataBytes, "application/zip");
            fileContentResult.FileDownloadName = $"{fileNameStart}.zip";
            return fileContentResult;
        }

        private static IActionResult LoadFileDirectly(string filePath, string downloadFileName)
        {
            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            var fileContentResult = new FileContentResult(dataBytes, "application/zip");
            fileContentResult.FileDownloadName = $"{downloadFileName}.zip";
            return fileContentResult;
        }

        private MapsData GetLatestMapsData()
        {
            var mapFiles = Directory.GetFiles(_updateFileFolder)
             .Where(f => f.StartsWith(_updateFileFolder + "maps"));

            List<MapsData> mapsData = new List<MapsData>();
            foreach (var mapFile in mapFiles)
            {
                int version = 0;

                if(int.TryParse(mapFile.Split("_v")[1].Replace(".zip", ""), out version))
                {
                    var mapData = new MapsData() { FilePath = mapFile, Version = version };
                    mapsData.Add(mapData);
                }
            }

            return mapsData.OrderByDescending(x => x.Version).First();
        }

        private async Task<GithubReleaseResponse> GetLatestLauncherReleaseFromGithub()
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
            "https://api.github.com/repos/w3champions/w3champions-launcher/releases/latest");
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

    class UpdateTo : IComparable<UpdateTo>
    {
        public int Patch { get; }
        public int Minor { get; }
        public int Major { get; }
        public string Path { get; }

        public UpdateTo(string path)
        {
            Path = path;

            var split = path
                .Split("/").Last()
                .Replace(".exe", "")
                .Replace(".dmg", "")
                .Replace(".AppImage", "")
                .Replace("w3champions Setup ", "")
                .Replace("w3champions-", "")
                .Split(".");

            Patch = int.Parse(split[2]);
            Minor = int.Parse(split[1]);
            Major = int.Parse(split[0]);
        }

        public int CompareTo(UpdateTo other)
        {
            if (Major != other.Major) return Major - other.Major;
            if (Minor != other.Minor) return Minor - other.Minor;
            return Patch - other.Patch;
        }
    }

    public enum SupportedOs
    {
        mac, win
    }
}