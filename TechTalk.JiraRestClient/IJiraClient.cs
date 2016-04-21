using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TechTalk.JiraRestClient
{
    public interface IJiraClient<TIssueFields, TIssue> : IDisposable 
        where TIssueFields : IssueFields, new()
        where TIssue : Issue<TIssueFields>, new()
    {
        CookieContainer SessionContainer { get; }

        /// <summary>Returns all issues for the given project</summary>
        IEnumerable<TIssue> GetIssues(String projectKey);
        /// <summary>Returns all issues of the specified type for the given project</summary>
        IEnumerable<TIssue> GetIssues(String projectKey, String issueType);
        /// <summary>Enumerates through all issues for the given project</summary>
        IEnumerable<TIssue> EnumerateIssues(String projectKey);
        /// <summary>Enumerates through all issues of the specified type for the given project</summary>
        IEnumerable<TIssue> EnumerateIssues(String projectKey, String issueType);
        /// <summary>Enumerates through all issues filtered by the specified jqlQuery starting form the specified startIndex</summary>
        IEnumerable<TIssue> EnumerateIssuesByQuery(String jqlQuery, String[] fields, Int32 startIndex);
        /// <summary>Returns a query provider for this JIRA connection</summary>
        IQueryable<TIssue> QueryIssues();

        /// <summary>Returns all issues of the given type and the given project filtered by the given JQL query</summary>
        [Obsolete("This method is no longer supported and might be removed in a later release. Use EnumerateIssuesByQuery(jqlQuery, fields, startIndex).ToArray() instead")]
        IEnumerable<TIssue> GetIssuesByQuery(String projectKey, String issueType, String jqlQuery);
        /// <summary>Enumerates through all issues of the specified type for the given project, returning the given issue fields</summary>
        [Obsolete("This method is no longer supported and might be removed in a later release. Use EnumerateIssuesByQuery(jqlQuery, fields, startIndex) instead")]
        IEnumerable<TIssue> EnumerateIssues(String projectKey, String issueType, String fields);

        /// <summary>Returns the issue identified by the given ref</summary>
        Task<TIssue> LoadIssueAsync(String issueRef);
        /// <summary>Returns the issue identified by the given ref</summary>
        Task<TIssue> LoadIssueAsync(IssueRef issueRef);
        /// <summary>Creates an issue of the specified type for the given project</summary>
        Task<TIssue> CreateIssueAsync(String projectKey, IssueType issueType, String summary);
        /// <summary>Creates an issue of the specified type for the given project</summary>
        Task<TIssue> CreateIssueAsync(String projectKey, IssueType issueType, TIssueFields issueFields);
        /// <summary>Updates the given issue on the remote system</summary>
        Task<TIssue> UpdateIssueAsync(TIssue issue);
        /// <summary>Deletes the given issue from the remote system</summary>
        void DeleteIssue(IssueRef issue);

        /// <summary>Returns all transitions avilable to the given issue</summary>
        IEnumerable<Transition> GetTransitions(IssueRef issue);
        /// <summary>Changes the state of the given issue as described by the transition</summary>
        Task<TIssue> TransitionIssueAsync(IssueRef issue, Transition transition);

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

        Task<IEnumerable<Worklog>> GetWorklogsAsync(IssueRef issue);
        Task<IEnumerable<Project>> GetProjectsAsync();
        Task<IssueMeta> GetCreateIssueMetaAsync(string projectKey);
        Task<JiraUser> GetUserAsync(string name);

        void ImportSessionAsync(CookieContainer container);

        Task EstablishSessionAsync();

        Task<Session> GetSessionAsync();
        Task<List<JiraUser>> FindUsersAsync(string userName, int startAt = 0, int maxResults = 50);
    }
}
