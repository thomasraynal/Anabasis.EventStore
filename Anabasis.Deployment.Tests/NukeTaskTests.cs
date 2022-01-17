using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Anabasis.Deployment.Tests
{
    public class TestAnabasisBuild: BaseAnabasisBuild
    {

    }

    [TestFixture]
    public class NukeTaskTests
    {
        private TestAnabasisBuild _testAnabasisBuild;
        private AppDescriptor _testApp;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _testAnabasisBuild = new TestAnabasisBuild
            {
                ApplicationGroup = "anabasis-test-group",
                SourceDirectory = Nuke.Common.NukeBuild.RootDirectory,
                BuildId = "1.0.67",
                DockerRegistryServer = "https://hub.docker.com",
                BuildProjectKustomizeDirectory = Nuke.Common.NukeBuild.RootDirectory / "Anabasis.Deployment" / "kustomize"
            };
        }

        [Test, Order(1)]
        public void ShouldGetAppsToBeDeployed()
        {
            var appToBeDeployed = _testAnabasisBuild.GetAppsToDeploy();

            Assert.AreEqual(1, appToBeDeployed.Length);

            _testApp = appToBeDeployed.First();

            Assert.AreEqual("anabasis-test-group-anabasis-deployment-test-app", _testApp.AppLongName);
        }

        [Test, Order(2)]
        public void ShouldSetupKustomize()
        {
            if (Directory.Exists(_testApp.AppSourceKustomizeBaseDirectory.FullName))
            {
                Directory.Delete(_testApp.AppSourceKustomizeBaseDirectory.FullName, true);
            }

            _testAnabasisBuild.SetupKustomize(_testApp);

            Assert.IsTrue(Directory.Exists(_testApp.AppSourceKustomizeDirectory.FullName));
            Assert.IsTrue(Directory.Exists(_testApp.AppSourceKustomizeBaseDirectory.FullName));

        }

        [Test, Order(3)]
        public async Task ShouldGenerateBaseKustomize()
        {
            await _testAnabasisBuild.GenerateBaseKustomize(_testApp);

            Assert.Greater(Directory.GetFiles(_testApp.AppSourceKustomizeBaseDirectory.FullName).Length, 0);
            Assert.Greater(Directory.GetFiles(Path.Combine(_testApp.AppSourceKustomizeDirectory.FullName, "prod")).Length, 0);
        }

        [Test, Order(4)]
        public async Task ShouldCopyKustomizationFiles()
        {

        }

    }
}
