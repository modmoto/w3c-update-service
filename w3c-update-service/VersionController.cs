using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace w3c_update_service
{
    [ApiController]
    [Route("api")]
    public class VersionController : ControllerBase
    {
        [HttpGet("client-version")]
        public IActionResult GetVersion()
        {
            var version = Directory.GetFiles("UpdateFiles").OrderByDescending(f => f).First().Split("_v")[1].Replace(".zip", "");
            return Ok(new { version });
        }

        [HttpGet("maps")]
        public IActionResult GetMaps()
        {
            return LoadFile("UpdateFiles/", "maps");
        }

        [HttpGet("webui")]
        public IActionResult GetWebUi()
        {
            return LoadFile("UpdateFiles/", "webui");
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
            var version = Directory.GetFiles("Launchers")
                .Where(f => f.EndsWith(".dmg"))
                .OrderByDescending(f => f)
                .First()
                .Split("-")[2]
                .Replace(".dmg", "");
            return Ok(new { version });
        }

        private static IActionResult ReturnResultFor(string fileEnding)
        {
            var strings = Directory.GetFiles("Installers");
            var ordered = strings.OrderByDescending(s => s);
            var filePath = ordered.First(f => f.EndsWith("." + fileEnding));
            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(dataBytes, $"application/{fileEnding}")
            {
                FileDownloadName = filePath.Split("/").Last()
            };
        }

        private static IActionResult LoadFile(string basePath, string fileNameStart)
        {
            var strings = Directory.GetFiles(basePath);
            var filePath = strings.Single(f => f.StartsWith(basePath + fileNameStart));
            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(dataBytes, "application/zip");
        }
    }

    public enum SupportedOs
    {
        mac, win
    }
}