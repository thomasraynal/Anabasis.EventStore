using Anabasis.Common;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.Common
{
    public class GroupConfigurationOptions : BaseConfiguration
    {
        [Required]
        public string GroupName { get; set; }
    }
}
