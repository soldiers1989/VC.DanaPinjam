using System;

namespace DBMonoUtility
{
	[Serializable]
	public class ParamItem
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string val = String.Empty;
        public string Val
        {
            get { return val; }
            set { val = value; }
        }

        private DataType dataType = DataType.STRING;
        public DataType DataType
        {
            get { return dataType; }
            set { dataType = value; }
        }

        private InOutFlag flag = InOutFlag.IN;
        public InOutFlag Flag
        {
            get { return flag; }
            set { flag = value; }
        }

        private int len = 1024;
        public int Length
        {
            get { return len; }
            set { len = value; }
        }

        public ParamItem()
        {
        }

        public ParamItem(string name, string val)
        {
            this.name = name;
            this.val = val;
        }

        public ParamItem(string name, string val, DataType dataType)
        {
            this.name = name;
            this.val = val;
            this.dataType = dataType;
        }

        public ParamItem(string name, string val, DataType dataType, InOutFlag flag)
        {
            this.name = name;
            this.val = val;
            this.dataType = dataType;
            this.flag = flag;
        }

        public ParamItem(string name, string val, DataType dataType, InOutFlag flag, int len)
        {
            this.name = name;
            this.val = val;
            this.dataType = dataType;
            this.flag = flag;
            this.len = len;
        }
    }
}

