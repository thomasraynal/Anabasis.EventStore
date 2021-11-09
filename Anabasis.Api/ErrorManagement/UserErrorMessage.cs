using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Anabasis.Api
{
    public partial class UserErrorMessage
    {
        [JsonConstructor]
        public UserErrorMessage() { }

        public UserErrorMessage(
            string code,
            string message,
            Dictionary<string, object> properties = null,
            Uri docUrl = null,
            string stackTrace = null
            )
        {

            DocUrl = docUrl;
            Code = code;
            Message = message;
            Properties = properties;
            StackTrace = stackTrace;
        }

        public Uri DocUrl { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public string Message { get; set; }

        public Dictionary<string, object> Properties { get; set; }

        public string StackTrace { get; set; }
    }
}
