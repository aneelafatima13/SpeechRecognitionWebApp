using Microsoft.OData.Edm;
using SpeechRecognitionWebApp.Db;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Tesseract;

namespace SpeechRecognitionWebApp.Controllers
{
    public class SpeechRecognitionController : Controller
    {
        SpeechRecognizorDbEntities db = new SpeechRecognizorDbEntities();
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadImage(IEnumerable<HttpPostedFileBase> images, string scanType, string[] selectedImagePaths)
        {
            if ((images == null || !images.Any() || images.All(f => f == null || f.ContentLength == 0)) && (selectedImagePaths == null || selectedImagePaths.Length == 0))
                return Content("No files uploaded.");

            string allText = "";
            string language = scanType == "Scan Images into Urdu" ? "urd" : "eng";

            // Process uploaded images
            if (images != null)
            {
                foreach (var file in images)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        allText += ProcessImage(file.InputStream, language);
                    }
                }
            }

            // Process selected images from gallery
            if (selectedImagePaths != null)
            {
                foreach (var path in selectedImagePaths)
                {
                    string fullPath = Server.MapPath(path);
                    using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        allText += ProcessImage(stream, language);
                    }
                }
            }

            return Content(allText);
        }

        private string ProcessImage(Stream stream, string language)
        {
            string text = "";
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                using (var engine = new TesseractEngine(Server.MapPath(@"~/tessdata"), language, EngineMode.Default))
                {
                    using (var img = Pix.LoadFromMemory(memoryStream.ToArray()))
                    {
                        using (var page = engine.Process(img))
                        {
                            byte[] utf8Bytes = Encoding.UTF8.GetBytes(page.GetText());
                            text = Encoding.UTF8.GetString(utf8Bytes);
                        }
                    }
                }
            }
            return text;
        }

        //public ActionResult UploadImage(IEnumerable<HttpPostedFileBase> images, string scanType)
        //{
        //    if (images == null || !images.Any() || images.All(f => f == null || f.ContentLength == 0))
        //        return Content("No files uploaded.");

        //    string allText = "";

        //    string language = "eng";
        //    if (scanType == "Scan Images into Urdu")
        //    {
        //        language = "urd";
        //    }

        //    foreach (var file in images)
        //    {
        //        if (file != null && file.ContentLength > 0)
        //        {
        //            using (var memoryStream = new MemoryStream())
        //            {
        //                file.InputStream.CopyTo(memoryStream);
        //                using (var engine = new TesseractEngine(Server.MapPath(@"~/tessdata"), language, EngineMode.Default))
        //                {
        //                    using (var img = Pix.LoadFromMemory(memoryStream.ToArray()))
        //                    {
        //                        using (var page = engine.Process(img))
        //                        {
        //                            byte[] utf8Bytes = Encoding.UTF8.GetBytes(page.GetText());
        //                            string utf8Text = Encoding.UTF8.GetString(utf8Bytes);

        //                            allText += utf8Text;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return Content(allText);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(UsersInfo model)
        {
            if (ModelState.IsValid)
            {
                db.UsersInfoes.Add(model);
                db.SaveChanges();
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
            var user = db.UsersInfoes.FirstOrDefault(u => u.UserName == model.UserName && u.UserPassword == model.UserPassword);

            if (user != null)
            {
                // Create and set authentication cookie
                HttpCookie cookie = new HttpCookie("User");
                cookie["username"] = model.UserName;
                cookie["password"] = model.UserPassword;
                cookie.Expires = DateTime.Now.AddDays(15);
                HttpContext.Response.Cookies.Add(cookie);
                //object localStorage = null;
                //localStorage.setItem("User", new Date().toISOString());
                //// Set session variable
                //Session["UserId"] = user.UserId;

                // Return success JSON response
                return Json(new { success = true });
            }
            else
            {
                // Return failure JSON response with error message
                return Json(new { success = false, message = "Invalid username or password." });
            }
        }



        [HttpPost]
        public ActionResult SaveImagesToGallery()
        {
            try
            {
                int? userId = null;

                // Check if the "User" cookie exists
                if (Request.Cookies["User"] != null)
                {
                    string username = Request.Cookies["User"]["username"];
                    string password = Request.Cookies["User"]["password"];

                    // Authenticate the user based on the cookie
                    var user = db.UsersInfoes.FirstOrDefault(u => u.UserName == username && u.UserPassword == password);
                    if (user != null)
                    {
                        // User authenticated successfully, retrieve user ID
                        userId = user.UserId;
                    }
                }
                if (userId == null)
                {
                    // Handle case where user is not authenticated
                    return RedirectToAction("Index");
                }
                // Get the files from the request
                HttpFileCollectionBase files = Request.Files;
                if (files == null || files.Count == 0)
                {
                    return Json(new { success = false, message = "No image selected to save." });
                }

                List<string> fileNames = new List<string>();

                // Loop through each file
                for (int i = 0; i < files.Count; i++)
                {
                    HttpPostedFileBase file = files[i];
                   
                    string fileName = Path.GetFileNameWithoutExtension(file.FileName) + Path.GetExtension(file.FileName);
                    // to save images using abseluet path
                    //string filePath = Server.MapPath("~/Uploads/") + fileName;
                    //file.SaveAs(filePath);

                    //// Save the file path to the database
                    //SaveImagePathToDatabase(filePath, userId.Value); // Assuming you have a method to save the image path to the database
                    // to save image using virtual path
                    
                    string virtualPath = "~/Uploads/" + fileName; // Construct virtual path
                    string physicalPath = Server.MapPath(virtualPath); // Map virtual path to physical path
                    file.SaveAs(physicalPath); // Save file to physical path

                    // Now you can save the virtual path into the database
                    SaveImagePathToDatabase(virtualPath, userId.Value);
                    fileNames.Add(fileName);
                }
                return Json(new { success = true, message = "Images saved successfully.", fileNames = fileNames });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving images: " + ex.Message });
            }
        }

        private void SaveImagePathToDatabase(string filePath, int userId)
        {
            
                UserImage userImage = new UserImage
                {
                    UserId = userId,
                    ImagePath = filePath
                };

                db.UserImages.Add(userImage);
                db.SaveChanges();
            
        }

        [HttpGet]
        public ActionResult GetUserImages()
        {
            // Get the logged-in user's ID
            //int? userId = Session["UserId"] as int?;
            int? userId = null;

            // Check if the "User" cookie exists
            if (Request.Cookies["User"] != null)
            {
                string username = Request.Cookies["User"]["username"];
                string password = Request.Cookies["User"]["password"];

                // Authenticate the user based on the cookie
                var user = db.UsersInfoes.FirstOrDefault(u => u.UserName == username && u.UserPassword == password);
                if (user != null)
                {
                    // User authenticated successfully, retrieve user ID
                    userId = user.UserId;
                }
            }
            if (userId == null)
            {
                return RedirectToAction("Index");
            }

            var userImages = db.UserImages.Where(ui => ui.UserId == userId).ToList();

            return PartialView("_UserImages", userImages);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            // Remove the authentication cookie
            if (Request.Cookies["User"] != null)
            {
                HttpCookie cookie = new HttpCookie("User");
                cookie.Expires = DateTime.Now.AddDays(-1); // Set the cookie's expiration date to the past
                HttpContext.Response.Cookies.Add(cookie);
            }

            // Sign out from forms authentication
            FormsAuthentication.SignOut();

            // Redirect to the home page or login page
            return RedirectToAction("Index");
        }

    }
}