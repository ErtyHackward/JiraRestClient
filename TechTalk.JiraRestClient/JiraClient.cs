using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Deserializers;

namespace TechTalk.JiraRestClient
{
    //JIRA REST API documentation: https://docs.atlassian.com/jira/REST/latest

    public class JiraClient<TIssueFields, TIssue> : 
        IJiraClient<TIssueFields, TIssue> 
        where TIssueFields : IssueFields, new() 
        where TIssue : Issue<TIssueFields>, new()
    {
        private readonly string _baseUrl;
        private readonly string username;
        private readonly string password;
        private readonly JsonDeserializer deserializer;
        private readonly string baseApiUrl;
        private readonly RestClient restClient;
        private readonly int _timeout;

        /// <summary>
        /// Occurs when request is finished
        /// </summary>
        public event EventHandler<RequestEventArgs> RequestComplete;
        
        protected virtual void OnRequest(RequestEventArgs e)
        {
            RequestComplete?.Invoke(this, e);
        }

        public CookieContainer SessionContainer => restClient.CookieContainer;

        public JiraClient(string baseUrl, string username, string password, int timeout = 10000)
        {
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";
            _baseUrl = baseUrl;
            this.username = username;
            this.password = password;
            _timeout = timeout;

            baseApiUrl = new Uri(new Uri(baseUrl), "rest/api/2/").ToString();
            deserializer = new JsonDeserializer();

            restClient = new RestClient(baseApiUrl);
        }

        public void ImportSessionAsync(CookieContainer container)
        {
            if (restClient.CookieContainer != null)
                throw new InvalidOperationException("Session was already established");

            restClient.CookieContainer = container;
        }

        public void EstablishSession()
        {
            var request = (HttpWebRequest)WebRequest.Create(_baseUrl + "rest/auth/1/session/");

            var postData = string.Format("{{ \"username\": \"{0}\", \"password\": \"{1}\" }}", username, password);

            var data = Encoding.UTF8.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = data.Length;
            request.CookieContainer = new CookieContainer();
            request.Timeout = _timeout;
            request.ServicePoint.CloseConnectionGroup(null);

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var resp = request.GetResponse();
            restClient.CookieContainer = request.CookieContainer;
        }

        public async Task EstablishSessionAsync()
        {
            var request = (HttpWebRequest)WebRequest.Create(_baseUrl + "rest/auth/1/session/");

            var postData = string.Format("{{ \"username\": \"{0}\", \"password\": \"{1}\" }}", username, password);

            var data = Encoding.UTF8.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = data.Length;
            request.CookieContainer = new CookieContainer();
            request.Timeout = _timeout;
            request.ServicePoint.CloseConnectionGroup(null);


            using (var stream = await Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null))
            {
                stream.Write(data, 0, data.Length);
            }

            var resp = await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            restClient.CookieContainer = request.CookieContainer;
        }

        public void Dispose()
        {
            //if (restClient.CookieContainer != null)
            //{
            //    var request = (HttpWebRequest)WebRequest.Create(_baseUrl + "rest/auth/1/session");

            //    request.Method = "DELETE";
            //    request.CookieContainer = restClient.CookieContainer;

            //    var response = request.GetResponse();
            //    restClient.CookieContainer = null;
            //}
        }

        private RestRequest CreateRequest(Method method, String path)
        {
            var request = new RestRequest { Method = method, Resource = path, RequestFormat = DataFormat.Json, Timeout = _timeout };
            return request;
        }

        private IRestResponse ExecuteRequest(RestRequest request)
        {
            if (restClient.CookieContainer == null)
                EstablishSession();

            var response = restClient.Execute(request);

            OnRequest(new RequestEventArgs
            {
                Uri = restClient.BuildUri(request),
                Request = request,
                Response = response
            });
            
            return response;
        }

        private async Task<IRestResponse> ExecuteRequestAsync(RestRequest request)
        {
            if (restClient.CookieContainer == null)
                await EstablishSessionAsync();

            var response = await restClient.ExecuteTaskAsync(request);
            
            OnRequest(new RequestEventArgs {
                Uri = restClient.BuildUri(request),
                Request = request,
                Response = response
            } );

            return response;
        }

        private void AssertStatus(IRestResponse response, HttpStatusCode status)
        {
            if (response.ErrorException != null)
                throw new JiraClientException("Transport level error: " + response.ErrorMessage, response.ErrorException);
            if (response.StatusCode != status)
                throw new JiraClientException("JIRA returned wrong status: " + response.StatusDescription, response.Content, response.StatusCode);
        }


        public IEnumerable<TIssue> GetIssues(String projectKey)
        {
            return EnumerateIssues(projectKey, null).ToArray();
        }

        public IEnumerable<TIssue> GetIssues(String projectKey, String issueType)
        {
            return EnumerateIssues(projectKey, issueType).ToArray();
        }

        public IEnumerable<TIssue> EnumerateIssues(String projectKey)
        {
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, null), null, 0);
        }

        public IEnumerable<TIssue> EnumerateIssues(String projectKey, String issueType)
        {
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, issueType), null, 0);
        }

        private static string CreateCommonJql(String projectKey, String issueType)
        {
            var queryParts = new List<String>();
            if (!String.IsNullOrEmpty(projectKey))
                queryParts.Add(String.Format("project={0}", projectKey));
            if (!String.IsNullOrEmpty(issueType))
                queryParts.Add(String.Format("issueType={0}", issueType));
            return String.Join(" AND ", queryParts);
        }

        [Obsolete("This method is no longer supported and might be removed in a later release. Use EnumerateIssuesByQuery(jqlQuery, fields, startIndex).ToArray() instead")]
        public IEnumerable<TIssue> GetIssuesByQuery(String projectKey, String issueType, String jqlQuery)
        {
            var jql = CreateCommonJql(projectKey, issueType);
            if (!String.IsNullOrEmpty(jql) && !String.IsNullOrEmpty(jqlQuery))
                jql += "+AND+";// if neither are empty, join them with an 'and'
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, issueType), null, 0).ToArray();
        }

        [Obsolete("This method is no longer supported and might be removed in a later release. Use EnumerateIssuesByQuery(jqlQuery, fields, startIndex) instead")]
        public IEnumerable<TIssue> EnumerateIssues(String projectKey, String issueType, String fields)
        {
            var fieldDef = fields == null ? null
                : fields.Split(',').Select(str => (str ?? "").Trim())
                    .Where(str => !string.IsNullOrEmpty(str)).ToArray();
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, issueType), fieldDef, 0);
        }

        public IEnumerable<TIssue> EnumerateIssuesByQuery(String jqlQuery, String[] fields, Int32 startIndex)
        {
            try
            {
                return EnumerateIssuesByQueryInternal(Uri.EscapeUriString(jqlQuery), fields, startIndex);
            }
            catch (Exception ex)
            {
                Trace.TraceError("EnumerateIssuesByQuery(jqlQuery, fields, startIndex) error: {0}", ex);
                throw new JiraClientException("Could not load issues", ex);
            }
        }

        private IEnumerable<TIssue> EnumerateIssuesByQueryInternal(String jqlQuery, String[] fields, Int32 startIndex)
        {
            var queryCount = 50;
            var resultCount = startIndex;
            while (true)
            {
                var path = String.Format("search?jql={0}&startAt={1}&maxResults={2}", jqlQuery, resultCount, queryCount);
                if (fields != null) path += String.Format("&fields={0}", String.Join(",", fields));

                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<IssueContainer<TIssueFields, TIssue>>(response);
                var issues = data.issues ?? Enumerable.Empty<TIssue>();

                foreach (var item in issues) yield return item;
                resultCount += issues.Count();

                if (resultCount < data.total) continue;
                else /* all issues received */ break;
            }
        }

        public IQueryable<TIssue> QueryIssues()
        {
            return new QueryableIssueCollection<TIssueFields, TIssue>(this);
        }


        public Task<TIssue> LoadIssueAsync(IssueRef issueRef)
        {
            return LoadIssueAsync(issueRef.JiraIdentifier);
        }

        public async Task<TIssue> LoadIssueAsync(String issueRef)
        {
            try
            {
                var path = String.Format("issue/{0}", issueRef);
                var request = CreateRequest(Method.GET, path);

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.OK);

                var issue = deserializer.Deserialize<TIssue>(response);
                issue.fields.comments = GetComments(issue).ToList();
                issue.fields.watchers = GetWatchers(issue).ToList();
                Issue.ExpandLinks(issue);
                return issue;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetIssue(issueRef) error: {0}", ex);
                throw new JiraClientException("Could not load issue", ex);
            }
        }

        public Task<TIssue> CreateIssueAsync(String projectKey, IssueType issueType, String summary)
        {
            return CreateIssueAsync(projectKey, issueType, new TIssueFields { summary = summary });
        }

        public async Task<TIssue> CreateIssueAsync(String projectKey, IssueType issueType, TIssueFields issueFields)
        {
            try
            {
                var request = CreateRequest(Method.POST, "issue");
                request.AddHeader("ContentType", "application/json");

                var issueData = new Dictionary<string, object>();
                issueData.Add("project", new { key = projectKey });
                issueData.Add("issuetype", new { issueType.id });

                if (issueFields.summary != null)
                    issueData.Add("summary", issueFields.summary);
                if (issueFields.description != null)
                    issueData.Add("description", issueFields.description);
                if (issueFields.labels != null && issueFields.labels.Count > 0)
                    issueData.Add("labels", issueFields.labels);
                if (issueFields.timetracking != null)
                    issueData.Add("timetracking", new { originalEstimate = TimeSpan.FromSeconds(issueFields.timetracking.originalEstimateSeconds).TotalMinutes + "m" });
                if (issueFields.assignee != null)
                    issueData.Add("assignee", new { issueFields.assignee.name });
                if (issueFields.parent != null)
                    issueData.Add("parent", new { issueFields.parent.key });
                if (issueFields.duedate != null)
                    issueData.Add("duedate", issueFields.duedate);


                var propertyList = typeof(TIssueFields).GetProperties().Where(p => p.Name.StartsWith("customfield_"));
                foreach (var property in propertyList)
                {
                    var value = property.GetValue(issueFields, null);
                    if (value != null) issueData.Add(property.Name, value);
                }

                request.AddBody(new { fields = issueData });

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.Created);

                var issueRef = deserializer.Deserialize<IssueRef>(response);
                return await LoadIssueAsync(issueRef);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateIssue(projectKey, typeCode) error: {0}", ex);
                throw new JiraClientException("Could not create issue", ex);
            }
        }

        public async Task<TIssue> UpdateIssueAsync(TIssue issue)
        {
            try
            {
                var path = String.Format("issue/{0}", issue.JiraIdentifier);
                var request = CreateRequest(Method.PUT, path);
                request.AddHeader("ContentType", "application/json");

                var updateData = new Dictionary<string, object>();
                if (issue.fields.summary != null)
                    updateData.Add("summary", new[] { new { set = issue.fields.summary } });
                if (issue.fields.description != null)
                    updateData.Add("description", new[] { new { set = issue.fields.description } });
                if (issue.fields.labels?.Count > 0)
                    updateData.Add("labels", new[] { new { set = issue.fields.labels } });
                if (issue.fields.timetracking != null)
                    updateData.Add("timetracking", new[] { new { set = new { originalEstimate = TimeSpan.FromSeconds(issue.fields.timetracking.originalEstimateSeconds).TotalMinutes + "m" } } });
                if (issue.fields.duedate != null)
                    updateData.Add("duedate", new[] { new { set = issue.fields.duedate.Value } });
                if (issue.fields.assignee != null)
                    updateData.Add("assignee", new[] { new { set = new { issue.fields.assignee.name } } });

                var propertyList = typeof(TIssueFields).GetProperties().Where(p => p.Name.StartsWith("customfield_"));
                foreach (var property in propertyList)
                {
                    var value = property.GetValue(issue.fields, null);
                    if (value != null) updateData.Add(property.Name, new[] { new { set = value } });
                }

                request.AddBody(new { update = updateData });

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.NoContent);

                return await LoadIssueAsync(issue);
            }
            catch (Exception ex)
            {
                Trace.TraceError("UpdateIssue(issue) error: {0}", ex);
                throw new JiraClientException("Could not update issue", ex);
            }
        }

        public void DeleteIssue(IssueRef issue)
        {
            try
            {
                var path = String.Format("issue/{0}?deleteSubtasks=true", issue.id);
                var request = CreateRequest(Method.DELETE, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteIssue(issue) error: {0}", ex);
                throw new JiraClientException("Could not delete issue", ex);
            }
        }


        public IEnumerable<Transition> GetTransitions(IssueRef issue)
        {
            try
            {
                var path = String.Format("issue/{0}/transitions?expand=transitions.fields", issue.id);
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<TransitionsContainer>(response);
                return data.transitions;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetTransitions(issue) error: {0}", ex);
                throw new JiraClientException("Could not load issue transitions", ex);
            }
        }

        public async Task<TIssue> TransitionIssueAsync(IssueRef issue, Transition transition)
        {
            try
            {
                var path = String.Format("issue/{0}/transitions", issue.id);
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("ContentType", "application/json");

                var update = new Dictionary<string, object>();
                update.Add("transition", new { id = transition.id });
                if (transition.fields != null)
                    update.Add("fields", transition.fields);

                request.AddBody(update);

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.NoContent);

                return await LoadIssueAsync(issue);
            }
            catch (Exception ex)
            {
                Trace.TraceError("TransitionIssue(issue, transition) error: {0}", ex);
                throw new JiraClientException("Could not transition issue state", ex);
            }
        }


        public IEnumerable<JiraUser> GetWatchers(IssueRef issue)
        {
            try
            {
                var path = String.Format("issue/{0}/watchers", issue.id);
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return deserializer.Deserialize<WatchersContainer>(response).watchers;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetWatchers(issue) error: {0}", ex);
                throw new JiraClientException("Could not load watchers", ex);
            }
        }


        public IEnumerable<Comment> GetComments(IssueRef issue)
        {
            try
            {
                var path = String.Format("issue/{0}/comment", issue.JiraIdentifier);
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<CommentsContainer>(response);
                return data.comments ?? Enumerable.Empty<Comment>();
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetComments(issue) error: {0}", ex);
                throw new JiraClientException("Could not load comments", ex);
            }
        }

        public Comment CreateComment(IssueRef issue, String comment)
        {
            try
            {
                var path = String.Format("issue/{0}/comment", issue.id);
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("ContentType", "application/json");
                request.AddBody(new Comment { body = comment });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                return deserializer.Deserialize<Comment>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateComment(issue, comment) error: {0}", ex);
                throw new JiraClientException("Could not create comment", ex);
            }
        }

        public void DeleteComment(IssueRef issue, Comment comment)
        {
            try
            {
                var path = String.Format("issue/{0}/comment/{1}", issue.id, comment.id);
                var request = CreateRequest(Method.DELETE, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteComment(issue, comment) error: {0}", ex);
                throw new JiraClientException("Could not delete comment", ex);
            }
        }


        public async Task<IEnumerable<Attachment>> GetAttachmentsAsync(IssueRef issue)
        {
            return (await LoadIssueAsync(issue)).fields.attachment;
        }

        public Attachment CreateAttachment(IssueRef issue, Stream fileStream, String fileName)
        {
            try
            {
                var path = String.Format("issue/{0}/attachments", issue.JiraIdentifier);
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("X-Atlassian-Token", "nocheck");
                request.AddHeader("ContentType", "multipart/form-data");
                request.AddFile("file", stream => fileStream.CopyTo(stream), fileName);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return deserializer.Deserialize<List<Attachment>>(response).Single();
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateAttachment(issue, fileStream, fileName) error: {0}", ex);
                throw new JiraClientException("Could not create attachment", ex);
            }
        }

        public void DeleteAttachment(Attachment attachment)
        {
            try
            {
                var path = String.Format("attachment/{0}", attachment.id);
                var request = CreateRequest(Method.DELETE, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteAttachment(attachment) error: {0}", ex);
                throw new JiraClientException("Could not delete attachment", ex);
            }
        }


        public async Task<IEnumerable<IssueLink>> GetIssueLinksAsync(IssueRef issue)
        {
            return (await LoadIssueAsync(issue)).fields.issuelinks;
        }

        public async Task<IssueLink> LoadIssueLinkAsync(IssueRef parent, IssueRef child, String relationship)
        {
            try
            {
                var issue = await LoadIssueAsync(parent);
                var links = issue.fields.issuelinks
                    .Where(l => l.type.name == relationship)
                    .Where(l => l.inwardIssue.id == parent.id)
                    .Where(l => l.outwardIssue.id == child.id)
                    .ToArray();

                if (links.Length > 1)
                    throw new JiraClientException("Ambiguous issue link");
                return links.SingleOrDefault();
            }
            catch (Exception ex)
            {
                Trace.TraceError("LoadIssueLink(parent, child, relationship) error: {0}", ex);
                throw new JiraClientException("Could not load issue link", ex);
            }
        }

        public async Task<IssueLink> CreateIssueLinkAsync(IssueRef parent, IssueRef child, String relationship)
        {
            try
            {
                var request = CreateRequest(Method.POST, "issueLink");
                request.AddHeader("ContentType", "application/json");
                request.AddBody(new
                {
                    type = new { name = relationship },
                    inwardIssue = new { id = parent.id },
                    outwardIssue = new { id = child.id }
                });

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.Created);

                return await LoadIssueLinkAsync(parent, child, relationship);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateIssueLink(parent, child, relationship) error: {0}", ex);
                throw new JiraClientException("Could not link issues", ex);
            }
        }

        public void DeleteIssueLink(IssueLink link)
        {
            try
            {
                var path = String.Format("issueLink/{0}", link.id);
                var request = CreateRequest(Method.DELETE, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteIssueLink(link) error: {0}", ex);
                throw new JiraClientException("Could not delete issue link", ex);
            }
        }


        public IEnumerable<RemoteLink> GetRemoteLinks(IssueRef issue)
        {
            try
            {
                var path = string.Format("issue/{0}/remotelink", issue.id);
                var request = CreateRequest(Method.GET, path);
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return deserializer.Deserialize<List<RemoteLinkResult>>(response)
                    .Select(RemoteLink.Convert).ToList();
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetRemoteLinks(issue) error: {0}", ex);
                throw new JiraClientException("Could not load external links for issue", ex);
            }
        }

        public RemoteLink CreateRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            try
            {
                var path = string.Format("issue/{0}/remotelink", issue.id);
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("ContentType", "application/json");
                request.AddBody(new
                {
                    application = new
                    {
                        type = "TechTalk.JiraRestClient",
                        name = "JIRA REST client"
                    },
                    @object = new
                    {
                        url = remoteLink.url,
                        title = remoteLink.title,
                        summary = remoteLink.summary
                    }
                });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                //returns: { "id": <id>, "self": <url> }
                var linkId = deserializer.Deserialize<RemoteLink>(response).id;
                return GetRemoteLinks(issue).Single(rl => rl.id == linkId);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateRemoteLink(issue, remoteLink) error: {0}", ex);
                throw new JiraClientException("Could not create external link for issue", ex);
            }
        }

        public RemoteLink UpdateRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            try
            {
                var path = string.Format("issue/{0}/remotelink/{1}", issue.id, remoteLink.id);
                var request = CreateRequest(Method.PUT, path);
                request.AddHeader("ContentType", "application/json");

                var updateData = new Dictionary<string, object>();
                if (remoteLink.url != null) updateData.Add("url", remoteLink.url);
                if (remoteLink.title != null) updateData.Add("title", remoteLink.title);
                if (remoteLink.summary != null) updateData.Add("summary", remoteLink.summary);
                request.AddBody(new { @object = updateData });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);

                return GetRemoteLinks(issue).Single(rl => rl.id == remoteLink.id);
            }
            catch (Exception ex)
            {
                Trace.TraceError("UpdateRemoteLink(issue, remoteLink) error: {0}", ex);
                throw new JiraClientException("Could not update external link for issue", ex);
            }
        }

        public void DeleteRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            try
            {
                var path = string.Format("issue/{0}/remotelink/{1}", issue.id, remoteLink.id);
                var request = CreateRequest(Method.DELETE, path);
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteRemoteLink(issue, remoteLink) error: {0}", ex);
                throw new JiraClientException("Could not delete external link for issue", ex);
            }
        }

        public IEnumerable<IssueType> GetIssueTypes()
        {
            try
            {
                var request = CreateRequest(Method.GET, "issuetype");
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<List<IssueType>>(response);
                return data;

            }
            catch (Exception ex)
            {
                Trace.TraceError("GetIssueTypes() error: {0}", ex);
                throw new JiraClientException("Could not load issue types", ex);
            }
        }

        public async Task<ServerInfo> GetServerInfoAsync()
        {
            try
            {
                var request = CreateRequest(Method.GET, "serverInfo");
                request.AddHeader("ContentType", "application/json");

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.OK);

                return deserializer.Deserialize<ServerInfo>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetServerInfo() error: {0}", ex);
                throw new JiraClientException("Could not retrieve server information", ex);
            }
        }

        public async Task<IEnumerable<Worklog>> GetWorklogsAsync(IssueRef issue)
        {
            try
            {
                var path = String.Format("issue/{0}/worklog", issue.JiraIdentifier);
                var request = CreateRequest(Method.GET, path);

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<WorklogContainer>(response);
                return data.worklogs ?? Enumerable.Empty<Worklog>();
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetWorklogs(issue) error: {0}", ex);
                throw new JiraClientException("Could not load worklogs", ex);
            }
        }

        public async Task<IEnumerable<Project>> GetProjectsAsync()
        {
            try
            {
                var request = CreateRequest(Method.GET, "project");

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<List<Project>>(response);
                return data ?? Enumerable.Empty<Project>();
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetProjects() error: {0}", ex);
                throw new JiraClientException("Could not load projects", ex);
            }
        }

        public async Task<IssueMeta> GetCreateIssueMetaAsync(string projectKey)
        {
            try
            {
                var request = CreateRequest(Method.GET, "issue/createmeta?expand=projects.issuetypes.fields&projectKeys=" + projectKey);

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<IssueMeta>(response);
                return data;
            }
            catch (Exception ex)
            {
                Trace.TraceError( nameof(GetCreateIssueMetaAsync) + "(projectKey) error: {0}", ex);
                throw new JiraClientException("Could not load create issue meta", ex);
            }
        }

        public async Task<JiraUser> GetUserAsync(string name)
        {
            try
            {
                var request = CreateRequest(Method.GET, "user?username=" + Uri.EscapeDataString(name));

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<JiraUser>(response);
                return data;
            }
            catch (Exception ex)
            {
                Trace.TraceError(nameof(GetUserAsync) + "(name) error: {0}", ex);
                throw new JiraClientException("Could not load user", ex);
            }
        }

        public async Task<Session> GetSessionAsync()
        {
            try
            {
                var request = CreateRequest(Method.GET, "session");

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<Session>(response);
                return data;
            }
            catch (Exception ex)
            {
                Trace.TraceError(nameof(GetSessionAsync) + "() error: {0}", ex);
                throw new JiraClientException("Could not load session info", ex);
            }
        }

        public async Task<List<JiraUser>> FindUsersAsync(string userName, int startAt = 0, int maxResults = 50)
        {
            try
            {
                var path = string.Format("user/search?username={0}&startat={1}&maxresults={2}",
                    Uri.EscapeDataString(userName), startAt, maxResults);
                var request = CreateRequest(Method.GET, path);

                var response = await ExecuteRequestAsync(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<List<JiraUser>>(response);
                return data;
            }
            catch (Exception ex)
            {
                Trace.TraceError(nameof(FindUsersAsync) + "() error: {0}", ex);
                throw new JiraClientException("Could not load user search results", ex);
            }
        }
    }

    public class RequestEventArgs : EventArgs
    {
        public Uri Uri { get; set; }
        public RestRequest Request { get; set; }
        public IRestResponse Response { get; set; }
    }
}
