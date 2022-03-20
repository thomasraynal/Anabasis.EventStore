using System;

namespace Anabasis.Common
{
    public class AnabasisAppContext
    {




        public AnabasisAppContext(
            string applicationName,
            string applicationGroup,
            Version apiVersion,
            string sentryDsn = null,
            AnabasisEnvironment environnement = AnabasisEnvironment.Development,
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
        public AnabasisEnvironment Environment { get; }
        public bool UseSentry => !string.IsNullOrEmpty(SentryDsn);

    }
}
