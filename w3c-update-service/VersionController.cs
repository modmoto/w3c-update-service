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
        public IActionResult GetInstaller(SupportedOs type, [FromQuery] string version)
        {
            switch (type)
            {
                case SupportedOs.mac : return ReturnResultFor($"{version}.dmg");
                case SupportedOs.win : return ReturnResultFor($"{version}.exe");
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
            var strings = Directory.GetFiles(_launcherFolder);
            var ordered = strings.OrderByDescending(s => s);
            var filePath = ordered.First(f => f.EndsWith(fileEnding));
            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(dataBytes, $"application/{fileEnding}")
            {
                FileDownloadName = filePath.Split("/").Last()
            };
        }

        private static IActionResult LoadFile(string basePath, string fileNameStart)
        {
            var filePath = Directory.GetFiles(basePath)
                .Where(f => f.StartsWith(basePath + fileNameStart))
                .OrderByDescending(f => f)
                .First();
            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(dataBytes, "application/zip");
        }
    }

    public enum SupportedOs
    {
        mac, win
    }
}