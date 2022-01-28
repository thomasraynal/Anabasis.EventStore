using k8s;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Deployment.Tests
{
    public static class Sanitizer
    {
        public static string SanitizeYamlForTests(this string yaml)
        {
            return yaml.Replace('\n', ' ').Replace('\r', ' ').Replace(" ","");
        }
    }

    [TestFixture]
    public class GenerateOverridesTests
    {
        private string _appName;
        private string _appGroup;
        private string _release;
        private string _branch;
        private string _repository;
        private string _imageTag;
        private string _imageName;
        private string _serviceName;
        private string _namespaceName;
        private string _configMapGroup;
        private string _configMapApp;

     

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {

            _appName = "test";
            _appGroup = "testgroup";
            _release = "1.0.0.5555";
            _branch = "master";
            _repository = "dockerhub.com";
            _imageTag = "5555";
            _imageName = $"{_repository}/{_branch}-{_appName}:{_imageTag}";

            _serviceName = $"svc-{_appName}";
            _namespaceName = $"namespace-{_appGroup}";

            _configMapGroup = $"config-group-{_appGroup}";
            _configMapApp = $"config-group-{_appName}";
        }

        [Test]
        public async Task ShouldGenerateNamespace()
        {
            var @namespace = (await Yaml.LoadAllFromFileAsync("./kustomize/templates/namespace.yaml")).First() as k8s.Models.V1Namespace;
            @namespace.Metadata.Name = _namespaceName;
            @namespace.Metadata.Labels["group"] = _appGroup;

            var namespaceYaml = Yaml.SaveToString(@namespace);

            Assert.AreEqual(File.ReadAllText("./data/expected-namespace.yaml").SanitizeYamlForTests(), namespaceYaml.SanitizeYamlForTests());
        }

        [Test]
        public async Task ShouldGenerateService()
        {

            var service = (await Yaml.LoadAllFromFileAsync("./kustomize/templates/service.yaml")).First() as k8s.Models.V1Service;

            service.Metadata.NamespaceProperty = _namespaceName;
            service.Metadata.Name = _serviceName;

            service.Metadata.Labels["app"] = _appName;
            service.Metadata.Labels["release"] = _release;
            service.Metadata.Labels["group"] = _appGroup;
            service.Spec.Selector["release"] = _release;
            service.Spec.Selector["app"] = _appName;

            var serviceYaml = Yaml.SaveToString(service);

            Assert.AreEqual(File.ReadAllText("./data/expected-service.yaml").SanitizeYamlForTests(), serviceYaml.SanitizeYamlForTests());
        }

        [Test]
        public async Task ShouldGenerateDeployment()
        {

            var deployment = (await Yaml.LoadAllFromFileAsync("./kustomize/templates/deployment.yaml")).First() as k8s.Models.V1Deployment;

            deployment.Metadata.NamespaceProperty = _appGroup;
            deployment.Metadata.Name = _appName;

            deployment.Metadata.Labels["app"] = _appName;
            deployment.Metadata.Labels["release"] = _release;
            deployment.Metadata.Labels["group"] = _appGroup;

            deployment.Spec.Selector.MatchLabels["app"] = _appName;
            deployment.Spec.Selector.MatchLabels["group"] = _appGroup;

            deployment.Spec.Template.Metadata.Labels["release"] = _release;
            deployment.Spec.Template.Metadata.Labels["app"] = _appName;

            var container = deployment.Spec.Template.Spec.Containers.First();
            container.Name = _appName;
            container.Image = _imageName;

            var deploymentYaml = Yaml.SaveToString(deployment);

            Assert.AreEqual(File.ReadAllText("./data/expected-deployment.yaml").SanitizeYamlForTests(), deploymentYaml.SanitizeYamlForTests());

        }

    }
}
