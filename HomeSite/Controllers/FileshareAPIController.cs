using HomeSite.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace HomeSite.Controllers
{
    [Route("shared/")]
    [ApiController]
    public class FileshareAPIController : ControllerBase
    {
        [HttpPost]
        [Route("uploadfile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationtoken)
        {
            try
            {
                var result = await FileShareManager.WriteFile(file);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("downloadfile")]
        public async Task<IActionResult> DownloadFile(string id)
        {
            ShareFileInfo? sharedFile = FileShareManager.SharedFiles!.Find(x => x.Filename.Split('.')[0] == id);
            if (sharedFile == null) return NotFound();

            string filepath = Path.Combine(FileShareManager.folder, sharedFile.Filename);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filepath, out var contenttype))
            {
                contenttype = "application/octet-stream";
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(filepath);
            return File(bytes, contenttype, sharedFile.OriginalFilename);
        }
    }
}
