using System;
using System.Diagnostics;
using EventStore.ClientAPI;
using System.Collections.Generic;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Integration.Tests
{

  //https://github.com/linedata/reactive-domain/blob/c2aa6b8ee6527406a8458f4093af4e61dd268749/src/ReactiveDomain.Testing/EmbeddedEventStoreFixture.cs
  public class DockerEventStoreFixture 
  {

    public DockerEventStoreFixture ()
    {
      EventStoreContainer = "eventstore";
    }

    private string EventStoreContainer { get; set; }


    public IEventStoreConnection Connection { get; private set; }


    private string _eventStoreImage = "eventstore/eventstore";

    public async Task Initialize()
    {
      var address = Environment.OSVersion.Platform == PlatformID.Unix
          ? new Uri("unix:///var/run/docker.sock")
          : new Uri("npipe://./pipe/docker_engine");
      var config = new DockerClientConfiguration(address);
      this.Client = config.CreateClient();

      var sysInfo = await this.Client.System.GetSystemInfoAsync();

      Trace.WriteLine(sysInfo.OSType);

      if (sysInfo.OSType != "linux")
      {
        _eventStoreImage = "idubnori/eventstore";
      }

      var images = await this.Client.Images.ListImagesAsync(new ImagesListParameters { MatchName = _eventStoreImage });
      if (images.Count == 0)
      {
        // No image found. Pulling latest ..
        Console.WriteLine("[docker] no image found - pulling latest");
        await this.Client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = _eventStoreImage, Tag = "latest" }, null, IgnoreProgress.Forever);
      }
      Console.WriteLine("[docker] creating container " + EventStoreContainer);
      //Create container ...
      var response = await this.Client.Containers.CreateContainerAsync(
           new CreateContainerParameters
           {
             Image = _eventStoreImage,
             Name = EventStoreContainer,
             Tty = true,
             Env= new[]
             {
              "EVENTSTORE_RUN_PROJECTIONS=All",
              "EVENTSTORE_START_STANDARD_PROJECTIONS=true",
              "EVENTSTORE_EXT_TCP_PORT=1113",
              "EVENTSTORE_EXT_HTTP_PORT=2113",
              "EVENTSTORE_INSECURE=true",
              "EVENTSTORE_ENABLE_EXTERNAL_TCP=true",
              "EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP=true"
             },
             HostConfig = new HostConfig
             {
               PortBindings = new Dictionary<string, IList<PortBinding>>
                   {
                            {
                                "2113/tcp",
                                new List<PortBinding> {
                                    new PortBinding
                                    {
                                        HostPort = "2113"
                                    }
                                }
                            },
                            {
                                "1113/tcp",
                                new List<PortBinding> {
                                    new PortBinding
                                    {
                                        HostPort = "1113"
                                    }
                                }
                            }
                   }
             }
           });



      // Starting the container ...
      Console.WriteLine("[docker] starting container " + EventStoreContainer);
      await this.Client.Containers.StartContainerAsync(EventStoreContainer, new ContainerStartParameters { });

      //var psi = new ProcessStartInfo
      //{
      //  FileName = "docker",
      //  Arguments = $"{EventStoreContainer} --insecure --run-projections=All --enable-external-tcp --enable-atom-pub-over-http",
      //  UseShellExecute = false,
      //  RedirectStandardOutput = true,
      //  RedirectStandardError = true
      //};

      //var proc = new Process
      //{
      //  StartInfo = psi
      //};

      //proc.Start();

      // string ip = string.Empty;

      //while (!proc.StandardOutput.EndOfStream)
      //{
      //  ip = proc.StandardOutput.ReadLine();
      //}

      //  return ip;

      //var endpoint = new Uri("tcp://127.0.0.1:1113");
      //var settings = ConnectionSettings
      //    .Create()
      //    .KeepReconnecting()
      //    .KeepRetrying()
      //    .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));
      //var connectionName = $"M={Environment.MachineName},P={Process.GetCurrentProcess().Id},T={DateTimeOffset.UtcNow.Ticks}";
      //this.Connection = EventStoreConnection.Create(settings, endpoint, connectionName);
      //Console.WriteLine("[docker] connecting to eventstore");
      //await this.Connection.ConnectAsync();
    }

    public async Task Dispose()
    {
      if (this.Client != null)
      {
        this.Connection?.Dispose();
        Console.WriteLine("[docker] stopping container " + EventStoreContainer);
        await this.Client.Containers.StopContainerAsync(EventStoreContainer, new ContainerStopParameters { });
        Console.WriteLine("[docker] removing container " + EventStoreContainer);
        await this.Client.Containers.RemoveContainerAsync(EventStoreContainer, new ContainerRemoveParameters { Force = true });
        this.Client.Dispose();
      }
    }

    private DockerClient Client { get; set; }

    private class IgnoreProgress : IProgress<JSONMessage>
    {
      public static readonly IProgress<JSONMessage> Forever = new IgnoreProgress();

      public void Report(JSONMessage value) { }
    }
  }
}

