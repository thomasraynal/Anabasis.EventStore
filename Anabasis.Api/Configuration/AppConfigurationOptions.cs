using Anabasis.Api.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class AppConfigurationOptions: BaseConfiguration
    {
        [Required]
        public string ApplicationName { get; set; }

    }
}
