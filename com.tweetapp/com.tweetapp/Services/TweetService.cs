using com.kafka.Constants;
using com.kafka.Interfaces;
using com.kafka.Messages.Tweeting;
using com.tweetapp.Interfaces;
using com.tweetapp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace com.tweetapp.Services
{
    class TweetService:ITweetService
    {
        private readonly ILogger<TweetService> _logger;
        private readonly IConfiguration _config;
        private readonly ITweetRepo _tweetRepo;
        private readonly IKafkaProducer<string, PostTweet> _kafkaProducer;
        private readonly IExternalCommunicationService _exCommService;

        public TweetService(ILogger<TweetService> logger, IConfiguration config, ITweetRepo tweetRepo, IKafkaProducer<string, PostTweet> kafkaProducer, IExternalCommunicationService exCommService)
        {
            _logger = logger;
            _config = config;
            _tweetRepo = tweetRepo;
            _kafkaProducer = kafkaProducer;
            _exCommService = exCommService;
        }

        public async Task<List<TweetModel>> GetAllTweets()
        {
            try
            {
                List<TweetModel> tweets = await _tweetRepo.GetAllTweetsAsync();
                if (tweets is null || tweets.Count == 0)
                {
                    _logger.LogInformation("No tweets in DB");
                    return new List<TweetModel>();
                }
                _logger.LogInformation("Fetched " + tweets.Count + " tweets");               
                return tweets;
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

        public async Task<List<TweetModel>> GetTweetsByLoginId(string loginId)
        {
            try
            {
                List<TweetModel> tweets = await _tweetRepo.GetTweetsForUserAsync(loginId);
                if (tweets is null || tweets.Count == 0)
                {
                    _logger.LogInformation("No tweets for login Id : "+loginId);
                    return new List<TweetModel>();
                }
                _logger.LogInformation("Fetched " + tweets.Count + " tweets");
                return tweets;
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


        public async Task<TweetModel> GetTweetsByTweetId(string tweetId)
        {
            try
            {
                if (!string.IsNullOrEmpty(tweetId))
                    return await _tweetRepo.GetTweetById(tweetId);
                return null;
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

        public async Task<bool> CreateTweet(TweetModel tweet)
        {
            try
            {
                _logger.LogInformation("Create tweet request recieved from : " + tweet.LoginId);
                bool isRegistered = false;
                if (!(tweet is null))
                {
                    if (!IfAlreadyExists(tweet.Id).Result)
                    {
                        PostTweet twt = new PostTweet()
                        {
                            LoginId = tweet.LoginId,
                            CreatedTime = DateTime.Now,
                            Likes = new List<string>(),                                 
                            Text=tweet.Text
                        };    

                        if(!(tweet.Tags is null))
                        {
                            List<KTag> ktags = new List<KTag>();
                            foreach (var tag in tweet.Tags)
                            {
                                KTag tempTag = new KTag()
                                {
                                    TaggedUser = tag.TaggedUser,
                                    IsNotified = tag.IsNotified
                                };
                                ktags.Add(tempTag);
                            }
                            twt.Tags = ktags;
                        }                        

                        await _kafkaProducer.ProduceAsync(KafkaTopics.PostTweet,null, twt);                                               

                        isRegistered = true;
                        _logger.LogInformation("Tweet queued successfully. TweetId : " + tweet.Id);
                        return isRegistered;
                    }
                    else
                    {
                        _logger.LogInformation("Tweeting failed. A tweet with same tweetId : " + tweet.Id);
                        return isRegistered;
                    }
                }
                else
                {
                    _logger.LogInformation("Tweet Failed. Data null or invalid");
                    return isRegistered;
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

        public async Task<bool> UpdateTweet(TweetModel tweet)
        {
            try
            {
                _logger.LogInformation("Update tweet request recieved for tweet with Id : "+tweet.Id);
                bool isUpdated = false;                
                if (!(tweet is null))
                {
                    if (tweet.Text.Length <= 144)
                    {                                               
                        if(!(tweet.Tags is null))
                            tweet.Tags=await _exCommService.NotifyTaggedUsers(tweet.LoginId, tweet.Tags, tweet.Id,"T");

                        if(!(tweet.Replies is null))
                        {
                            List<Reply> checkTweetReplies = _tweetRepo.GetTweetById(tweet.Id).Result.Replies;
                            List<Reply> invalidReplies = new List<Reply>();
                            if (checkTweetReplies is null)
                            {                               
                                foreach (var reply in tweet.Replies)
                                {
                                    if (reply.LoginId == tweet.LoginId)
                                    {
                                        reply.CreatedTime = DateTime.Now;
                                        continue;
                                    }
                                    tweet.Replies.Remove(reply);
                                }
                            }
                            else
                            {
                                foreach (var reply in tweet.Replies)
                                {
                                    if (reply.LoginId != tweet.LoginId && !checkTweetReplies.Contains(reply))
                                    {
                                        invalidReplies.Add(reply);
                                        continue;
                                    }
                                    reply.Tags= await _exCommService.NotifyTaggedUsers(reply.LoginId, reply.Tags, tweet.Id, "R");
                                }
                            }
                            if (!(invalidReplies is null))
                                foreach (var reply in invalidReplies)
                                    if (tweet.Replies.Contains(reply))
                                        tweet.Replies.Remove(reply);

                        }
                        await _tweetRepo.UpdateTweetAsync(tweet);
                        isUpdated = true;
                        _logger.LogInformation("Successfully updated tweet with Id : " + tweet.Id);
                        return isUpdated;
                    }
                    _logger.LogInformation("Tweet not updated - Message(size=144) and Tag(size=50) fields validation failed");
                    return isUpdated;
                }
                else
                {
                    _logger.LogInformation("Tweet not updated - No tweet found with Id : "+tweet.Id);
                    return isUpdated;
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

        public async Task<bool> DeleteTweet(string tweetId)
        {
            try
            {
                _logger.LogInformation("Delete tweet request recieved for tweet with Id : " + tweetId);
                bool isUpdated = false;
                if (!(string.IsNullOrEmpty(tweetId)))
                {
                    if (IfAlreadyExists(tweetId).Result)
                    {                        
                        await _tweetRepo.DeleteTweetAsync(tweetId);
                        isUpdated = true;
                        _logger.LogInformation("Successfully deleted tweet with Id : " + tweetId);
                        return isUpdated;
                    }
                    _logger.LogInformation("Tweet not Deleted - No tweet found with Id : "+tweetId);
                    return isUpdated;
                }
                else
                {
                    _logger.LogInformation("Tweet not deleted - tweet Id null or invalid");
                    return isUpdated;
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

        public async Task<bool> LikeTweet(string tweetId,string loginId)
        {
            try
            {
                _logger.LogInformation("Like tweet request recieved for tweet with Id : " + tweetId+" from user : "+loginId);

                TweetModel tweet = await _tweetRepo.GetTweetById(tweetId);
                if (tweet is null)
                {
                    _logger.LogInformation("No Tweet with Id : " + tweetId);
                    return false;
                }

                if (tweet.Likes.Contains(loginId))
                {
                    _logger.LogInformation("User : " + loginId + " already liked the Tweet with Id : " + tweetId);
                    return false;
                }

                tweet.Likes.Add(loginId);
                await _tweetRepo.UpdateTweetAsync(tweet);
                await _exCommService.NotifyLikedUser(loginId,tweet.LoginId);
                _logger.LogInformation("1 like added to tweet with Id : " + tweetId + " current likes: " + tweet.Likes.Count);
                return true;
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

        public async Task<bool> ReplyToTweet(string tweetId, Reply reply, string replier)
        {
            try
            {
                _logger.LogInformation("Reply to tweet request recieved for tweet with Id : " + tweetId);
                if (!string.IsNullOrEmpty(reply.ReplyText))
                {
                    reply.CreatedTime = DateTime.Now;
                    TweetModel tweet = await _tweetRepo.GetTweetById(tweetId);
                    if (tweet is null)
                    {
                        _logger.LogInformation("No Tweet with Id : " + tweetId);
                        return false;
                    }

                    if (tweet.Replies is null)
                        tweet.Replies = new List<Reply>();

                    tweet.Replies.Add(reply);
                    await _exCommService.NotifyReplyToUser(replier,tweet.LoginId);

                    if (!(reply.Tags is null))
                        reply.Tags= await _exCommService.NotifyTaggedUsers(replier, reply.Tags, tweetId, "R");
                    
                    await _tweetRepo.UpdateTweetAsync(tweet);
                    _logger.LogInformation("Replied to tweet with Id : " + tweetId);
                    return true;

                }
                else
                {
                    _logger.LogInformation("Reply failed - empty text");
                    return false;
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

        private async Task<bool> IfAlreadyExists(string tweetId)
        {
            try
            {
                TweetModel existingTweetwithSameId = await _tweetRepo.GetTweetById(tweetId);
                if (existingTweetwithSameId is null)
                    return false;
                return true;
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
