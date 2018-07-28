using System;
using System.Collections;
using System.Collections.Generic;

namespace DBMonoUtility
{
	public class ParamCollections
    {
        private List<ParamItem> paramItems = new List<ParamItem>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="val"></param>
		public void Add(string name, object val)
		{
			paramItems.Add(new ParamItem(name, Convert.ToString(val), DataType.STRING, InOutFlag.IN, 1024));
		}

        /// <summary>
        /// 新增参数（默认为字符型，输入参数，长度为1024）
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void Add(string name, string val)
        {
            paramItems.Add(new ParamItem(name, val, DataType.STRING, InOutFlag.IN, 1024));
        }

        /// <summary>
        /// 新增参数（默认输入参数，长度为1024）
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <param name="dataType"></param>
        public void Add(string name, string val, DataType dataType)
        {
            paramItems.Add(new ParamItem(name, val, dataType, InOutFlag.IN, 1024));
        }

		public void Add(string name, object val, DataType dataType)
		{
			paramItems.Add(new ParamItem(name, Convert.ToString(val), dataType, InOutFlag.IN, 1024));
		}

        /// <summary>
        /// 新增参数（默认长度为1024）
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <param name="dataType"></param>
        /// <param name="flag"></param>
        public void Add(string name, string val, DataType dataType, InOutFlag flag)
        {
            paramItems.Add(new ParamItem(name, val, dataType, flag, 1024));
        }

        /// <summary>
        /// 新增参数，指定各项参数类型
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <param name="dataType"></param>
        /// <param name="flag"></param>
        /// <param name="len"></param>
        public void Add(string name, string val, DataType dataType, InOutFlag flag, int len)
        {
            paramItems.Add(new ParamItem(name, val, dataType, flag, len));
        }

        /// <summary>
        /// 获取参数列表
        /// </summary>
        /// <returns></returns>
        public List<ParamItem> GetParams()
        {
            return GetParams(false);
        }

        /// <summary>
        /// 获取参数列表
        /// </summary>
        /// <param name="isClear">是否清空参数列表</param>
        /// <returns></returns>
        public List<ParamItem> GetParams(bool isClear)
        {
            List<ParamItem> tempParamItems = new List<ParamItem>();
            for (int i = 0; i < paramItems.Count; i++)
            {
                tempParamItems.Add(paramItems[i]);
            }
            if (isClear)
            {
                paramItems.Clear();
            }
            return tempParamItems;
        }
    }
}

