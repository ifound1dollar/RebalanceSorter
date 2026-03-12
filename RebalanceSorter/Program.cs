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
            List<StationData> stations = [];
            List<UnitData> units = [];

            PrintMessage("Manual data entry? y/n : ", ConsoleColor.Yellow, newLine: false);
            string? input = Console.ReadLine();
            Console.WriteLine();

            if (string.IsNullOrEmpty(input) || input.Trim() != "y")
            {
                stations = GenerateStations();
                units = GenerateUnits();

                Console.WriteLine("Station count: " + stations.Count);
                PrintUnits(units);
            }
            else
            {
                try
                {
                    Console.WriteLine("Enter the patient count for all 21 units, separated by a space. Leave empty to generate random data.");
                    Console.Write("> ");
                    input = Console.ReadLine();

                    // Split input and try to parse into int[].
                    string[] inputSplit = input.Split(' ');
                    int[] patientsPerUnit = new int[21];
                    for (int i = 0; i < patientsPerUnit.Length; i++)
                    {
                        patientsPerUnit[i] = int.Parse(inputSplit[i]);

                        if (patientsPerUnit[i] > 24 || patientsPerUnit[i] < 0)
                        {
                            throw new Exception("A unit can only have between 0-24 units (inclusive).");
                        }
                    }

                    // If we have a valid int[], generate UnitData and associated PatientData for use.
                    for (int i = 0; i < patientsPerUnit.Length; i++)
                    {
                        units.Add(new UnitData()
                        {
                            UnitID = $"UNIT_{i + 1}",
                            Patients = GeneratePatients(patientsPerUnit[i])
                        });
                    }

                    // Now that we have patient data and units, retrieve stations from user.
                    Console.WriteLine("Enter the available screens for each of the 8 stations. Enter 0 if not in use.");
                    Console.Write("> ");
                    input = Console.ReadLine();

                    inputSplit = input.Split(' ');
                    int[] screensPerStation = new int[8];
                    for (int i = 0; i < screensPerStation.Length; i++)
                    {
                        screensPerStation[i] = int.Parse(inputSplit[i]);

                        if (screensPerStation[i] > 5 || screensPerStation[i] < 0)
                        {
                            throw new Exception("A station can only have between 0-5 available screens (inclusive).");
                        }    
                    }

                    // If we have valid int[], generate StationData.
                    for (int i = 0; i < screensPerStation.Length; i++)
                    {
                        if (screensPerStation[i] == 0) continue;

                        stations.Add(new StationData()
                        {
                            StationID = $"STATION_{i + 1}",
                            ScreensAvailable = screensPerStation[i],
                            Units = []
                        });
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }

            stations = BalanceStations(stations, units);
            PrintStations(stations);
        }





        private static List<StationData> BalanceStations(List<StationData> stations, List<UnitData> units)
        {
            int stationsCount = stations.Count;
            PrintMessage($"\nBALANCING INTO {stationsCount} STATIONS", ConsoleColor.White, newLine: true);

            // Calculate how many screens are required and how many are available.
            int screensRequired = 0;
            foreach (UnitData unit in units)
            {
                screensRequired += (unit.PatientCount > 16) ? 2 : 1;    // 2 screens if > 16 patients
            }
            int screensAvailable = stations.Sum(s => s.ScreensAvailable);

            // Display screens required vs. available, logging error and returning if there are not enough.
            Console.WriteLine("{0} screens are required for the total number of patients in each unit. Screens available: {1}",
                screensRequired, screensAvailable);
            if (screensRequired > screensAvailable)
            {
                PrintMessage("[ERROR]: There are not enough screens available for all units. Some screens must be combined.",
                    ConsoleColor.Red, newLine: true);
                return stations;
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

            // Store our target sum and display it. This is our goal sum for each station to get as close to as possible.
            double targetSum = (units.Sum(u => u.PatientCount) / (double)stationsCount);
            PrintMessage($"\tTarget sum: {targetSum:f2}", ConsoleColor.Green, newLine: true);

            // Sort bins in descending order, then iterate over them and add each unit to the currently-smallest station.
            List<UnitData> sortedUnits = units.OrderByDescending(u => u.PatientCount).ToList();
            for (int index = 0; index < sortedUnits.Count; index++)
            {
                // First, sort our stations by current sum, putting the smallest first. We want to add the largest units
                //  to our emptiest stations.
                stations = stations.OrderBy(s => s.Units.Sum(u => u.PatientCount)).ToList();

                // Find the first station with enough room for the current unit. Units with >16 patients require 2, else 1.
                int spaceNeeded = (sortedUnits[index].PatientCount > 16) ? 2 : 1;
                int firstWithRoom = FindFirstWithRoom(stations, spaceNeeded);

                // Finally, once we have the index of our first smallest station with room, we add the unit to the station.
                stations[firstWithRoom].Units.Add(sortedUnits[index]);
            }

            return stations;
        }

        private static int FindFirstWithRoom(List<StationData> sortedStations, int screensNeeded = 1)
        {
            for (int i = 0; i < sortedStations.Count; i++)
            {
                if (sortedStations[i].ScreensAvailable - sortedStations[i].UnitsCount >= screensNeeded) return i;
            }

            // Logically, should never wind up here. If so, return first index.
            return 0;
        }



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
