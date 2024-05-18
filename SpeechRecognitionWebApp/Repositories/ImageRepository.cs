using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using SpeechRecognitionWebApp.Db;
using SpeechRecognitionWebApp.Interfaces;
using Tesseract;

namespace SpeechRecognitionWebApp.Repositories
{
    public class ImageRepository : IImageRepository
    {
        private readonly SpeechRecognizorDbEntities db;

        public ImageRepository(SpeechRecognizorDbEntities dbContext)
        {
            db = dbContext;
        }

        public string ProcessImage(Stream stream, string language)
        {
            string text = "";
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                using (var engine = new TesseractEngine(HttpContext.Current.Server.MapPath(@"~/tessdata"), language, EngineMode.Default))
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

        public string ProcessImages(IEnumerable<HttpPostedFileBase> images, string scanType, string[] selectedImagePaths, HttpServerUtilityBase server)
        {
            string allText = "";
            string language = scanType == "Scan Images into Urdu" ? "urd" : "eng";

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

            if (selectedImagePaths != null)
            {
                foreach (var path in selectedImagePaths)
                {
                    string fullPath = server.MapPath(path);
                    using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        allText += ProcessImage(stream, language);
                    }
                }
            }

            return allText;
        }

        public void AddUserImage(UserImage userImage)
        {
            db.UserImages.Add(userImage);
            db.SaveChanges();
        }

        public List<UserImage> GetUserImages(int userId)
        {
            return db.UserImages.Where(ui => ui.UserId == userId).ToList();
        }

        public void SaveImages(HttpFileCollectionBase files, int userId, HttpServerUtilityBase server)
        {
            if (files == null || files.Count == 0)
            {
                throw new ArgumentException("No image selected to save.");
            }

            foreach (string fileName in files)
            {
                HttpPostedFileBase file = files[fileName];
                if (file != null && file.ContentLength > 0)
                {
                    string saveFileName = Path.GetFileNameWithoutExtension(file.FileName) + Path.GetExtension(file.FileName);
                    string virtualPath = "~/Uploads/" + saveFileName;
                    string physicalPath = server.MapPath(virtualPath);
                    file.SaveAs(physicalPath);

                    AddUserImage(new UserImage { UserId = userId, ImagePath = virtualPath });
                }
            }
        }
    }
}
