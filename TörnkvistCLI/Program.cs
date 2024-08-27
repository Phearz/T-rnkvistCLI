using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Threading;

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
        public PrintOutHoursLeftToMidnightOption(ILogger logger)
        {
            Logger = logger;
        }

        public int MenuIdentifier => 1;
        public string Description => "Hur lång tid är det till midnatt?";

        public ILogger Logger { get; }

        public void Execute()
        {
            TimeSpan timeLeft = CalcHoursLeftToMidnight();
            Console.WriteLine("Timmarna kvar till midnatt är: " + timeLeft.Hours + " timmar, " + timeLeft.Minutes + " Minuter och " + timeLeft.Seconds + " Sekunder!");
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
        public TimeNowOption(ILogger logger)
        {
            Logger = logger;
        }

        public int MenuIdentifier => 2;
        public string Description => "Vad är det för Tid och Datum just nu?";

        public ILogger Logger { get; }

        public void Execute()
        {
            System.Console.WriteLine("Datum och Tid just nu är " + DateTime.Now);
        }
    }
    public class ExitOption : IMenuOption
    {
        public int MenuIdentifier => 5;
        public string Description => "Avsluta Programet";
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        public ExitOption(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
        }

        public void Execute()
        {
            // Logga att applikationen stängs ner
            _logger.LogInformation("Application is shutting down.");

            Log.CloseAndFlush();
            Environment.Exit(0);
        }
    }
    public class FetchDataFromAPI : IMenuOption
    {
        public int MenuIdentifier => 3;
        public string Description => "Hämta data ifrån SMHI API";

        private readonly WaterTemperatureService _waterTemperatureService;
        private readonly AppConfig _config;

        public FetchDataFromAPI(WaterTemperatureService waterTemperatureService, AppConfig config)
        {
            _waterTemperatureService = waterTemperatureService;
            _config = config;
        }

        public async void Execute()
        {
            System.Console.WriteLine("Fetching data...");

            var temperature = await _waterTemperatureService.FetchWaterTempatureAsync();

            if (temperature.HasValue)
            {
                System.Console.WriteLine($"Aktuell kustvattentemperatur i Varberg: {temperature.Value}°C");
                _waterTemperatureService.SaveWaterTemperatureToDB(temperature.Value,_config);
            }
            else
            {
                System.Console.WriteLine("Kunde inte hämta kustvatten temperaturen ifrån SMHI.");
            }

        }
    }
        public class AgentOption : IMenuOption
        {
            public int MenuIdentifier => 4;
            public string Description => "Starta vatten temperatur monitorering";

        public WaterTemperature WaterTemperatureService { get; }
        public ILogger Logger { get; }

        private readonly WaterTemperatureService _waterTemperatureService;
            private readonly ILogger _logger;
            private readonly AppConfig _config;
            private bool _isRunning = true;

        public AgentOption(WaterTemperatureService waterTemperatureService, ILogger logger, AppConfig config)
        {
            _config = config;
            _waterTemperatureService = waterTemperatureService;
            _logger = logger;
        }

        public void Execute()
            {
                Thread agentThread = new Thread(StartAgent);
                agentThread.IsBackground = true;
                agentThread.Start();
            }

            private void StartAgent()
            {
                _logger.LogInformation("Monitoreringsagenten har startat och sparar temperaturen var 10:e sekund.");

                while (_isRunning)
                {
                    try
                    {
                        //hämtar temperaturen och skriv till databasen
                        var temperatur = _waterTemperatureService.FetchWaterTempatureAsync().Result;
                        if (temperatur.HasValue)
                        {
                            _logger.LogInformation($"Vatten temperatur hämtad: {temperatur.Value}°C");
                            _waterTemperatureService.SaveWaterTemperatureToDB(temperatur.Value,_config);
                        }
                        else
                        {
                            _logger.LogWarning("Misslyckades med att hämta vatten temperaturen.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Ett fel inträffade i agenten.");
                        throw;
                    }
                    // Avvakta 10 sekunder innan den hämtar temperaturen igen.
                    Thread.Sleep(_config.PollingInterval * 1000);
                }
                _logger.LogInformation("Agenten har stängts av");
            }
            public void StopAgent()
            {
                _isRunning = false;
            }
        }
    }

    
    public class CLI
    {
        private List<IMenuOption> menuOptions;
        private ILogger _logger;

        private readonly AppConfig _config;
        private WaterTemperatureService _waterTemperatureService;
        //private static readonly HttpClient client = new HttpClient();
        //private static readonly string apiKey = Environment.GetEnvironmentVariable("HOMEY_API_KEY");

        public CLI(ILogger logger,AppConfig appConfig)
        {
            _config = appConfig;
            _logger = logger;
            _waterTemperatureService = new WaterTemperatureService(_logger,_config);

            menuOptions = new List<IMenuOption>

            {
                new PrintOutHoursLeftToMidnightOption(_logger),
                new TimeNowOption(_logger),
                new FetchDataFromAPI(_waterTemperatureService,_config),
                new AgentOption(_waterTemperatureService,_logger,_config),
                new ExitOption(_logger)
            };
        }

        public void Run()
        {
            PrintSplashScreen();
            ShowMenyOptions();
            _logger.LogInformation("CLI started");

            while (true)
            {
                var key = Console.ReadKey(true);
                if (char.IsDigit(key.KeyChar) && int.TryParse(key.KeyChar.ToString(), out int choice))
                {
                    var selectedOption = menuOptions.Find(option => option.MenuIdentifier == choice);
                    if (selectedOption != null)
                    {
                        PrintSplashScreen();
                        ShowMenyOptions();
                        selectedOption.Execute();
                        _logger.LogInformation($"Executed menu option {selectedOption.Description}");
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
    class Program
    {
        static void Main(string[] args)
        {
            //Ladda in .env filen
            //EnvLoader.Load();
            var config = AppConfig.Load("app.config");
            
            //skapa logger
            using var loggerFactory = LoggingConfig.CreateLoggerFactory(config.LoggingLevel);
            ILogger logger = loggerFactory.CreateLogger<Program>();

            //Logga applicationsstart
            logger.LogInformation("Application started");

            try
            {
                CLI cLI = new CLI(logger,config);
                cLI.Run();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred. " + ex);
                throw;
            }
            finally
            {
                logger.LogInformation("Application Ended");
                Serilog.Log.CloseAndFlush();
            }
        }
    }