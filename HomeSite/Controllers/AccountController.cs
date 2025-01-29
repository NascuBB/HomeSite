using HomeSite.Entities;
using HomeSite.Helpers;
using HomeSite.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
			return View();
		}

		public IActionResult Register()
		{
			return View();
		}

		[HttpPost]
		public IActionResult Register(RegisterViewModel model)
		{
			if(ModelState.IsValid)
			{
				Random rnd = new Random();
				UserAccount newAccount = new UserAccount {Username = model.Username, ServerID = "no", Email = model.Email, PasswordHash = SecurePasswordHasher.Hash(model.Password) };
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
			return View();
		}


		[HttpPost]
		public IActionResult Login(LoginViewModel model)
		{
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
                return RedirectToAction("Index");
			}
			return View(model);
		}

		public IActionResult Logout()
		{
			HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Index", "Home");
		}
	}
}
