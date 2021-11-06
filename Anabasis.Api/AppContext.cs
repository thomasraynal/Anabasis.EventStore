using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class AppContext
    {
        public Uri DocUrl { get; }
        public int ApiPort { get;  }
        public Version ApiVersion { get; }
        public string Environment { get; }
        public string ApplicationName { get;  }
        public IServiceCollection ServiceCollection { get; }
        public int MemoryCheckTresholdInMB { get; } = 200;
    }
}
