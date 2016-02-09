using System.Collections.Generic;

namespace TechTalk.JiraRestClient
{
    public class IssueMeta
    {
        public string expand { get; set; }

        public List<Project> projects { get; set; }
    }
}