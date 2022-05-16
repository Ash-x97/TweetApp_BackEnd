using System;
using System.Collections.Generic;
using System.Text;

namespace com.kafka.Constants
{
	/// <summary>
	/// Represents the list of topics in Kafka
	/// </summary>
	public static class KafkaTopics
	{
		/// <summary>
		/// Denotes post tweet topic
		/// </summary>
		public static string PostTweet => "PostTweet";

		/// <summary>
		/// Denotes tweet posted topic
		/// </summary>
		public static string TweetPosted => "TweetPosted";
	}
}
