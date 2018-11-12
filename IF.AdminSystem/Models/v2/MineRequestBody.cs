using System;
using System.Collections.Generic;

namespace NF.AdminSystem.Models.v2
{
    public class QuestionsRequestBody
    {
        public string content {get;set;}
        
        public string userId {get;set;}

        public string url {get;set;}
    }
}