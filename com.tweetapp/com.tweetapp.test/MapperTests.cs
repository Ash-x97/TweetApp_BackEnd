using com.tweetapp.Models;
using com.tweetapp.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace com.tweetapp.test
{
    public class Tests
    {
        Mock<ITweetAppDBSettings> mongoConfig;
        Mock<ILogger<UserRepo>> mockLogUser;
        Mock<ILogger<TweetRepo>> mockLogTweet;
        Mock<ILogger<ExceptionRepo>> mockLogException;
        UserModel testUser;
        TweetModel testTweet;
        ExceptionModel testException;
        Reply testReply;

       [SetUp]
        public void Setup()
        {
            testUser = new UserModel()
            {
                FirstName="TestFirstName",
                LastName="TestLastName",
                LoginId="TestLogintId",
                Email="TestEmail",
                Password="Test@123",
                ConfirmPassword="Test@123",
                ContactNumber="0123456789"
            };
            testReply = new Reply()
            {
                LoginId = "TestLogintId",
                ReplyText = "New Reply Test",
                Tags = new List<Tag>()
                {
                    new Tag(),
                    new Tag()
                }
            };
            testReply.Tags[0].TaggedUser = "Tag1";
            testReply.Tags[1].TaggedUser = "Tag2";

            testTweet = new TweetModel()
            {
                LoginId = "TestLogintId",
                Text = "TestTweetContent",
                Likes = { "a", "b" },
                Replies = new List<Reply>()
                {
                  testReply
                },
                Tags = new List<Tag>() 
                {
                    new Tag(),
                    new Tag()
                },
                CreatedTime= System.DateTime.Now
            };
            testTweet.Tags[0].TaggedUser = "Tag1";
            testTweet.Tags[0].TaggedUser = "Tag2";

            testException = new ExceptionModel()
            {
                Id=null,
                Message="Test ExceptionMethod",
                Method="TestExceptionMethod",
                StackTrace="TestExceptionStackTrace"
            };            

            mongoConfig = new Mock<ITweetAppDBSettings>();
            mongoConfig.Setup(m => m.ConnectionString).Returns("mongodb://localhost:27017");
            mongoConfig.Setup(m => m.DatabaseName).Returns("TweetAppTestDB");
            mongoConfig.Setup(m => m.UsersCollectionName).Returns("Users");
            mongoConfig.Setup(m => m.TweetsCollectionName).Returns("Tweets");
            mongoConfig.Setup(m => m.ExceptionsCollectionName).Returns("Exceptions");

            mockLogUser = new Mock<ILogger<UserRepo>>();
            mockLogTweet = new Mock<ILogger<TweetRepo>>();
            mockLogException = new Mock<ILogger<ExceptionRepo>>();
        }

        #region UserRepo

        [Test]
        public void CreateUserAsync_test()
        {
            //Arrange
            UserRepo repository = new UserRepo(mongoConfig.Object, mockLogUser.Object);
            //Act
            repository.CreateUserAsync(testUser).Wait();
            //Assert
            Assert.IsTrue(true);
        }

        [Test]
        public void UpdateUserAsync_test()
        {
            //Arrange
            UserRepo repository = new UserRepo(mongoConfig.Object, mockLogUser.Object);
            //Act
            var tempData = repository.GetUserByLoginIdAsync(testUser.LoginId).Result;
            tempData.FirstName = "UpdateNameFirstChangeTest";
            repository.UpdateUserAsync(tempData).Wait();
            //Assert
            Assert.IsTrue(true);
        }

        [Test]
        public void GetAllUsersAsync_test()
        {
            //Arrange
            
            UserRepo repository = new UserRepo(mongoConfig.Object, mockLogUser.Object);            
            //Act
            var data = repository.GetAllUsersAsync().Result;            
            //Assert
            Assert.IsNotNull(data);
            Assert.AreEqual(typeof(List<UserModel>), data.GetType());
        }

        [Test]
        public void GetUserByLoginId_test()
        {
            //Arrange
            UserRepo repository = new UserRepo(mongoConfig.Object, mockLogUser.Object);
            //Act
            var data = repository.GetUserByLoginIdAsync(testUser.LoginId).Result;
            //Assert
            Assert.IsNotNull(data);
            Assert.AreEqual(typeof(UserModel), data.GetType());
        }

        [Test]
        public void GetUserByEmailId_test()
        {
            //Arrange
            UserRepo repository = new UserRepo(mongoConfig.Object, mockLogUser.Object);
            //Act
            var data = repository.GetUserByEmailIdAsync(testUser.Email).Result;
            //Assert
            Assert.IsNotNull(data);
            Assert.AreEqual(typeof(UserModel), data.GetType());
        }

        [Test]
        public void GetUserBySearchPattern_test()
        {
            //Arrange
            UserRepo repository = new UserRepo(mongoConfig.Object, mockLogUser.Object);
            //Act
            var data = repository.GetUsersBysearchPatternAsync(testUser.LoginId.Substring(2,6)).Result;
            //Assert
            Assert.IsNotNull(data);
            Assert.AreEqual(typeof(List<UserModel>), data.GetType());
        }

        #endregion

        #region TweetRepo

        [Test]
        public void CreateTweetAsync_test()
        {
            //Arrange
            TweetRepo repository = new TweetRepo(mongoConfig.Object, mockLogTweet.Object);
            //Act
            repository.CreateTweetAsync(testTweet).Wait();
            //Assert
            Assert.IsTrue(true);
        }

        [Test]
        public void UpdateTweetAsync_test()
        {
            //Arrange
            TweetRepo repository = new TweetRepo(mongoConfig.Object, mockLogTweet.Object);
            //Act
            var tempData = repository.GetTweetsForUserAsync(testUser.LoginId).Result[0];
            tempData.Text = "UpdatedTweeContentTest";
            repository.UpdateTweetAsync(tempData).Wait();
            //Assert
            Assert.IsTrue(true);
        }

        [Test]
        public void GetAllTweetsAsync_test()
        {
            //Arrange

            TweetRepo repository = new TweetRepo(mongoConfig.Object, mockLogTweet.Object);
            //Act
            var data = repository.GetAllTweetsAsync().Result;
            //Assert
            Assert.IsNotNull(data);
            Assert.AreEqual(typeof(List<TweetModel>), data.GetType());
        }

        [Test]
        public void GetTweetsForUsers_test()
        {
            //Arrange

            TweetRepo repository = new TweetRepo(mongoConfig.Object, mockLogTweet.Object);
            //Act
            var data = repository.GetTweetsForUserAsync(testUser.LoginId).Result;
            //Assert
            Assert.IsNotNull(data);
            Assert.AreEqual(typeof(List<TweetModel>), data.GetType());
        }

        [Test]
        public void GetTweetById_test()
        {
            //Arrange
            TweetRepo repository = new TweetRepo(mongoConfig.Object, mockLogTweet.Object);
            //Act
            var tempData = repository.GetTweetsForUserAsync(testUser.LoginId).Result[0];
            var data = repository.GetTweetById(tempData.Id).Result;
            //Assert
            Assert.IsNotNull(data);
            Assert.AreEqual(typeof(TweetModel), data.GetType());
        }

        [Test]
        public void DeleteTweet_test()
        {
            //Arrange
            TweetRepo repository = new TweetRepo(mongoConfig.Object, mockLogTweet.Object);
            //Act
            var tempData = repository.GetTweetsForUserAsync(testUser.LoginId).Result[0];
            repository.DeleteTweetAsync(tempData.Id).Wait();
            //Assert
            Assert.IsTrue(true); ;
        }

        #endregion

        #region exceptionRepo

        [Test]
        public void InsertException_GetExceptionById_tests()
        {
            //Arrange
            ExceptionRepo repository = new ExceptionRepo(mongoConfig.Object, mockLogException.Object);
            //Act
            string exId= repository.InsertExceptionAsync(testException).Result;
            var data = repository.GetExceptionByIdAsync(exId).Result;
            //Assert
            Assert.IsNotNull(exId);
            Assert.IsNotNull(data);
            Assert.IsTrue(exId.Length>0);
            Assert.AreEqual(typeof(string), exId.GetType());
            Assert.AreEqual(typeof(ExceptionModel), data.GetType());
        }
              
        #endregion
    }
}