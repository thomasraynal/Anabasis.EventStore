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
                GroupToBeDeployed = "test-group",
                SourceDirectory = Nuke.Common.NukeBuild.RootDirectory,
                BuildProjectKustomizeDirectory = Nuke.Common.NukeBuild.RootDirectory / "Anabasis.Deployment" / "kustomize"
            };
        }

        [Test, Order(1)]
        public void ShouldGetAppsToBeDeployed()
        {
            var appToBeDeployed = _testAnabasisBuild.GetAppsToBeDeployed();

            Assert.AreEqual(1, appToBeDeployed.Length);

            _testApp = appToBeDeployed.First();

            Assert.AreEqual("test-group-anabasis-deployment-test-app", _testApp.AppLongName);
        }

        [Test, Order(2)]
        public void ShouldSetupKustomize()
        {
            if (Directory.Exists(_testApp.AppSourceKustomizeDirectory.FullName))
            {
                Directory.Delete(_testApp.AppSourceKustomizeDirectory.FullName, true);
            }

            _testAnabasisBuild.SetupKustomize(_testApp);

            Assert.IsTrue(Directory.Exists(_testApp.AppSourceKustomizeDirectory.FullName));
            Assert.IsTrue(Directory.Exists(_testApp.AppSourceKustomizeBaseDirectory.FullName));
            Assert.IsTrue(Directory.Exists(_testApp.AppSourceKustomizeOverlaysDirectory.FullName));

        }

        [Test, Order(3)]
        public async Task ShouldGenerateBaseKustomize()
        {
            await _testAnabasisBuild.GenerateBaseKustomize(_testApp);

            Assert.Greater(Directory.GetFiles(Path.Combine(_testApp.AppSourceKustomizeBaseDirectory.FullName, "prod", "api")).Length, 0);
            Assert.Greater(Directory.GetFiles(Path.Combine(_testApp.AppSourceKustomizeBaseDirectory.FullName, "prod", "group")).Length, 0);
            Assert.Greater(Directory.GetFiles(Path.Combine(_testApp.AppSourceKustomizeBaseDirectory.FullName, "prod", "namespace")).Length, 0);

        }

    }
}
