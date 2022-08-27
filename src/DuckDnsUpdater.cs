using System.Diagnostics.CodeAnalysis;

namespace DDnsService;

public class DuckDnsUpdater : BackgroundService
{
    private const string CacheFolder = "/var/lib/ddns";
    private const string CacheFile = "cache";

    private readonly ILogger<DuckDnsUpdater> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public DuckDnsUpdater(ILogger<DuckDnsUpdater> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    [UnconditionalSuppressMessage("TrimAnalysis", "IL2026", Justification = "Only reads from configuration basic types")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var currentIp = "";

                try
                {
                    currentIp = await _httpClient.GetStringAsync("http://whatismyip.akamai.com/", stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Cannot connect to http://whatismyip.akamai.com. Error: {error}", ex.ToString());
                }

                if (!string.IsNullOrEmpty(currentIp))
                {
                    var cachedIp = GetLastIpRegistered();

                    if (string.Equals(currentIp, cachedIp, StringComparison.Ordinal))
                    {
                        _logger.LogInformation("IP address has not changed");
                        return;
                    }
                }

                var updateUrl = "https://www.duckdns.org/update";

                var domains = _configuration.GetValue<string>("Domains");
                if (string.IsNullOrEmpty(domains))
                {
                    _logger.LogError("DuckDns domains not configured");
                    return;
                }

                var token = _configuration.GetValue<string>("Token");
                if (string.IsNullOrEmpty(domains))
                {
                    _logger.LogError("DuckDns token not configured");
                    return;
                }

                var url = $"{updateUrl}?domains={domains}&token={token}&ip={currentIp}";
                var response = await _httpClient.GetStringAsync(url, stoppingToken);

                switch (response)
                {
                    case "OK":
                        CacheIpAddress(currentIp);
                        _logger.LogWarning("IP address has changed. Current address is {ip}", currentIp);
                        break;

                    case "KO":
                        _logger.LogError("DuckDns has rejected the update");
                        break;

                    default:
                        _logger.LogError("Error updating DuckDns. Reason: {reason}", response);
                        break;
                }

                if (int.TryParse(_configuration.GetValue<string>("Internal") ?? "", out var interval))
                {
                    if (interval < 5)
                    {
                        interval = 5;
                    }
                }
                else
                {
                    interval = 15;
                }

                await Task.Delay(interval * 60 * 1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("{exception}", ex.ToString());
            }
        }
    }

    private static string GetLastIpRegistered()
    {
        var folder = CacheFolder;

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var file = Path.Combine(folder, CacheFile);

        if (File.Exists(file))
        {
            return File.ReadAllText(file);
        }
        else
        {
            return "";
        }
    }

    private static void CacheIpAddress(string ipAddress)
    {
        var folder = CacheFolder;

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var file = Path.Combine(folder, CacheFile);

        File.WriteAllText(file, ipAddress);
    }
}
