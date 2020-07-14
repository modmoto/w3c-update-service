using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace w3c_update_service
{
    [ApiController]
    [Route("api")]
    public class VersionController : ControllerBase
    {
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            var version = int.Parse(Directory.GetFiles("UpdateFiles").First().Split("_v")[1].Replace(".zip", ""));
            return Ok(new { version });
        }

        [HttpGet("maps")]
        public IActionResult GetMaps()
        {
            return LoadFile("maps");
        }

        [HttpGet("webui")]
        public IActionResult GetWebUi()
        {
            return LoadFile("webui");
        }

        private static IActionResult LoadFile(string fileNameStart)
        {
            var strings = Directory.GetFiles("UpdateFiles");
            var filePath = strings.Single(f => f.StartsWith("UpdateFiles/" + fileNameStart));
            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            return new FileContentResult(dataBytes, "application/zip");
        }

    }
}