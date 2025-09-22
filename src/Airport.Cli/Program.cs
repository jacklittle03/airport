using Airport.Application;
using Airport.Domain;
using Airport.Infrastructure;

namespace Airport.Cli
{
    internal static class Program
    {
        // Shared in-memory repository for this run
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
                        RegisterUserMenu();
                        Pause();
                        break;
                    case "3":
                        Console.WriteLine("Thank you. Safe travels.");
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        Pause();
                        break;
                }
            }
        }

        // ================== LOGIN ==================

        private static void DoLogin()
        {
            var login = new LoginUseCase(UserRepo);

            Console.WriteLine();
            Console.WriteLine("Login Menu.");
            Console.Write("Please enter in your email: ");
            var email = (Console.ReadLine() ?? string.Empty).Trim();

            Console.Write("Please enter in your password: ");
            var password = Console.ReadLine() ?? string.Empty;

            var result = login.HandleAsync(new LoginRequest(email, password)).Result;
            if (result is null)
            {
                Console.WriteLine("Login failed. Please check your credentials.");
                Pause();
                return;
            }

            var firstName = ExtractFirstName(result.Name);
            Console.WriteLine($"Welcome back {firstName}.");
            Pause();

            // For now we route to Traveller menu; later you can branch by actual type.
            TravellerMenu(result.UserId);
        }

        private static string ExtractFirstName(string fullName)
        {
            var trimmed = (fullName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed)) return "User";
            var i = trimmed.IndexOf(' ');
            return i < 0 ? trimmed : trimmed[..i];
        }

        // ================== TRAVELLER MENU ==================

        private static void TravellerMenu(Guid userId)
        {
            bool inSession = true;
            while (inSession)
            {
                Console.WriteLine();
                Console.WriteLine("Traveller Menu.");
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. See my details.");
                Console.WriteLine("2. Change password.");
                Console.WriteLine("3. Book an arrival flight.");
                Console.WriteLine("4. Book a departure flight.");
                Console.WriteLine("5. See flight details.");
                Console.WriteLine("6. Logout.");
                Console.Write("Please enter a choice between 1 and 6: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        ShowMyDetails(userId);
                        break;
                    case "2":
                        ChangePassword(userId);
                        break;
                    case "3":
                    case "4":
                    case "5":
                        Console.WriteLine("Feature not yet available.");
                        Pause();
                        break;
                    case "6":
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

            Console.WriteLine();
            Console.WriteLine($"Name  : {user.Name}");
            Console.WriteLine($"Age   : {user.Age}");
            Console.WriteLine($"Email : {user.Email}");
            Console.WriteLine($"Mobile: {user.Mobile}");
            Pause();
        }

        private static void ChangePassword(Guid userId)
        {
            var user = UserRepo.GetByIdAsync(userId).Result;
            if (user is null)
            {
                Console.WriteLine("User not found.");
                Pause();
                return;
            }

            // Current password check
            Console.Write("Please enter your current password:");
            var current = Console.ReadLine() ?? string.Empty;

            if ((user.PasswordHash ?? string.Empty) != current)
            {
                Console.WriteLine("Error: Incorrect password.");
                Pause();
                return;
            }

            // New password prompt 
            Console.Write("Please enter your new password:");
            var newPassword = Console.ReadLine() ?? string.Empty;

            // Check if new password is the same as the old one
            if (newPassword == user.PasswordHash)
            {
                Console.WriteLine("Password already in use.");
                Pause();
                return;
            }

            // Validate new password
            if (!Validation.IsValidPassword(newPassword))
            {
                Console.WriteLine("Error: Invalid password.");
                Pause();
                return;
            }

            // Update
            user.UpdatePassword(newPassword);
            UserRepo.UpdateAsync(user).Wait();

            // Success
            Console.WriteLine("Password changed successfully.");
            Pause();
        }

        // ================== REGISTRATION ==================

        private static void RegisterUserMenu()
        {
            var useCase = new RegisterUserUseCase(UserRepo);

            Console.WriteLine();
            Console.WriteLine("Which user type would you like to register?");
            Console.WriteLine("1. A standard traveller.");
            Console.WriteLine("2. A frequent flyer.");
            Console.WriteLine("3. A flight manager.");
            Console.Write("Please enter a choice between 1 and 3: ");
            var choice = Console.ReadLine();

            Console.WriteLine();
            switch (choice)
            {
                case "1":
                    Console.WriteLine("Registering as a traveller.");
                    RegisterTraveller(useCase);
                    break;
                case "2":
                    Console.WriteLine("Registering as a frequent flyer.");
                    RegisterFrequentFlyer(useCase);
                    break;
                case "3":
                    Console.WriteLine("Registering as a flight manager.");
                    RegisterManager(useCase);
                    break;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }

        private static void RegisterTraveller(RegisterUserUseCase useCase)
        {
            var (name, age, mobile, email) = PromptCommonRegistrationFields();
            var password = PromptPasswordWithRules();

            if (!ValidateCommon(name, age, mobile, email, password)) return;

            var result = useCase.RegisterTraveller(new RegisterTravellerRequest(name, age, email, mobile, password));
            var firstName = ExtractFirstName(name);
            Console.WriteLine(result is null
                ? "Error: Email already exists."
                : $"Congratulations {firstName}. You have registered as a traveller.");
        }

        private static void RegisterFrequentFlyer(RegisterUserUseCase useCase)
        {
            var (name, age, mobile, email) = PromptCommonRegistrationFields();
            var password = PromptPasswordWithRules();

            Console.Write("Please enter in your Frequent Flyer Number: ");
            int.TryParse(Console.ReadLine(), out var ffNumber);

            Console.Write("Please enter in your Frequent Flyer Points: ");
            int.TryParse(Console.ReadLine(), out var ffPoints);

            if (!ValidateCommon(name, age, mobile, email, password)) return;
            if (ffNumber < 100000 || ffNumber > 999999) { Console.WriteLine("Error: Invalid frequent flyer number."); return; }
            if (ffPoints < 0 || ffPoints > 1_000_000) { Console.WriteLine("Error: Invalid frequent flyer points."); return; }

            var result = useCase.RegisterFrequentFlyer(
                new RegisterFrequentFlyerRequest(name, age, email, mobile, password, ffNumber, ffPoints));

            var firstName = ExtractFirstName(name);
            Console.WriteLine(result is null
                ? "Error: Email already exists."
                : $"Congratulations {firstName}. You have registered as a frequent flyer.");
        }

        private static void RegisterManager(RegisterUserUseCase useCase)
        {
            var (name, age, mobile, email) = PromptCommonRegistrationFields();
            var password = PromptPasswordWithRules();

            Console.Write("Please enter in your Staff ID: ");
            var staffId = (Console.ReadLine() ?? "").Trim();

            if (!ValidateCommon(name, age, mobile, email, password)) return;
            if (string.IsNullOrWhiteSpace(staffId)) { Console.WriteLine("Error: Invalid staff ID."); return; }

            var result = useCase.RegisterManager(
                new RegisterManagerRequest(name, age, email, mobile, password, staffId));

            var firstName = ExtractFirstName(name);
            Console.WriteLine(result is null
                ? "Error: Email already exists."
                : $"Congratulations {firstName}. You have registered as a flight manager.");
        }

        private static (string name, int age, string mobile, string email) PromptCommonRegistrationFields()
        {
            Console.Write("Please enter in your name: ");
            var name = (Console.ReadLine() ?? "").Trim();

            Console.Write("Please enter in your age between 0 and 99: ");
            int.TryParse(Console.ReadLine(), out var age);

            Console.Write("Please enter in your mobile number: ");
            var mobile = (Console.ReadLine() ?? "").Trim();

            Console.Write("Please enter in your email: ");
            var email = (Console.ReadLine() ?? "").Trim();

            return (name, age, mobile, email);
        }

        private static string PromptPasswordWithRules()
        {
            Console.WriteLine("Please enter in your password:");
            Console.WriteLine("Your password must:");
            Console.WriteLine("- be at least 8 characters long");
            Console.WriteLine("- contain a number");
            Console.WriteLine("- contain a lowercase letter");
            Console.WriteLine("- contain an uppercase letter");

            while (true)
            {
                var pw = Console.ReadLine() ?? string.Empty;
                if (Validation.IsValidPassword(pw))
                    return pw;

                Console.WriteLine("Error: Invalid password.");
                Console.WriteLine("Please enter in your password:");
            }
        }

        private static bool ValidateCommon(string name, int age, string mobile, string email, string password)
        {
            if (!Validation.IsValidName(name)) { Console.WriteLine("Error: Invalid name."); return false; }
            if (!Validation.IsValidAge(age)) { Console.WriteLine("Error: Invalid age."); return false; }
            if (!Validation.IsValidMobile(mobile)) { Console.WriteLine("Error: Invalid mobile."); return false; }
            if (!Validation.IsValidEmail(email)) { Console.WriteLine("Error: Invalid email."); return false; }
            if (!Validation.IsValidPassword(password)) { Console.WriteLine("Error: Invalid password."); return false; }
            return true;
        }

        // ================== Helpers ==================

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
