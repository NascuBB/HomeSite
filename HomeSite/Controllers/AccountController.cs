using HomeSite.Entities;
using HomeSite.Helpers;
using HomeSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace HomeSite.Controllers
{
	public class AccountController : Controller
	{
		public IActionResult Index()
		{
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
				UserAccount newAccount = new UserAccount { Email = model.Email, Id = rnd.Next(1000,9999), Password = model.Password, PasswordHash = SecurePasswordHasher.Hash(model.Password) };
			}
			return View(model);
		}
	}
}
