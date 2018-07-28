using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YYLog.ClassLibrary
{
	public enum LogType : int
	{
		/// <summary>
		/// 日志1级，调试信息
		/// </summary>
		Debug = 1,
		/// <summary>
		/// 日志2级，成功信息
		/// </summary>
		Success = 2,
		/// <summary>
		/// 日志3级，系统日志
		/// </summary>
		SystemLog = 3,
		/// <summary>
		/// 日志4级，警告信息
		/// </summary>
		Warning = 4,
		/// <summary>
		/// 日志5级，异常
		/// </summary>
		Error = 5
	}
}
