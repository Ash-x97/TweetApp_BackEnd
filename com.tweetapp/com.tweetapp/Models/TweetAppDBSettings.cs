using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Models
{
    public class TweetAppDBSettings : ITweetAppDBSettings
    {
        public string UsersCollectionName { get; set; }
        public string TweetsCollectionName { get; set; }
        public string ExceptionsCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
    public interface ITweetAppDBSettings
    {
        public string UsersCollectionName { get; set; }
        public string TweetsCollectionName { get; set; }
        public string ExceptionsCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}
