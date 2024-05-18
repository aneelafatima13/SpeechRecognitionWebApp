using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using SpeechRecognitionWebApp.Db;
using SpeechRecognitionWebApp.Interfaces;
using SpeechRecognitionWebApp.Repositories;

namespace SpeechRecognitionWebApp.Controllers
{
    public class SpeechRecognitionController : Controller
    {
        private readonly IUserRepository userRepository;
        private readonly IImageRepository imageRepository;

        public SpeechRecognitionController()
        {
            var dbContext = new SpeechRecognizorDbEntities();
            userRepository = new UserRepository(dbContext);
            imageRepository = new ImageRepository(dbContext);
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadImage(IEnumerable<HttpPostedFileBase> images, string scanType, string[] selectedImagePaths)
        {
            try
            {
                string allText = imageRepository.ProcessImages(images, scanType, selectedImagePaths, Server);
                return Content(allText);
            }
            catch (Exception ex)
            {
                return Content($"Error processing images: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(UsersInfo model)
        {
            if (ModelState.IsValid)
            {
                userRepository.AddUser(model);
                return Json(new { success = true, message = "Registration successful." });
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, errors = errors });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UsersInfo model)
        {
            var user = userRepository.GetUser(model.UserName, model.UserPassword);

            if (user != null)
            {
                HttpCookie cookie = new HttpCookie("User");
                cookie["username"] = model.UserName;
                cookie["password"] = model.UserPassword;
                cookie.Expires = DateTime.Now.AddDays(15);
                HttpContext.Response.Cookies.Add(cookie);

                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "Invalid username or password." });
            }
        }

        [HttpPost]
        public ActionResult SaveImagesToGallery()
        {
            try
            {
                int? userId = userRepository.GetUserIdFromCookies(Request);
                if (userId == null)
                {
                    return RedirectToAction("Index");
                }

                imageRepository.SaveImages(Request.Files, userId.Value, Server);
                return Json(new { success = true, message = "Images saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error saving images: {ex.Message}" });
            }
        }

        [HttpGet]
        public ActionResult GetUserImages()
        {
            int? userId = userRepository.GetUserIdFromCookies(Request);
            if (userId == null)
            {
                return RedirectToAction("Index");
            }

            var userImages = imageRepository.GetUserImages(userId.Value);
            return PartialView("_UserImages", userImages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            if (Request.Cookies["User"] != null)
            {
                HttpCookie cookie = new HttpCookie("User");
                cookie.Expires = DateTime.Now.AddDays(-1);
                HttpContext.Response.Cookies.Add(cookie);
            }

            FormsAuthentication.SignOut();
            return RedirectToAction("Index");
        }
    }
}
