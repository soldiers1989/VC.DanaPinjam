using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NF.AdminSystem.Models
{
    /// <summary>
    /// 状态
    /// </summary>
    [Serializable]
    public class Result
    {
        /// <summary>
        /// 成功
        /// </summary>
        public const int SUCCESS = 0;

        /// <summary>
        /// 失败
        /// </summary>
        public const int ERROR = -1;
    }

    /// <summary>
    /// 请求返回结果
    /// </summary>
    [Serializable]
    public class HttpResultModel
    {
        /// <summary>
        /// 请求状态
        /// </summary>
        public int result = Result.SUCCESS;

        public int errorCode = 0;
        /// <summary>
        /// 
        /// </summary>
        public string message = String.Empty;

        /// <summary>
        /// 返回数据
        /// </summary>
        public object data = new List<string>();
    }
}