using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Models
{
    public class TweetModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required(ErrorMessage ="Login id required")]
        public string LoginId { get; set; }

        [Required(ErrorMessage ="Tweet message required")]
        [StringLength(144,ErrorMessage ="Tweet must not be larger than 144 characters")]
        public string Text { get; set; }

        [BsonDateTimeOptions]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedTime { get; set; }     
        
        public List<Reply> Replies { get; set; }

        public List<string> Likes { get; set; }

        public List<Tag> Tags { get; set; }
    }
    
    public partial class Reply
    {
        [Required(ErrorMessage = "Login id required")]
        public string LoginId { get; set; }

        [Required(ErrorMessage = "Reply message required")]
        [StringLength(144, ErrorMessage = "Reply must not be larger than 144 characters")]
        public string ReplyText { get; set; }

        [BsonDateTimeOptions]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedTime { get; set; }
        
        public List<Tag> Tags { get; set; }
    }

    public partial class Tag
    {
        [StringLength(50, ErrorMessage = "Tag must not be larger than 50 characters")]
        public string TaggedUser { get; set; }
        public bool IsNotified { get; set; }
    }
}
