using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Deployment
{
    public class AppDescriptor
    {
        public AppDescriptor(DirectoryInfo appSourceDirectory, string app, string appRelease, string appGroup, string appLongName, string appShortName)
        {
            AppSourceDirectory = appSourceDirectory;
            App = app;
            AppRelease = appRelease;
            AppGroup = appGroup;
            AppLongName = appLongName;
            AppShortName = appShortName;
        }

        public DirectoryInfo AppSourceDirectory { get; }

        public DirectoryInfo AppSourceKustomizeDirectory => new(Path.Combine(AppSourceDirectory.FullName, BuildConst.Kustomize));
        public DirectoryInfo AppSourceKustomizeBaseDirectory => new(Path.Combine(AppSourceKustomizeDirectory.FullName, BuildConst.Base));

        public string App { get; }
        public string AppRelease { get; }
        public string AppGroup { get; }
        public string AppLongName { get; }
        public string AppShortName { get; }

    }
}
