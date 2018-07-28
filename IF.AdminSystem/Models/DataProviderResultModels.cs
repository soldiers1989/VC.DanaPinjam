using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NF.AdminSystem.Models
{
    [Serializable]
    public class DataProviderResultModel
    {
        public int result = 0;

        public string message = String.Empty;

        public object data;
    }
}