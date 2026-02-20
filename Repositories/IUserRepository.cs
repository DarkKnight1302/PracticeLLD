using PracticeLLD.Entities;

namespace PracticeLLD.Repositories
{
    public interface IUserRepository
    {
        public Task<User> CreateUser(User user);
        public Task<User> CreateUser(string id, string name, bool isGoogleLogin, string email);
        public Task<User> GetUser(string userId);
        public Task<User> GetUserByEmail(string email);
        public Task<User> UpdateUser(User user);
    }
}
