using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Anabasis.Api
{
    public partial class UserErrorMessage
    {
        [JsonConstructor]
        internal UserErrorMessage() { }

        public UserErrorMessage(
            HttpStatusCode? httpStatusCode,
            string message,
            Dictionary<string, object>? properties = null,
            Uri? docUrl = null,
            string? stackTrace = null
            )
        {

            DocUrl = docUrl;
            HttpStatusCode = httpStatusCode;
            Message = message;
            Properties = properties;
            StackTrace = stackTrace;
        }

        public Uri? DocUrl { get; set; }

        [Required]
        public HttpStatusCode? HttpStatusCode { get; set; }

        [Required]
        public string? Message { get; set; }

        public Dictionary<string, object>? Properties { get; set; }

        public string? StackTrace { get; set; }
    }
}
