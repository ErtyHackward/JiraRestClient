using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TechTalk.JiraRestClient
{
    public class JiraClient : JiraClient<IssueFields, Issue>
    {
        public JiraClient(string baseUrl, string username, string password, int timeout = 10000) : 
            base(baseUrl, username, password, timeout)
        {

        }
    }

    public class Issue : Issue<IssueFields>
    {

    }
}
