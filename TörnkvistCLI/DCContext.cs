using Microsoft.EntityFrameworkCore;
public class WaterTemperature
{
    public int Id { get; set; }  // Primärnyckel
    public double Temperature { get; set; }
    public DateTime Date { get; set; }
    public string Location { get; set; } // Exempel på var temperaturmätningen gjordes
}

public class AppDbContext : DbContext
{
    public DbSet<WaterTemperature> WaterTemperatures { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = @"C:\code\TörnkvistCLI\TörnkvistCLI\temperatures.db";
        Console.WriteLine($"Using database at: {dbPath}");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
    
}
