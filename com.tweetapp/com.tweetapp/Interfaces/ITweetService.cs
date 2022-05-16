using com.tweetapp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Interfaces
{
    public interface ITweetService
    {
        Task<List<TweetModel>> GetAllTweets();
        Task<List<TweetModel>> GetTweetsByLoginId(string loginId);
        Task<TweetModel> GetTweetsByTweetId(string tweetId);
        Task<bool> CreateTweet(TweetModel tweet);
        Task<bool> UpdateTweet(TweetModel tweet);
        Task<bool> DeleteTweet(string tweetId);
        Task<bool> LikeTweet(string tweetId,string loginId);
        Task<bool> ReplyToTweet(string tweetId ,Reply reply,string replier);
    }
}
