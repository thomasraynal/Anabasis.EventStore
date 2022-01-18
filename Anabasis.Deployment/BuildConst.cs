using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Deployment
{
    public static class BuildConst
    {
        public const string GroupConfigurationFileName = "config.group.json";
        public const string AppConfigurationFileName = "config.app.json";
        public const string KustomizeFolderName = "kustomize";
        public const string Base = "base";
        public const string Templates = "templates";
        public static readonly DirectoryInfo BuildKustomizeTemplatesDirectory = new(Templates);
        public static readonly string Production = AnabasisBuildEnvironment.Prod.ToString().ToLower();
    }
}
