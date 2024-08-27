using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
public class WaterTemperature
{
    public int Id { get; set; }  // Primärnyckel
    public double Temperature { get; set; }
    public DateTime Date { get; set; }
    public required string Location { get; set; } // Exempel på var temperaturmätningen gjordes

}

public class AppDbContext : DbContext
{
    private readonly AppConfig _config;
    public AppDbContext(AppConfig config)
    {
        _config = config;
    }

    public DbSet<WaterTemperature> WaterTemperatures { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = $"{_config.DatabasePath}";
        Console.WriteLine($"Using database at: {dbPath}");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
    
}
public class WaterTemperatureService
{
    private readonly AppConfig _config;
    private readonly ILogger _logger;

    public WaterTemperatureService(ILogger logger, AppConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<double?> FetchWaterTempatureAsync()
    {
        try
        {
            string url = $"{_config.ApiUrl}{_config.StationId}/period/latest-day/data.json";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonData = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonData);
                    var temperature = jsonObject["value"]?[0]?["value"]?.ToObject<double>();
                    return temperature;
                }
                else
                {
                    _logger.LogWarning($"Misslyckades med att hämta data från SMHI API. StatusCode: {response.StatusCode}");
                    return null;
                }
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Ett fel inträffade vid hämtning av vattentemperatur.");
            return null;
        }
    }
    public void SaveWaterTemperatureToDB(double temperatur,AppConfig config)
    {
        using (var db = new AppDbContext(config))
        {
            var waterTemperature = new WaterTemperature
            {
                Temperature = temperatur,
                Date = DateTime.UtcNow,
                Location = "Station 35133"
            };

            try
            {
                db.WaterTemperatures.Add(waterTemperature);
                db.SaveChanges();
                _logger.LogInformation("Vatten temperaturen sparad i databasen.");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "ett fel har inträffat när temperaturen skulle sparas.");
            }
        }
    }
}

