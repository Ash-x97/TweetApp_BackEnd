using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.tweetapp.Models
{
    public class MailModel
    {
        private readonly string _fromMailId;
        private readonly string _fromMailPassword;

        public MailModel(IConfiguration config)
        {
            _fromMailId = config["EmailConfig:SenderMail"];
            _fromMailPassword = config["EmailConfig:SenderPassword"];
        }

        public string FromMailId { get { return this._fromMailId; } }
        public string FromMailIdPassword { get { return this._fromMailPassword; } }
        public List<string> ToMailIds { get; set; } = new List<string>();
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsBodyHtml { get; set; } = true;
        public List<string> Attachments { get; set; } = new List<string>();

    }
}
