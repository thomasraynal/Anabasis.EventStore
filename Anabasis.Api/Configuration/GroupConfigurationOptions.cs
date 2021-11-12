using Anabasis.Api.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class GroupConfigurationOptions : BaseConfiguration
    {
        [Required]
        public string GroupName { get; set; }
        public string Environment { get; set; } = "PROD";
    }
}
