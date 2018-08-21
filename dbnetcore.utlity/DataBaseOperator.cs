using System;
using System.Data;
using System.Collections;
using System.Data.SqlClient;
using System.Collections.Generic;

//using Oracle.DataAccess.Client;
//using TY.DatabaseOperation;
using System.Text;
using MySql.Data;
//using Oracle.DataAccess.Types;
using System.IO;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace DBMonoUtility
{
	public class DataBaseOperator : IDisposable
	{
		private static string mPoolName = String.Empty;
        private string mUsePoolName = String.Empty;
		private DataBasePool mPool = null;
        private static IniFile mIniFile = new IniFile();
        //private IDbConnection mConnection = null;

		public DataBaseOperator ()
		{
            
			mPool = new DataBasePool();
			if (null != mPoolName) {
                mUsePoolName = mPoolName;
                ///mConnection = mPool.Open(mUsePoolName);
                ///mConnection = mPool.GetConnection(mUsePoolName);
            } else {
				throw new ArgumentNullException("poolName is null.");
			}
		}

        public DataBaseOperator(string poolName)
        {
            mPool = new DataBasePool();
            if (null != poolName)
            {
                mUsePoolName = poolName;
                //mConnection = mPool.GetConnection(mUsePoolName);
            }
            else
            {
                throw new ArgumentNullException("poolName is null.");
            }
        }

		public static void SetDbIniFilePath(string path)
		{
            mIniFile = new IniFile(path);
		}

		/// <summary>
		/// 初使化数据库连接池名称
		/// </summary>
		/// <param name='poolName'>
		/// Pool name.
		/// </param>
		public static void Init(string poolName)
		{
			mPoolName = poolName;
		}

		public DataTable GetTable(string expr)
		{
			return GetTable(expr, new ParamCollections().GetParams());
		}

		/// <summary>
		/// 获取数据文法，返回数据表对象
		/// </summary>
		/// <returns>
		/// 返回的数据对象.
		/// </returns>
		/// <param name='expr'>
		/// Expr Sql 表达式.
		/// </param>
		/// <param name='param'>
		/// Parameter 参数列表.
		/// </param>
		public DataTable GetTable(string expr, List<ParamItem> param)
		{
			DataTable data = null;
			IDbCommand command = null;
			IDataReader reader = null;
            
			try {
				if (prepareCommand (ref expr, param, out command)) {
                    command.CommandTimeout = 120;
                    reader = command.ExecuteReader ();
                    if (null != reader)
						data = new DataTable ();
					for (int i = 0; i < reader.FieldCount; i++) {
						data.Columns.Add (reader.GetName (i));
					}

					object o = null;
                    int count = reader.FieldCount;
					while (reader.Read()) {
						DataRow row = data.NewRow ();
                        row.BeginEdit();
                        for (int i = 0; i < count; i++)
                        {
                            o = reader.GetValue(i);
                            row[i] = null != o ? o.ToString().Replace(@"\0", "").Replace("\0", "").Replace("&#x0;", "") : null;
                        }
						row.EndEdit ();
						data.Rows.Add (row);
					}
				} else {
					throw new Exception ("prepareCommand 发生异常，返回失败。");
				}
			}
			//catch (OracleException ex1) {
			//	throw new Exception ("Executing SQL statement (" + expr + ") failed(OracleException). Reason: " + ex1.Message);
			//}
			catch (Exception ex2) {
				throw new Exception ("Executing SQL statement (" + expr + ") failed(Exception). Reason: " + ex2.StackTrace);
			} finally {
				DataBasePool.ReleaseConnection(mUsePoolName, command.Connection);
				if (null != reader) {
					reader.Close ();
					reader.Dispose ();
					reader = null;
				}
				if (null != command) {
					command.Parameters.Clear ();
					command.Dispose ();
					command = null;
				}
			}
			return data;
		}

		public int GetCount(string expr, List<ParamItem> param)
		{
			int count = 0;
			IDbCommand command = null;
			try {
				if (prepareCommand (ref expr, param, out command)) {
                    command.CommandTimeout = 120;
                    object o = command.ExecuteScalar ();
					if (o != null && !System.DBNull.Equals (o, null))
						count = Convert.ToInt32 (o);
				}
			} catch (Exception ex) {
				throw new Exception ("Executing SQL statement (" + expr + ") failed. Reason: " + ex.Message);
			} finally {
				DataBasePool.ReleaseConnection(mUsePoolName, command.Connection);
				if (null != command) {
					command.Parameters.Clear ();
					command.Dispose ();
					command = null;
				}
			}
			return count;
		}

		public DataRow GetRow(string expr, List<ParamItem> param)
		{
			DataTable dt = GetTable(expr, param);
			if (null != dt && dt.Rows.Count == 1)
			{
				return dt.Rows[0];
			}
			return null;
		}

		public object GetScalar(string expr, List<ParamItem> param)
		{
			DataTable dt = GetTable(expr, param);
			if (null != dt && dt.Rows.Count == 1 && dt.Columns.Count == 1)
			{
				return dt.Rows[0][0];
			}
			return null;
		}

		public int ExecuteStatement (string expr, List<ParamItem> param)
		{
			int result = -1;
			IDbCommand command = null;
            IDbTransaction tran = null;

            try {
				if (prepareCommand (ref expr, param, out command)) {
                    try
                    {
                        tran = command.Connection.BeginTransaction();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                    result = command.ExecuteNonQuery ();
				}
			} catch (Exception ex1) {
				throw new Exception ("Executing SQL statement (" + expr + ") failed. Reason: " + ex1.Message);
			} finally {
                try
                {
                    if (null != tran)
                    {
                        tran.Commit();
                        tran.Dispose();
                        tran = null;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
                DataBasePool.ReleaseConnection(mUsePoolName, command.Connection);
                if (null != command) {
					command.Parameters.Clear ();
					command.Dispose ();
					command = null;
				}
            }
			return result;
		}

		public ArrayList ExecProcedure(string procName, List<ParamItem> param)
		{
			validChecked (procName, param);

			ArrayList l1 = new ArrayList ();
			IDbCommand command = null;
			IDataParameter[] dataParameters = initParameters (param);
			IDataReader reader = null;
            IDbConnection conn = mPool.GetConnection(mUsePoolName);
			if (null == conn)
			{
				//写异常
				return null;
			}
			try {
				if (null != conn && ConnectionState.Open == conn.State)
				{
                    command = initCommand();
					command.Connection = conn;
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = procName;
					for (int i = 0; i < dataParameters.Length; i++) {
						command.Parameters.Add (dataParameters [i]);
					}
                    command.CommandTimeout = 120;
                    reader = command.ExecuteReader();

					object o = null;
					int count = reader.FieldCount;
					if (reader.Read())
					{
						for (int i = 0; i < count; i++)
						{
							o = reader.GetValue(i);
							l1.Add(null != o ? o.ToString().Replace(@"\0", "").Replace("\0", "").Replace("&#x0;", "") : null);
						}
					}
				}
			} catch (Exception ex1) {
				throw new Exception ("Executing procedure (" + procName + ") failed. Reason: " + ex1.Message);
			} finally {
				if (null != reader)
				{
					reader.Close();
					reader.Dispose();
					reader = null;
				}
                DataBasePool.ReleaseConnection(mUsePoolName, conn);
                if (null != command) {
					command.Parameters.Clear ();
					command.Dispose ();
					command = null;
				}
			}
			return l1;
		}

		public DataTable ExecProcedure(string procName, List<ParamItem> param, out Hashtable outAl)
		{
			validChecked(procName, param);
			DataSet ds = new DataSet();
			DataTable dt = new DataTable();
			IDbCommand command = null;
			IDataParameter[] dataParameters = initParameters(param);
			outAl = new Hashtable();
			//IDataReader reader = null;
			IDataAdapter adapter = null;
            IDbConnection conn = mPool.GetConnection(mUsePoolName);
			try
			{
				if (null != conn && ConnectionState.Open == conn.State)
				{
					command = initCommand();
					command.Connection = conn;
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = procName;
                    command.CommandTimeout = 120;
                    for (int i = 0; i < dataParameters.Length; i++)
					{
						command.Parameters.Add(dataParameters[i]);
						if (dataParameters[i].Direction == ParameterDirection.InputOutput || dataParameters[i].Direction == ParameterDirection.Output)
						{
							outAl[dataParameters[i].SourceColumn.Replace("@", "")] = (dataParameters[i]);
						}
					}
					adapter = initDataAdapter(command);
					adapter.Fill(ds);
					for (int i = 0; i < dataParameters.Length; i++)
					{
						if (dataParameters[i].Direction == ParameterDirection.InputOutput || dataParameters[i].Direction == ParameterDirection.Output)
						{
							outAl[dataParameters[i].SourceColumn.Replace("@", "")] = ((IDataParameter)outAl[dataParameters[i].SourceColumn.Replace("@", "")]).Value;
						}
					}

					return ds.Tables.Count > 0 ? ds.Tables[0] : null;
				}
			}
			catch (Exception ex1)
			{
				throw new Exception("Executing procedure (" + procName + ") failed. Reason: " + ex1.Message);
			}
			finally
			{
				DataBasePool.ReleaseConnection(mUsePoolName, conn);
				if (null != command)
				{
					command.Parameters.Clear();
					command.Dispose();
					command = null;
				}
			}
			return null;
		}

		public DataTable ExecProcedureByDataReader(string procName, List<ParamItem> param, out ArrayList outAl)
		{
			validChecked(procName, param);
			DataSet ds = new DataSet();
			DataTable dt = new DataTable();
			IDbCommand command = null;
			IDataParameter[] dataParameters = initParameters(param);
			outAl = new ArrayList();
			IDataReader reader = null;
            IDbConnection conn = mPool.GetConnection(mUsePoolName);
			try
			{
				if (null != conn && ConnectionState.Open == conn.State)
				{
					command = initCommand();
					command.Connection = conn;
					command.CommandType = CommandType.StoredProcedure;
					command.CommandText = procName;
					for (int i = 0; i < dataParameters.Length; i++)
					{
						command.Parameters.Add(dataParameters[i]);
						if (dataParameters[i].Direction == ParameterDirection.InputOutput || dataParameters[i].Direction == ParameterDirection.Output)
						{
							outAl.Add(dataParameters[i]);
						}
					}
                    command.CommandTimeout = 120;
                    reader = command.ExecuteReader();

					for (int i = 0; i < outAl.Count; i++)
					{
						outAl[i] = ((IDataParameter)outAl[i]).Value;
					}
					object o = null;
					int count = reader.FieldCount;
					for (int i = 0; i < count; i++)
					{
						dt.Columns.Add(reader.GetName(i));
					}

					while (reader.Read())
					{
						DataRow dr = dt.NewRow();
						for (int i = 0; i < count; i++)
						{
							o = reader.GetValue(i);
							dr[i] = (null != o ? o.ToString().Replace(@"\0", "").Replace("\0", "").Replace("&#x0;", "") : null);
						}
						dt.Rows.Add(dr);
					}
					return dt;
				}
			}
			catch (Exception ex1)
			{
				throw new Exception("Executing procedure (" + procName + ") failed. Reason: " + ex1.Message);
			}
			finally
			{
				if (null != reader)
				{
					reader.Close();
					reader.Dispose();
					reader = null;
				}
                DataBasePool.ReleaseConnection(mUsePoolName, conn);
				if (null != command)
                {
					command.Parameters.Clear();
					command.Dispose();
					command = null;
				}
            }
			return null;
		}

		/// <summary>
		/// 连接关闭
		/// </summary>
		public void Close ()
		{
            //DataBasePool.ReleaseConnection(mUsePoolName, mConnection);
            GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sqlStr"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		public string MysqlReadBlob(string sqlStr, List<ParamItem> param)
		{
			validChecked(sqlStr, param);
			IDbCommand command = null;
			IDataParameter[] dataParameters = initParameters(param);
			MySqlDataReader reader = null;

			byte[] buffer = null;
			StringBuilder content = null;
			try
			{
				if (prepareCommand(ref sqlStr, param, out command))
				{
					content = new StringBuilder();
					reader = (MySqlDataReader)command.ExecuteReader();

					using (reader)
					{
						if (reader.HasRows)
						{
							reader.Read();
							long len = reader.GetBytes(0, 0, null, 0, 0);
							buffer = new byte[len];
							len = reader.GetBytes(0, 0, buffer, 0, (int)len);

							return Encoding.UTF8.GetString(buffer);
						}
					}
				}
			}
			catch (Exception ex1)
			{
				throw new Exception("Executing SQL statement (" + sqlStr + ") failed. Reason: " + ex1.Message);
			}
			finally
			{
				if (null != reader)
				{
					reader.Close();
					reader.Dispose();
					reader = null;
				}
				DataBasePool.ReleaseConnection(mUsePoolName, command.Connection);
				if (null != command)
				{
					command.Parameters.Clear();
					command.Dispose();
					command = null;
				}
			}

			return null != content ? content.ToString() : null;
		}

		#region 事务
		/// <summary>
		/// 事务开始
		/// </summary>
		public IDbTransaction BeginTransaction (IDbConnection conn)
		{
			if (null != conn && ConnectionState.Open == conn.State)
			{
				return conn.BeginTransaction();
			}
			return null;
		}

		/// <summary>
		/// 提交
		/// </summary>
		public void Commit(IDbTransaction tran)
		{
			if (null == tran)
				return;
			tran.Commit();
		}

		/// <summary>
		/// 回滚
		/// </summary>
		public void Rollback(IDbTransaction tran)
		{
			if (null == tran)
				return;
			tran.Rollback();
		}
		#endregion

		private bool prepareCommand(ref string expr, List<ParamItem> param, out IDbCommand cmd)
        {
            bool isOK = false;
            cmd = null;
            validChecked(expr, param);

			cmd = initCommand();
			IDataParameter[] dataParameter = initParameters(param);

			for (int i = 0; null != dataParameter && i < dataParameter.Length; i++)
			{
				string dbType = String.Empty;
				dbType = mIniFile.GetInitPramas(mUsePoolName, "DBTYPE").ToLower();
				switch (dbType)
				{
					case "oracle":
						if (param[i].Name.IndexOf('@') == 0)
						{
							expr = expr.Replace(param[i].Name, ":" + param[i].Name.Substring(1));
							dataParameter[i].ParameterName = ":" + param[i].Name.Substring(1);
						}
						if (param[i].Name.IndexOf(':') == 0)
						{
							dataParameter[i].ParameterName = ":" + param[i].Name.Substring(1);
						}
						break;
					case "mysql":
						if (param[i].Name.IndexOf(':') == 0)
						{
							expr = expr.Replace(param[i].Name, "@" + param[i].Name.Substring(1));
							dataParameter[i].ParameterName = "@" + param[i].Name.Substring(1);
						}
						if (param[i].Name.IndexOf('@') == 0)
						{
							dataParameter[i].ParameterName = "@" + param[i].Name.Substring(1);
						}
						break;
				}
				cmd.Parameters.Add(dataParameter[i]);
			}
			cmd.CommandText = expr;
            cmd.Connection = mPool.GetConnection(mUsePoolName);
            cmd.CommandTimeout = 120;
			cmd.CommandType = CommandType.Text;
			isOK = true;
            return isOK;
        }

		private IDbCommand initCommand()
		{
			string dbType = String.Empty;
            dbType = mIniFile.GetInitPramas(mUsePoolName, "DBTYPE").ToLower();
			switch (dbType) {
			case "oracle":
				//return new OracleCommand();
			case "sqlserver":
				return new SqlCommand();
			case "mysql":
				return new MySqlCommand();
			default:
					//return new OracleCommand();
					return new MySqlCommand();
			}
		}

		private IDataAdapter initDataAdapter(IDbCommand cmd)
		{
			string dbType = String.Empty;
			dbType = mIniFile.GetInitPramas(mUsePoolName, "DBTYPE").ToLower();
			switch (dbType)
			{
				case "oracle":
					//return new OracleDataAdapter((OracleCommand)cmd);
				case "sqlserver":
					return new SqlDataAdapter((SqlCommand)cmd);
				case "mysql":
					return new MySqlDataAdapter((MySqlCommand)cmd);
				default:
					//return new OracleDataAdapter((OracleCommand)cmd);
					return new MySqlDataAdapter((MySqlCommand)cmd);
			}
		}

		private IDataParameter[] initParameters(List<ParamItem> param)
        { 
			string dbType = String.Empty;
            dbType = mIniFile.GetInitPramas(mUsePoolName, "DBTYPE").ToLower();
			IDataParameter[] dataParameters = null;

			switch (dbType) {
			//case "oracle":
			//	dataParameters = new OracleParameter[param.Count];
			//	break;
			case "sqlserver":
				dataParameters = new SqlParameter[param.Count];
				break;
			case "mysql":
				dataParameters = new MySqlParameter[param.Count];
				break;
			default:
				//dataParameters = new OracleParameter[param.Count];
				break;
			}

            for (int i = 0; i < param.Count; i ++)
            {
				dataParameters[i] = initParameter(param[i]);
            }
            return dataParameters;
        }

		private IDataParameter initParameter(ParamItem paramItem)
		{
			string dbType = String.Empty;
            dbType = mIniFile.GetInitPramas(mUsePoolName, "DBTYPE").ToLower();
			IDataParameter dataParameter = null;

			object obj = getDataValue(paramItem.DataType, paramItem.Val);
			switch (dbType) {
			case "oracle":
				//dataParameter = new OracleParameter(paramItem.Name, getOracleDataType(paramItem.DataType), paramItem.Length, initInOut(paramItem.Flag), true, 0, 0, paramItem.Name, DataRowVersion.Default, obj);
				break;
			case "sqlserver":
				dataParameter = new SqlParameter(paramItem.Name, getSqlServerDataType(paramItem.DataType), paramItem.Length, initInOut(paramItem.Flag), true, 0, 0, paramItem.Name, DataRowVersion.Default, obj);
				break;
			case "mysql":
				dataParameter = new MySqlParameter(paramItem.Name, getMySqlDataType(paramItem.DataType), paramItem.Length, initInOut(paramItem.Flag), true, 0, 0, paramItem.Name, DataRowVersion.Default, obj);
				break;
			default:
				//dataParameter = new OracleParameter(paramItem.Name, getOracleDataType(paramItem.DataType), initInOut(paramItem.Flag));
				break;
			}
			return dataParameter;
		}

		private SqlDbType getSqlServerDataType (DataType type)
		{
			SqlDbType dbType = SqlDbType.VarChar;
			switch (type) {
			case DataType.STRING:
				dbType = SqlDbType.VarChar;
				break;
			case DataType.CURSOR:
				dbType = SqlDbType.Xml;
				break;
			case DataType.DATE:
				dbType = SqlDbType.Date;
				break;
			case DataType.INT:
				dbType = SqlDbType.Int;
				break;
			case DataType.FLOAT:
				dbType = SqlDbType.Money;
				break;
			}
			return dbType;
		}

		private MySqlDbType getMySqlDataType(DataType type)
		{
			MySqlDbType dbType = MySqlDbType.VarChar;
			switch (type)
			{
				case DataType.STRING:
					dbType = MySqlDbType.VarChar;
					break;
				case DataType.CURSOR:
					dbType = MySqlDbType.Text;
					break;
				case DataType.DATE:
					dbType = MySqlDbType.Date;
					break;
				case DataType.DATETIME:
					dbType = MySqlDbType.DateTime;
					break;
				case DataType.INT:
					dbType = MySqlDbType.Int32;
					break;
				case DataType.FLOAT:
					dbType = MySqlDbType.Float;
					break;
			}
			return dbType;
		}
		
		private object getDataValue (DataType type, string val)
		{
			object dbVal = Convert.DBNull;
			if (null == val)
				return dbVal;

			switch (type) {
			case DataType.STRING:
				dbVal = val;
				break;
			case DataType.CURSOR:
				break;
			case DataType.DATE:
				dbVal = Convert.ToDateTime (val); 
				break;
			case DataType.INT:
				dbVal = Convert.ToDecimal (val); 
				break;
			case DataType.FLOAT:
				dbVal = Convert.ToDouble (val); 
				break;
			default:
				dbVal = Convert.ToString (val);
				break;
			}
			return dbVal;
		}

		private ParameterDirection initInOut(InOutFlag flag)
        {
            switch (flag)
            {
                case InOutFlag.IN:
                    return ParameterDirection.Input;
                case InOutFlag.OUT:
                    return ParameterDirection.Output;
                case InOutFlag.INOUT:
                    return ParameterDirection.InputOutput;
                case InOutFlag.RETURNVAL:
                    return ParameterDirection.ReturnValue;
                default:
                    return ParameterDirection.Input;
            }
        }

		/// <summary>
		/// 检查Sql表达式中参数顺序
		/// </summary>
		/// <param name='expr'>
		/// Sql 标达式
		/// </param>
		/// <param name='param'>
		/// Parameter.
		/// </param>
		private void validChecked(string expr, List<ParamItem> param)
        {
            if (String.IsNullOrEmpty(expr)) throw new Exception("数据库操作错误，Sql 表达式为空。");
            int beforColumnIndex = -1;
            for (int i = 0; i < param.Count; i++)
            {
                int current = expr.IndexOf(param[i].Name);
                if (current > -1 && current < beforColumnIndex)
                {
                    throw new Exception(String.Format("{0}的参数顺序不正确，请检查", param[i].Name));
                }
                beforColumnIndex = current;
            }
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }
    }
}