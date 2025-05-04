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
			UserAccount user = _usersContext.UserAccounts.First(x => x.username == HttpContext.User.Identity.Name);

			string? userServerId = user.serverid;
			MinecraftServerWrap? wrap = null;
			if(userServerId != null)
			{
				var server = MinecraftServerManager.GetServerSpecs(userServerId);
				wrap = new MinecraftServerWrap
				{
					ServerState = MinecraftServerManager.serversOnline.Any(x => x.Id == userServerId)
					? MinecraftServerManager.serversOnline.First(x => x.Id == userServerId).ServerState
					: ServerState.stopped,
					Description = server.description,
					Id = userServerId,
					Name = server.name,
				};
			}
			return View(new AccountViewModel
			{
				HasServer = userServerId != null,
				OwnServer = wrap,
				ShortLogs = user.shortlogs
			});
		}

		public IActionResult Register()
		{
            if (HttpContext.User.Identity.Name != null)
            {
                return RedirectToAction("Index");
            }
            return View();
		}

		[HttpPost]
		public IActionResult Register(RegisterViewModel model)
		{
            if (HttpContext.User.Identity.Name != null)
            {
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
			{
				Random rnd = new Random();
				UserAccount newAccount = new UserAccount {username = model.Username, serverid = null, email = model.Email, passwordhash = SecurePasswordHasher.Hash(model.Password) };
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

		public IActionResult Login()
		{
            if (HttpContext.User.Identity.Name != null)
            {
                return RedirectToAction("Index");
            }
            return View();
		}


		[HttpPost]
		public IActionResult Login(LoginViewModel model)
		{
            if (HttpContext.User.Identity.Name != null)
            {
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
			{
				UserAccount? user = _usersContext.UserAccounts.FirstOrDefault(x => (x.username == model.EmailOrUsername || x.email == model.EmailOrUsername));
				if (user == null)
				{
					ModelState.AddModelError("", "Логин или почта неверные");
					return View(model);
				}
				if(!SecurePasswordHasher.Verify(model.Password, user.passwordhash))
				{
					ModelState.AddModelError("", "Пароль неверный");
					return View(model);
				}
				List<Claim> claims = new List<Claim>
				{
					new Claim(ClaimTypes.Name, user.username),
					new Claim(ClaimTypes.Email, user.email),
					new Claim(ClaimTypes.Role, "User")
				};

				ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                //if(model.RememberMe)
                //                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties {IsPersistent = model.RememberMe});
                //            else
                //                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = model.RememberMe });
                return RedirectToAction("Index");
			}
			return View(model);
		}

		public IActionResult Logout()
		{
			HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Index", "Home");
		}

		[HttpPost("Account/setpref")]
		public IActionResult ChangeShortLogs([FromBody] PreferenceRequest request)
		{
			if(HttpContext.User.Identity.Name == null)
			{
				return RedirectToAction("Login");
			}
			_usersContext.UserAccounts.First(x => x.username == HttpContext.User.Identity.Name).shortlogs = request.Value == "true" ? true : false;
			_usersContext.SaveChanges();
			return Ok();
		}


	}
}
