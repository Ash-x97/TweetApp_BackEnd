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
    public class TweetRepo: ITweetRepo
    {
        private readonly IMongoCollection<TweetModel> _tweets;
        private readonly ILogger<TweetRepo> _logger;

        public TweetRepo(ITweetAppDBSettings settings, ILogger<TweetRepo> logger)
        {
            _logger = logger;
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _tweets = database.GetCollection<TweetModel>(settings.TweetsCollectionName);
        }

        public async Task<List<TweetModel>> GetAllTweetsAsync()
        {
            try
            {
                List<TweetModel> tweets = await _tweets.Find(twt => true).ToListAsync();
                if (!(tweets is null))
                    _logger.LogInformation("Fetched All Tweets");
                return tweets;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }

        public async Task<List<TweetModel>> GetTweetsForUserAsync(string loginId)
        {
            try
            {
                List<TweetModel> tweets = await _tweets.Find(twt => twt.LoginId == loginId).ToListAsync();
                if (!(tweets is null))
                    _logger.LogInformation("Fetched all tweets of LoginId : " + loginId);
                return tweets;

            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }

        public async Task<TweetModel> GetTweetById(string tweetId)
        {
            try
            {
                TweetModel tweet = await _tweets.Find(twt => twt.Id == tweetId).FirstOrDefaultAsync();
                if (!(tweet is null))
                    _logger.LogInformation("Fetched tweet with Id : " + tweetId);
                return tweet;

            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }

        public async Task<string> CreateTweetAsync(TweetModel tweet)
        {
            try
            {
                await _tweets.InsertOneAsync(tweet);
                string id = _tweets.Find(twt=>true).SortByDescending(twt=>twt.Id).Limit(1).FirstOrDefaultAsync().Result.Id;
                return id;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }

        public async Task UpdateTweetAsync(TweetModel tweet)
        {
            try
            {
                var a=await _tweets.ReplaceOneAsync(twt => twt.LoginId == tweet.LoginId && twt.Id == tweet.Id, tweet);
                _logger.LogInformation("Updated tweet with id : " + tweet.Id);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }

        public async Task DeleteTweetAsync(string tweetId)
        {
            try
            {
                var a=await _tweets.DeleteOneAsync(twt => twt.Id == tweetId);
                _logger.LogInformation("Deleted tweet with Id : " + tweetId);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception : " + e.Message);
                e.Data.Add("CurrentMethod", MethodBase.GetCurrentMethod().ReflectedType.Name + "." + MethodBase.GetCurrentMethod().Name);
                e.Data.Add("CurrentApplication", Assembly.GetExecutingAssembly().GetName().Name);
                throw e;
            }
        }

        public async Task<bool> ReplyToTweet(string tweetId, Reply reply, string replier)
        {
            try
            {
                TweetModel tweet = await _tweets.Find(twt => twt.Id == tweetId).FirstOrDefaultAsync();
                if(tweet is null)
                {
                    _logger.LogInformation("No Tweet with Id : " + tweetId);
                    return false;
                }
                else
                {
                    if (tweet.Replies is null)
                        tweet.Replies = new List<Reply>();
                    tweet.Replies.Add(reply);
                    var a = await _tweets.ReplaceOneAsync(twt => twt.Id == tweetId, tweet);
                    _logger.LogInformation("Added new reply to tweet with Id : " + tweetId);
                    return true;
                }
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
