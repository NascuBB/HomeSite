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
        private readonly IFileShareManager _fileShareManager;
        private readonly IMinecraftServerManager _minecraftServerManager;
        public FileshareAPIController(UserDBContext userDBContext, ISharedAdministrationManager sharedAdministration,
            IUserHelper userHelper, IFileShareManager fileShareManager, IMinecraftServerManager minecraftServerManager)
        {
            _sharedManager = sharedAdministration;
            _usersContext = userDBContext;
            _userHelper = userHelper;
            _fileShareManager = fileShareManager;
            _minecraftServerManager = minecraftServerManager;
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
            long used = _userHelper.GetUserSizeUsed(HttpContext.User.Identity.Name);
            if (used >= 1073741824 || used + file.Length >= 1073741824) return BadRequest("No storage left");
            try
            {
                var result = await _fileShareManager.WriteFile(file, HttpContext.User.Identity.Name);
                _userHelper.ChangeUserSizeUsed(file.Length, HttpContext.User.Identity.Name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("checkupload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public IActionResult CheckUpload([FromBody] long fileSize)
        {
            var username = HttpContext.User.Identity?.Name;
            if (username == null)
                return BadRequest("Not logged in");

            var currentSize = _userHelper.GetUserSizeUsed(username);
            const long maxSize = 1073741824;

            if (currentSize + fileSize > maxSize)
                return BadRequest("Недостаточно места на диске");

            return Ok("Можно загружать");
        }

        [HttpDelete]
        [Route("deletefile")]
        public async Task<IActionResult> DeleteFile(string id)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                return BadRequest("Not logged in");
            }
            if (!long.TryParse(id, out long fileId)) return NotFound();
            ShareFileInfo? sharedFile = _fileShareManager.GetFile(fileId);
            if (sharedFile == null) return NotFound();
            if (sharedFile.UserId != _userHelper.GetUserId(HttpContext.User.Identity.Name)) return NotFound();
            if (_fileShareManager.DeleteFile(fileId))
            {
                return Ok();
            }
            else
            {
                return Problem(statusCode: 500, title: "Error deleting file", detail: "An error occured while deleting file.");
            }

        }

        [HttpPost]
        [Route("changeshare")]
        public async Task<IActionResult> ChangeShare([FromQuery] string id, [FromBody] string selectedValue)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                return BadRequest("Not logged in");
            }
            if (!long.TryParse(id, out long fileId)) return NotFound();
            ShareFileInfo? sharedFile = _fileShareManager.GetFile(fileId);
            if (sharedFile == null) return NotFound();
            if (sharedFile.UserId != _userHelper.GetUserId(HttpContext.User.Identity.Name)) return NotFound();
            if (!bool.TryParse(selectedValue, out bool newShare)) return BadRequest();

            if (_fileShareManager.ChangeShareOfFile(fileId, newShare))
                return Ok();
            else
                return Problem(statusCode: 500, title: "Error sharing file", detail: "An error occured while sharing file.");
        }

        [HttpPost]
        [Route("feature")]
        public async Task<IActionResult> FeatureFile([FromQuery] string id, [FromBody] string feat)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                return BadRequest("Not logged in");
            }
            if (!long.TryParse(id, out long fileId)) return NotFound();
            ShareFileInfo? sharedFile = _fileShareManager.GetFile(fileId);
            if (sharedFile == null) return NotFound();
            if (sharedFile.UserId != _userHelper.GetUserId(HttpContext.User.Identity.Name)) return NotFound();
            if (!bool.TryParse(feat, out bool newFeat)) return BadRequest();

            if (_fileShareManager.ChangeFeatureOfFile(fileId, newFeat))
                return Ok();
            else
                return Problem(statusCode: 500, title: "Error featuring file", detail: "An error occured while featuring file.");
        }

        [HttpPost]
        [Route("rename")]
        public async Task<IActionResult> RenameFile([FromQuery] string id, [FromBody] string newName)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                return BadRequest("Not logged in");
            }
            if (!long.TryParse(id, out long fileId)) return NotFound();
            ShareFileInfo? sharedFile = _fileShareManager.GetFile(fileId);
            if (sharedFile == null) return NotFound();
            if (sharedFile.UserId != _userHelper.GetUserId(HttpContext.User.Identity.Name)) return NotFound();

            if (_fileShareManager.RenameFile(fileId, newName))
                return Ok();
            else
                return Problem(statusCode: 500, title: "Error shering file", detail: "An error occured while sharing file.");
        }

        [HttpGet]
        [Route("filename")]
        public async Task<IActionResult> GetFilename([FromQuery] string id)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                return BadRequest("Not logged in");
            }
            if (!long.TryParse(id, out long fileId)) return NotFound();
            ShareFileInfo? sharedFile = _fileShareManager.GetFile(fileId);
            if (sharedFile == null) return NotFound();
            if (sharedFile.UserId != _userHelper.GetUserId(HttpContext.User.Identity.Name)) return NotFound();
            return Ok(sharedFile.OriginalName);
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
            if(!long.TryParse(id, out long fileId)) return NotFound();
            ShareFileInfo? sharedFile = _fileShareManager.GetFile(fileId);
            if (sharedFile == null) return NotFound();
            if(HttpContext.User.Identity.Name == null)
            {
                if (!sharedFile.Share) return NotFound();
            }    
            else
            {
                if (!sharedFile.Share)
                    if (sharedFile.UserId != _userHelper.GetUserId(HttpContext.User.Identity.Name))
                        return NotFound();
            }


            string filepath = Path.Combine(FileShareManager.folder, id);

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filepath, out var contenttype))
            {
                contenttype = "application/octet-stream";
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(filepath);
            return File(bytes, contenttype, sharedFile.OriginalName);
        }

        [HttpGet("getlogs")]
        public async Task<IActionResult> GetLogs(string id)
        {
            if(HttpContext.User.Identity.Name == null || !_minecraftServerManager.ServerExists(id).Result)
            {
                return NotFound();
            }

                if (_usersContext.UserAccounts.Find(_userHelper.GetUserId(HttpContext.User.Identity.Name)).ServerId != id)
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
