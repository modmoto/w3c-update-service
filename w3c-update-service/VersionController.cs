using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace w3c_update_service
{
    [ApiController]
    [Route("api")]
    public class VersionController : ControllerBase
    {
        private static string _launcherFolder = "Launchers";
        private string _updateFileFolder = "UpdateFiles/";

        [HttpGet("client-version")]
        public IActionResult GetVersion()
        {
            var version = Directory.GetFiles(_updateFileFolder)
                .Where(f => f.StartsWith(_updateFileFolder + "maps"))
                .OrderByDescending(f => f)
                .First()
                .Split("_v")[1]
                .Replace(".zip", "");
            return Ok(new { version });
        }

        [HttpGet("maps")]
        public IActionResult GetMaps()
        {
            return LoadFile(_updateFileFolder, "maps");
        }

        [HttpGet("webui")]
        public IActionResult GetWebUi(bool ptr)
        {
            return LoadFile(_updateFileFolder, ptr ? "ptr-webui" : "webui");
        }


        [HttpGet("launcher/{type}")]
        public IActionResult GetInstaller(SupportedOs type)
        {
            switch (type)
            {
                case SupportedOs.mac : return ReturnResultFor("dmg");
                case SupportedOs.win : return ReturnResultFor("exe");
                default: return BadRequest("Unsupported OS Version");
;            }
        }

        [HttpGet("launcher-version")]
        public IActionResult GetInstallerVersion()
        {
            var version = Directory.GetFiles(_launcherFolder)
                .Where(f => f.EndsWith(".dmg"))
                .OrderByDescending(f => f)
                .First()
                .Split("-")[1]
                .Replace(".dmg", "");
            return Ok(new { version });
        }

        private static IActionResult ReturnResultFor(string fileEnding)
        {
            var strings = Directory.GetFiles(_launcherFolder).Where(f => f.EndsWith(fileEnding));
            var versions = strings.Select(s => new UpdateTo(s));
            var ordered = versions.OrderByDescending(s => s);
            var filePath = ordered.First();
            var dataBytes = System.IO.File.ReadAllBytes(filePath.Path);
            return new FileContentResult(dataBytes, $"application/{fileEnding}")
            {
                FileDownloadName = filePath.Path.Split("/").Last()
            };
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

            Patch = int.Parse(split[0]);
            Minor = int.Parse(split[1]);
            Major = int.Parse(split[2]);
        }

        public int CompareTo(UpdateTo other)
        {
            var version = Major * 10000 + Minor * 1000 + Patch * 100 - (other.Major * 10000 + other.Minor * 1000 +
                                                                        other.Patch * 100);
            return version;
        }
    }

    public enum SupportedOs
    {
        mac, win
    }
}