namespace Anabasis.Identity.Dto
{
    public interface IUserLoginDto
    {
        string? Password { get; init; }
        string? Username { get; init; }
    }
}