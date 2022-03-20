using Anabasis.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.Common
{
    public class AppConfigurationOptions: BaseConfiguration
    {
        [Required(AllowEmptyStrings = false)]
        public string ApplicationName { get; set; }
        [Required]
        public Version ApiVersion { get; set; }
        public string SentryDsn { get; set; }
        public Uri DocUrl { get; set; }

    }
}
