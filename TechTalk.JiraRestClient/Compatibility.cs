﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TechTalk.JiraRestClient
{
    public interface IJiraClient : IDisposable
    {
        CookieContainer SessionContainer { get; }
        /// <summary>Returns all issues for the given project</summary>
        IEnumerable<Issue> GetIssues(String projectKey);
        /// <summary>Returns all issues of the specified type for the given project</summary>
        IEnumerable<Issue> GetIssues(String projectKey, String issueType);
        /// <summary>Enumerates through all issues for the given project</summary>
        IEnumerable<Issue> EnumerateIssues(String projectKey);
        /// <summary>Enumerates through all issues of the specified type for the given project</summary>
        IEnumerable<Issue> EnumerateIssues(String projectKey, String issueType);

        /// <summary>Returns all issues of the given type and the given project filtered by the given JQL query</summary>
        [Obsolete("This method is no longer supported and might be removed in a later release.")]
        IEnumerable<Issue> GetIssuesByQuery(String projectKey, String issueType, String jqlQuery);
        /// <summary>Enumerates through all issues of the specified type for the given project, returning the given issue fields</summary>
        [Obsolete("This method is no longer supported and might be removed in a later release.")]
        IEnumerable<Issue> EnumerateIssues(String projectKey, String issueType, String fields);

        /// <summary>Returns the issue identified by the given ref</summary>
        Task<Issue> LoadIssueAsync(String issueRef);
        /// <summary>Returns the issue identified by the given ref</summary>
        Task<Issue> LoadIssueAsync(IssueRef issueRef);
        /// <summary>Creates an issue of the specified type for the given project</summary>
        Task<Issue> CreateIssueAsync(String projectKey, IssueType issueType, String summary);
        /// <summary>Creates an issue of the specified type for the given project</summary>
        Task<Issue> CreateIssueAsync(String projectKey, IssueType issueType, IssueFields issueFields);
        /// <summary>Updates the given issue on the remote system</summary>
        Task<Issue> UpdateIssueAsync(Issue issue);
        /// <summary>Deletes the given issue from the remote system</summary>
        void DeleteIssue(IssueRef issue);

        /// <summary>Returns all transitions available to the given issue</summary>
        IEnumerable<Transition> GetTransitions(IssueRef issue);
        /// <summary>Changes the state of the given issue as described by the transition</summary>
        Task<Issue> TransitionIssueAsync(IssueRef issue, Transition transition);

        /// <summary>Returns all watchers for the given issue</summary>
        IEnumerable<JiraUser> GetWatchers(IssueRef issue);

        /// <summary>Returns all comments for the given issue</summary>
        IEnumerable<Comment> GetComments(IssueRef issue);
        /// <summary>Adds a comment to the given issue</summary>
        Comment CreateComment(IssueRef issue, String comment);
        /// <summary>Deletes the given comment</summary>
        void DeleteComment(IssueRef issue, Comment comment);

        /// <summary>Return all attachments for the given issue</summary>
        Task<IEnumerable<Attachment>> GetAttachmentsAsync(IssueRef issue);
        /// <summary>Creates an attachment to the given issue</summary>
        Attachment CreateAttachment(IssueRef issue, Stream stream, String fileName);
        /// <summary>Deletes the given attachment</summary>
        void DeleteAttachment(Attachment attachment);

        /// <summary>Returns all links for the given issue</summary>
        Task<IEnumerable<IssueLink>> GetIssueLinksAsync(IssueRef issue);
        /// <summary>Returns the link between two issues of the given relation</summary>
        Task<IssueLink> LoadIssueLinkAsync(IssueRef parent, IssueRef child, String relationship);
        /// <summary>Creates a link between two issues with the given relation</summary>
        Task<IssueLink> CreateIssueLinkAsync(IssueRef parent, IssueRef child, String relationship);
        /// <summary>Removes the given link of two issues</summary>
        void DeleteIssueLink(IssueLink link);

        /// <summary>Returns all remote links (attached urls) for the given issue</summary>
        IEnumerable<RemoteLink> GetRemoteLinks(IssueRef issue);
        /// <summary>Creates a remote link (attached url) for the given issue</summary>
        RemoteLink CreateRemoteLink(IssueRef issue, RemoteLink remoteLink);
        /// <summary>Updates the given remote link (attached url) of the specified issue</summary>
        RemoteLink UpdateRemoteLink(IssueRef issue, RemoteLink remoteLink);
        /// <summary>Removes the given remote link (attached url) of the specified issue</summary>
        void DeleteRemoteLink(IssueRef issue, RemoteLink remoteLink);

        /// <summary>Returns all issue types</summary>
        IEnumerable<IssueType> GetIssueTypes();

        /// <summary>Returns information about the JIRA server</summary>
        Task<ServerInfo> GetServerInfoAsync();

        Task<bool> ImportSessionAsync(CookieContainer container);
    }

    public class JiraClient : IJiraClient
    {
        public CookieContainer SessionContainer => client.SessionContainer;

        private readonly IJiraClient<IssueFields> client;
        public JiraClient(string baseUrl, string username, string password, int timeout = 10000)
        {
            client = new JiraClient<IssueFields>(baseUrl, username, password, timeout);
        }

        public IEnumerable<Issue> GetIssues(String projectKey)
        {
            return client.GetIssues(projectKey).Select(Issue.From).ToArray();
        }

        public IEnumerable<Issue> GetIssues(String projectKey, String issueType)
        {
            return client.GetIssues(projectKey, issueType).Select(Issue.From).ToArray();
        }

        public IEnumerable<Issue> EnumerateIssues(String projectKey)
        {
            return client.EnumerateIssues(projectKey).Select(Issue.From);
        }

        public IEnumerable<Issue> EnumerateIssues(String projectKey, String issueType)
        {
            return client.EnumerateIssues(projectKey, issueType).Select(Issue.From);
        }

        [Obsolete("This method is no longer supported and might be removed in a later release.")]
        public IEnumerable<Issue> GetIssuesByQuery(string projectKey, string issueType, string jqlQuery)
        {
            return client.GetIssuesByQuery(projectKey, issueType, jqlQuery).Select(Issue.From).ToArray();
        }

        [Obsolete("This method is no longer supported and might be removed in a later release.")]
        public IEnumerable<Issue> EnumerateIssues(string projectKey, string issueType, string fields)
        {
            return client.EnumerateIssues(projectKey, issueType, fields).Select(Issue.From);
        }

        public async Task<Issue> LoadIssueAsync(String issueRef)
        {
            return Issue.From(await client.LoadIssueAsync(issueRef));
        }

        public async Task<Issue> LoadIssueAsync(IssueRef issueRef)
        {
            return Issue.From(await client.LoadIssueAsync(issueRef));
        }

        public async Task<Issue> CreateIssueAsync(String projectKey, IssueType issueType, String summary)
        {
            return Issue.From(await client.CreateIssueAsync(projectKey, issueType, summary));
        }

        public async Task<Issue> CreateIssueAsync(String projectKey, IssueType issueType, IssueFields issueFields)
        {
            return Issue.From(await client.CreateIssueAsync(projectKey, issueType, issueFields));
        }

        public async Task<Issue> UpdateIssueAsync(Issue issue)
        {
            return Issue.From(await client.UpdateIssueAsync(issue));
        }

        public void DeleteIssue(IssueRef issue)
        {
            client.DeleteIssue(issue);
        }

        public IEnumerable<Transition> GetTransitions(IssueRef issue)
        {
            return client.GetTransitions(issue);
        }

        public async Task<Issue> TransitionIssueAsync(IssueRef issue, Transition transition)
        {
            return Issue.From(await client.TransitionIssueAsync(issue, transition));
        }

        public IEnumerable<JiraUser> GetWatchers(IssueRef issue)
        {
            return client.GetWatchers(issue);
        }

        public IEnumerable<Comment> GetComments(IssueRef issue)
        {
            return client.GetComments(issue);
        }

        public Comment CreateComment(IssueRef issue, string comment)
        {
            return client.CreateComment(issue, comment);
        }

        public void DeleteComment(IssueRef issue, Comment comment)
        {
            client.DeleteComment(issue, comment);
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentsAsync(IssueRef issue)
        {
            return await client.GetAttachmentsAsync(issue);
        }

        public Attachment CreateAttachment(IssueRef issue, Stream stream, string fileName)
        {
            return client.CreateAttachment(issue, stream, fileName);
        }

        public void DeleteAttachment(Attachment attachment)
        {
            client.DeleteAttachment(attachment);
        }

        public Task<IEnumerable<IssueLink>> GetIssueLinksAsync(IssueRef issue)
        {
            return client.GetIssueLinksAsync(issue);
        }

        public Task<IssueLink> LoadIssueLinkAsync(IssueRef parent, IssueRef child, string relationship)
        {
            return client.LoadIssueLinkAsync(parent, child, relationship);
        }

        public Task<IssueLink> CreateIssueLinkAsync(IssueRef parent, IssueRef child, string relationship)
        {
            return client.CreateIssueLinkAsync(parent, child, relationship);
        }

        public void DeleteIssueLink(IssueLink link)
        {
            client.DeleteIssueLink(link);
        }

        public IEnumerable<RemoteLink> GetRemoteLinks(IssueRef issue)
        {
            return client.GetRemoteLinks(issue);
        }

        public RemoteLink CreateRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            return client.CreateRemoteLink(issue, remoteLink);
        }

        public RemoteLink UpdateRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            return client.UpdateRemoteLink(issue, remoteLink);
        }

        public void DeleteRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            client.DeleteRemoteLink(issue, remoteLink);
        }

        public IEnumerable<IssueType> GetIssueTypes()
        {
            return client.GetIssueTypes();
        }

        public Task<ServerInfo> GetServerInfoAsync()
        {
            return client.GetServerInfoAsync();
        }

        public IEnumerable<Worklog> GetWorklogs(IssueRef issue)
        {
            return client.GetWorklogs(issue);
        }

        public Task<IEnumerable<Project>> GetProjectsAsync()
        {
            return client.GetProjectsAsync();
        }

        public Task<IssueMeta> GetCreateIssueMetaAsync(string projectKey)
        {
            return client.GetCreateIssueMetaAsync(projectKey);
        }

        public Task<JiraUser> GetUserAsync(string name)
        {
            return client.GetUserAsync(name);
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public Task<bool> ImportSessionAsync(CookieContainer sessionContainer)
        {
            return client.ImportSessionAsync(sessionContainer);
        }
    }

    public class Issue : Issue<IssueFields>
    {
        internal static Issue From(Issue<IssueFields> other)
        {
            if (other == null)
                return null;

            return new Issue
            {
                expand = other.expand,
                id = other.id,
                key = other.key,
                self = other.self,
                fields = other.fields,
            };
        }
    }
}
