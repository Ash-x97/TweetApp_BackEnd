using com.tweetapp.Interfaces;
using com.tweetapp.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace com.tweetapp.Services
{
    class ExceptionService:IExceptionService
    {
        private readonly ILogger<ExceptionService> _logger;
        private readonly IExceptionRepo _exceptionRepo;

        public ExceptionService(ILogger<ExceptionService> logger, IExceptionRepo exceptionRepo)
        {
            _logger = logger;
            _exceptionRepo = exceptionRepo;
        }

        public async Task<string> LogExceptionToDB(Exception exception)
        {
            try
            {
                _logger.LogInformation("Exception raised and reached exception service");
                if (exception is null)
                {
                    _logger.LogError("Cannot insert null object of exception to database.");
                    return null;
                }
                else
                {
                    ExceptionModel ex = new ExceptionModel();
                    if (string.IsNullOrEmpty(exception.Message) && string.IsNullOrEmpty(exception.Data["CurrentMethod"].ToString()) && string.IsNullOrEmpty(exception.StackTrace))
                    {
                        _logger.LogError("Cannot insert null object of exception to database.");
                        return null;
                    }
                    else
                    {
                        ex.Message = exception.Message;
                        ex.Method = exception.Data["CurrentMethod"].ToString();
                        ex.StackTrace = exception.StackTrace;
                        string exceptionId = await _exceptionRepo.InsertExceptionAsync(ex);
                        if(string.IsNullOrEmpty(exceptionId))
                            _logger.LogError("Exception not inserted correctly");
                        return exceptionId;
                    }
                }
            }
            catch (Exception e)
            {
                if (!e.Data.Contains("CurrentUser") && !e.Data.Contains("CurrentApplication"))
                {
                    _logger.LogError("Exception : " + e.Message);
                    e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                    e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                }
                throw e;
            }
        }

        public async Task<ExceptionModel> GetExceptionById(string id)
        {
            try
            {
                _logger.LogInformation("Get exception by Id request arrived");
                ExceptionModel exception = await _exceptionRepo.GetExceptionByIdAsync(id);
                if(exception is null)
                {
                    _logger.LogError("Invalid exception Id");
                    return exception;
                }
                return exception;

            }
            catch (Exception e)
            {
                if (!e.Data.Contains("CurrentUser") && !e.Data.Contains("CurrentApplication"))
                {
                    _logger.LogError("Exception : " + e.Message);
                    e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                    e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                }
                throw e;
            }
        }
    }
}
