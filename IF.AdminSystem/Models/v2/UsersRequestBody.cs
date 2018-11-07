using System;

namespace NF.AdminSystem.Models.v2
{
    public class VerificateRequestBody
    {
        public string phone { get; set; }
    }

    public class RegisterRequestBody
    {
        public string phone { get; set; }

        public string password { get; set; }

        public string code { get; set; }
    }

    public class UserRegistrationRequestBody
    {
        public string userId { get; set; }
        public string registrationId { get; set; }
    }

    public class UserLoginRequestBody
    {
        public string phone { get; set; }

        public string password { get; set; }

        public string code { get; set; }

        public int loginType { get; set; }
    }

    public class UserInfoRequestBody
    {
        public int userId { get; set; }
    }

    public class UserPhotosRequestBody
    {
        public int userId { get; set; }

        public int type { get; set; }

        public string url { get; set; }
    }


}