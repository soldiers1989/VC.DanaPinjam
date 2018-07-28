using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace YYLog.ClassLibrary
{
	/// <summary>
	/// 文件日志类库
	/// </summary>
	public class Log
	{
		private static ArrayList _logMsg = new ArrayList();
		private static bool _isInit = false;
		private static DateTime _modifyTime = DateTime.Now;
		private static int _serverId = -1;
		private static int _logFileSize = 1024 * 4;
		private static int _logLevel = 1;

		private static string _fileNameFormat = "yyyyMMdd";
		private static string _logType = "file";
		private static string _logFilePath = @"c:\";
		
		//private static HttpReqHandler _request = new HttpReqHandler();
		public static bool IsInit
		{
			get
			{
				return _isInit;
			}
		}

		/// <summary>
		/// 初使化日志组件
		/// </summary>
		/// <param name="helper"></param>
		public static void Init(int serverId, int logFileSize, string fileNameFormat, string logFilePath, LogType logLevel)
		{
			_serverId = serverId;
			_logFileSize = logFileSize;
			_fileNameFormat = fileNameFormat;
			_logFilePath = logFilePath;
			_logLevel = (int)logLevel;
			WriteSystemLog("Log::Init", "***************************FY.Logfiles日志初使化***************************");
			WriteSystemLog("Log::Init", "GetLocalServerId={0}", serverId);
			WriteSystemLog("Log::Init", "GetLogFileLength={0}", logFileSize);
			WriteSystemLog("Log::Init", "GetLogFileNameForamt={0}", fileNameFormat);
			WriteSystemLog("Log::Init", "GetLogFilePath={0}", logFilePath);
			WriteSystemLog("Log::Init", "GetLogLevel={0}", logLevel);
			WriteSystemLog("Log::Init", "Name：FY.Logfiles，Version：1.0.0.2，Author：F1，Phone：15988482677，QQ：535550100");
			WriteSystemLog("Log::Init", "***************************FY.Logfiles日志初使化结束************************");
			_isInit = true;
		}

		/// <summary>
		/// 写日志
		/// </summary>
		/// <param name="objId">对象ID</param>
		/// <param name="logType">日志等级、日志类型</param>
		/// <param name="moduleName">调用模块名称</param>
		/// <param name="msg">日志内容</param>
		public static void WriteLog(int objId, LogType logType, string moduleName, string msg)
		{
			msg = string.Format("[{0}]号服务器::{1}", _serverId, msg);
			if (_isInit)
			{
				if (_logLevel <= (int)logType)
				{
					writeLog(objId, logType, moduleName, msg);
				}
			}

			Trace.WriteLine(String.Format("{0} {1}", moduleName, msg));
			if (_logLevel == 1)
				Console.WriteLine(String.Format("{0} {1}", moduleName, msg));
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		private static void writeLog(int objId, LogType logType, string moduleName, string msg)
		{

			//if ((int)logType <= config.GetLogLevel()) return;
			string logStr = DateTime.Now.ToString("HH:mm:ss") + " " + String.Format("[{0}] {1} {2}", logType, moduleName, msg);

			lock (_logMsg)
			{
				_logMsg.Add(logStr);

				long ticks = DateTime.Now.Ticks - _modifyTime.Ticks;

				if (_logMsg.Count < 100 && TimeSpan.FromTicks(ticks).TotalSeconds < 10)
				{
					return;
				}

				_modifyTime = DateTime.Now;
			}

			if (String.IsNullOrEmpty(_logType) || _logType == "file")
			{
				///文件日志
				StreamWriter sw = null;
				try
				{
					string filePath = _logFilePath;
					filePath = String.IsNullOrEmpty(filePath) ? System.AppDomain.CurrentDomain.BaseDirectory + "/log" : filePath;
					string fileName = DateTime.Now.ToString(_fileNameFormat);
					string fileFullName = Path.Combine(filePath, fileName + ".log");

					Console.WriteLine(fileFullName);
					if (!Directory.Exists(filePath))
					{
						Directory.CreateDirectory(filePath);
					}
					FileInfo fi = new FileInfo(fileFullName);
					int i = 1;
					if (!fi.Exists)
					{
						FileStream fs = fi.Create();
						fi.Refresh();
						fs.Close();
						fs = null;
					}
					while (fi.Length >= _logFileSize)
					{
						fileFullName = fileFullName = Path.Combine(filePath, fileName + "(" + i + ").log");
						fi = new FileInfo(fileFullName);
						if (!fi.Exists)
						{
							FileStream fs = fi.Create();
							fi.Refresh();
							fs.Close();
							fs = null;
						}
						i++;
					}
					sw = fi.AppendText();

					lock (_logMsg)
					{
						foreach (object o in _logMsg)
						{
							sw.WriteLine(Convert.ToString(o));
						}

						_logMsg.Clear();
					}
				}
				catch (Exception ex)
				{
					Trace.WriteLine(String.Format("Log::writeLog {0}", ex.Message));
				}
				finally
				{
					if (null != sw)
					{
						sw.Flush();
						sw.Close();
						sw = null;
					}
				}
			}
			else if (_logType == "http")
			{
				///Http 日志接口
				//_request.BaseUrl = config.GetLogHttpWriteUrl();
				///writelog.php?modulename=test&msgtype=1&msgcontent=gfadgfda&tag=php&client=aaa&msglevel=1

				/*
				string url = String.Format("/writelog.php?modulename={0}&msgtype={1}&tag=csharp&client={2}&msglevel={3}"
					, moduleName, (int)logType, config.GetLocalServerId(), 1);

				msg = msg.Length > 200 ? msg.Substring(0, 200) : msg;
				string param = String.Format("msgcontent={0}", HttpUtility.UrlEncode(msg));
				_request.Request(url, param);
				 * */
			}
		}

		/// <summary>
		/// 异常消息日志
		/// </summary>
		/// <param name="moduleName"></param>
		/// <param name="msg"></param>
		public static void WriteErrorLog(string moduleName, string msg)
		{
			WriteLog(0, LogType.Error, moduleName, msg);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="moduleName"></param>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public static void WriteErrorLog(string moduleName, string format, params object[] args)
		{
			WriteLog(0, LogType.Error, moduleName, string.Format(format, args));
		}

		public static void WriteWarning(string moduleName, string msg)
		{
			WriteLog(0, LogType.Warning, moduleName, msg);
		}

		public static void WriteWarning(string moduleName, string format, params object[] args)
		{
			WriteLog(0, LogType.Warning, moduleName, string.Format(format, args));
		}

		public static void WriteSystemLog(string moduleName, string format, params object[] args)
		{
			WriteLog(0, LogType.SystemLog, moduleName, string.Format(format, args));
		}

		public static void WriteLog(string moduleName, string msg)
		{
			WriteLog(0, LogType.Success, moduleName, msg);
		}

		public static void WriteLog(string moduleName, string format, params object[] args)
		{
			WriteLog(0, LogType.Success, moduleName, string.Format(format, args));
		}

		public static void WriteDebugLog(string moduleName, string format, params object[] args)
		{
			WriteLog(0, LogType.Debug, moduleName, string.Format(format, args));
		}
	}
}
