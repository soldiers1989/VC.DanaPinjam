using System;
using Newtonsoft.Json;
namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //object obj = HelperProvider.GetToken(27);
            
            //string result = JsonConvert.SerializeObject(obj);

            //Console.WriteLine("the result is :" + result);

//$paramSignature = $email . $timestamp . $bankCode . $bankAccount . $amountTransfer . $purpose . $key; 
//$signature = hash('sha256', $paramSignature);

/*
{'userId':3551,
'amountTransfer':1,
'bankAccount':'1680001297876',
'bankCode':'008',
'email':'test@chakratechnology.com',
'purpose':'test',
'timestamp':1,
'senderId':27,
'senderName':'f1',
'signature':''
} */

            /*
            string email = "test@chakratechnology.com";
            string timestamp = Convert.ToString((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000);
            string bankCode = "008";
            string bankAccount = "001001001";
            string amountTransfer = "10001";
            string purpose = "test";
            string key = "de56f832487bc1ce1de5ff2cfacf8d9486c61da69df6fd61d5537b6b7d6d354d";

            string paramSignature = email + timestamp + bankCode+bankAccount+amountTransfer+purpose+key;
            string signature = HelperProvider.SHA256(paramSignature);
            Console.WriteLine("signature:");

            Console.WriteLine(signature);

            Console.WriteLine("timestamp:");

            Console.WriteLine(timestamp);

            */
            LoanBank bank = new LoanBank();

            DebitUserRecord record = new DebitUserRecord();
            record.debitId = 111;
            record.bankAccount = "1680001297876";
            record.bankCode = "008";
            record.amountTransfer = 10000;
            record.purpose = "test";
            record.userId = 27;
            record.userName = "HENDRA";

            //bank.Transfer(record);


            //bank.CheckTransferStatus("10013");
            //$paramSignature = $email . $timestamp . $bankCode . $bankAccount . $accountName . $custRefNumber . $amountTransfer . $purpose . $disburseId . $secretKey; 
        }
    }
}
