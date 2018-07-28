using System;
using System.Collections.Generic;
using System.Text;

namespace DBMonoUtility
{
	[Serializable]
	public class HttpResult
	{
		public string Result = String.Empty;
		public double Time = 0.0f;
		public long QueueId = 0;
		public string ErrMsg = String.Empty;
	}

	[Serializable]
	public class QueueStatus
	{
		public string name = String.Empty;
		public int maxqueue = 0;
		public int putpos = 0;
		public int putlap = 0;
		public int getpos = 0;
		public int unread = 0;
	}
}
