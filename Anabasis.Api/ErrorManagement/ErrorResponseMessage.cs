﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Anabasis.Api
{

    [Serializable]
    [DataContract]
    public partial class ErrorResponseMessage
    {
        [JsonConstructor]
        public ErrorResponseMessage() { }

        public ErrorResponseMessage(UserErrorMessage[] errors)
        {
            Errors = errors;
        }

        [Required]
        [DataMember]
        public UserErrorMessage[] Errors { get; set; }

    }
}