using SpeechRecognitionWebApp.Db;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace SpeechRecognitionWebApp.Interfaces
{
    public interface IImageRepository
    {
        string ProcessImage(Stream stream, string language);
        string ProcessImages(IEnumerable<HttpPostedFileBase> images, string scanType, string[] selectedImagePaths, HttpServerUtilityBase server);
        void AddUserImage(UserImage userImage);
        List<UserImage> GetUserImages(int userId);
        void SaveImages(HttpFileCollectionBase files, int userId, HttpServerUtilityBase server);

    }
}
