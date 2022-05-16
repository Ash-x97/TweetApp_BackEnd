using com.tweetapp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Interfaces
{
    public interface IExceptionRepo
    {
        Task<ExceptionModel> GetExceptionByIdAsync(string exceptionId);
        Task<string> InsertExceptionAsync(ExceptionModel exception);
    }
}
