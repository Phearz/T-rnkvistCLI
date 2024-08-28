using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Threading;
using TörnkvistCLI.Data;
using TörnkvistCLI;

namespace TörnkvistCLI
{

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
                _waterTemperatureService.SaveWaterTemperatureToDB(temperature.Value, _config);
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
        public WaterTemperatureService WaterTemperatureService { get; }
        public ILogger Logger { get; }

        //private readonly WaterTemperatureService _waterTemperatureService;
        //private readonly ILogger _logger;
        private readonly AppConfig _config;
        private bool _isRunning = true;

        public AgentOption(WaterTemperatureService waterTemperatureService, ILogger logger, AppConfig config)
        {
            _config = config ?? throw new ArgumentException(nameof(config));
            WaterTemperatureService = waterTemperatureService ?? throw new ArgumentException(nameof(waterTemperatureService));
            Logger = logger ?? throw new ArgumentException(nameof(logger));
        }

        public void Execute()
        {
            Thread agentThread = new Thread(StartAgent);
            agentThread.IsBackground = true;
            agentThread.Start();
        }

        private void StartAgent()
        {
            Logger.LogInformation("Monitoreringsagenten har startat och sparar temperaturen var 10:e sekund.");

            while (_isRunning)
            {
                try
                {
                    //hämtar temperaturen och skriv till databasen
                    var temperatur = WaterTemperatureService.FetchWaterTempatureAsync().Result;
                    if (temperatur.HasValue)
                    {
                        Logger.LogInformation($"Vatten temperatur hämtad: {temperatur.Value}°C");
                        WaterTemperatureService.SaveWaterTemperatureToDB(temperatur.Value, _config);
                    }
                    else
                    {
                        Logger.LogWarning("Misslyckades med att hämta vatten temperaturen.");
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.LogError(ex, "Ett fel inträffade i agenten.");
                    throw;
                }
                // Avvakta 10 sekunder innan den hämtar temperaturen igen.
                Thread.Sleep(_config.PollingInterval * 1000);
            }
            Logger.LogInformation("Agenten har stängts av");
        }
        public void StopAgent()
        {
            _isRunning = false;
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



    public class CLI
    {
        private List<IMenuOption> menuOptions;
        private ILogger _logger;

        private readonly AppConfig _config;
        private WaterTemperatureService _waterTemperatureService;
        private MenuIdentifier _menuIdentifier;

        public CLI(ILogger logger, AppConfig appConfig)
        {
            _config = appConfig;
            _logger = logger;
            _waterTemperatureService = new WaterTemperatureService(_logger, _config);
            _menuIdentifier = new MenuIdentifier();

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
            MenuIdentifier.PrintSplashScreen();
            _menuIdentifier.ShowMenyOptions(menuOptions);
            _logger.LogInformation("CLI started");

            while (true)
            {
                var key = Console.ReadKey(true);
                if (char.IsDigit(key.KeyChar) && int.TryParse(key.KeyChar.ToString(), out int choice))
                {
                    var selectedOption = menuOptions.Find(option => option.MenuIdentifier == choice);
                    if (selectedOption != null)
                    {
                        MenuIdentifier.PrintSplashScreen();
                        _menuIdentifier.ShowMenyOptions(menuOptions);
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
                CLI cLI = new CLI(logger, config);
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
}