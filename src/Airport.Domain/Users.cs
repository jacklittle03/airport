namespace Airport.Domain;

/// Base user with shared fields used by Traveller, FrequentFlyer, and FlightManager.

public abstract class User
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; protected set; } = string.Empty;
    public int Age { get; protected set; }

    public string Email { get; protected set; } = string.Empty;
    public string Mobile { get; protected set; } = string.Empty;

    // plain pass word text, will hash later.
    public string PasswordHash { get; protected set; } = string.Empty;

    protected User() { }

    protected User(string name, int age, string email, string mobile, string passwordHash)
    {
        Name = name;
        Age = age;
        Email = email;
        Mobile = mobile;
        PasswordHash = passwordHash;
    }
    public void UpdatePassword(string newPassword)
    {
        PasswordHash = newPassword;
    }
    
}

/// A standard traveller.
public class Traveller : User
{
    public Traveller() { }

    public Traveller(string name, int age, string email, string mobile, string passwordHash)
        : base(name, age, email, mobile, passwordHash) { }

}


/// Traveller with loyalty benefits.
public class FrequentFlyer : Traveller
{
    public int FrequentFlyerNumber { get; private set; }
    public int Points { get; private set; }

    public FrequentFlyer() { }

    public FrequentFlyer(
        string name, int age, string email, string mobile, string passwordHash,
        int frequentFlyerNumber, int points)
        : base(name, age, email, mobile, passwordHash)
    {
        FrequentFlyerNumber = frequentFlyerNumber;
        Points = points;
    }
}

/// Employee responsible for flight operations.

public class FlightManager : User
{
    public string StaffId { get; private set; } = string.Empty;

    public FlightManager() { }

    public FlightManager(string name, int age, string email, string mobile, string passwordHash, string staffId)
        : base(name, age, email, mobile, passwordHash)
    {
        StaffId = staffId;
    }
}

