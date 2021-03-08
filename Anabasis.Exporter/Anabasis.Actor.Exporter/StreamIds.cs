using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
  public static class StreamIds
  {
    public static int PersistentSubscriptionGroupCount = 5;
    public static string[] AllStreams => new[] { "googledoc", "bobby", "illiad" };
    public static string GoogleDoc => "googledoc";
    public static string Bobby => "bobby";
    public static string Illiad => "illiad";
  }
}
