using ValidationService;
using Shared.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configurar cliente HTTP para la API global
builder.Services.AddHttpClient<IGlobalDbApiClient, GlobalDbApiClient>(client =>
{
    var apiUrl = builder.Configuration["GlobalDbApi:BaseUrl"] ?? "https://fun-bernetta-johannson-systems-v2-ba75677f.koyeb.app";
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configurar servicio de base de datos local
builder.Services.AddSingleton<IDatabaseService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<DatabaseService>>();
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no encontrada");
    return new DatabaseService(connectionString, logger);
});

// Registrar el Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
