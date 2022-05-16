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
    public class ExceptionRepo:IExceptionRepo
    {
        private readonly IMongoCollection<ExceptionModel> _exceptions;
        private readonly ILogger<ExceptionRepo> _logger;

        public ExceptionRepo(ITweetAppDBSettings settings, ILogger<ExceptionRepo> logger)
        {
            _logger = logger;
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _exceptions = database.GetCollection<ExceptionModel>(settings.ExceptionsCollectionName);
        }

        public async Task<ExceptionModel> GetExceptionByIdAsync(string exceptionId)
        {
            try
            {
                ExceptionModel exception = await _exceptions.Find(ex => ex.Id == exceptionId).FirstOrDefaultAsync();
                if (!(exception is null))
                    _logger.LogInformation("Fetched exception with Id : " + exceptionId);
                return exception;

            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }
        public async Task<string> InsertExceptionAsync(ExceptionModel exception)
        {
            try
            {
                await _exceptions.InsertOneAsync(exception);
                if (exception.Id is null)
                    return null;
                _logger.LogInformation("Logged new exception to database with Id : " + exception.Id);
                return exception.Id;
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
