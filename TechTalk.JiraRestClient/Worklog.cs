using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TechTalk.JiraRestClient
{
    public class Worklog
    {
        public string self { get; set; }

        public JiraUser author { get; set; }

        public string comment { get; set; }

        public string timeSpent { get; set; }

        public string timeSpentSeconds { get; set; }

        public int id { get; set; }
    }

    internal class PagingContainer
    {
        public int startAt { get; set; }
        public int maxResults { get; set; }
        public int total { get; set; }
    }

    internal class WorklogContainer : PagingContainer
    {
        public List<Worklog> worklogs { get; set; }
    }
}
