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
		private readonly AccountVerificationManager _accountVerificationManager;
		private readonly UserPasswordManager _userPasswordManager;
		private readonly IMinecraftServerManager _minecraftServerManager;

        public AccountController(UserDBContext usersContext, AccountVerificationManager accountVerificationManager,
			UserPasswordManager passwordManager, IMinecraftServerManager minecraftServerManager)
		{
			_usersContext = usersContext;
			_accountVerificationManager = accountVerificationManager;
			_userPasswordManager = passwordManager;
			_minecraftServerManager = minecraftServerManager;
		}
		public IActionResult Index()
		{
			if(HttpContext.User.Identity.Name == null)
			{
				return RedirectToAction("Login");
			}
            if (_accountVerificationManager.RequiresVerification(HttpContext.User.Identity.Name))
            {
                return RedirectToAction("Verification", "Account");
            }
            ViewBag.Name = HttpContext.User.Identity.Name;
			UserAccount user = _usersContext.UserAccounts.First(x => x.Username == HttpContext.User.Identity.Name);

			string? userServerId = user.ServerId;
			MinecraftServerWrap? wrap = null;
			if(userServerId != null && userServerId != "no")
			{
				var server = _minecraftServerManager.GetServerSpecs(userServerId).Result;
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
				bool verificate = _accountVerificationManager.CheckVerification(user);
				user.DateLogged = DateTime.UtcNow.Date;
				if(verificate)
				{
					user.Verified = false;
					if(!_accountVerificationManager.UserCode.Any(x => x.User.Id == user.Id))
					{
						_accountVerificationManager.SendVerificate(user);
					}
                    _usersContext.SaveChanges();
                    return RedirectToAction("Verification");
				}
                _usersContext.SaveChanges();
                return RedirectToAction("Index");
			}
			return View(model);
		}

		[Route("resetpassword")]
		public IActionResult SendResetPassword()
		{
			return View();
		}

        [HttpPost("resetpassword")]
        public async Task<IActionResult> SendResetPassword(SendResetPasswordViewModel model)
        {
			if(ModelState.IsValid)
			{
				var user = _usersContext.UserAccounts.FirstOrDefault(x => x.Email == model.Email);
				if(user == null)
				{
					ModelState.AddModelError("", "К этой почте не привязан аккаунт");
					return View(model);
				}
				if(_userPasswordManager.IsResetCodeSent(user))
				{
                    ModelState.AddModelError("", "На эту почту письмо уже отправлено");
                    return View(model);
                }
				await _userPasswordManager.SendPasswordReset(user);
				return RedirectToAction("ResetPasswordSended");
			}
            return View();
        }

        [Route("resetpassword/sended")]
        public IActionResult ResetPasswordSended()
        {
            return View();
        }

        [Route("resetpassword/{code}")]
		public IActionResult ResetPassword(string code)
		{
			if (!_userPasswordManager.CheckResetCode(code)) return RedirectToAction("Index", "Home");

			return View();
		}

        [HttpPost("resetpassword/{code}")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, string code)
        {
            if (!_userPasswordManager.CheckResetCode(code)) return RedirectToAction("Index","Home");
			if (ModelState.IsValid)
			{
				if (!await _userPasswordManager.ResetPassword(code, model.NewPassword))
				{
					ModelState.AddModelError("", "Ошибка замены пароля");
					return View(model);
				}
			}
            return RedirectToAction("Login");
        }

        [Route("verification")]
		public IActionResult Verification()
		{
            if (HttpContext.User.Identity.Name == null)
            {
                return RedirectToAction("Login");
            }
            if (!_accountVerificationManager.RequiresVerification(HttpContext.User.Identity.Name))
            {
                return RedirectToAction("Index");
            }
            return View();
		}

        [HttpPost("verification")]
        public IActionResult Verification(VerificationViewModel model)
        {
            if (HttpContext.User.Identity.Name == null)
            {
                return RedirectToAction("Login");
            }
            if(!_accountVerificationManager.RequiresVerification(HttpContext.User.Identity.Name))
            {
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
			{
                UserAccount user = _usersContext.UserAccounts.First(x => x.Username == HttpContext.User.Identity.Name);
				if (_accountVerificationManager.Verificate(user, model.Code))
				{
					user.Verified = true;
					_usersContext.SaveChanges();
					return RedirectToAction("Index");
				}
				else
				{
					ModelState.AddModelError("", "Код неверный");
					return View(model);
				}
			}
            return View(model);
        }

        [Route("logout")]
        public IActionResult Logout()
		{
			HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Index", "Home");
		}

		[HttpPost("setpref")]
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
