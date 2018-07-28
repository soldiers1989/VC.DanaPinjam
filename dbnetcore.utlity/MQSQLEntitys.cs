using System;
using System.Collections.Generic;
using System.Text;

namespace DBMonoUtility
{
	[Serializable]
	public class MQSQLEntitys
	{
		public long QueueId = 0;
		public string DBPoolName = String.Empty;
		public string PublicKey = String.Empty;
		public string SqlStr = String.Empty;
		public List<ParamItem> Param;
	}

	[Serializable]
	public class MQEntitys<T>
	{
		public long QueueId = 0;
		public MQType MQType = MQType.MQSQL;
		public string IVStr = String.Empty;
		public T Entitys;
	}

	[Serializable]
	public enum MQType
	{
		MQSQL
	}
}
