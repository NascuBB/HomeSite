using HomeSite.Entities;
using HomeSite.Helpers;
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
		private readonly UserDBContext _usersContext;
		private readonly ISharedAdministrationManager _sharedManager;
        private readonly IUserHelper _userHelper;
		public FileshareAPIController(UserDBContext userDBContext, ISharedAdministrationManager sharedAdministration, IUserHelper userHelper)
		{
			_sharedManager = sharedAdministration;
			_usersContext = userDBContext;
            _userHelper = userHelper;
		}

		[HttpPost]
        [Route("uploadfile")]
		[RequestSizeLimit(1073741824)]
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

        [HttpGet("getlogs")]
        public async Task<IActionResult> GetLogs(string id)
        {
            if(HttpContext.User.Identity.Name == null || !MinecraftServerManager.ServerExists(id))
            {
                return NotFound();
            }

                if (_usersContext.UserAccounts.Find(_userHelper.GetUserId(HttpContext.User.Identity.Name)).serverid != id)
                    if(!_sharedManager.HasSharedThisServer(id, HttpContext.User.Identity.Name))
                    //|| SharedAdministrationManager.GetUserSharedRights(HttpContext.User.Identity.Name, id).)
                    {
                        return Unauthorized();
                    }

            string filepath = Path.Combine(MinecraftServerManager.folder, id, "logs", "latest.log");

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filepath, out var contenttype))
            {
                contenttype = "application/octet-stream";
            }

            byte[]? bytes = null;

			try
            {
                bytes = await System.IO.File.ReadAllBytesAsync(filepath);
            }
            catch (Exception ex)
            {
                if(ex is IOException)
                {
                    string temp = Path.Combine(MinecraftServerManager.folder, id, "logs", "temp.log");
					System.IO.File.Copy(filepath, temp, true);
					bytes = await System.IO.File.ReadAllBytesAsync(temp);
                    System.IO.File.Delete(temp);
				}
                else
                {
                    throw;
				}
				if (bytes == null)
				{
					throw;
				}
			}
            return File(bytes, contenttype, "latest.log");
        }

		[HttpGet]
		[Route("downloadlogs")]
		public async Task<IActionResult> DownloadLogs(string id)
		{
			//ShareFileInfo? sharedFile = FileShareManager.SharedFiles!.Find(x => x.Filename.Split('.')[0] == id);
			//if (sharedFile == null) return NotFound();
            if(HttpContext.User.Identity.Name == null)
            {
                return Unauthorized();
            }

			string filepath = Path.Combine(MinecraftServerManager.folder, id, "logs", "latest.log");

			var provider = new FileExtensionContentTypeProvider();
			if (!provider.TryGetContentType(filepath, out var contenttype))
			{
				contenttype = "application/octet-stream";
			}

			var bytes = await System.IO.File.ReadAllBytesAsync(filepath);
			return File(bytes, contenttype, "latest.log");
		}
	}
}
