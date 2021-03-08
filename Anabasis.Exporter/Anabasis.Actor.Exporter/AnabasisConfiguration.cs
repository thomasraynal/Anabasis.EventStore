namespace Anabasis.Common
{
  public abstract class AnabasisConfiguration : IAnabasisConfiguration
  {
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RefreshToken { get; set; }
    public string DriveRootFolder { get; set; }
  }
}
