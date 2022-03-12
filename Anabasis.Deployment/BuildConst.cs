using Anabasis.Common;
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
        public const string KustomizeFolderName = "kustomize";
        public const string Base = "base";
        public const string Templates = "templates";
        public static readonly DirectoryInfo BuildKustomizeTemplatesDirectory = new(Templates);
    }
}
