using System.Collections.Generic;

namespace TechTalk.JiraRestClient
{
    public class Project
    {
        public string self { get; set; }
        public string key { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public List<IssueType> issuetypes { get; set; }

        public override string ToString()
        {
            return key;
        }
    }
}