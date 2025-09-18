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
        // Ensure email is unique
        var existing = await _userRepo.ListAsync();
        if (existing.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
        {
            return null; // email already registered
        }

        var traveller = new Traveller(
            request.Name,
            request.Age,
            request.Email,
            request.Mobile,
            request.Password
        );

        await _userRepo.AddAsync(traveller);

        return new RegisterUserResponse(traveller.Id, traveller.Email);
    }
}
