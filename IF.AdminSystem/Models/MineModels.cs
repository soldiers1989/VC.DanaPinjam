using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NF.AdminSystem.Models
{
    public class UserQuestions
    {
        public string id { get; set; }

        public string userId { get; set; }
        public string content { get; set; }

        public string createTime { get; set; }

        public string replyTime { get; set; }

        public string feedback { get; set; }

        public string url { get; set; }
    }
}
