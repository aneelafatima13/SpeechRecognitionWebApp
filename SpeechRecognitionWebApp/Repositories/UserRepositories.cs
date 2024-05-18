using System.Linq;
using System.Web;
using SpeechRecognitionWebApp.Db;
using SpeechRecognitionWebApp.Interfaces;

namespace SpeechRecognitionWebApp.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SpeechRecognizorDbEntities db;

        public UserRepository(SpeechRecognizorDbEntities dbContext)
        {
            db = dbContext;
        }

        public UsersInfo GetUser(string username, string password)
        {
            return db.UsersInfoes.FirstOrDefault(u => u.UserName == username && u.UserPassword == password);
        }

        public void AddUser(UsersInfo user)
        {
            db.UsersInfoes.Add(user);
            db.SaveChanges();
        }

        public int? GetUserIdFromCookies(HttpRequestBase request)
        {
            if (request.Cookies["User"] != null)
            {
                string username = request.Cookies["User"]["username"];
                string password = request.Cookies["User"]["password"];

                var user = GetUser(username, password);
                return user?.UserId;
            }
            return null;
        }
    }
}
