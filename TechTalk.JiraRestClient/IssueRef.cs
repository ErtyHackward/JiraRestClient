using System;

namespace TechTalk.JiraRestClient
{
    public class IssueRef
    {
        public int id { get; set; }
        public string key { get; set; }

        internal string JiraIdentifier
        {
            get { return id == 0 ? key : id.ToString(); }
        }

        public static implicit operator IssueRef(string issue)
        {
            int id;
            if (int.TryParse(issue, out id))
            {
                return new IssueRef() { id = id };
            }
            return new IssueRef() { key = issue };
        }
    }
}
