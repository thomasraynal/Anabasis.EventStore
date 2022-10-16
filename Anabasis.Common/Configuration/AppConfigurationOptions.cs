using System;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.Common
{
    public class AppConfigurationOptions: BaseConfiguration
    {
#nullable disable
        [Required(AllowEmptyStrings = false)]
        public string ApplicationName { get; set; }
        [Required]
        public Version ApiVersion { get; set; }
#nullable enable
        public string? SentryDsn { get; set; }
        public string? HoneycombServiceName { get; set; }
        public string? HoneycombApiKey { get; set; }
        public Uri? DocUrl { get; set; }

    }
}
