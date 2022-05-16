using com.tweetapp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Interfaces
{
    public interface IUserRepo
    {
        Task<List<UserModel>> GetAllUsersAsync();
        Task<List<UserModel>> GetUsersBysearchPatternAsync(string loginIdPattern);
        Task<UserModel> GetUserByLoginIdAsync(string loginId);
        Task<UserModel> GetUserByEmailIdAsync(string email);
        Task CreateUserAsync(UserModel user);
        Task UpdateUserAsync(UserModel user);
    }
}
