using System;
using Newtonsoft.Json;
namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            int _x = 0;
            int y = 0;
            int z = 0;
            float f = 0f;
            double d = 0d;
            decimal de = 0;
            long l = 0;
            char c = 'a';
            string s = "Hello World";

            int[] ints = new int[10];

            ints[3] = 10 + Convert.ToInt32("10000");
            ints[4] = 19;

            for (int i = 0; i < ints.Length; i ++)
            {
                Console.WriteLine(i + " = " + ints[i]);
            }
            Console.WriteLine("Hello World!");
            _x = 1+1;
            _x = _x > 0 ? y: z;
            if (y != z)
            {

            }
            /*
            for (int i = 1; i < 10; i ++)
            {
                Console.WriteLine(i);
            }
            */
            string a = "gfahkghfakghkjfdahgkfdagfhdakjghfdakjhgda";
            //下面是一个demo，99乘法表

            for (int i = 1; i < 10; i ++)
            {
                for (int j = 0; j < i; j ++)
                {
                    Console.Write(String.Format("{0} * {1} = {2} ", j+1 , i, i*(j+1)));
                }
                Console.WriteLine();
            }
            /*
            object obj = HelperProvider.GetToken(27);
            
            string result = JsonConvert.SerializeObject(obj);

            Console.WriteLine("the result is :" + result);

             */
        }
    }
}
