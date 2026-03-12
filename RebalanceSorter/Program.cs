namespace RebalanceSorter
{
    internal class Program
    {
        struct PatientData
        {
            public int PatientID
            {
                get; init;
            }
            public string PatientString
            {
                get; init; 
            }
        }

        struct UnitData
        {
            public string UnitID
            {
                get; init;
            }

            public List<PatientData> Patients
            {
                get; init; 
            }

            public int PatientCount
            {
                get => Patients.Count;
            }
        }

        struct StationData
        {
            public string StationID
            {
                get; init;
            }

            public int ScreensAvailable
            {
                get; init;
            }

            public List<UnitData> Units
            {
                get; init;
            }

            public int UnitsCount
            {
                get => Units.Count;
            }
        }

        static void Main(string[] args)
        {
            List<StationData> stations;
            List<UnitData> units;
            int patientsPerScreen;

            PrintMessage("Manual data entry? y/n : ", ConsoleColor.Yellow, newLine: false);
            string? input = Console.ReadLine();
            Console.WriteLine();

            // Generate random data if no input (null or empty) or is not "y". Else get input from user.
            if (string.IsNullOrEmpty(input) || input.Trim() != "y")
            {
                stations = GenerateStations();
                units = GenerateUnits();
                patientsPerScreen = 16;

                Console.WriteLine("Station count: " + stations.Count);
                PrintUnits(units);
            }
            else
            {
                try
                {
                    patientsPerScreen = GetPatientsPerScreenFromInput();
                    Console.WriteLine();

                    units = GetUnitsFromInput();
                    Console.WriteLine();

                    stations = GetStationsFromInput();
                    Console.WriteLine();

                    PromptUserForReservations(units, stations, patientsPerScreen);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }

            stations = BalanceStations(stations, units, patientsPerScreen);
            PrintStations(stations);
        }





        private static List<StationData> BalanceStations(List<StationData> stations, List<UnitData> units, int patientsPerScreen)
        {
            PrintMessage($"\nBALANCING INTO {stations.Count} STATIONS", ConsoleColor.White, newLine: true);

            // Calculate how many screens are required and how many are available.
            int screensRequired = CalculateTotalScreensRequired(units, stations, patientsPerScreen);
            int screensAvailable = stations.Sum(s => s.ScreensAvailable);

            // Display screens required vs. available, logging error and returning if there are not enough.
            Console.WriteLine("{0} screens are required for the total number of patients in each unit. Screens available: {1}",
                screensRequired, screensAvailable);
            if (screensRequired > screensAvailable)
            {
                PrintMessage("[ERROR]: There are not enough screens available for all units. Exiting.",
                    ConsoleColor.Red, newLine: true);
                Environment.Exit(1);
            }

            // ----- LOGIC EXPLANATION -----
            // We implement a pretty basic algorithm to try and distribute patient load as evenly as possible across the stations
            //  that we have available. This entire system revolves around adding the largest unit to the station with the smallest
            //  current sum, ensuring that the station has room to receive the unit (units with >16 patients take up two screens).
            //  With this algorithm, the targetSum variable is only required for informational purposes. Some more complex
            //  algorithms will conditionall add and swap elements (units) between bins (stations) in order to try and get as close
            //  as possible to this target, but we are effectively just performing a sort of convergence operation with this
            //  relatively simple algorithm (units ascending but stations descending tends to effectively converge near the average).
            // We first sort our units by patient count in descending order so we can grab the largest units first. Then, we
            //  iterate over these units and add each individual unit into the station which currently has the smallest sum. Because
            //  we are iterating over the units in descending order but the stations in ascending order, we should generally
            //  converge somewhere in the middle that does not deviate too far from the average.
            // IMPORTANTLY, this does not compensate for extreme situations where a station has multiple broken monitors or, as
            //  checked above, there are not enough active stations to monitor all existing patients.
            // It also does not (yet) allow multiple units that sum to <=16 to be combined into one screen.
            // ----- END EXPLANATON -----

            // Calculate our target sum and display it. This is our goal sum for each station to get as close to as possible.
            double targetSum = CalculateTargetSum(units, stations);
            PrintMessage($"\tTarget sum: {targetSum:f2}", ConsoleColor.Green, newLine: true);

            // Sort bins in descending order, then iterate over them and add each unit to the currently-smallest station.
            List<UnitData> sortedUnits = units.OrderByDescending(u => u.PatientCount).ToList();
            for (int index = 0; index < sortedUnits.Count; index++)
            {
                // Each iteration, re-sort our stations by current sum of patients, putting the smallest first.
                stations = stations.OrderBy(s => s.Units.Sum(u => u.PatientCount)).ToList();

                // Find the first station with enough room for the current unit.
                int spaceNeeded = CalculateSpaceNeeded(sortedUnits[index].PatientCount, patientsPerScreen);
                int firstWithRoom = FindFirstWithRoom(stations, spaceNeeded);

                // Finally, once we have the index of our first smallest station with room, we add the unit to the station.
                stations[firstWithRoom].Units.Add(sortedUnits[index]);
            }

            return stations;
        }

        #region Private: Algorithm Utility

        private static int FindFirstWithRoom(List<StationData> sortedStations, int spaceNeeded = 1)
        {
            for (int i = 0; i < sortedStations.Count; i++)
            {
                if (sortedStations[i].ScreensAvailable - sortedStations[i].UnitsCount >= spaceNeeded) return i;
            }

            // Logically, should never wind up here. If so, return first index.
            return 0;
        }

        private static double CalculateTargetSum(List<UnitData> units, List<StationData> stations)
        {
            double sum = 0;

            // First, sum remainder of what exists in the units array (any non-reserved units).
            sum = (units.Sum(u => u.PatientCount));

            // Then, iterate over all stations and sum the units within each station.
            foreach (var station in stations)
            {
                sum += (station.Units.Sum(u => u.PatientCount));
            }

            return sum / (double)stations.Count;
        }

        private static int CalculateSpaceNeeded(int count, int patientsPerScreen)
        {
            // We need as many spaces as count/patientsPerScreen, but rounded up.
            return (int)Math.Ceiling((double)count / patientsPerScreen);
        }

        private static int CalculateTotalScreensRequired(List<UnitData> units, List<StationData> stations, int patientsPerScreen)
        {
            int screensRequired = 0;

            // First, get space needed from the units still in the List<UnitData> (non-locked/reserved).
            foreach (UnitData unit in units)
            {
                screensRequired += CalculateSpaceNeeded(unit.PatientCount, patientsPerScreen);
            }

            // Then, iterate over all stations and sum the space needed for each locked/reserved unit.
            foreach (StationData station in stations)
            {
                foreach (var unit in station.Units)
                {
                    screensRequired += CalculateSpaceNeeded(unit.PatientCount, patientsPerScreen);
                }
            }

            return screensRequired;
        }

        #endregion

        #region Private: Input Utility

        private static int GetPatientsPerScreenFromInput()
        {
            string? input;
            int patientsPerScreen = 16;

            PrintMessage("Please enter the number of patients that can fit on one screen.", ConsoleColor.Yellow, newLine: true);
            while (true)
            {
                input = Console.ReadLine();

                // Try to parse input, breaking from loop if valid.
                if (int.TryParse(input, out patientsPerScreen) && patientsPerScreen > 0)
                {
                    break;
                }

                // Else invalid input, so log error and ask for input again.
                PrintMessage("Please enter a positive integer for number of patients per screen.");
            }

            return patientsPerScreen;
        }

        private static List<UnitData> GetUnitsFromInput()
        {
            List<UnitData> units = [];
            string? input;
            int unitCount;

            // Ask the user how many units there are and store to variable.
            PrintMessage("Please enter the number of units to balance. Empty units should be omitted.", ConsoleColor.Yellow, newLine: true);
            while (true)
            {
                input = Console.ReadLine();

                // Try to parse input, breaking from loop and moving on if valid.
                if (int.TryParse(input, out unitCount) && unitCount > 0)
                {
                    break;
                }

                PrintMessage("Please enter a positive integer value.", ConsoleColor.Red, newLine: true);
            }

            // Prompt the user to input the patient count for all 21 units, one at a time.
            PrintMessage("For each unit, please enter the unit name and the number of patients within the unit, separated by a space.",
                ConsoleColor.Yellow, newLine: true);
            for (int i = 0; i < unitCount; i++)
            {
                // Continue prompting for input until valid input is entered.
                while (true)
                {
                    Console.Write($"{i + 1}: ");
                    input = Console.ReadLine();
                    if (input == null)
                    {
                        PrintMessage("Failed to read input, please try again.", ConsoleColor.Red, newLine: true);
                        continue;
                    }

                    // Split input and break into ID and patient count, respectively.
                    string[] inputSplit = input.Split(' ');
                    if (inputSplit.Length != 2)
                    {
                        PrintMessage("Please enter a string and an integer separated by a space.");
                        continue;
                    }

                    // Ensure a unit does not already have this ID.
                    if (units.Any(u => u.UnitID == inputSplit[0]))
                    {
                        PrintMessage("Please enter a unit name that is not already in use.", ConsoleColor.Red, newLine: true);
                        continue;
                    }

                    // Try to parse second input and verify >0.
                    if (!int.TryParse(inputSplit[1], out int value) || value < 0)
                    {
                        PrintMessage("Please enter a positive integer for the patient count for this unit.");
                        continue;
                    }

                    // Input is valid, so create new UnitData and add to List.
                    units.Add(new UnitData()
                    {
                        UnitID = inputSplit[0],
                        Patients = Enumerable.Repeat(new PatientData(), value).ToList() // TODO: Adds empty patient data for now.
                    });
                    break;
                }
            }

            return units;
        }

        private static List<StationData> GetStationsFromInput()
        {
            List<StationData> stations = [];
            string? input;
            int stationCount;

            // Ask the user how many units there are and store to variable.
            PrintMessage("Please enter the number of stations in operation. Stations not in operation should be omitted.",
                ConsoleColor.Yellow, newLine: true);
            while (true)
            {
                input = Console.ReadLine();

                // Try to parse input, breaking from loop and moving on if valid.
                if (int.TryParse(input, out stationCount) && stationCount > 0)
                {
                    break;
                }

                PrintMessage("Please enter a positive integer value.", ConsoleColor.Red, newLine: true);
            }

            // Prompt the user to input the patient count for all 21 units, one at a time.
            PrintMessage("For each station, please enter the station name and the number of available screens, separated by a space.",
                ConsoleColor.Yellow, newLine: true);
            for (int i = 0; i < stationCount; i++)
            {
                // Continue prompting for input until valid input is entered.
                while (true)
                {
                    Console.Write($"{i + 1}: ");
                    input = Console.ReadLine();
                    if (input == null)
                    {
                        PrintMessage("Failed to read input, please try again.", ConsoleColor.Red, newLine: true);
                        continue;
                    }

                    // Split input and break into ID and number of available screens, respectively.
                    string[] inputSplit = input.Split(' ');
                    if (inputSplit.Length != 2)
                    {
                        PrintMessage("Please enter a string and an integer separated by a space.", ConsoleColor.Red, newLine: true);
                        continue;
                    }

                    // Ensure a station does not already have this ID.
                    if (stations.Any(u => u.StationID == inputSplit[0]))
                    {
                        PrintMessage("Please enter a station name that is not already in use.");
                        continue;
                    }

                    // Try to parse second input and verify >0.
                    if (!int.TryParse(inputSplit[1], out int value) || value < 0)
                    {
                        PrintMessage("Please enter a positive integer for the number of screens available.");
                        continue;
                    }

                    // Input is valid, so create new StationData and add to List.
                    stations.Add(new StationData()
                    {
                        StationID = inputSplit[0],
                        ScreensAvailable = value,
                        Units = []
                    });
                    break;
                }
            }

            return stations;
        }

        private static void PromptUserForReservations(List<UnitData> units, List<StationData> stations, int patientsPerScreen)
        {
            string? input = string.Empty;
            int index = 0;

            // Ask the user if they would like to make any locks/reservations. Only allow up to units.Count iterations.
            PrintMessage("Please enter any unit:station locks/reservations. If there are none or to finish submitting, input nothing." +
                "\nFor any locks, enter the unit name and the station name it is locked to, separated by a space.",
                ConsoleColor.Yellow, newLine: true);
            while (index < units.Count)
            {
                Console.Write($"{index + 1}: ");

                input = Console.ReadLine();
                if (input == string.Empty) break;  // Intentionally break when no input.
                if (input == null)
                {
                    PrintMessage("Failed to read input, please try again.", ConsoleColor.Red, newLine: true);
                    continue;
                }

                // Split input and validate two parts.
                string[] inputSplit = input.Split(' ');
                if (inputSplit.Length != 2)
                {
                    PrintMessage("Please enter a unit name and a station name, separated by a space.", ConsoleColor.Red, newLine: true);
                    continue;
                }

                // Try to find matching unit from first input.
                if (!units.Any(u => u.UnitID == inputSplit[0]))
                {
                    PrintMessage("Could not find an existing unit with that name, please try again.", ConsoleColor.Red, newLine: true);
                    continue;
                }
                UnitData unit = units.Find(u => u.UnitID == inputSplit[0]);         // This will succeed since we checked with Any().

                // Try to find matching station from second input.
                if (!stations.Any(s => s.StationID == inputSplit[1]))
                {
                    PrintMessage("Could not find an existing station with that name, please try again.", ConsoleColor.Red, newLine: true);
                    continue;
                }
                StationData station = stations.Find(s => s.StationID == inputSplit[1]);     // Again, this will succeed.

                // We successfully retrieved input for the unit and station, now ensure the station has room for the unit.
                int slotsRequired = CalculateSpaceNeeded(unit.PatientCount, patientsPerScreen);
                if (station.ScreensAvailable - station.UnitsCount < slotsRequired)
                {
                    PrintMessage($"{station.StationID} does not have enough available screens to support {unit.UnitID} (requires {slotsRequired}.",
                        ConsoleColor.Red, newLine: true);
                    continue;
                }

                // After fully validating input, finally add the unit to the station and remove the now-assigned unit from the List.
                station.Units.Add(unit);
                units.RemoveAll(u => u.UnitID == inputSplit[0]);    // We cannot Remove() a by-value struct, so remove by ID.
                index++;                                            // Increment index only when successful.
            }
        }

        #endregion

        #region Private: Random Station / Unit / Patient Generation

        private static List<StationData> GenerateStations(int min = 5, int max = 8)
        {
            List<StationData> stations = [];
            Random random = new();
            int count = random.Next(min, max);

            for (int i = 0; i < count; i++)
            {
                stations.Add(new StationData { StationID = $"STATION_{i + 1}", ScreensAvailable = random.Next(4, 6), Units = [] });
            }

            return stations;
        }

        private static List<UnitData> GenerateUnits(int count = 21)
        {
            List<UnitData> units = [];

            for (int i = 0; i < count; i++)
            {
                units.Add(new UnitData { UnitID = $"UNIT_{i + 1}", Patients = GeneratePatients() });
            }

            return units;
        }

        private static List<PatientData> GeneratePatients()
        {
            List<PatientData> patients = [];
            Random random = new();
            int count = random.Next(1, 17);
            if (random.Next() % 2 == 1)
            {
                // 50/50 chance to append a value of 0-8 to the value.
                count += random.Next(0, 9);
            }

            for (int i = 0; i < count; i++)
            {
                patients.Add(new PatientData { PatientID = random.Next(), PatientString = "a string" });
            }

            return patients;
        }

        private static List<PatientData> GeneratePatients(int count)
        {
            List<PatientData> patients = [];
            Random random = new();

            for (int i = 0; i < count; i++)
            {
                patients.Add(new PatientData { PatientID = random.Next(), PatientString = "a string" });
            }

            return patients;
        }

        #endregion

        #region Private: Print Utility

        private static void PrintUnits(List<UnitData> units)
        {
            foreach (UnitData unit in units)
            {
                Console.WriteLine($"{unit.UnitID} patient count: {unit.PatientCount}");
            }
        }

        private static void PrintStations(List<StationData> stations)
        {
            stations = stations.OrderBy(s => s.StationID).ToList();

            foreach (StationData station in stations)
            {
                PrintMessage(station.StationID, ConsoleColor.White, newLine: false);
                PrintMessage(" | SCREENS AVAILABLE: ", newLine: false);
                PrintMessage(station.ScreensAvailable.ToString(), ConsoleColor.Red, newLine: false);

                PrintMessage(" | UNITS: ", newLine: false);
                Console.ForegroundColor = ConsoleColor.Blue;
                for (int i = 0; i < 5; i++)
                {
                    string text = (i < station.UnitsCount) ? station.Units[i].UnitID : string.Empty;

                    PrintMessage($"{text,7}", ConsoleColor.White, newLine: false);
                    PrintMessage(":", newLine: false);

                    text = (i < station.UnitsCount) ? (station.Units[i].PatientCount.ToString()) : string.Empty;

                    PrintMessage($"{text,-2} ", ConsoleColor.Blue, newLine: false);
                }
                PrintMessage("| ", newLine: false);
                PrintMessage($"TOTAL: {station.Units.Sum(u => u.PatientCount)}", ConsoleColor.Green, newLine: true);
            }
        }

        private static void PrintMessage(string message, ConsoleColor color = ConsoleColor.Gray, bool newLine = true)
        {
            Console.ForegroundColor = color;

            if (newLine)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.Write(message);
            }

            Console.ForegroundColor = ConsoleColor.Gray;
        }

        #endregion

    }
}
