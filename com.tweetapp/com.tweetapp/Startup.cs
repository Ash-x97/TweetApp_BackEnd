using com.kafka.Interfaces;
using com.tweetapp.Interfaces;
using com.tweetapp.Models;
using com.tweetapp.Repositories;
using com.tweetapp.Services;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using com.kafka.Producer;
using com.kafka.Messages.Tweeting;
using com.kafka.Consumer;
using com.tweetapp.KafkaEvents.Tweeting.Consumers;
using com.tweetapp.KafkaEvents.Tweeting.Handlers;
using Prometheus;
using System;

namespace com.tweetapp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            //current context 
            services.AddHttpContextAccessor();

            //mongoDB connectionSettings 
            services.Configure<TweetAppDBSettings>(
                Configuration.GetSection(nameof(TweetAppDBSettings)));
            services.AddSingleton<ITweetAppDBSettings>(sp =>
                sp.GetRequiredService<IOptions<TweetAppDBSettings>>().Value);

            //Kafka configuration registration
            ClientConfig kafkaClientConfig = new ClientConfig()
            {
                BootstrapServers = Configuration["Kafka:ClientConfigs:BootstrapServers"]

            };
            var consumerConfig = new ConsumerConfig(kafkaClientConfig)
            {
                GroupId = Configuration["Kafka:ClientConfigs:GroupID"],
                EnableAutoCommit = true,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                StatisticsIntervalMs = 5000,
                SessionTimeoutMs = 6000
            };
            services.AddSingleton(new ProducerConfig(kafkaClientConfig));
            services.AddSingleton(consumerConfig);
            services.AddSingleton(typeof(IKafkaProducer<,>), typeof(KafkaProducer<,>));
            services.AddScoped<IKafkaHandler<string, PostTweet>, PostTweetHandler>();
            services.AddSingleton(typeof(IKafkaConsumer<,>), typeof(KafkaConsumer<,>));
            services.AddHostedService<PostTweetConsumer>();

            //Register dependent services for DI (constructor based)
            services.AddTransient<IExternalCommunicationService, ExternalCommunicationService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IUserRepo, UserRepo>();
            services.AddTransient<ITweetService, TweetService>();
            services.AddTransient<ITweetRepo, TweetRepo>();
            services.AddTransient<IExceptionService, ExceptionService>();
            services.AddTransient<IExceptionRepo, ExceptionRepo>();

            //Register swagger
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "TweetApp",
                    Version = "v1",
                    Description = "Official Documentation for TweetApp",
                });
            });


            ////Register Authentication Scheme            
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
            {
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    //what to validate 
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    //setup validate data 
                    ValidIssuer = Configuration["JWT:Issuer"],
                    ValidAudience = Configuration["JWT:Audience"],                 
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:Key"])),
                    ClockSkew = TimeSpan.Zero
                };
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            ////adding logger,swagger to pipeline
            loggerFactory.AddLog4Net();
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "TweetApp"));

            app.UseCors(x => x
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

            app.UseHttpsRedirection();

            //Authentication, Routing and Authorizatiov in that order
            app.UseAuthentication();
            app.UseRouting();
            app.UseHttpMetrics();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapControllers();
            });
        }
    }
}
