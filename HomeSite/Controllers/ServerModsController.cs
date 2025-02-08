using HomeSite.Managers;
using Microsoft.AspNetCore.Mvc;

namespace HomeSite.Controllers
{
    [ApiController]
    public class ServerModsController : ControllerBase
    {
        [HttpDelete]
        [Route("/Server/configure/{Id}/deletemod")]
        public IActionResult DeleteMod(string Id, string file)
        {
            if (HttpContext.User.Identity?.Name == null)
            {
                return Unauthorized("Вы не авторизованы");
            }

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
            if (HttpContext.User.Identity?.Name == null)
            {
                return Unauthorized("Вы не авторизованы");
            }

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
