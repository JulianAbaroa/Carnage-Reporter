using Microsoft.Extensions.Configuration;

namespace CarnageWatcher;

public class Config
{
    public string? UserName { get; set; }
    public string? DiscordWebhookLink { get; set; }
}

public static class ConfigLoader
{
    public static Config? Load()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration config = builder.Build();

        return config.Get<Config>();
    }
}