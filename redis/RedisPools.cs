using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StackExchange.Redis;
using YYLog.ClassLibrary;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RedisPools
{
    public class RedisPools
    {
        /// <summary>
        /// 最少连接
        /// </summary>
        private static int _minConns = 200;

        private static bool _isBreak = false;

        /// <summary>
        /// 空闲连接的回收时间
        /// </summary>
        private static int _maxIdle = 1000 * 60 * 3;

        /// <summary>
        /// 最大连接
        /// </summary>
        private static int _maxConns = 500;
        private static bool _initialized = false;

        private static ConfigurationOptions _config = new ConfigurationOptions();
        //private static Stack<ConnectionMultiplexer> _aLivePool = new Stack<ConnectionMultiplexer>();
        private static Hashtable _aLivePool = new Hashtable();

        private static Hashtable _busyPool = new Hashtable();
        private static Hashtable _pools = new Hashtable();
        private static string _defaultPoolName = "f1redispools";

        private static Thread _checkThread = null;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static RedisPools GetInstance(string poolName)
        {
            if (_pools.ContainsKey(poolName))
                return _pools[poolName] as RedisPools;

            RedisPools pool = new RedisPools();
            _pools[poolName] = pool;

            return pool;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static RedisPools GetInstance()
        {
            return GetInstance(_defaultPoolName);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool Init(string serverInfo, int initConns)
        {
            return Init(serverInfo, Proxy.Twemproxy, initConns, String.Empty);
        }

        /// <summary>
        /// 初使化Redis连接池
        /// </summary>
        /// <param name="serverInfo">服务器信息</param>
        /// <param name="proxy"></param>
        /// <param name="initConns">初使连接数</param>
        /// <param name="password">密钥</param>
        /// <returns>成功或失败</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool Init(string serverInfo, Proxy proxy, int initConns, string password)
        {
            if (_initialized) return false;
            _initialized = true;

            Log.WriteDebugLog("RedisPools::Init", "准备初使化Redis连接池,配置信息：{0}", serverInfo);

            try
            {
                if (null == _checkThread)
                {
                    _checkThread = new Thread(new ThreadStart(checkConnection));
                    _checkThread.Name = "RedisPoolsCheckThread";
                    _checkThread.IsBackground = true;
                    _checkThread.Start();
                }

                ConfigurationOptions config = new ConfigurationOptions();

                string[] servers = serverInfo.Split(';');
                foreach (string si in servers)
                {
                    if (!String.IsNullOrEmpty(si))
                        config.EndPoints.Add(si);
                }

                if (proxy != Proxy.None)
                {
                    config.Proxy = proxy;
                }
                else
                {
                    config.Password = password;
                }
                config.AbortOnConnectFail = false;

                _config = config;

                ThreadPool.QueueUserWorkItem(connectRedis, initConns);
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("RedisPools::Init", ex.Message);
            }

            return _initialized;
        }

        private static void connectRedis(object state)
        {
            int initConns = 0;
            int.TryParse(Convert.ToString(state), out initConns);
            initConns = initConns == 0 ? 5 : initConns;

            for (int i = 0; i < initConns && initConns > _aLivePool.Count; i++)
            {
                ConnectionMultiplexer redis = getConnection();
                //添加到活跃池
                if (redis.IsConnected)
                {
                    lock (_aLivePool)
                    {
                        _aLivePool[redis] = DateTime.Now;
                    }
                }
                else
                {
                    Log.WriteDebugLog("RedisPools::Init", "初使化Redis连接池,新建连接失败，返回redis.IsConnected=={0},serverInfo={1}", redis.IsConnected, _config);
                }
            }

            if (_aLivePool.Count > 0)
            {
                Log.WriteSystemLog("RedisPools::Init", "初使化Redis连接池,成功创建[{0}]个连接！", _aLivePool.Count);
            }
        }

        private static void checkConnection()
        {
            while (!_isBreak)
            {
                Log.WriteDebugLog("RedisPools::checkConnection", "空闲连接数：{0}, 进行中连接数：{1}", _aLivePool.Count, _busyPool.Count);

                try
                {
                    if (_busyPool.Count == 0 && _aLivePool.Count > 0 && _aLivePool.Count > _minConns)
                    {
                        int clearCount = 0;
                        Log.WriteWarning("RedisPools::checkConnection", "总连接数：{0}，都是空闲连接：{1}，将对多余空闲连接进行回收。", _aLivePool.Count + _busyPool.Count, _aLivePool.Count);
                        foreach (ConnectionMultiplexer conn in new IteratorIsolateCollection(_aLivePool.Keys))
                        {
                            DateTime expire = (DateTime)_aLivePool[conn];

                            if (_aLivePool.Count + _busyPool.Count > _minConns
                                && ((expire.AddMilliseconds(_maxIdle)) < DateTime.Now || !conn.IsConnected))
                            {
                                Log.WriteWarning("RedisPools::checkConnection", "检测到连接已超时,最近使用该连接的时间为【{0}】,即将回收,conn.IsConnected == {1}！", expire, conn.IsConnected);

                                try
                                {
                                    conn.Dispose();
                                    lock (_aLivePool)
                                    {
                                        _aLivePool.Remove(conn);
                                        clearCount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteErrorLog("RedisPools::checkConnection", ex.Message);
                                }
                            }

                            if (clearCount > 0)
                            {
                                Log.WriteWarning("RedisPools::checkConnection", "清除了：{0}个空闲连接。", clearCount);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteErrorLog("RedisPools::checkConnection", "发生异常：{0}", ex.Message);
                }
                Thread.Sleep(30 * 1000);
            }
        }

        #region redis 事件处理
        static void redis_InternalError(object sender, InternalErrorEventArgs e)
        {
            //异常处理
            Log.WriteErrorLog("RedisPools::redis_InternalError", "Redis抛出异常,连接类型：{0},Origin：{1},Message：{2}", e.ConnectionType, e.Origin, e.Exception.Message);
        }

        static void redis_ErrorMessage(object sender, RedisErrorEventArgs e)
        {
            //异常处理
            Log.WriteErrorLog("RedisPools::redis_ErrorMessage", "Redis抛出异常,连接EndPoint：{0},Message：{1}", e.EndPoint, e.Message);
        }

        static void redis_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            //连接失败
            Log.WriteErrorLog("RedisPools::redis_ConnectionFailed", "Redis抛出异常,连接类型：{0},EndPoint：{1},FailureType：{2},Message：{3}", e.ConnectionType, e.EndPoint, e.FailureType, e.Exception.Message);
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ConnectionMultiplexer GetConnection()
        {
            ConnectionMultiplexer conn = null;
            Random r = new Random();
            lock (_aLivePool)
            {
                if (_aLivePool.Count > 0)
                {
                    for (int i = 0; i < _aLivePool.Count; i++)
                    {
                        int index = r.Next(0, _aLivePool.Count);
                        int eachIndex = 0;
                        bool match = false;
                        foreach (ConnectionMultiplexer cm in new IteratorIsolateCollection(_aLivePool))
                        {
                            if (eachIndex == index ||match)
                            {
                                match = true;
                                if (cm.IsConnected)
                                {
                                    conn = cm;
                                    _aLivePool.Remove(cm);
                                    break;
                                }
                                else
                                {
                                    Log.WriteWarning("RedisPools::GetConnection", "获取连接时发现cm.IsConnected==false,从连接池中移除");
                                    cm.Close();
                                    _aLivePool.Remove(cm);
                                }
                            }
                            eachIndex ++;    
                        }
                    }
                }
            }

            if (null == conn)
            {
                conn = getConnection();
                if (!conn.IsConnected)
                {
                    Log.WriteErrorLog("RedisPools::GetConnection", "获取连接时返回IsConnected为false. config = {0}", _config.ToString());
                    return null;
                }
            }

            if (null != conn)
            {
                lock (_busyPool)
                {
                    _busyPool[conn] = DateTime.Now;
                }
            }

            return conn;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static ConnectionMultiplexer getConnection()
        {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(_config);
            redis.ConnectionFailed += redis_ConnectionFailed;
            redis.ErrorMessage += redis_ErrorMessage;
            redis.InternalError += redis_InternalError;
            return redis;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public bool ReleaseConnection(ConnectionMultiplexer conn)
        {
            try
            {
                lock (_busyPool)
                {
                    _busyPool.Remove(conn);
                }

                lock (_aLivePool)
                {
                    _aLivePool[conn] = DateTime.Now;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("RedisPools::ReleaseConnection", ex.Message);
            }
            return false;
        }

        public static void Exit()
        {
            _isBreak = true;

            _checkThread.Abort();
            _checkThread = null;
        }
    }
}