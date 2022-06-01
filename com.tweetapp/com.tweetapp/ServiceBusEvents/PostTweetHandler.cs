using com.tweetapp.Interfaces;
using com.tweetapp.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace com.tweetapp.ServiceBusEvents
{
    public class PostTweetHandler:BackgroundService
    {
        private readonly string _connection;
        private readonly string _queueName;
        private readonly ILogger<PostTweetHandler> _logger;
        private readonly ITweetRepo _tweetRepo;
        private readonly IExternalCommunicationService _exCommService;
        private IQueueClient _postTweetQueueClient;

        public PostTweetHandler(IConfiguration config, ILogger<PostTweetHandler> logger, ITweetRepo tweetRepo, IExternalCommunicationService exCommService)
        {
            _connection = config["serviceBus:QueueConnectionString"];
            _queueName = config["serviceBus:QueueName"];
            _logger = logger;
            _tweetRepo = tweetRepo;
            _exCommService = exCommService;
        }

        public async Task Handle(Message message, CancellationToken cancelToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var body = Encoding.UTF8.GetString(message.Body);
            _logger.LogInformation($"Create Order Details are: {body}");

            TweetModel tweetToCreate = JsonConvert.DeserializeObject<TweetModel>(body);

            //TweetModel tweetToCreate = new TweetModel()
            //{
            //    Id = null,
            //    LoginId = value.LoginId,
            //    Text = value.Text,
            //    CreatedTime = value.CreatedTime,
            //    Replies = null,
            //    Likes = value.Likes
            //};

            var tempTags = new List<Tag>();
            if (!(tweetToCreate.Tags is null))
                foreach (var tag in tweetToCreate.Tags)
                {
                    Tag tempTag = new Tag()
                    {
                        TaggedUser = HttpUtility.HtmlEncode(tag.TaggedUser),
                        IsNotified = Convert.ToBoolean(HttpUtility.HtmlEncode(tag.IsNotified))
                    };
                    tempTags.Add(tempTag);
                }
            tweetToCreate.Tags = tempTags;


            string id = await _tweetRepo.CreateTweetAsync(tweetToCreate);

            tweetToCreate.Tags = await _exCommService.NotifyTaggedUsers(tweetToCreate.LoginId, tweetToCreate.Tags, id, "T");

            await _tweetRepo.UpdateTweetAsync(tweetToCreate);

            await _postTweetQueueClient.CompleteAsync(message.SystemProperties.LockToken).ConfigureAwait(false);
        }
        public virtual Task HandleFailureMessage(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            if (exceptionReceivedEventArgs == null)
                throw new ArgumentNullException(nameof(exceptionReceivedEventArgs));
            return Task.CompletedTask;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var messageHandlerOptions = new MessageHandlerOptions(HandleFailureMessage)
            {
                MaxConcurrentCalls = 5,
                AutoComplete = false,
                MaxAutoRenewDuration = TimeSpan.FromMinutes(10)
            };
            _postTweetQueueClient = new QueueClient(_connection, _queueName);
            _postTweetQueueClient.RegisterMessageHandler(Handle, messageHandlerOptions);
            _logger.LogInformation($"{nameof(PostTweetHandler)} service has started.");
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(PostTweetHandler)} service has stopped.");
            await _postTweetQueueClient.CloseAsync().ConfigureAwait(false);
        }
    }
}
