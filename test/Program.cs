using System;
using Newtonsoft.Json;
namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            object obj = HelperProvider.GetToken(27);
            
            string result = JsonConvert.SerializeObject(obj);

            Console.WriteLine("the result is :" + result);
        }
    }
}
