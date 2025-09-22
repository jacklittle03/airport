using Airport.Application;
using Airport.Domain;
using Airport.Infrastructure;

namespace Airport.Cli
{
    internal static class Program
    {
        private static readonly IRepository<User> UserRepo =
            new InMemoryRepository<User>(u => u.Id);

        private static void Main()
        {
            bool running = true;
            while (running)
            {
                Console.Clear();
                PrintBanner();

                Console.WriteLine();
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. Login as a registered user.");
                Console.WriteLine("2. Register as a new user.");
                Console.WriteLine("3. Exit.");
                Console.Write("Please enter a choice between 1 and 3: ");

                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        DoLogin();
                        break;
                    case "2":
                        RegisterUser();
                        Pause();
                        break;
                    case "3":
                        Console.WriteLine("Thank you. Safe travels.");
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Feature not implemented yet.");
                        Pause();
                        break;
                }
            }
        }

        // ---------------- Login + User Menu ----------------

        private static void DoLogin()
        {
            var login = new LoginUseCase(UserRepo);

            Console.Write("Enter your email: ");
            var email = (Console.ReadLine() ?? string.Empty).Trim();

            Console.Write("Enter your password: ");
            var password = Console.ReadLine() ?? string.Empty;

            var result = login.HandleAsync(new LoginRequest(email, password)).Result;
            if (result is null)
            {
                Console.WriteLine("Login failed. Please check your credentials.");
                Pause();
                return;
            }

            // Authenticated session
            UserSessionMenu(result.UserId);
        }

        private static void UserSessionMenu(Guid userId)
        {
            bool inSession = true;
            while (inSession)
            {
                Console.Clear();
                Console.WriteLine("== User Menu ==");
                Console.WriteLine("1. View my registered details.");
                Console.WriteLine("2. Logout.");
                Console.Write("Please enter a choice between 1 and 2: ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        ShowMyDetails(userId);
                        break;
                    case "2":
                        Console.WriteLine("Logged out.");
                        inSession = false;
                        Pause();
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        Pause();
                        break;
                }
            }
        }

        private static void ShowMyDetails(Guid userId)
        {
            var user = UserRepo.GetByIdAsync(userId).Result;
            if (user is null)
            {
                Console.WriteLine("User not found.");
                Pause();
                return;
            }

            // print the four fields clearly
            Console.WriteLine();
            Console.WriteLine($"Name  : {user.Name}");
            Console.WriteLine($"Age   : {user.Age}");
            Console.WriteLine($"Email : {user.Email}");
            Console.WriteLine($"Mobile: {user.Mobile}");

            Pause();
        }

        // ---------------- Register ----------------

        private static void RegisterUser()
    {
        var useCase = new RegisterUserUseCase(UserRepo);

        Console.WriteLine("Which user type would you like to register?");
        Console.WriteLine("1. A standard traveller.");
        Console.WriteLine("2. A frequent flyer.");
        Console.WriteLine("3. A flight manager.");
        Console.Write("Please enter a choice between 1 and 3: ");
        var choice = Console.ReadLine();

        Console.Write("Please enter in your name: ");
        var name = (Console.ReadLine() ?? "").Trim();

        Console.Write("Please enter in your age between 0 and 99: ");
        int.TryParse(Console.ReadLine(), out var age);

        Console.Write("Please enter in your mobile number: ");
        var mobile = (Console.ReadLine() ?? "").Trim();

        Console.Write("Please enter in your email: ");
        var email = (Console.ReadLine() ?? "").Trim();

        Console.Write("Please enter in your password: ");
        var password = Console.ReadLine() ?? "";

        switch (choice)
        {
            case "1":
                Console.WriteLine("Registering as a traveller.");
                var travellerResult = useCase.RegisterTraveller(new RegisterTravellerRequest(name, age, email, mobile, password));
                Console.WriteLine(travellerResult is null ? "Error: Email already exists." : "Traveller registered successfully.");
                break;

            case "2":
                Console.WriteLine("Registering as a frequent flyer.");
                Console.Write("Please enter in your Frequent Flyer Number: ");
                int.TryParse(Console.ReadLine(), out var ffNumber);
                Console.Write("Please enter in your Frequent Flyer Points: ");
                int.TryParse(Console.ReadLine(), out var ffPoints);

                var ffResult = useCase.RegisterFrequentFlyer(new RegisterFrequentFlyerRequest(name, age, email, mobile, password, ffNumber, ffPoints));
                Console.WriteLine(ffResult is null ? "Error: Email already exists." : "Frequent flyer registered successfully.");
                break;

            case "3":
                Console.WriteLine("Registering as a flight manager.");
                Console.Write("Please enter in your Staff ID: ");
                var staffId = (Console.ReadLine() ?? "").Trim();

                var mgrResult = useCase.RegisterManager(new RegisterManagerRequest(name, age, email, mobile, password, staffId));
                Console.WriteLine(mgrResult is null ? "Error: Email already exists." : "Flight manager registered successfully.");
                break;

            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
    }

        // ---------------- Helpers ----------------

        private static void PrintBanner()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("= Welcome to Brisbane Domestic Airport =");
            Console.WriteLine("===========================================");
        }

        private static void Pause()
        {
            Console.WriteLine();
            Console.Write("Press Enter to continue...");
            Console.ReadLine();
        }
    }
}
