using com.tweetapp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Interfaces
{
    public interface ITweetRepo
    {
        Task<List<TweetModel>> GetAllTweetsAsync();
        Task<List<TweetModel>> GetTweetsForUserAsync(string loginId);
        Task<TweetModel> GetTweetById(string tweetId);
        Task<string> CreateTweetAsync(TweetModel tweet);
        Task UpdateTweetAsync(TweetModel tweet);
        Task DeleteTweetAsync(string tweetId);
        Task<bool> ReplyToTweet(string tweetId,Reply reply,string replier);
    }
}
