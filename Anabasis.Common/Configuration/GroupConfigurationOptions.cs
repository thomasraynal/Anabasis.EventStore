using Anabasis.Common;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.Common
{
    public class GroupConfigurationOptions : BaseConfiguration
    {
#nullable disable
        [Required(AllowEmptyStrings = false)]
        public string GroupName { get; set; }
#nullable enable
    }
}
