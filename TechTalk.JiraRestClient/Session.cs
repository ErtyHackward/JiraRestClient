using System;

namespace TechTalk.JiraRestClient
{
    public class Session
    {
        public string self { get; set; }
        public string name { get; set; }

        public LoginInfo loginInfo { get; set; }
    }

    public class LoginInfo
    {
        public int failedLoginCount { get; set; }
        public int loginCount { get; set; }
        public DateTime lastFailedLoginTime { get; set; }
        public DateTime previousLoginTime { get; set; }
    }
}