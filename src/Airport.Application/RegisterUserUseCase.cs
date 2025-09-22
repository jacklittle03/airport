using System.Linq;
using Airport.Domain;

namespace Airport.Application;

public record RegisterTravellerRequest(string Name, int Age, string Email, string Mobile, string Password);
public record RegisterFrequentFlyerRequest(string Name, int Age, string Email, string Mobile, string Password, int FrequentFlyerNumber, int Points);
public record RegisterManagerRequest(string Name, int Age, string Email, string Mobile, string Password, string StaffId);

public record RegisterUserResponse(Guid UserId, string Email);

public class RegisterUserUseCase
{
    private readonly IRepository<User> _userRepo;
    public RegisterUserUseCase(IRepository<User> userRepo) => _userRepo = userRepo;

    private bool EmailExists(string email)
    {
        var normalized = (email ?? "").Trim().ToLowerInvariant();
        return _userRepo.ListAsync().Result.Any(u =>
            (u.Email ?? "").Trim().ToLowerInvariant() == normalized
        );
    }

    public RegisterUserResponse? RegisterTraveller(RegisterTravellerRequest request)
    {
        if (EmailExists(request.Email)) return null;
        var user = new Traveller(request.Name, request.Age, request.Email, request.Mobile, request.Password);
        _userRepo.AddAsync(user).Wait();
        return new RegisterUserResponse(user.Id, user.Email);
    }

    public RegisterUserResponse? RegisterFrequentFlyer(RegisterFrequentFlyerRequest request)
    {
        if (EmailExists(request.Email)) return null;
        var user = new FrequentFlyer(
            request.Name, request.Age, request.Email, request.Mobile, request.Password,
            request.FrequentFlyerNumber, request.Points
        );
        _userRepo.AddAsync(user).Wait();
        return new RegisterUserResponse(user.Id, user.Email);
    }

    public RegisterUserResponse? RegisterManager(RegisterManagerRequest request)
    {
        if (EmailExists(request.Email)) return null;
        var user = new FlightManager(request.Name, request.Age, request.Email, request.Mobile, request.Password, request.StaffId);
        _userRepo.AddAsync(user).Wait();
        return new RegisterUserResponse(user.Id, user.Email);
    }
}
