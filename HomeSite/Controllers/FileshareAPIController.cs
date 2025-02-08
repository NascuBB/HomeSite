using HomeSite.Managers;
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
		[RequestSizeLimit(209715200)]
		[ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> UploadFile(IFormFile file, CancellationToken cancellationtoken)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                return BadRequest("Not logged in");
            }
            try
            {
                var result = await FileShareManager.WriteFile(file, HttpContext.User.Identity.Name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("uploadmods")]
        [RequestSizeLimit(1073741824)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Uploadmods(IFormFile file, CancellationToken cancellationtoken)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                return BadRequest("Not logged in");
            }
            var Id = HttpContext.Request.Query["Id"].ToString();
            try
            {
                await FileShareManager.WriteAndUnpackMods(file, Id);
                return Ok();
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
