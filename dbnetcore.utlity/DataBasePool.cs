using System;
using System.Text;
using System.Data;
using System.Collections;
using System.Data.SqlClient;

using Encryption;
//using Oracle.DataAccess;
//using Oracle.DataAccess.Client;
using MySql.Data.MySqlClient;
//using TY.DatabaseOperation;
using System.Runtime.CompilerServices;
using System.Threading;
using YYLog.ClassLibrary;

namespace DBMonoUtility
{
    public class DataBasePool : IDisposable
    {
        //private long mConnectionID = 0;
        //private IDbConnection mConnection;
        private static IniFile mIniFile = new IniFile();
        private static Hashtable mConnectionTable = new Hashtable();

        private static int _maxIdle = 5 * 60 * 1000;
        private static Hashtable _pools = new Hashtable();
        private static Hashtable _busyPools = new Hashtable();
        private static int _minConns = 5;
        private static int _maxConns = 100;
        private static Thread _checkThread = null;
        private static bool _isBreak = false;
        public DataBasePool()
        {
        }

        private static string getInitConnectionString(string poolName)
        {
            string val = mIniFile.GetInitPramas(poolName, "DBCONNECTIONSTRING");
            return val;
        }

        private static string getInitVector(string poolName)
        {
            string val = mIniFile.GetInitPramas(poolName, "INITVECTOR");
            return val;
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        private static IDbConnection getDbConnection(string poolName)
        {
            string dbType = String.Empty;
            dbType = mIniFile.GetInitPramas(poolName, "DBTYPE").ToLower();
            switch (dbType)
            {
                //case "oracle":
                //		return new OracleConnection(mConnectionTable[poolName] as string);
                case "sqlseSqlConnectionrver":
                    return new SqlConnection(mConnectionTable[poolName] as string);
                case "mysql":
                    return new MySqlConnection(mConnectionTable[poolName] as string);
                default:
                    return new MySqlConnection(mConnectionTable[poolName] as string);
            }
        }

        public static string AddDataBaseConnectionString(string poolName, string publicKey, int minConns, int initConns)
        {
            _minConns = minConns;
            if (String.IsNullOrEmpty(poolName)) throw new ArgumentException("参数没有传。", "poolName");
            poolName = poolName.Trim().ToLower();
            string connectionString = getInitConnectionString(poolName);
            string initVector = getInitVector(poolName);
            string destStr = null;
            if (String.IsNullOrEmpty(connectionString) || String.IsNullOrEmpty(initVector))
            {
                return "config exception";
            }
            Decryptor dec = new Decryptor(EncryptionAlgorithm.TripleDes);
            dec.InitVec = Convert.FromBase64String(initVector);
            byte[] plain = dec.Decrypt(Convert.FromBase64String(connectionString), Encoding.ASCII.GetBytes(publicKey));
            destStr = Encoding.ASCII.GetString(plain);
            if (!mConnectionTable.ContainsKey(poolName))
            {
                mConnectionTable.Add(poolName, destStr);
            }
            else
            {
                mConnectionTable[poolName] = destStr;
            }

            Hashtable conns = new Hashtable(initConns);
            for (int i = 0; i < initConns; i++)
            {
                IDbConnection conn = getDbConnection(poolName);
                conn.Open();
                if (conn.State == ConnectionState.Open)
                {
                    conns[conn] = DateTime.Now;
                }
            }
            _pools[poolName] = conns;


            if (null == _checkThread)
            {
                _checkThread = new Thread(new ThreadStart(checkProc));
                _checkThread.Name = "database pools connection check thread.";
                _checkThread.IsBackground = true;
                _checkThread.Start();
            }
            return destStr;
        }

        public void Dispose()
        {
            //Close ();
            GC.SuppressFinalize(this);
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        public IDbConnection GetConnection(string poolName)
        {
            poolName = poolName.ToLower().Trim();
            IDbConnection conn = null;
            Hashtable conns = null;

            lock (_pools)
            {
                conns = _pools[poolName] as Hashtable;
                if (null != conns && conns.Count > 0)
                {
                    foreach (IDbConnection c in new IteratorIsolateCollection(conns.Keys))
                    {
                        if (c.State == ConnectionState.Open)
                        {
                            conn = c;
                            conns.Remove(c);
                            break;
                        }
                        else
                        {
                            Log.WriteWarning("DataBasePool::GetConnection", "检查连接的[{0}]!=Open,将关闭该连接。", c.State);
                            c.Close();
                            conns.Remove(c);
                        }
                    }
                }
            }

            if (null == conn)
            {
                ///如果没取到空闲连接，并且总连接数小于最大连接时，创建新连接
                lock (_busyPools)
                {
                    if (_maxConns > (conns.Count + _busyPools.Count))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            conn = getDbConnection(poolName);
                            conn.Open();
                            if (conn.State == ConnectionState.Open)
                            {
                                break;
                            }
                        }
                    }
                }

                if (null == conn)
                {
                    Log.WriteWarning("DataBasePool::GetConnection", "已超出设定的最大连接，{0}, 空闲：{1}, 进行中：{2}", _maxConns, conns.Count, _busyPools.Count);

                    throw new Exception(String.Format("已超出设定的最大连接，{0}", _maxConns));
                }
            }

            if (null != conn)
            {
                lock (_busyPools)
                {
                    _busyPools[conn] = DateTime.Now;
                }
            }

            return conn;
        }

        public static void ReleaseConnection(string poolName, IDbConnection conn)
        {
            if (null == conn) return;
            poolName = poolName.Trim().ToLower();
            lock (_busyPools)
            {
                _busyPools.Remove(conn);
            }

            Hashtable conns = null;
            lock (_pools)
            {
                conns = _pools[poolName] as Hashtable;

                if (null != conns)
                {
                    conns[conn] = DateTime.Now;
                    _pools[poolName] = conns;
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void checkProc()
        {
            while (!_isBreak)
            {
                try
                {
                    lock (_pools)
                        foreach (string name in new IteratorIsolateCollection(_pools.Keys))
                        {
                            Hashtable conns = _pools[name] as Hashtable;
                            Log.WriteDebugLog("DataBasePool::checkProc", "空闲连接数为：{0}，处理中连接：{1}", conns.Count, _busyPools.Count);
                            foreach (IDbConnection c in new IteratorIsolateCollection(conns.Keys))
                            {
                                try
                                {
                                    DateTime expire = (DateTime)conns[c];
                                    if (((expire.AddMilliseconds(_maxIdle)) < DateTime.Now))
                                    {
                                        Log.WriteDebugLog("DataBasePool::checkProc", "链接已超时，最后使用时间{0}，从链接池中移除，将重新打开。", expire.ToString("yyyy-MM-dd hh:mm:ss"));
                                        c.Close();
                                        conns.Remove(c);

                                        try
                                        {
                                            IDbConnection conn = getDbConnection(name);
                                            if (conn.State == ConnectionState.Open)
                                            {
                                                Log.WriteDebugLog("DataBasePool::checkProc", "重新打开成功。");
                                                conns[conn] = DateTime.Now;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.WriteErrorLog("DataBasePool::checkProc", "重新打开连接失败，{0}", ex.Message);
                                        }
                                    }
                                    else
                                    {
                                        conns.Remove(c);
                                        c.Close();

                                        try
                                        {
                                            IDbConnection conn = getDbConnection(name);
                                            if (conn.State == ConnectionState.Open)
                                            {
                                                Log.WriteDebugLog("DataBasePool::checkProc", "重新打开成功。");
                                                conns[conn] = DateTime.Now;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.WriteErrorLog("DataBasePool::checkProc", "重新打开连接失败，{0}", ex.Message);
                                        }

                                        //Log.WriteWarning("DataBasePool::checkProc", "连接状态：c.State = {0}, 最后使用时间为：{1}", c.State, expire);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteErrorLog("DataBasePool::checkProc", ex.Message);
                                }
                            }
                            /*
                            if (null != conns && conns.Count > 0 && _minConns < (conns.Count + _busyPools.Count))
                            {
                                foreach (IDbConnection c in new IteratorIsolateCollection(conns.Keys))
                                {
                                    if (_minConns > (conns.Count + _busyPools.Count)) break;
                                    DateTime expire = (DateTime)conns[c];
                                    if (c.State != ConnectionState.Open || ((expire.AddMilliseconds(_maxIdle)) < DateTime.Now))
                                    {
                                        c.Close();
                                        conns.Remove(c);
                                        Log.WriteWarning("DataBasePool::checkProc", "从连接池中移除连接，c.State = {0}, 最后使用时间为：{1}", c.State, expire);
                                    }
                                }
                            }
                            else
                            {
                                
                            }

                            */
                        }
                }
                catch (Exception ex)
                {
                    Log.WriteErrorLog("DataBasePool::checkProc", "发生异常：{0}", ex.Message);
                }
                Thread.Sleep(10 * 1000);
            }
        }
    }
}

