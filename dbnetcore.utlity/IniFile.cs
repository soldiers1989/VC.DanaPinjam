using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;

namespace DBMonoUtility
{
	public class IniFile
	{
		private static Hashtable mTable = new Hashtable(100);
		private string filePath = String.Empty;
		private static bool mInit = false;
		public string FilePath
		{
			set
			{
				filePath = value;
			}
		}

		public IniFile()
		{
			if (!mInit)
			{
				mInit = true;
				Init();
			}
		}

        public IniFile(string path)
        {
            filePath = path;
            if (!mInit)
            {
                mInit = true;
                Init();
            }
        }

		public void Init()
		{
			mTable.Clear();
			FileStream fs = null;
			StreamReader sr = null;
			StreamWriter sw = null;
			try
			{
				if (String.IsNullOrEmpty(filePath))
					filePath = System.IO.Path.GetPathRoot(Directory.GetCurrentDirectory());

				string iniPath = Path.Combine(filePath, "dbinit.ini");

				if (!File.Exists(iniPath))
				{
					fs = new FileStream(iniPath, FileMode.Create, FileAccess.Write, FileShare.None);
					sw = new StreamWriter(fs);
					sw.WriteLine(@"
######################################################################################
# Key											Value
# NAME 是配置连接名
# DBTYPE 用于扩展（Oracle,MySql,SqlServer,Memcached）
# DBCONNECTIONSTRING 加密后的数据库连接字符串
# INITVECTOR 用于解密连接字符串的向量
######################################################################################
# 以下是示例
# NAME								|Pomoho
# DBTYPE							|Oracle
# DBCONNECTIONSTRING				|50XyjQtlscgsXR+4e+v4i1bfiyCROe54Je+3pOUe4ArlGVwKPMiuVc/Tiu0DboPdEGmMO2GO3YIdvHNHT/2t2vVtjmDLD4N6tUTplVXyVktt3z6QJ/mOLi37lj/Qllz+Xk9xk2+AeGYBnqeU3pK6vwn+ICRMkN685f2mQTAWJlNAcsychP1Fu2rseLFRXkgEvcMiu1b3SWXA9EK6VLTRuz3S5Zx0T5KKIO9RW2AE+8o=
# INITVECTOR 						|jUMcu471RB0=");
					sw.Flush();
				}
				else
				{
					fs = new FileStream(iniPath, FileMode.Open, FileAccess.Read, FileShare.Read);
					sr = new StreamReader(fs);
					string line = String.Empty;
					while (null != (line = sr.ReadLine()))
					{
						if (line.Trim().ToLower().IndexOf("name") != 0)
						{
							continue;
						}
						string[] kvs = line.Split('|');
						if (kvs.Length != 2) throw new Exception("Config is Error, File is :" + filePath);
						string appName = kvs[1].Trim().ToLower();
						while (!String.IsNullOrEmpty(line = sr.ReadLine()))
						{
							if (line.Trim().IndexOf("#") != 0 && !String.IsNullOrEmpty(line))
							{
								kvs = line.Split('|');
								if (kvs.Length == 2)
								{
									mTable.Add(appName + kvs[0].Trim().ToLower(), kvs[1].Trim());
								}
								else
								{
									mTable.Add(appName + kvs[0].Trim().ToLower(), true);
								}
							}
						}
					}
				}
			}
			catch (FileNotFoundException fnfe)
			{
				throw fnfe;
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (IOException ioex)
			{
				throw ioex;
			}
			catch (Exception ex)
			{
				throw ex;
			}
			finally
			{
				if (null != fs)
				{
					fs.Close();
					fs = null;
				}
				if (null != sr)
				{
					sr.Close();
					sr = null;
				}
			}
		}

		public string GetInitPramas(string appName, string key)
		{
			if (mTable.Count == 0)
			{
				return String.Empty;
			}
			return Convert.ToString(mTable[appName.Trim().ToLower() + key.Trim().ToLower()]);
		}
	}
}
