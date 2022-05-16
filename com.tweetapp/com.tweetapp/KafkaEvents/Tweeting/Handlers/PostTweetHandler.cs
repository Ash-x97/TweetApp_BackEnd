using com.kafka.Constants;
using com.kafka.Interfaces;
using com.kafka.Messages.Tweeting;
using com.tweetapp.Interfaces;
using com.tweetapp.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace com.tweetapp.KafkaEvents.Tweeting.Handlers
{
    public class PostTweetHandler : IKafkaHandler<string, PostTweet>
	{
		private readonly IKafkaProducer<string, TweetPosted> _producer;
		private readonly ITweetRepo _tweetRepo;
        private readonly IExternalCommunicationService _exCommService;

        public PostTweetHandler(IKafkaProducer<string, TweetPosted> producer, ITweetRepo tweetRepo,IExternalCommunicationService exCommService)
		{
			_producer = producer;
			_tweetRepo = tweetRepo;
			_exCommService = exCommService;
		}	
		public async Task HandleAsync(string key, PostTweet value)
		{
			//_logger.LogInformation($"New Tweet arrived from kafka");
			TweetModel tweetToCreate = new TweetModel()
			{
				Id = null,
				LoginId = value.LoginId,
				Text = value.Text,
				CreatedTime = value.CreatedTime,
				Replies = null,
				Likes = value.Likes
			};
			var tempTags = new List<Tag>();
			if(!(value.Tags is null))
			foreach (var tag in value.Tags)
			{
				Tag tempTag = new Tag()
				{
					TaggedUser = HttpUtility.HtmlEncode(tag.TaggedUser),
					IsNotified = Convert.ToBoolean(HttpUtility.HtmlEncode(tag.IsNotified))
				};
				tempTags.Add(tempTag);
			}
			tweetToCreate.Tags = tempTags;


			string id=await _tweetRepo.CreateTweetAsync(tweetToCreate);

			tweetToCreate.Tags= await _exCommService.NotifyTaggedUsers(tweetToCreate.LoginId,tweetToCreate.Tags,id,"T");

			await _tweetRepo.UpdateTweetAsync(tweetToCreate);
			//_logger.LogInformation($"Consumed tweet from kafka :- id({id}) by( {value.LoginId}) text({value.Text})");
			await _producer.ProduceAsync(KafkaTopics.TweetPosted, "", new TweetPosted { TweetId = id });

		}
	}
}
