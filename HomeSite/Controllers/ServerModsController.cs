using HomeSite.Entities;
using HomeSite.Managers;
using Microsoft.AspNetCore.Mvc;

namespace HomeSite.Controllers
{
    [ApiController]
    public class ServerModsController : ControllerBase
    {
        private readonly UserDBContext _usersContext;
        private readonly ISharedAdministrationManager _sharedManager;
		public ServerModsController(UserDBContext userDBContext, ISharedAdministrationManager sharedAdministration)
		{
			_sharedManager = sharedAdministration;
            _usersContext = userDBContext;
		}

		[HttpDelete]
        [Route("/Server/configure/{Id}/deletemod")]
        public IActionResult DeleteMod(string Id, string file)
        {
            if (HttpContext.User.Identity.Name == null || !_usersContext.UserAccounts.Any(x => x.serverid == Id))
                if (HttpContext.User.Identity.Name == null || !_sharedManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name)
                    || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).editmods)
                    return Unauthorized();

            string filePath = Path.Combine(MinecraftServerManager.folder, Id, "mods", file);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                return Ok();
            }

            return NotFound("Файл не найден");
        }

        [HttpPost]
        [Route("/Server/configure/{Id}/uploadmod")]
        public async Task<IActionResult> UploadMod(string Id, IFormFile file)
        {
            if (HttpContext.User.Identity.Name == null || !_usersContext.UserAccounts.Any(x => x.serverid == Id))
                if (HttpContext.User.Identity.Name == null || !_sharedManager.HasSharedThisServer(Id, HttpContext.User.Identity.Name) 
                    || !_sharedManager.GetUserSharedRights(HttpContext.User.Identity.Name, Id).editmods)
                    return Unauthorized();
            if (file == null || file.Length == 0)
            {
                return BadRequest("Файл не выбран");
            }

            string modsFolder = Path.Combine(MinecraftServerManager.folder, Id, "mods");
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
            }

            string filePath = Path.Combine(modsFolder, file.FileName.Replace('+', ' '));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok();
        }
    }

}
