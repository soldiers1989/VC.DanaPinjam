using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using StackExchange.Redis;
using System.Threading.Tasks;

namespace RedisPools
{
	class ConnectThread
	{
		Thread _connectThread;

		private ConnectionMultiplexer _conn;
		public ConnectionMultiplexer Conn
		{
			get
			{
				return _conn;
			}
		}

		private bool _isConnected;
		public bool IsConnected
		{
			get {
				return _isConnected;
			}
		}

		public void Start()
		{
			_connectThread = new Thread(new ThreadStart(connect));
		}

		private void connect()
		{ 
			
		}
	}
}
