using SpeechRecognitionWebApp.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SpeechRecognitionWebApp.Interfaces
{
    public interface IUserRepository
    {
        UsersInfo GetUser(string username, string password);
        void AddUser(UsersInfo user);
        int? GetUserIdFromCookies(HttpRequestBase request);
    }
}
