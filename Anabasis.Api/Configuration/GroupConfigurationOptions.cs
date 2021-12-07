using Anabasis.Common;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.Api
{
    public class GroupConfigurationOptions : BaseConfiguration
    {
        [Required]
        public string GroupName { get; set; }
        public string Environment { get; set; } = "PROD";
    }
}
