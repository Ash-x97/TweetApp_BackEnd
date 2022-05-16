using com.tweetapp.Interfaces;
using com.tweetapp.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace com.tweetapp.Repositories
{
    public class UserRepo:IUserRepo
    {
        private readonly IMongoCollection<UserModel> _users;
        private readonly ILogger<UserRepo> _logger;

        public UserRepo(ITweetAppDBSettings settings, ILogger<UserRepo> logger)
        {
            _logger = logger;
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _users = database.GetCollection<UserModel>(settings.UsersCollectionName);
        }

        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            try
            {
                List<UserModel> users= await _users.Find(usr => true).ToListAsync();
                if (!(users is null))
                    _logger.LogInformation("Fetched All Users");
                return users;
            }
            catch(Exception e)
            {
                _logger.LogError("Exception : "+e.Message);
                e.Data.Add("CurrentMethod",MethodBase.GetCurrentMethod().ReflectedType.Name+"."+MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }

        public async Task<UserModel> GetUserByLoginIdAsync(string loginId)
        {
            try
            {
                UserModel user = await _users.Find(usr => usr.LoginId == loginId).FirstOrDefaultAsync();
                if (!(user is null))
                    _logger.LogInformation("Fetched User with LoginId : "+loginId);
                return user;

            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }

        public async Task<UserModel> GetUserByEmailIdAsync(string email)
        {
            try
            {
                UserModel user = await _users.Find(usr => usr.Email == email).FirstOrDefaultAsync();
                if(!(user is null))
                    _logger.LogInformation("Fetched User with Email : " + email);
                return user;

            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }
        
        public async Task<List<UserModel>> GetUsersBysearchPatternAsync(string loginIdPattern)
        {
            try
            {
                List<UserModel> users = await _users.Find(usr => usr.LoginId.Contains(loginIdPattern)).ToListAsync();
                if (!(users is null))
                    _logger.LogInformation("Fetched Users with search pattern : " + loginIdPattern);
                return users;

            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }

        public async Task CreateUserAsync(UserModel user)
        {
            try
            {
                await _users.InsertOneAsync(user);
                _logger.LogInformation("Inserted new user with loginId : "+user.LoginId);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }

        public async Task UpdateUserAsync(UserModel user)
        {
            try
            {
                var b=await _users.ReplaceOneAsync(usr => usr.LoginId == user.LoginId, user);
                var a = _users.Find(usr => usr.LoginId == user.LoginId).FirstOrDefaultAsync();
                _logger.LogInformation("Changed password for user with loginId : " + user.LoginId);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }           
        }
    }
}