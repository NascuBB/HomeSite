using HomeSite.Helpers;
using HomeSite.Managers;
using HomeSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeSite.Controllers
{
    [Route("fileshare")]
    public class FileshareController : Controller
    {
        // GET: FileshareController
        private readonly IFileShareManager _fileShareManager;
        private readonly IUserHelper _userHelper;
        public FileshareController(IFileShareManager fileShareManager, IUserHelper userHelper)
        {
            _fileShareManager = fileShareManager;
            _userHelper = userHelper;
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (HttpContext.User.Identity.Name == null)
            {
                return RedirectToAction("Index", "Home");
            }
            FileShareViewModel model = new FileShareViewModel();
            model.Files = _fileShareManager.UserSharedFiles(_userHelper.GetUserId(HttpContext.User.Identity.Name))
                .OrderByDescending(file => file.Featured)
                .ToList();
            long size = _userHelper.GetUserSizeUsed(HttpContext.User.Identity.Name);
            foreach (var file in model.Files)
            {
                file.OriginalName = Helper.ShortenFileName(file.OriginalName!);
            }
            model.SpaceUsed = Math.Round((double)(size / (1024.0 * 1024 * 1024)), 2);
            long oneGB = 1024 * 1024 * 1024; // байт в одном гигабайте
            model.percentUsed = (int)(((double)size / oneGB) * 100);
            return View(model);
        }

        [HttpGet("{id}")]
        public ActionResult GetFile(long id)
        {
            var file = _fileShareManager.GetFile(id);
            
            if(file == null || !file.Share)
                return View("notfound");


            return View("get", model: new GetFileShareViewModel
            {
                File = file,
                Username = _userHelper.GetUsername(file.UserId)!
            });
        }





        //// GET: FileshareController/Details/5
        //public ActionResult Details(int id)
        //{
        //    return View();
        //}

        //// GET: FileshareController/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        //// POST: FileshareController/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create(IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: FileshareController/Edit/5
        //public ActionResult Edit(int id)
        //{
        //    return View();
        //}

        //// POST: FileshareController/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: FileshareController/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    return View();
        //}

        //// POST: FileshareController/Delete/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Delete(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}
    }
}
