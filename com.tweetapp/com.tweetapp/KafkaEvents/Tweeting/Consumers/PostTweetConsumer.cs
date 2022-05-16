using com.kafka.Constants;
using com.kafka.Interfaces;
using com.kafka.Messages.Tweeting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.tweetapp.KafkaEvents.Tweeting.Consumers
{
    public class PostTweetConsumer: BackgroundService
    {
		private readonly IKafkaConsumer<string, PostTweet> _consumer;
		public PostTweetConsumer(IKafkaConsumer<string, PostTweet> kafkaConsumer)
		{
			_consumer = kafkaConsumer;
		}
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			try
			{
				await _consumer.Consume(KafkaTopics.PostTweet, stoppingToken);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"{(int)HttpStatusCode.InternalServerError} ConsumeFailedOnTopic - {KafkaTopics.PostTweet}, {ex}");
			}
		}

		public override void Dispose()
		{
			_consumer.Close();
			_consumer.Dispose();

			base.Dispose();
		}
	}
}
