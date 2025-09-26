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
        
        private static readonly IRepository<Flight> FlightRepo =
            new InMemoryRepository<Flight>(f => f.Id);

        private static void Main()
        {
            bool running = true;
            while (running)
            {
                Console.Clear();
                PrintBanner();
        // ================== MAIN MENU ==================
                Console.WriteLine();
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. Login as a registered user.");
                Console.WriteLine("2. Register as a new user.");
                Console.WriteLine("3. Exit.");
                Console.WriteLine("Please enter a choice between 1 and 3: ");

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
            Console.WriteLine("Please enter in your email: ");
            var email = (Console.ReadLine() ?? string.Empty).Trim();

            Console.WriteLine("Please enter in your password: ");
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

            // Different menus depending on user type
            var user = UserRepo.GetByIdAsync(result.UserId).Result;
            if (user is FlightManager)
                FlightManagerMenu(result.UserId);
            else
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
                Console.WriteLine("Please enter a choice between 1 and 6: ");

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

        // Standard show my details for every user
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
            Console.WriteLine("Your details.");
            Console.WriteLine($"Name  : {user.Name}");
            Console.WriteLine($"Age   : {user.Age}");
            Console.WriteLine($"Mobile phone number: {user.Mobile}");
            Console.WriteLine($"Email : {user.Email}");
            
            // User type specific details
            switch (user)
            {
             /*   case FrequentFlyer ff:
                    Console.WriteLine($"Frequent Flyer Number: {ff.FrequentFlyerNumber}");
                    Console.WriteLine($"Frequent Flyer Points: {ff.FrequentFlyerPoints}");
                    break; */
                case FlightManager fm:
                    Console.WriteLine($"Staff ID: {fm.StaffId}");
                    break;
            }

            Pause();
        }
        
        // Change Password
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
            Console.WriteLine("Please enter your current password:");
            var current = Console.ReadLine() ?? string.Empty;

            if ((user.PasswordHash ?? string.Empty) != current)
            {
                Console.WriteLine("Error: Incorrect password.");
                Pause();
                return;
            }

            // New password prompt 
            Console.WriteLine("Please enter your new password:");
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

        // ================== FLIGHT MANAGER MENU ==================
        private static void FlightManagerMenu(Guid userId)
        {
            bool inSession = true;
            while (inSession)
            {
                Console.WriteLine();
                Console.WriteLine("Flight Manager Menu.");
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. See my details.");
                Console.WriteLine("2. Change password.");
                Console.WriteLine("3. Create an arrival flight.");
                Console.WriteLine("4. Create a departure flight.");
                Console.WriteLine("5. Delay an arrival flight.");
                Console.WriteLine("6. Delay a departure flight.");
                Console.WriteLine("7. See the details of all flights.");
                Console.WriteLine("8. Logout.");
                Console.WriteLine("Please enter a choice between 1 and 8:");
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
                        RegisterArrivalFlightCli();   // already implemented
                        Pause();
                        break;
                    case "4":
                        RegisterDepartureFlightCli(); // already implemented
                        Pause();
                        break;
                    case "5":
                        DelayArrivalFlightCli();      // stub below
                        Pause();
                        break;
                    case "6":
                        DelayDepartureFlightCli();    // stub below
                        Pause();
                        break;
                    case "7":
                        ListFlightsCli();             // already implemented
                        Pause();
                        break;
                    case "8":
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

        // STUBS
        private static void DelayArrivalFlightCli()
        {
            Console.WriteLine("Feature not yet available.");
            // Next step: ask for FlightCode or PlaneId, ask for delay minutes,
            // call a DelayFlightUseCase that updates the arrival and propagates
            // the same delay to the paired departure (by PlaneId).
        }

        private static void DelayDepartureFlightCli()
        {
            Console.WriteLine("Feature not yet available.");
            // Similar to above but for departures.
        }

        // Create new arrival flight
        private static void RegisterArrivalFlightCli()
        {
            var use = new RegisterArrivalFlightUseCase(FlightRepo);

            // Airline
            var airlineIdx = PromptAirlineChoice() - 1;
            var airlineCode = AirlinesMenu[airlineIdx].Code;

            // City
            var cityIdx = PromptMenuChoice("Please enter the departing city:", CitiesMenu) - 1;
            var city = CitiesMenu[cityIdx];

            // Flight id number (100–900) -> FlightCode = ABC{num}
            var flightNum = PromptNumberInRange("Please enter in your flight id between 100 and 900:", 100, 900);
            var flightCode = $"{airlineCode}{flightNum}";

            // Plane id digit (0–9) -> PlaneId = ABC{digit}A  (A for arrival)
            var planeDigit = PromptNumberInRange("Please enter in your plane id between 0 and 9:", 0, 9);
            var planeId = $"{airlineCode}{planeDigit}A";

            // Date/time
            Console.WriteLine("Please enter in the arrival date and time in the format HH:mm dd/MM/yyyy:");
            var dtText = Console.ReadLine() ?? "";
            if (!TryParseExactLocal(dtText, out var when))
            {
                Console.WriteLine("Error: Invalid date/time.");
                return;
            }

            // Use case
            var res = use.HandleAsync(new RegisterArrivalFlightRequest(
                airlineCode, flightCode, city, planeId, when)).Result;

            Console.WriteLine(res is null
                ? "Error: Invalid input or duplicate plane ID."
                : $"Flight {flightCode} on plane {planeId} has been added to the system.");
        }

        // Create new departure flight
        private static void RegisterDepartureFlightCli()
        {
            var use = new RegisterDepartureFlightUseCase(FlightRepo);

            // Airline
            var airlineIdx = PromptAirlineChoice() - 1;
            var airlineCode = AirlinesMenu[airlineIdx].Code;

            // City
            var cityIdx = PromptMenuChoice("Please enter the arriving city:", CitiesMenu) - 1;
            var city = CitiesMenu[cityIdx];

            // Flight id number (100–900) -> FlightCode = ABC{num}
            var flightNum = PromptNumberInRange("Please enter in your flight id between 100 and 900:", 100, 900);
            var flightCode = $"{airlineCode}{flightNum}";

            // Plane id digit (0–9) -> PlaneId = ABC{digit}D  (D for departure)
            var planeDigit = PromptNumberInRange("Please enter in your plane id between 0 and 9:", 0, 9);
            var planeId = $"{airlineCode}{planeDigit}D";

            // Date/time
            Console.WriteLine("Please enter in the departure date and time in the format HH:mm dd/MM/yyyy:");
            var dtText = Console.ReadLine() ?? "";
            if (!TryParseExactLocal(dtText, out var when))
            {
                Console.WriteLine("Error: Invalid date/time.");
                return;
            }

            // Use case
            var res = use.HandleAsync(new RegisterDepartureFlightRequest(
                airlineCode, flightCode, city, planeId, when)).Result;

            Console.WriteLine(res is null
                ? "Error: Invalid input or duplicate plane ID."
                : $"Flight {flightCode} on plane {planeId} has been added to the system.");
        }

        // List flights
        private static void ListFlightsCli()
        {
            var use = new ListFlightsUseCase(FlightRepo);
            var flights = use.HandleAsync().Result;

            if (flights.Count == 0)
            {
                Console.WriteLine("No scheduled flights.");
                return;
            }

            // Split & sort each group chronologically
            var arrivals = flights
                .Where(f => f.Direction == FlightDirection.Arrival)
                .OrderBy(f => f.ScheduledUtc)
                .ToList();

            var departures = flights
                .Where(f => f.Direction == FlightDirection.Departure)
                .OrderBy(f => f.ScheduledUtc)
                .ToList();

            // --- Arrival flights block ---
            Console.WriteLine();
            Console.WriteLine("Arrival Flights:");
            if (arrivals.Count == 0)
            {
                Console.WriteLine("(none)");
            }
            else
            {
                foreach (var f in arrivals)
                {
                    // Example: Flight JST150 operated by Jetstar arriving at 09:00 01/04/2025 from Sydney on plane JST6A.
                    var airlineName = GetAirlineNameFromCode(f.AirlineCode);
                    Console.WriteLine(
                        $"Flight {f.FlightCode} operated by {airlineName} arriving at {f.ScheduledUtc:HH:mm dd/MM/yyyy} from {f.City} on plane {f.PlaneId}.");
                }
            }

            // --- Departure flights block ---
            Console.WriteLine();
            Console.WriteLine("Departure Flights:");
            if (departures.Count == 0)
            {
                Console.WriteLine("(none)");
            }
            else
            {
                foreach (var f in departures)
                {
                    // Example: Flight QFA251 operated by Qantas departing at 12:00 01/04/2025 to Melbourne on plane QFA3D.
                    var airlineName = GetAirlineNameFromCode(f.AirlineCode);
                    Console.WriteLine(
                        $"Flight {f.FlightCode} operated by {airlineName} departing at {f.ScheduledUtc:HH:mm dd/MM/yyyy} to {f.City} on plane {f.PlaneId}.");
                }
            }
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
            Console.WriteLine("Please enter a choice between 1 and 3: ");
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

        // ================== REGISTER TRAVELLER ==================
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

        // ================== REGISTER FREQUENT FLYER ==================
        private static void RegisterFrequentFlyer(RegisterUserUseCase useCase)
        {
            var (name, age, mobile, email) = PromptCommonRegistrationFields();
            var password = PromptPasswordWithRules();

            Console.WriteLine("Please enter in your Frequent Flyer Number: ");
            int.TryParse(Console.ReadLine(), out var ffNumber);

            Console.WriteLine("Please enter in your Frequent Flyer Points: ");
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

        // ================== REGISTER FLIGHT MANAGER ==================
        private static void RegisterManager(RegisterUserUseCase useCase)
        {
            var (name, age, mobile, email) = PromptCommonRegistrationFields();
            var password = PromptPasswordWithRules();

            Console.WriteLine("Please enter in your Staff ID between 1000 and 9000: ");
            if (!int.TryParse(Console.ReadLine(), out var staffId) || staffId < 1000 || staffId > 9000)
            {
                Console.WriteLine("Error: Invalid staff ID.");
                return;
            }
            if (!ValidateCommon(name, age, mobile, email, password)) return;

            var result = useCase.RegisterManager(
                new RegisterManagerRequest(name, age, email, mobile, password, staffId.ToString()));

            var firstName = ExtractFirstName(name);
            Console.WriteLine(result is null
                ? "Error: Email already exists."
                : $"Congratulations {firstName}. You have registered as a flight manager.");
        }

        // ================== COMMON REGISTRATION QUESTIONS ==================
        private static (string name, int age, string mobile, string email) PromptCommonRegistrationFields()
        {
            Console.WriteLine("Please enter in your name: ");
            var name = (Console.ReadLine() ?? "").Trim();

            Console.WriteLine("Please enter in your age between 0 and 99: ");
            int.TryParse(Console.ReadLine(), out var age);

            Console.WriteLine("Please enter in your mobile number: ");
            var mobile = (Console.ReadLine() ?? "").Trim();

            Console.WriteLine("Please enter in your email: ");
            var email = (Console.ReadLine() ?? "").Trim();

            return (name, age, mobile, email);
        }

        // password rules
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

        // validation
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

        /// Maps menu choices (1–5) to airline names and their codes.
        /// Example: 1 = Jetstar (JST), 2 = Qantas (QFA), etc.
        private static readonly (string Name, string Code)[] AirlinesMenu =
        {
            ("Jetstar", "JST"),
            ("Qantas", "QFA"),
            ("Regional Express", "RXA"),
            ("Virgin", "VOZ"),
            ("Fly Pelican", "FRE")
        };

        /// Supported cities for arrivals and departures.
        /// Maps menu choices (1–5) to city names.
        private static readonly string[] CitiesMenu =
        {
            "Sydney",
            "Melbourne",
            "Rockhampton",
            "Adelaide",
            "Perth"
        };

        /// Prompts the user to enter an integer within a given range (inclusive).
        /// Keeps re-asking until the user provides valid input.
        private static int PromptNumberInRange(string prompt, int min, int max)
        {
            Console.WriteLine(prompt);
            while (true)
            {
                var text = Console.ReadLine() ?? string.Empty;
                if (int.TryParse(text, out var n) && n >= min && n <= max)
                    return n;

                Console.WriteLine("Error: Invalid number.");
                Console.WriteLine(prompt);
            }
        }

        /// Displays a numbered menu of items and ensures a valid selection.
        /// Returns the chosen item’s index (1-based).
        private static int PromptMenuChoice(string title, string[] items)
        {
            Console.WriteLine(title);
            for (int i = 0; i < items.Length; i++)
                Console.WriteLine($"{i + 1}. {items[i]}");
            Console.WriteLine($"Please enter a choice between 1 and {items.Length}:");

            while (true)
            {
                var input = Console.ReadLine() ?? "";
                if (int.TryParse(input, out var n) && n >= 1 && n <= items.Length)
                    return n;

                Console.WriteLine("Invalid choice.");
                Console.WriteLine($"Please enter a choice between 1 and {items.Length}:");
            }
        }

        /// Displays the airline menu and validates a selection (1–5).
        /// Returns the user’s choice as an integer index (1–5).
        private static int PromptAirlineChoice()
        {
            Console.WriteLine("Please enter the airline:");
            for (int i = 0; i < AirlinesMenu.Length; i++)
                Console.WriteLine($"{i + 1}. {AirlinesMenu[i].Name}");
            Console.WriteLine("Please enter a choice between 1 and 5:");

            while (true)
            {
                var input = Console.ReadLine() ?? "";
                if (int.TryParse(input, out var n) && n is >= 1 and <= 5)
                    return n;

                Console.WriteLine("Invalid choice.");
                Console.WriteLine("Please enter a choice between 1 and 5:");
            }
        }

        /// Attempts to parse a date/time string in the expected format: HH:mm dd/MM/yyyy.
        /// Returns true if valid, false otherwise.
        private static bool TryParseExactLocal(string text, out DateTime dt)
        {
            return DateTime.TryParseExact(
                text,
                "HH:mm dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out dt);
        }

        /// Returns the full airline name for a given 3-letter airline code (e.g., "QFA" -> "Qantas").
        /// Falls back to the code if the name is unknown.
        private static string GetAirlineNameFromCode(string code)
        {
            // Reuse the AirlinesMenu we already defined.
            var match = AirlinesMenu.FirstOrDefault(a => 
                string.Equals(a.Code, code, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(match.Name) ? code : match.Name;
        }

        // pretty
        private static void PrintBanner()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("= Welcome to Brisbane Domestic Airport =");
            Console.WriteLine("===========================================");
        }

        private static void Pause()
        {
            Console.WriteLine();
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }
    }
}
