using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class AnabasisAppContext
    {
        public const string AppConfigurationFile = "config.app.json";
        public const string GroupConfigurationFile = "config.group.json";

        public AnabasisAppContext(
            string applicationName,
            string applicationGroup,
            Version apiVersion,
            string sentryDsn,
            string environnement,
            Uri docUrl = null,
            int apiPort = 80,
            int memoryCheckTresholdInMB = 200,
            string machineName = null)
        {
            DocUrl = docUrl;
            ApiPort = apiPort;
            ApiVersion = apiVersion;
            ApplicationName = applicationName;
            ApplicationGroup = applicationGroup;
            MemoryCheckTresholdInMB = memoryCheckTresholdInMB;
            MachineName = machineName;
            SentryDsn = sentryDsn;
            Environment = environnement;
        }

        public Uri DocUrl { get; }
        public int ApiPort { get;  }
        public Version ApiVersion { get; }
        public string ApplicationName { get;  }
        public string ApplicationGroup { get; }
        public int MemoryCheckTresholdInMB { get; }
        public string MachineName { get;  }
        public string SentryDsn { get; }
        public string Environment { get; }
    }
}
