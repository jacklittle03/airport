using Airport.Application;
using Airport.Domain;
using Airport.Infrastructure;

namespace Airport.Cli
{
    internal static class Program
    {
        // In-memory stores for the prototype
        private static readonly IRepository<User> UserRepo =
            new InMemoryRepository<User>(u => u.Id);

        private static void Main()
        {
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
                case "2":
                    RegisterTraveller();
                    break;
                case "3":
                    Console.WriteLine("Thank you. Safe travels.");
                    return;
                default:
                    Console.WriteLine("Feature not implemented yet.");
                    break;
            }
        }

        private static void RegisterTraveller()
        {
            var useCase = new RegisterUserUseCase(UserRepo);

            // --- Prompts ---
            Console.Write("Enter your name: ");
            var name = (Console.ReadLine() ?? string.Empty).Trim();

            Console.Write("Enter your age: ");
            var ageText = Console.ReadLine() ?? "";
            _ = int.TryParse(ageText, out var age);

            Console.Write("Enter your email: ");
            var email = (Console.ReadLine() ?? string.Empty).Trim();

            Console.Write("Enter your mobile: ");
            var mobile = (Console.ReadLine() ?? string.Empty).Trim();

            Console.Write("Enter your password: ");
            var password = (Console.ReadLine() ?? string.Empty).Trim();

            // --- Validation (fast-fail with clear messages) ---
            if (!Validation.IsValidName(name))
            {
                Console.WriteLine("Error: Invalid name.");
                return;
            }
            if (!Validation.IsValidAge(age))
            {
                Console.WriteLine("Error: Invalid age.");
                return;
            }
            if (!Validation.IsValidEmail(email))
            {
                Console.WriteLine("Error: Invalid email.");
                return;
            }
            if (!Validation.IsValidMobile(mobile))
            {
                Console.WriteLine("Error: Invalid mobile.");
                return;
            }
            if (!Validation.IsValidPassword(password))
            {
                Console.WriteLine("Error: Invalid password.");
                return;
            }

            // --- Execute use case ---
            var result = useCase.HandleAsync(
                new RegisterUserRequest(name, age, email, mobile, password)
            ).Result;

            if (result is null)
            {
                Console.WriteLine("Error: Email already exists.");
            }
            else
            {
                Console.WriteLine($"Registration successful. Welcome, {name}!");
            }
        }

        private static void PrintBanner()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("= Welcome to Brisbane Domestic Airport =");
            Console.WriteLine("===========================================");
        }
    }
}
