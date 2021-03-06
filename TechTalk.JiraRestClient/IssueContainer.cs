﻿using System;
using System.Collections.Generic;

namespace TechTalk.JiraRestClient
{
    internal class IssueContainer<TIssueFields, TIssue>
        where TIssueFields : IssueFields, new()
        where TIssue : Issue<TIssueFields>, new()
    {
        public string expand { get; set; }

        public int maxResults { get; set; }
        public int total { get; set; }
        public int startAt { get; set; }

        public List<TIssue> issues { get; set; }
    }
}
