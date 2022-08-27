using DDnsService;

var host = Host.CreateDefaultBuilder(args)
    .UseSystemd()
    .ConfigureAppConfiguration(x => x.AddJsonFile("/etc/ddns/ddns.conf", optional: true, reloadOnChange: true))
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddHostedService<DuckDnsUpdater>();
    })
    .Build();

host.Run();
