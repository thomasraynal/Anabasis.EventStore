﻿using System;

namespace Anabasis.Common
{
    public class AnabasisAppContext
    {

        public AnabasisAppContext(
            string applicationName,
            string applicationGroup,
            Version apiVersion,
            string? sentryDsn = null,
            string? honeycombServiceName = null,
            string? honeycombApiKey = null,
            AnabasisEnvironment environnement = AnabasisEnvironment.Development,
            Uri? docUrl = null,
            int apiPort = 80,
            int memoryCheckTresholdInMB = 200,
            string? machineName = null)
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
            HoneycombServiceName = honeycombServiceName;
            HoneycombApiKey = honeycombApiKey;
        }

        public Uri? DocUrl { get; }
        public int ApiPort { get;  }
        public Version ApiVersion { get; }
        public string ApplicationName { get;  }
        public string ApplicationNameAndApiVersion => $"{ApplicationName}{ApiVersion}";
        public string ApplicationGroup { get; }
        public int MemoryCheckTresholdInMB { get; }
        public string? MachineName { get;  }
        public string? SentryDsn { get; }
        public string? HoneycombServiceName { get; }
        public string? HoneycombApiKey { get; }
        public AnabasisEnvironment Environment { get; }
        public bool UseSentry => !string.IsNullOrEmpty(SentryDsn);
        public bool UseHoneycomb => !string.IsNullOrEmpty(HoneycombServiceName) && !string.IsNullOrEmpty(HoneycombApiKey);

    }
}
