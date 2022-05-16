using com.tweetapp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Interfaces
{
    public interface IExceptionService
    {
        Task<string> LogExceptionToDB(Exception exception);
        Task<ExceptionModel> GetExceptionById(string id);
    }
}
