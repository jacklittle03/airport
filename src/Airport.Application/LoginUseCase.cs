using System.Linq;
using Airport.Domain;

namespace Airport.Application;

public record LoginRequest(string Email, string Password);
public record LoginResult(Guid UserId, string Name, string Email);

public sealed class LoginUseCase
{
    private readonly IRepository<User> _userRepo;

    public LoginUseCase(IRepository<User> userRepo) => _userRepo = userRepo;

    public async Task<LoginResult?> HandleAsync(LoginRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var users = await _userRepo.ListAsync();

        var match = users.FirstOrDefault(u =>
            (u.Email ?? "").Trim().ToLowerInvariant() == email &&
            (u.PasswordHash ?? "") == (request.Password ?? "")
        );

        return match is null ? null : new LoginResult(match.Id, match.Name, match.Email);
    }
}
