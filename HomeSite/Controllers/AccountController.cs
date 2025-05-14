using HomeSite.Entities;
using HomeSite.Helpers;
using HomeSite.Managers;
using HomeSite.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace HomeSite.Controllers
{
	[Route("account")]
	public class AccountController : Controller
	{
		private readonly UserDBContext _usersContext;

        public AccountController(UserDBContext usersContext)
		{
			_usersContext = usersContext;
		}
		public IActionResult Index()
		{
			if(HttpContext.User.Identity.Name == null)
			{
				return RedirectToAction("Login");
			}
			ViewBag.Name = HttpContext.User.Identity.Name;
			var n = _usersContext.UserAccounts.ToList();
			UserAccount user = _usersContext.UserAccounts.First(x => x.Username == HttpContext.User.Identity.Name);

			string? userServerId = user.ServerId;
			MinecraftServerWrap? wrap = null;
			if(userServerId != null && userServerId != "no")
			{
				var server = MinecraftServerManager.GetServerSpecs(userServerId);
				wrap = new MinecraftServerWrap
				{
					ServerState = MinecraftServerManager.serversOnline.Any(x => x.Id == userServerId)
					? MinecraftServerManager.serversOnline.First(x => x.Id == userServerId).ServerState
					: ServerState.stopped,
					Description = server.Description,
					Id = userServerId,
					Name = server.Name,
				};
			}
			return View(new AccountViewModel
			{
				HasServer = (userServerId != null && userServerId != "no"),
				OwnServer = wrap,
				ShortLogs = user.ShortLogs
			});
		}
        [Route("register")]
        public IActionResult Register()
		{
            if (HttpContext.User.Identity.Name != null)
            {
                return RedirectToAction("Index");
            }
            return View();
		}

		[HttpPost]
        [Route("register")]
        public IActionResult Register(RegisterViewModel model)
		{
            if (HttpContext.User.Identity.Name != null)
            {
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
			{
				Random rnd = new Random();
				UserAccount newAccount = new UserAccount {Username = model.Username, ServerId = null, Email = model.Email, PasswordHash = SecurePasswordHasher.Hash(model.Password) };
				_usersContext.Add(newAccount);
				_usersContext.SaveChanges();

				try
				{
					ModelState.Clear();
					ViewBag.Message = $"Регистрация успешна {model.Username}";
					return RedirectToAction("Login");
				}
				catch (DbUpdateException e)
				{
					ModelState.AddModelError("", "почта или имя пользователя уже заняты");
				}
			}
			return View(model);
		}
        [Route("login")]
        public IActionResult Login()
		{
            if (HttpContext.User.Identity.Name != null)
            {
                return RedirectToAction("Index");
            }
            return View();
		}


		[HttpPost]
        [Route("login")]
        public IActionResult Login(LoginViewModel model)
		{
            if (HttpContext.User.Identity.Name != null)
            {
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
			{
				UserAccount? user = _usersContext.UserAccounts.FirstOrDefault(x => (x.Username == model.EmailOrUsername || x.Email == model.EmailOrUsername));
				if (user == null)
				{
					ModelState.AddModelError("", "Логин или почта неверные");
					return View(model);
				}
				if(!SecurePasswordHasher.Verify(model.Password, user.PasswordHash))
				{
					ModelState.AddModelError("", "Пароль неверный");
					return View(model);
				}
				List<Claim> claims = new List<Claim>
				{
					new Claim(ClaimTypes.Name, user.Username),
					new Claim(ClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, "User")
				};

				ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                //if(model.RememberMe)
                //                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties {IsPersistent = model.RememberMe});
                //            else
                //                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = model.RememberMe });
				user.DateLogged = DateTime.UtcNow.Date;
				_usersContext.SaveChanges();
                return RedirectToAction("Index");
			}
			return View(model);
		}
        [Route("logout")]
        public IActionResult Logout()
		{
			HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Index", "Home");
		}

		[HttpPost("account/setpref")]
		public IActionResult ChangeShortLogs([FromBody] PreferenceRequest request)
		{
			if(HttpContext.User.Identity.Name == null)
			{
				return RedirectToAction("Login");
			}
			_usersContext.UserAccounts.First(x => x.Username == HttpContext.User.Identity.Name).ShortLogs = request.Value == "true" ? true : false;
			_usersContext.SaveChanges();
			return Ok();
		}


	}
}
