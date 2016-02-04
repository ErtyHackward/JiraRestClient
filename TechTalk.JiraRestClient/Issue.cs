using System;
using System.Collections.Generic;

namespace TechTalk.JiraRestClient
{
    public class Issue<TIssueFields> : IssueRef where TIssueFields : IssueFields, new()
    {
        public Issue() { fields = new TIssueFields(); }

        public string expand { get; set; }

        public string self { get; set; }

        public TIssueFields fields { get; set; }

        internal static void ExpandLinks<T>(Issue<T> issue) where T : IssueFields, new()
        {
            foreach (var link in issue.fields.issuelinks)
            {
                if (link.inwardIssue.id == 0)
                {
                    link.inwardIssue.id = issue.id;
                    link.inwardIssue.key = issue.key;
                }
                if (link.outwardIssue.id == 0)
                {
                    link.outwardIssue.id = issue.id;
                    link.outwardIssue.key = issue.key;
                }
            }
        }
    }
}
