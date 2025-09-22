using System.Linq;
using Airport.Domain;

namespace Airport.Application;

public record RegisterUserRequest(string Name, int Age, string Email, string Mobile, string Password);
public record RegisterUserResponse(Guid UserId, string Email);

public class RegisterUserUseCase
{
    private readonly IRepository<User> _userRepo;

    public RegisterUserUseCase(IRepository<User> userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<RegisterUserResponse?> HandleAsync(RegisterUserRequest request)
    {
        // Normalize
        var normalizedEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

        var existing = await _userRepo.ListAsync();
        if (existing.Any(u => (u.Email ?? string.Empty).Trim().ToLowerInvariant() == normalizedEmail))
            return null;

        var traveller = new Traveller(
            request.Name?.Trim() ?? string.Empty,
            request.Age,
            normalizedEmail,
            request.Mobile?.Trim() ?? string.Empty,
            request.Password // (hash later)
        );

        await _userRepo.AddAsync(traveller);
        return new RegisterUserResponse(traveller.Id, traveller.Email);
    }

}
