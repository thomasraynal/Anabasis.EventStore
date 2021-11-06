using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Anabasis.Api
{
    [Serializable]
    [DataContract]
    public partial class UserErrorMessage
    {
        [JsonConstructor]
        public UserErrorMessage() { }

        public UserErrorMessage(
            string code,
            string message,
            string cultureName = null,
            Dictionary<string, object> arguments = null,
            Uri docUrl = null,
            string stackTrace = null
            )
        {

            DocUrl = docUrl;
            Code = code;
            Message = message;
            CultureName = cultureName;
            Arguments = arguments;
            StackTrace = stackTrace;
        }

        [DataMember]
        public Uri DocUrl { get; set; }

        [Required]
        [DataMember]
        public string Code { get; set; }

        [Required]
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string CultureName { get; set; }

        [DataMember]
        public Dictionary<string, object> Arguments { get; set; }

        public string StackTrace { get; set; }
    }
}
