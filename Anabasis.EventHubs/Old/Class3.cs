using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Old
{
    public static class MessageConstants
    {
        /// <summary>
        /// 230 Ko (This size is lower than 256 Ko to anticipate the size of the header we let 26 Ko for the potential header)
        /// </summary>
        public static readonly int MessageSizeToZip = 200000; // 235520; // As dotnet core version of EventHub EventData, let's lower again this limit
    }
}
