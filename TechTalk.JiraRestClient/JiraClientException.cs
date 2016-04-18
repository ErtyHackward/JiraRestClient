using System;
using System.Net;
using System.Runtime.Serialization;

namespace TechTalk.JiraRestClient
{
    [Serializable]
    public class JiraClientException : Exception
    {
        private readonly string response;
        private HttpStatusCode _statusCode;
        public JiraClientException() { }
        public JiraClientException(string message) : base(message) { }
        public JiraClientException(string message, string response, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
            this.response = response;
        }

        public JiraClientException(string message, Exception inner) : base(message, inner)
        {
            var ji = inner as JiraClientException;
            if (ji != null)
            {
                response = ji.ErrorResponse;
            }
        }
        protected JiraClientException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public string ErrorResponse { get { return response; } }

        public HttpStatusCode StatusCode
        {
            get { return _statusCode; }
            set { _statusCode = value; }
        }
    }
}
