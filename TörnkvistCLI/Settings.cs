using System.Xml.Linq;

namespace TörnkvistCLI
{

    public class AppConfig
    {
        public bool MonitorTemperature { get; set; }
        public int StationId { get; set; }
        public int PollingInterval { get; set; }
        public string DatabasePath { get; set; }
        public string LoggingLevel { get; set; }
        public string ApiUrl { get; set; }

        public static AppConfig Load(string configFilePath)
        {
            var doc = XDocument.Load(configFilePath);
            var appSettings = doc.Element("configuration")?.Element("appSettings");

            return new AppConfig
            {
                MonitorTemperature = bool.Parse(appSettings?.Element("monitorTemperature")?.Value ?? "true"),
                StationId = int.Parse(appSettings?.Element("stationId")?.Value ?? "25133"),
                PollingInterval = int.Parse(appSettings?.Element("pollingInterval")?.Value ?? "10"),
                DatabasePath = appSettings?.Element("databasePath")?.Value ?? @"C:\code\TörnkvistCLI\TörnkvistCLI\temperatures.db",
                LoggingLevel = appSettings?.Element("loggingLevel")?.Value ?? "Information",
                ApiUrl = appSettings?.Element("apiUrl")?.Value ?? "https://opendata-download-ocobs.smhi.se/api/version/1.0/parameter/5/station/"
            };
        }
    }
}