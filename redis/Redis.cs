using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using YYLog.ClassLibrary;

namespace RedisPools
{
    /// <summary>
    /// Redis操作类，内部采用了连接池
    /// </summary>
    public class Redis
    {
        RedisPools _pool = null;

        public Redis()
        {
            _pool = RedisPools.GetInstance("TestRedisPool");
        }

        public Redis(string poolName)
        {
            _pool = RedisPools.GetInstance(poolName);
        }

        public long HashDelete(RedisKey key, RedisValue hashField)
        {
            return HashDelete(key, new RedisValue[] { hashField });
        }

        public long HashDelete(RedisKey key, RedisValue[] hashField)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                conn = _pool.GetConnection();
                IDatabase db = conn.GetDatabase();

                return db.HashDelete(key, hashField);
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::HashExists", ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return -1;
        }

        public long KeyDelete(RedisKey key)
        {
            return KeyDelete(new RedisKey[] { key });
        }

        public long KeyDelete(RedisKey[] key)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                conn = _pool.GetConnection();
                IDatabase db = conn.GetDatabase();

                return db.KeyDelete(key);
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::HashExists", ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return -1;
        }

        public bool LockTake(RedisKey key, RedisValue value, int t = 10)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                if (RedisValue.Null != value)
                {
                    conn = _pool.GetConnection();
                    IDatabase db = conn.GetDatabase();

                    return db.LockTake(key, value, new TimeSpan(0, 0, t));
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::LockTake", ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return false;
        }

        public bool LockRelease(RedisKey key, RedisValue value)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                if (RedisValue.Null != value)
                {
                    conn = _pool.GetConnection();
                    IDatabase db = conn.GetDatabase();
                    return db.LockRelease(key, value);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::LockRelease", ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return false;
        }

        public bool StringSet(string key, RedisValue val)
        {
            return StringSet(key, val, TimeSpan.MaxValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="t">失效时间，单位秒</param>
        /// <returns></returns>
        public bool StringSet(string key, RedisValue val, int t)
        {
            TimeSpan ts = new TimeSpan(0, 0, t);
            return StringSet(key, val, ts);
        }

        public bool StringSet(string key, RedisValue val, TimeSpan ts)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                conn = _pool.GetConnection();

                if (null == conn)
                {
                    Log.WriteErrorLog("Redis::StringSet", "获取连接返回为空。");
                }
                else
                {
                    IDatabase db = conn.GetDatabase();
                    return db.StringSet(key, val, ts);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::StringSet", "{0} {1}", null == conn ? "" : Convert.ToString(conn.IsConnected), ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return false;
        }

        public bool HashExists(string key, RedisValue val)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                conn = _pool.GetConnection();
                if (null == conn)
                {
                    Log.WriteErrorLog("Redis::HashExists", "获取连接返回为空。");
                }
                else
                {
                    IDatabase db = conn.GetDatabase();

                    return db.HashExists(key, val);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::HashExists", ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return false;
        }

        public RedisValue StringIncrement(string key, RedisValue val)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                conn = _pool.GetConnection();
                if (null == conn)
                {
                    Log.WriteErrorLog("Redis::StringIncrement", "获取连接返回为空。");
                }
                else
                {
                    IDatabase db = conn.GetDatabase();

                    return db.HashGet(key, val);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::StringIncrement", ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return 0;
        }

        public double StringIncrement(string key, double d)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                conn = _pool.GetConnection();
                if (null == conn)
                {
                    Log.WriteErrorLog("Redis::StringIncrement", "获取连接返回为空。");
                }
                else
                {
                    IDatabase db = conn.GetDatabase();
                    return db.StringIncrement(key, d);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::StringIncrement", ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return -1;
        }

        public RedisValue HashGet(RedisKey key, RedisValue hashFeld)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                conn = _pool.GetConnection();
                if (null == conn)
                {
                    Log.WriteErrorLog("Redis::HashGet", "获取连接返回为空。");
                }
                else
                {
                    IDatabase db = conn.GetDatabase();
                    return db.HashGet(key, hashFeld);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::HashGet", ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return RedisValue.EmptyString;
        }

        public RedisValue[] HashGet(RedisKey key, RedisValue[] hashFelds)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                conn = _pool.GetConnection();
                if (null == conn)
                {
                    Log.WriteErrorLog("Redis::HashGet", "获取连接返回为空。");
                }
                else
                {
                    IDatabase db = conn.GetDatabase();
                    return db.HashGet(key, hashFelds);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::HashGet", ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return new RedisValue[] { };
        }

        public RedisValue StringGet(string key)
        {
            ConnectionMultiplexer conn = null;

            try
            {
                conn = _pool.GetConnection();
                if (null == conn)
                {
                    Log.WriteErrorLog("Redis::StringGet", "获取连接返回为空。");
                }
                else
                {
                    IDatabase db = conn.GetDatabase();
                    return db.StringGet(key);
                }
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("Redis::StringSet", "{0} {1}", null == conn ? "" : Convert.ToString(conn.IsConnected), ex.Message);
            }
            finally
            {
                if (null != conn)
                {
                    _pool.ReleaseConnection(conn);
                }
            }

            return String.Empty;
        }
    }
}