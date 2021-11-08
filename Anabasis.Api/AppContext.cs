using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class AppContext
    {
        public AppContext(
            string applicationName,
            string environment,
            Version apiVersion,            
            Uri docUrl = null,
            int apiPort= 80,
            int memoryCheckTresholdInMB = 200,
            string machineName = null)
        {
            DocUrl = docUrl;
            ApiPort = apiPort;
            ApiVersion = apiVersion;
            Environment = environment;
            ApplicationName = applicationName;
            MemoryCheckTresholdInMB = memoryCheckTresholdInMB;
            MachineName = machineName;
        }

        public Uri DocUrl { get; }
        public int ApiPort { get;  }
        public Version ApiVersion { get; }
        public string Environment { get; }
        public string ApplicationName { get;  }
        public int MemoryCheckTresholdInMB { get; }
        public string MachineName { get;  }
    }
}
