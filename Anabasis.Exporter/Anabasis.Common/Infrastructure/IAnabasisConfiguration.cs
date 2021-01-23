using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
  public interface IAnabasisConfiguration
  {
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RefreshToken { get; set; }
    public string DriveRootFolder { get; set; }

  }
}
