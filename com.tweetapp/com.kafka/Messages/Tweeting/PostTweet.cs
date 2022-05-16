using System;
using System.Collections.Generic;
using System.Text;

namespace com.kafka.Messages.Tweeting
{
    public class PostTweet
    {
        public string LoginId { get; set; }
        public string Text { get; set; }
        public DateTime CreatedTime { get; set; }
        public List<string> Likes { get; set; }
        public List<KTag> Tags { get; set; }
    }
    public partial class KTag
    {
        public string TaggedUser { get; set; }
        public bool IsNotified { get; set; }
    }

}
