using Airport.Application;
using Airport.Domain;
using Airport.Infrastructure;

namespace Airport.Cli

{
    internal static class Program
    {
        // Shared in-memory repository 
        private static readonly IRepository<User> UserRepo =
            new InMemoryRepository<User>(u => u.Id);
        
        private static readonly IRepository<Booking> BookingRepo =
            new InMemoryRepository<Booking>(b => b.Id);
        
        private static readonly IRepository<Flight> FlightRepo =
            new InMemoryRepository<Flight>(f => f.Id);

        private static void Main()
        {
            PrintBanner();
            bool running = true;
            while (running)
            {
                SafeClear();

        // ================== MAIN MENU ==================
                Console.WriteLine();
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. Login as a registered user.");
                Console.WriteLine("2. Register as a new user.");
                Console.WriteLine("3. Exit.");
                Console.WriteLine("Please enter a choice between 1 and 3:");
                var input = ReadLineOrExit();

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
            Console.WriteLine("Login Menu.");
            // If no users exist yet
            if (!UserRepo.ListAsync().Result.Any())
            {
                Console.WriteLine("#####");
                Console.WriteLine("# Error - There are no people registered.");
                Console.WriteLine();
                Console.WriteLine("#####");
                return;
            }
        
            // ask for email
            string email;
            while (true)
            {
                Console.WriteLine("Please enter in your email:");
                email = (ReadLineOrExit() ?? "").Trim();

                if (!Validation.IsValidEmail(email))
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Supplied email is invalid.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    continue;
                }

                // check if it's registered
                var existingUser = UserRepo.ListAsync().Result
                    .FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (existingUser == null)
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Email is not registered.");
                    Console.WriteLine();
                    Console.WriteLine("#####");
                    continue;
                }

                break;   // valid + registered email
            }

            // Prompt for password with retry loop
            string password;
            while (true)
            {
                Console.WriteLine("Please enter in your password:");
                password = ReadLineOrExit() ?? string.Empty;

                // Check password format first
                if (!Validation.IsValidPassword(password))
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Supplied password is invalid.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    continue;
                }

                // Check correct password for that email
                var login = new LoginUseCase(UserRepo);
                var result = login.HandleAsync(new LoginRequest(email, password)).Result;
                if (result == null)
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Incorrect Password.");
                    Console.WriteLine();
                    Console.WriteLine("#####");
                    continue;
                }

                // Successful login
                Console.WriteLine($"Welcome back {result.Name}.");

                // Route by user type
                var user = UserRepo.GetByIdAsync(result.UserId).Result;
                if (user is FlightManager)
                    FlightManagerMenu(result.UserId);
                else if (user is FrequentFlyer)
                    FrequentFlyerMenu(result.UserId);
                else
                    TravellerMenu(result.UserId);

                break;
            }
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
                Console.WriteLine("Please enter a choice between 1 and 6:");
                var choice = ReadLineOrExit();

                switch (choice)
                {
                    case "1":
                        ShowMyDetails(userId);
                        break;
                    case "2":
                        ChangePassword(userId);
                        break;
                    case "3":
                        BookArrivalFlightCli(userId);
                        break;
                    case "4":
                        BookDepartureFlightCli(userId);
                        break;
                    case "5":
                        ShowMyTicketsCli(userId);
                        break;
                    case "6":
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
            if (user is null) return;

            Console.WriteLine();
            Console.WriteLine("Your details.");
            Console.WriteLine($"Name: {user.Name}");
            Console.WriteLine($"Age: {user.Age}");
            Console.WriteLine($"Mobile phone number: {user.Mobile}");
            Console.WriteLine($"Email: {user.Email}");

            // Frequent Flyer extras
            if (user is FrequentFlyer ff)
            {
                Console.WriteLine($"Frequent flyer number: {ff.FrequentFlyerNumber}");
                Console.WriteLine($"Frequent flyer points: {ff.Points:N0}");
            }

            // Flight Manager extra
            if (user is FlightManager fm)
            {
                Console.WriteLine($"Staff ID: {fm.StaffId}");
            }

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

            // Loop until CURRENT password matches
            while (true)
            {
                Console.WriteLine("Please enter your current password.");
                var current = ReadLineOrExit() ?? string.Empty;

                if ((user.PasswordHash ?? string.Empty) == current)
                    break; 

                Console.WriteLine("#####");
                Console.WriteLine("# Error - Entered password does not match existing password.");
                Console.WriteLine("# Please try again.");
                Console.WriteLine("#####");
                // loop and ask for current password again
            }

            // NEW password step 
            while (true)
            {
                Console.WriteLine("Please enter your new password.");
                var newPassword = ReadLineOrExit() ?? string.Empty;

                // not allowed to reuse the same password
                if (newPassword == user.PasswordHash)
                {
                    Console.WriteLine("####");
                    Console.WriteLine("# Error - Password already used in the past.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("####");
                    continue;
                }

                // enforce password rules
                if (!Validation.IsValidPassword(newPassword))
                {
                    Console.WriteLine("####");
                    Console.WriteLine("# Error - Supplied password is invalid.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("####");
                    continue;
                }

                // success: persist and confirm
                user.UpdatePassword(newPassword);
                UserRepo.UpdateAsync(user).Wait();

                break;
            }
}

        /// Book an ARRIVAL flight 
        private static void BookArrivalFlightCli(Guid userId)
        {
            var lf = new ListFlightsUseCase(FlightRepo);
            var arrivals = lf.HandleAsync().Result
                .Where(f => f.Direction == FlightDirection.Arrival)
                .OrderBy(f => f.ScheduledUtc)
                .ToList();

            // Prevent double arrival bookings for same user
            var myBookings = BookingRepo.ListAsync().Result.Where(b => b.UserId == userId).ToList();
            if (myBookings.Any(b => b.Direction == FlightDirection.Arrival))
            {
                Console.WriteLine("#####");
                Console.WriteLine("# Error - You already have an arrival flight. You can not book another.");
                Console.WriteLine("#####");
                Pause();
                return;
            }

            if (arrivals.Count == 0)
            {
                Console.WriteLine("No arrival flights available.");
                Pause();
                return;
            }

            // Check if user already has a departure flight
            var existingDeparture = myBookings.FirstOrDefault(b => b.Direction == FlightDirection.Departure);

            int idx;

            while (true)
            {
                Console.WriteLine("Please enter the arrival flight:");
                for (int i = 0; i < arrivals.Count; i++)
                {
                    var airlineName = GetAirlineNameFromCode(arrivals[i].AirlineCode);
                    Console.WriteLine($"{i + 1}. Flight {arrivals[i].FlightCode} operated by {airlineName} arriving at {arrivals[i].ScheduledUtc:HH:mm dd/MM/yyyy} from {arrivals[i].City} on plane {arrivals[i].PlaneId}.");
                }
                Console.WriteLine($"Please enter a choice between 1 and {arrivals.Count}:");

                while (!int.TryParse(ReadLineOrExit(), out idx) || idx < 1 || idx > arrivals.Count)
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Supplied value is out of range.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    Console.WriteLine($"Please enter a choice between 1 and {arrivals.Count}:");
                }
                
                var chosenArrival = arrivals[idx - 1];

                // If there's a departure booked, enforce arrival < departure
                if (existingDeparture != null && chosenArrival.ScheduledUtc >= existingDeparture.WhenUtc)
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - The arrival time must be before the departure time.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    continue;
                }

                break; // valid flight selected
            }

            var selectedFlight = arrivals[idx - 1];

            // who is booking?
            var currentUser = UserRepo.GetByIdAsync(userId).Result;
            bool isFrequentFlyer = currentUser is FrequentFlyer;

            bool validSeatChosen = false;
            while (!validSeatChosen)
            {
                // --- Prompt for seat row ---
                Console.WriteLine("Please enter in your seat row between 1 and 10:");
                int seatRow;
                while (!int.TryParse(ReadLineOrExit(), out seatRow) || seatRow < 1 || seatRow > 10)
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Supplied seat row is invalid.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    Console.WriteLine("Please enter in your seat row between 1 and 10:");
                }

                // --- Prompt for seat column ---
                Console.WriteLine("Please enter in your seat column between A and D:");
                string seatCol;
                while (true)
                {
                    seatCol = (ReadLineOrExit() ?? "").Trim().ToUpperInvariant();
                    if (seatCol is "A" or "B" or "C" or "D") break;

                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Supplied seat column is invalid.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    Console.WriteLine("Please enter in your seat column between A and D:");
                }

                var seatForRequest = $"{seatRow}{seatCol}";
                var seatForDisplay = $"{seatRow}:{seatCol}";

                // Re-fetch current bookings for the flight
                var existingBookings = BookingRepo.ListAsync().Result
                .Where(b => b.FlightId == selectedFlight.Id)
                .ToList();

                bool seatAlreadyTaken = existingBookings
                    .Any(b => string.Equals(b.Seat, seatForRequest, StringComparison.OrdinalIgnoreCase));

                if (seatAlreadyTaken && !isFrequentFlyer)
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Seat is already occupied.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    continue;
                    // Loop repeats and asks for row/col again
                }
                else
                {
                    // Seat is free → proceed with booking & exit the loop
                    validSeatChosen = true;

                    var use = new BookArrivalFlightUseCase(UserRepo, FlightRepo, BookingRepo);
                    var res = use.HandleAsync(
                        new BookArrivalRequest(userId, selectedFlight.Id, seatForRequest)
                    ).Result;

                    if (res is null)
                    {
                        Console.WriteLine("Error: Could not complete booking (plane full or another rule).");
                    }
                    else
                    {
                        var (outRow, outCol) = SplitSeat(res.Seat);
                        Console.WriteLine(
                            $"Congratulations. You have booked flight {res.FlightCode} " +
                            $"from {res.City} arriving at {res.WhenUtc:HH:mm dd/MM/yyyy} " +
                            $"and are seated in {outRow}:{outCol}.");
                    }

                    Pause();
                    break;
                }
            }
        }


        /// Book a DEPARTURE flight
        private static void BookDepartureFlightCli(Guid userId)
        {
            var lf = new ListFlightsUseCase(FlightRepo);
            var departures = lf.HandleAsync().Result
                .Where(f => f.Direction == FlightDirection.Departure)
                .OrderBy(f => f.ScheduledUtc)
                .ToList();

            // Block booking a second departure for the same user
            var myBookings = BookingRepo.ListAsync().Result.Where(b => b.UserId == userId).ToList();
            if (myBookings.Any(b => b.Direction == FlightDirection.Departure))
            {
                Console.WriteLine("#####");
                Console.WriteLine("# Error - You already have a departure flight. You can not book another.");
                Console.WriteLine("#####");
                Pause();
                return;
            }

            // prevent booking if no flights
            if (departures.Count == 0)
            {
                Console.WriteLine("No departure flights available.");
                Pause();
                return;
            }

            // PICK DEPARTURE (with time check against existing ARRIVAL)
            int idx;
            while (true)
            {
                Console.WriteLine("Please enter the departure flight:");
                for (int i = 0; i < departures.Count; i++)
                {
                    var airlineName = GetAirlineNameFromCode(departures[i].AirlineCode);
                    Console.WriteLine($"{i + 1}. Flight {departures[i].FlightCode} operated by {airlineName} " +
                                    $"departing at {departures[i].ScheduledUtc:HH:mm dd/MM/yyyy} to {departures[i].City} on plane {departures[i].PlaneId}.");
                }
                Console.WriteLine($"Please enter a choice between 1 and {departures.Count}:");

                while (!int.TryParse(ReadLineOrExit(), out idx) || idx < 1 || idx > departures.Count)
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Supplied value is out of range.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    Console.WriteLine($"Please enter a choice between 1 and {departures.Count}:");
                }

                // TIME ORDER CHECK: if an ARRIVAL exists, this DEPARTURE must be AFTER it
                var chosenDeparture = departures[idx - 1];
                var myArrival = myBookings.FirstOrDefault(b => b.Direction == FlightDirection.Arrival);
                if (myArrival != null && chosenDeparture.ScheduledUtc <= myArrival.WhenUtc)
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - The departure time must be after the arrival time.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    // re-loop to choose another departure
                    continue;
                }

                // valid choice/time ordering OK
                break;
            }

            // we already validated idx and time ordering above
            var selectedFlight = departures[idx - 1];

            // who is booking?
            var currentUser = UserRepo.GetByIdAsync(userId).Result;
            bool isFrequentFlyer = currentUser is FrequentFlyer;

            // find user type once (Traveller vs FrequentFlyer)
            var userObj = UserRepo.GetByIdAsync(userId).Result;
            bool isFrequent = userObj is FrequentFlyer;

            // keep asking for a seat until we get a free one (or FF books and succeeds)
            while (true)
            {
                // --- Prompt for seat row ---
                Console.WriteLine("Please enter in your seat row between 1 and 10:");
                int seatRow;
                while (!int.TryParse(ReadLineOrExit(), out seatRow) || seatRow < 1 || seatRow > 10)
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Supplied seat row is invalid.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    Console.WriteLine("Please enter in your seat row between 1 and 10:");
                }

                // --- Prompt for seat column ---
                Console.WriteLine("Please enter in your seat column between A and D:");
                string seatCol;
                while (true)
                {
                    seatCol = (ReadLineOrExit() ?? "").Trim().ToUpperInvariant();
                    if (seatCol is "A" or "B" or "C" or "D") break;

                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Supplied seat column is invalid.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    Console.WriteLine("Please enter in your seat column between A and D:");
                }

                // --- Seat strings ---
                var seatForRequest = $"{seatRow}{seatCol}";   // send to use case (no colon)
                var seatForDisplay = $"{seatRow}:{seatCol}";  // print only

                // For standard Travellers, block if seat already taken (FFs are allowed; use case handles displacement)
                if (!isFrequent)
                {
                    var existingBookings = BookingRepo.ListAsync().Result
                        .Where(b => b.FlightId == selectedFlight.Id)
                        .ToList();

                    bool seatAlreadyTaken = existingBookings
                        .Any(b => string.Equals(b.Seat, seatForRequest, StringComparison.OrdinalIgnoreCase));

                    if (seatAlreadyTaken && !isFrequentFlyer)
                    {
                        Console.WriteLine("#####");
                        Console.WriteLine("# Error - Seat is already occupied.");
                        Console.WriteLine("# Please try again.");
                        Console.WriteLine("#####");
                        // loop continues and re-prompts row/col
                        continue;
                    }
                }

                // --- Attempt booking ---
                var use = new BookDepartureFlightUseCase(UserRepo, FlightRepo, BookingRepo);
                var res = use.HandleAsync(new BookDepartureRequest(userId, selectedFlight.Id, seatForRequest)).Result;

                if (res is null)
                {
                    Console.WriteLine("Error: Could not complete booking (seat taken, plane full, or other rule).");
                    Pause();
                    return;
                }

                // success: show the actual seat assigned (may differ when FF displaced a traveller)
                var (outRow, outCol) = SplitSeat(res.Seat);
                Console.WriteLine(
                    $"Congratulations. You have booked flight {res.FlightCode} to {res.City} " +
                    $"departing at {res.WhenUtc:HH:mm dd/MM/yyyy} and are seated in {outRow}:{outCol}.");
                Pause();
                break; // done
            }
        }


        /// Show User's tickets 
        private static void ShowMyTicketsCli(Guid userId)
        {
            var use = new ListMyTicketsUseCase(BookingRepo);
            var tickets = use.HandleAsync(userId).Result;

            if (tickets.Count == 0) { Console.WriteLine("You have no tickets."); Pause(); return; }

            // Load user once to print header + frequent flag
            var userObj = UserRepo.GetByIdAsync(userId).Result;
            var isFrequent = userObj is FrequentFlyer;
            var displayName = (userObj as User)?.Name ?? "User";

            Console.WriteLine($"Showing flight details for {displayName}:");
            Console.WriteLine();

            foreach (var t in tickets)
            {
                // seat as row:column (e.g., 8:B)
                var (row, col) = SplitSeat(t.Seat);

                if (t.Direction == FlightDirection.Arrival)
                {
                    Console.WriteLine(
                        $"Arrival Flight: Flight {t.FlightCode} from {t.City} " +
                        $"arriving at {t.WhenUtc:HH:mm dd/MM/yyyy} in seat {row}:{col}.");
                }
                else
                {
                    Console.WriteLine(
                        $"Departure Flight: Flight {t.FlightCode} to {t.City} " +
                        $"departing at {t.WhenUtc:HH:mm dd/MM/yyyy} in seat {row}:{col}.");
                }
            }

            Pause();
        }

        // ================== FREQUENT FLYER MENU ==================
        private static void FrequentFlyerMenu(Guid userId)
        {
            bool inSession = true;
            while (inSession)
            {
                Console.WriteLine();
                Console.WriteLine("Frequent Flyer Menu.");
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. See my details.");
                Console.WriteLine("2. Change password.");
                Console.WriteLine("3. Book an arrival flight.");
                Console.WriteLine("4. Book a departure flight.");
                Console.WriteLine("5. See flight details.");
                Console.WriteLine("6. See frequent flyer points.");
                Console.WriteLine("7. Logout.");
                Console.WriteLine("Please enter a choice between 1 and 7:");
                var choice = ReadLineOrExit();

                switch (choice)
                {
                    case "1":
                        ShowMyDetails(userId);
                        break;
                    case "2":
                        ChangePassword(userId);
                        break;
                    case "3":
                        BookArrivalFlightCli(userId);
                        break;
                    case "4":
                        BookDepartureFlightCli(userId);
                        break;
                    case "5":
                        ShowMyTicketsCli(userId);
                        break;
                    case "6":
                        ShowFrequentFlyerPointsCli(userId);
                        break;
                    case "7":
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
   
        /// Shows current FF points, the points from the user's booked arrival and departure flights,
        /// and the new total after both flights are completed. Matches the example output style.
        private static void ShowFrequentFlyerPointsCli(Guid userId)
        {
            var userObj = UserRepo.GetByIdAsync(userId).Result;
            if (userObj is not FrequentFlyer ff)
            {
                Console.WriteLine("This option is only available to frequent flyers.");
                Pause();
                return;
            }

            // Find the user's booked arrival/departure 
            var myBookings = BookingRepo.ListAsync().Result.Where(b => b.UserId == userId).ToList();
            var myArrival   = myBookings.FirstOrDefault(b => b.Direction == FlightDirection.Arrival);
            var myDeparture = myBookings.FirstOrDefault(b => b.Direction == FlightDirection.Departure);

            // Lookup points by city using the assignment table
            int arrivalPts = 0;
            int departurePts = 0;
            bool hasArrival = false, hasDeparture = false;

            if (myArrival != null && AirportRules.CityPoints.TryGetValue(myArrival.City, out var ap))
            {
                arrivalPts = ap;
                hasArrival = true;
            }
            
            if (myDeparture != null && AirportRules.CityPoints.TryGetValue(myDeparture.City, out var dp))
            {
                departurePts = dp;
                hasDeparture = true;
            }

            // Format numbers with thousands separators 
            string F(int n) => n.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

            // Always show current points
            Console.WriteLine($"Your current points are: {F(ff.Points)}.");

            // Only show these if applicable
            if (hasArrival)
                Console.WriteLine($"Your points from your arrival flight will be : {F(arrivalPts)}.");
            if (hasDeparture)
                Console.WriteLine($"Your points from your departure flight will be: {F(departurePts)}.");

            // Only show new total if at least one flight exists
            if (hasArrival || hasDeparture)
            {
                var newTotal = ff.Points + arrivalPts + departurePts;
                var flightWord = (hasArrival && hasDeparture) ? "flights" : "flight";

                Console.WriteLine($"After completing your {flightWord} your new points will be: {F(newTotal)}.");
            }

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
                var choice = ReadLineOrExit();

                switch (choice)
                {
                    case "1":
                        ShowMyDetails(userId);
                        break;
                    case "2":
                        ChangePassword(userId);
                        break;
                    case "3":
                        RegisterArrivalFlightCli();
                        Pause();
                        break;
                    case "4":
                        RegisterDepartureFlightCli();
                        Pause();
                        break;
                    case "5":
                        DelayArrivalFlightCli();
                        Pause();
                        break;
                    case "6":
                        DelayDepartureFlightCli();
                        Pause();
                        break;
                    case "7":
                        ListFlightsCli();
                        Pause();
                        break;
                    case "8":
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
            var dtText = ReadLineOrExit() ?? "";
            if (!TryParseExactLocal(dtText, out var when))
            {
                Console.WriteLine("Error: Invalid date/time.");
                return;
            }

            // Ensure flight isn't already assigned
            if (PlaneAlreadyAssigned(planeId, out var assignedType))
            {
                var article = assignedType == "arrival" ? "an" : "a";
                Console.WriteLine("#####");
                Console.WriteLine($"# Error - Plane {planeId} has already been assigned to {article} {assignedType} flight.");
                Console.WriteLine("#####");
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
            var cityIdx = PromptMenuChoice("Please enter the arrival city:", CitiesMenu) - 1;
            var city = CitiesMenu[cityIdx];

            // Flight id number (100–900) -> FlightCode = ABC{num}
            var flightNum = PromptNumberInRange("Please enter in your flight id between 100 and 900:", 100, 900);
            var flightCode = $"{airlineCode}{flightNum}";

            // Plane id digit (0–9) -> PlaneId = ABC{digit}D  (D for departure)
            var planeDigit = PromptNumberInRange("Please enter in your plane id between 0 and 9:", 0, 9);
            var planeId = $"{airlineCode}{planeDigit}D";

            // Date/time
            Console.WriteLine("Please enter in the departure date and time in the format HH:mm dd/MM/yyyy:");
            var dtText = ReadLineOrExit() ?? "";
            if (!TryParseExactLocal(dtText, out var when))
            {
                Console.WriteLine("Error: Invalid date/time.");
                return;
            }

            // Ensure flight isn't already assigned
            if (PlaneAlreadyAssigned(planeId, out var assignedType))
            {
                var article = assignedType == "arrival" ? "an" : "a";
                Console.WriteLine("#####");
                Console.WriteLine($"# Error - Plane {planeId} has already been assigned to {article} {assignedType} flight.");
                Console.WriteLine("#####");
                return;
            }

            // Use case
            var res = use.HandleAsync(new RegisterDepartureFlightRequest(
                airlineCode, flightCode, city, planeId, when)).Result;

            Console.WriteLine(res is null
                ? "Error: Invalid input or duplicate plane ID."
                : $"Flight {flightCode} on plane {planeId} has been added to the system.");
        }

        /// Delay ARRIVAL Flight
        private static void DelayArrivalFlightCli()
        {
            var listUse = new ListFlightsUseCase(FlightRepo);
            var flights = listUse.HandleAsync().Result
                .Where(f => f.Direction == FlightDirection.Arrival)
                .OrderBy(f => f.ScheduledUtc)
                .ToList();

            if (flights.Count == 0)
            {
                Console.WriteLine("The airport does not have any arrival flights.");
                return;
            }

            Console.WriteLine("Please enter the arrival flight:");
            for (int i = 0; i < flights.Count; i++)
                Console.WriteLine($"{i + 1}. {FormatArrivalLine(flights[i])}");
            Console.WriteLine($"Please enter a choice between 1 and {flights.Count}:");

            int index;
            while (!int.TryParse(ReadLineOrExit(), out index) || index < 1 || index > flights.Count)
            {
                Console.WriteLine("Invalid choice.");
                Console.WriteLine($"Please enter a choice between 1 and {flights.Count}:");
            }

            var chosen = flights[index - 1];

            // Delay minutes
            Console.WriteLine("Please enter in your minutes delayed:");
            int minutes;
            while (!int.TryParse(ReadLineOrExit(), out minutes) || minutes <= 0)
            {
                Console.WriteLine("Error: Invalid number.");
                Console.WriteLine("Please enter in your minutes delayed:");
            }

            // Run use case
            var delayUse = new DelayFlightUseCase(FlightRepo, BookingRepo);
            var res = delayUse.HandleAsync(
                new DelayFlightRequest(chosen.FlightCode, TimeSpan.FromMinutes(minutes))
            ).Result;

            if (res is null)
            {
                Console.WriteLine("Error: Flight not found or invalid input.");
                return;
            }
}

        /// Delay DEPARTURE flight only 
        private static void DelayDepartureFlightCli()
        {
            var listUse = new ListFlightsUseCase(FlightRepo);
            var flights = listUse.HandleAsync().Result
                .Where(f => f.Direction == FlightDirection.Departure)
                .OrderBy(f => f.ScheduledUtc)
                .ToList();

            if (flights.Count == 0)
            {
                Console.WriteLine("The airport does not have any departure flights.");
                return;
            }

            Console.WriteLine("Please enter the departure flight:");
            for (int i = 0; i < flights.Count; i++)
                Console.WriteLine($"{i + 1}. {FormatDepartureLine(flights[i])}");
            Console.WriteLine($"Please enter a choice between 1 and {flights.Count}:");

            int index;
            while (!int.TryParse(ReadLineOrExit(), out index) || index < 1 || index > flights.Count)
            {
                Console.WriteLine("Invalid choice.");
                Console.WriteLine($"Please enter a choice between 1 and {flights.Count}:");
            }

            var chosen = flights[index - 1];

            Console.WriteLine("Please enter in your minutes delayed:");
            int minutes;
            while (!int.TryParse(ReadLineOrExit(), out minutes) || minutes <= 0)
            {
                Console.WriteLine("Error: Invalid number.");
                Console.WriteLine("Please enter in your minutes delayed:");
            }

            var delayUse = new DelayFlightUseCase(FlightRepo, BookingRepo);
            var res = delayUse.HandleAsync(
                new DelayFlightRequest(chosen.FlightCode, TimeSpan.FromMinutes(minutes))
            ).Result;

            if (res is null)
            {
                Console.WriteLine("Error: Flight not found or invalid input.");
                return;
            }

        }

        /// List flights
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
                Console.WriteLine("There are no arrival flights.");
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
                Console.WriteLine("There are no departure flights.");
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
            Console.WriteLine("Please enter a choice between 1 and 3:");
            var choice = ReadLineOrExit();

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
            string name = PromptValidName();
            int age = PromptValidAge();
            string mobile = PromptValidMobile();
            string email = PromptValidEmail();
            var password = PromptValidPasswordWithRules();

            var result = useCase.RegisterTraveller(
                new RegisterTravellerRequest(name, age, email, mobile, password)
            );

            Console.WriteLine($"Congratulations {name}. You have registered as a traveller.");
        }

        // ================== REGISTER FREQUENT FLYER ==================
        private static void RegisterFrequentFlyer(RegisterUserUseCase useCase)
        {
            string name = PromptValidName();
            int age = PromptValidAge();
            string mobile = PromptValidMobile();
            string email = PromptValidEmail();
            var password = PromptValidPasswordWithRules();

            int ffNumber = PromptValidFrequentFlyerNumber();
            int ffPoints = PromptValidFrequentFlyerPoints();

            var result = useCase.RegisterFrequentFlyer(
                new RegisterFrequentFlyerRequest(name, age, email, mobile, password, ffNumber, ffPoints));

            Console.WriteLine($"Congratulations {name}. You have registered as a frequent flyer.");
        }

        // ================== REGISTER FLIGHT MANAGER ==================
        private static void RegisterManager(RegisterUserUseCase useCase)
        {
            string name = PromptValidName();
            int age = PromptValidAge();
            string mobile = PromptValidMobile();
            string email = PromptValidEmail();
            var password = PromptValidPasswordWithRules();
            int staffId = PromptValidStaffId();

            var result = useCase.RegisterManager(
                new RegisterManagerRequest(name, age, email, mobile, password, staffId.ToString()));

            Console.WriteLine($"Congratulations {name}. You have registered as a flight manager.");
        }

        // ================== Helpers ==================


        /// COMMON REGISTRATION QUESTIONS 
        // Prints the two-line error block exactly as in the reference outputs.
        private static void PrintError(string message)
        {
            Console.WriteLine($"# Error - {message}");
            Console.WriteLine("# Please try again.");
        }

        /// Prompt loops that re-ask until valid, emitting the error block on failure.
        private static string PromptValidName()
        {
            while (true)
            {
                Console.WriteLine("Please enter in your name:");
                var name = ReadLineOrExit();

                if (Validation.IsValidName(name))
                    return name;
                
                Console.WriteLine("#####");
                PrintError("Supplied name is invalid.");
                Console.WriteLine("#####");
                // loop repeats and reprints the prompt
            }
        }

        private static int PromptValidAge()
        {
            while (true)
            {
                Console.WriteLine("Please enter in your age between 0 and 99:");
                var s = ReadLineOrExit();

                // FIRST: check if it's a number at all
                if (!int.TryParse(s, out var age))
                {
                    Console.WriteLine("#####");
                    PrintError("Supplied value is invalid.");
                    Console.WriteLine("#####");
                    continue;
                }

                // THEN: check if it's a valid age range
                if (Validation.IsValidAge(age))
                {
                    return age;
                }

                Console.WriteLine("#####");
                PrintError("Supplied age is invalid.");
                Console.WriteLine("#####");
            }
        }

        private static string PromptValidMobile()
        {
            while (true)
            {
                Console.WriteLine("Please enter in your mobile number:");
                var mobile = ReadLineOrExit();
                if (Validation.IsValidMobile(mobile))
                    return mobile;
                Console.WriteLine("#####");
                PrintError("Supplied mobile number is invalid.");
                Console.WriteLine("#####");
            }
        }

        private static string PromptValidEmail()
        {
            while (true)
            {
                Console.WriteLine("Please enter in your email:");
                string email = (ReadLineOrExit() ?? "").Trim();

                // Check if valid format
                if (!Validation.IsValidEmail(email))
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Supplied email is invalid.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    continue;
                }

                // Check if already registered
                if (UserRepo.ListAsync().Result.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("#####");
                    Console.WriteLine("# Error - Email already registered.");
                    Console.WriteLine("# Please try again.");
                    Console.WriteLine("#####");
                    continue;
                }

                return email;
            }
        }

        private static string PromptValidPasswordWithRules()
        {
            while (true)
            {
                Console.WriteLine("Please enter in your password:");
                Console.WriteLine("Your password must:");
                Console.WriteLine("-be at least 8 characters long ");
                Console.WriteLine("-contain a number");
                Console.WriteLine("-contain a lowercase letter");
                Console.WriteLine("-contain an uppercase letter");

                var pw = ReadLineOrExit() ?? string.Empty;

                if (Validation.IsValidPassword(pw))
                    return pw;

                Console.WriteLine("#####");
                Console.WriteLine("# Error - Supplied password is invalid.");
                Console.WriteLine("# Please try again.");
                Console.WriteLine("#####");
            }
        }


        private static int PromptValidFrequentFlyerNumber()
        {
            while (true)
            {
                Console.WriteLine("Please enter in your frequent flyer number between 100000 and 999999:");
                if (int.TryParse(ReadLineOrExit(), out int ffNumber) 
                    && ffNumber >= 100000 && ffNumber <= 999999)
                    return ffNumber;

                Console.WriteLine("#####");
                Console.WriteLine("# Error - Supplied frequent flyer number is invalid.");
                Console.WriteLine("# Please try again.");
                Console.WriteLine("#####");
            }
        }

        private static int PromptValidFrequentFlyerPoints()
        {
            while (true)
            {
                Console.WriteLine("Please enter in your current frequent flyer points between 0 and 1000000:");
                if (int.TryParse(ReadLineOrExit(), out int ffPoints)
                    && ffPoints >= 0 && ffPoints <= 1000000)
                    return ffPoints;

                Console.WriteLine("#####");
                Console.WriteLine("# Error - Supplied current frequent flyer points is invalid.");
                Console.WriteLine("# Please try again.");
                Console.WriteLine("#####");
            }
        }
        
        private static int PromptValidStaffId()
        {
            while (true)
            {
                Console.WriteLine("Please enter in your staff id between 1000 and 9000:");
                var input = ReadLineOrExit();
                if (int.TryParse(input, out int staffId) && staffId >= 1000 && staffId <= 9000)
                    return staffId;

                Console.WriteLine("#####");
                Console.WriteLine("# Error - Supplied staff id is invalid.");
                Console.WriteLine("# Please try again.");
                Console.WriteLine("#####");
            }
        }


        // Only clear when running interactively (prevents "The handle is invalid")
        private static void SafeClear()
        {
            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
                Console.Clear();
        }

        // Only pause interactively; never pause during redirected input
        private static void Pause()
        {
            if (Console.IsInputRedirected) return;
            Console.WriteLine("Press Enter to continue.");
            Console.ReadLine();
        }

        // Read a line; if input is exhausted (EOF), exit gracefully for autograder
        private static string ReadLineOrExit()
        {
            var line = Console.ReadLine();
            if (line is null)
                Environment.Exit(0); // stop re-prompt loops when tests end
            return line;
        }

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

        /// Pretty airline name from code (QFA -> Qantas). Falls back to code if unknown.
        private static string GetAirlineNameFromCode(string code)
        {
            var m = AirlinesMenu.FirstOrDefault(a => 
                string.Equals(a.Code, code, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(m.Name) ? code : m.Name;
        }

        /// Formats a single arrival line
        private static string FormatArrivalLine(FlightView f)
        {
            var airlineName = GetAirlineNameFromCode(f.AirlineCode);
            return $"Flight {f.FlightCode} operated by {airlineName} arriving at {f.ScheduledUtc:HH:mm dd/MM/yyyy} from {f.City} on plane {f.PlaneId}.";
        }

        /// Formats a single departure line 
        private static string FormatDepartureLine(FlightView f)
        {
            var airlineName = GetAirlineNameFromCode(f.AirlineCode);
            return $"Flight {f.FlightCode} operated by {airlineName} departing at {f.ScheduledUtc:HH:mm dd/MM/yyyy} to {f.City} on plane {f.PlaneId}.";
        }

        // Returns true if planeId already used on any flight; out 'type' is "arrival" or "departure".
        private static bool PlaneAlreadyAssigned(string planeId, out string type)
        {
            // Pull all flights 
            var lf = new ListFlightsUseCase(FlightRepo);
            var all = lf.HandleAsync().Result;

            var existing = all.FirstOrDefault(f =>
                f.PlaneId.Equals(planeId, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                type = existing.Direction == FlightDirection.Arrival ? "arrival" : "departure";
                return true;
            }

            type = string.Empty;
            return false;
        }

        // bumps a traveller to another seat
        private static (string row, string col) SplitSeat(string seat)
        {
            // Accept "8B" or "8:B"
            if (string.IsNullOrWhiteSpace(seat)) return ("?", "?");
            seat = seat.Trim();
            var colon = seat.IndexOf(':');
            if (colon >= 0) return (seat.Substring(0, colon), seat[(colon + 1)..]);

            // no colon -> last char is the column
            return (seat[..^1], seat[^1..]);
        }


        // pretty
        private static void PrintBanner()
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("=  Welcome to Brisbane Domestic Airport  =");
            Console.WriteLine("==========================================");
        }


    }
}
