using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace TörnkvistCLI
{
    public interface IMenuOption
    {
        int MenuIdentifier { get; }
        string Description { get; }
        void Execute();
    }
    public class PrintOutHoursLeftToMidnightOption : IMenuOption
    {
        public int MenuIdentifier => 1;
        public string Description => "Hur lång tid är det till midnatt?";
        public void Execute()
        {
            TimeSpan timeLeft = CalcHoursLeftToMidnight();
            Console.WriteLine("Timmarna kvar till midnatt är: "+ timeLeft.Hours +" timmar, "+timeLeft.Minutes+" Minuter och "+timeLeft.Seconds+" Sekunder!");
        }
        private TimeSpan CalcHoursLeftToMidnight()
        {
            DateTime now = DateTime.Now;
            DateTime midnight = now.Date.AddDays(1);
            TimeSpan timeToMidnight = midnight - now;
            return timeToMidnight;
        }
    }
    public class TimeNowOption : IMenuOption
    {
        public int MenuIdentifier => 2;
        public string Description => "Vad är det för Tid och Datum just nu?";
        public void Execute()
        {
            System.Console.WriteLine("Datum och Tid just nu är "+DateTime.Now);
        }
    }
        public class ExitOption : IMenuOption
        {
            public int MenuIdentifier => 4;
            public string Description => "Avsluta Programet";
        
            public void Execute()
            {
                Environment.Exit(0);
            }
        }
    public class FetchDataFromAPI : IMenuOption
    {
        public int MenuIdentifier => 3;
        public string Description => "Hämta data ifrån SMHI API";

        public async void Execute()
        {
            System.Console.WriteLine("Fetching data...");

            var temperature = await FetchWaterTemperatureAsync();
        
             if (temperature.HasValue)
            {
                System.Console.WriteLine($"Aktuell kustvattentemperatur i Varberg: {temperature.Value}°C");
                SaveWaterTemperatureToDB(temperature.Value);
            }
            else
            {
                System.Console.WriteLine("Kunde inte hämta kustvatten temperaturen ifrån SMHI.");
            }

        }

        private void SaveWaterTemperatureToDB(double temperature)
        {
            using (var db = new AppDbContext())
            {
                var waterTemperature = new WaterTemperature
                {
                    Temperature = temperature,
                    Date = DateTime.UtcNow,
                    Location = "Station 61420"
                };
                try
                {
                    db.WaterTemperatures.Add(waterTemperature);
                    db.SaveChanges();
                    System.Console.WriteLine($"Temperatur sparad i databasen");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SQLite Error: {ex.Message}");
                    throw;
                }
            }
        }

        private async Task<double?> FetchWaterTemperatureAsync()
        {
            try
            {
                string url = "https://opendata-download-ocobs.smhi.se/api/version/1.0/parameter/5/station/35133/period/latest-day/data.json";

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonData = await response.Content.ReadAsStringAsync();
                        var jsonObject = JObject.Parse(jsonData);
                        // Hämta det senaste temperaturvärdet
                        var temperature = jsonObject["value"]?[0]?["value"]?.ToObject<double>();
                        return temperature;
                    }
                    else
                    {
                        System.Console.WriteLine("Misslyckades med att hämta data från SMHI API. PGA: "+ response);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Ett fel inträffade: {ex.Message}");
                return null;
            }
        }
    }
public class CLI
{
    private List<IMenuOption> menuOptions;
    //private static readonly HttpClient client = new HttpClient();
    //private static readonly string apiKey = Environment.GetEnvironmentVariable("HOMEY_API_KEY");

    public CLI()
    {
        menuOptions = new List<IMenuOption>
        {
            new PrintOutHoursLeftToMidnightOption(),
            new TimeNowOption(),
            new FetchDataFromAPI(),
            new ExitOption()
        };
    }

        public void Run()
    {
        PrintSplashScreen();
        ShowMenyOptions();
        while(true)
        {
            var key  = Console.ReadKey(true);
            if (char.IsDigit(key.KeyChar) && int.TryParse(key.KeyChar.ToString(), out int choice))
            {
                var selectedOption = menuOptions.Find(option => option.MenuIdentifier == choice);
                if (selectedOption != null)
                {   
                    PrintSplashScreen();
                    ShowMenyOptions();
                    selectedOption.Execute();
                }
                else
                {
                    System.Console.WriteLine("Invalid choise, please try again.");
                }
            }
            else
            {
                System.Console.WriteLine("Invalid input, please select a number.");
            }
        } 
    }

    private void ShowMenyOptions()
    {
        System.Console.WriteLine("What do you want to do?");
        foreach (var option in menuOptions)
        {
            System.Console.WriteLine($"{option.MenuIdentifier}. {option.Description}");
        }

    }
    private static void PrintSplashScreen()
    {
        Console.Clear();
        Console.WriteLine("Welcome to Törnkvist Feature CLI");
        Console.WriteLine(" _____  \\/\\/  ____  _      _  __ _     _  ____  _____  ");
        Console.WriteLine("/__ __\\/  _ \\/  __\\/ \\  /|/ |/ // \\ |\\/ \\/ ___\\/__ __\\ ");
        Console.WriteLine("  / \\  | / \\||  \\/|| |\\ |||   / | | //| ||    \\  / \\   ");
        Console.WriteLine("  | |  | \\_/||    /| | \\|||   \\ | \\// | |\\___ |  | |   ");
        Console.WriteLine("  \\_/  \\____/\\_/\\_\\\\_/  \\|\\_|\\_\\\\__/  \\_/\\____/  \\_/   ");
        Console.WriteLine("                                                      ");
        Console.WriteLine(" ____  _     ____  _____ ____  _      _____ _____ ____ ");
        Console.WriteLine("/ ___\\/ \\ /\\/  __\\/  __//  __\\/ \\__/|/  __//  __//  _ \\");
        Console.WriteLine("|    \\| | |||  \\/||  \\  |  \\/|| |\\/|||  \\  | |  _| / \\|");
        Console.WriteLine("\\___ || \\_/||  __/|  /_ |    /| |  |||  /_ | |_//| |-||");
        Console.WriteLine("\\____/\\____/\\_/   \\____\\\\_/\\_\\\\_/  \\|\\____\\\\____\\\\_/ \\|");
        Console.WriteLine("                                                      ");
    }
}
public static class EnvLoader
{
    public static void Load(string filePath = ".env")
    {
        string fullPath = Path.GetFullPath(filePath);
        Console.WriteLine($"Loading .env file from: {fullPath}");

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"The file '{fullPath}' does not exist.");
        }

        foreach (var line in File.ReadAllLines(fullPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            var parts = line.Split('=', 2);
            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"Invalid line in .env file: '{line}'");
            }

            Environment.SetEnvironmentVariable(parts[0], parts[1]);
        }
    }
}
    class Program
    {
        static void Main(string[] args)
        {
            //Ladda in .env filen
            EnvLoader.Load();

            CLI cLI = new CLI();
            cLI.Run();
        }
    }
}