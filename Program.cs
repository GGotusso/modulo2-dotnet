using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modulo2Web.Config;
using Modulo2Web.Processor;
using Modulo2Web.Services;

class Program
{
    private static PatentProcessor? _processor;

    static async Task Main(string[] args)
    {
        // Configurar la configuración
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Configurar AppConfig
        var appConfig = new AppConfig();
        configuration.Bind(appConfig);

        // Aplicar valores de variables de entorno si existen
        appConfig.RabbitMQ.Url = Environment.GetEnvironmentVariable("RABBITMQ_URL") ?? appConfig.RabbitMQ.Url;
        appConfig.RabbitMQ.Queues.Entrada = Environment.GetEnvironmentVariable("QUEUE_ENTRADA") ?? appConfig.RabbitMQ.Queues.Entrada;
        appConfig.RabbitMQ.Queues.Pagos = Environment.GetEnvironmentVariable("QUEUE_PAGOS") ?? appConfig.RabbitMQ.Queues.Pagos;
        appConfig.RabbitMQ.Queues.Multas = Environment.GetEnvironmentVariable("QUEUE_MULTAS") ?? appConfig.RabbitMQ.Queues.Multas;
        appConfig.Api.Url = Environment.GetEnvironmentVariable("API_URL") ?? appConfig.Api.Url;
        
        if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out int port))
        {
            appConfig.Server.Port = port;
        }

        // Configurar servicios
        var serviceProvider = new ServiceCollection()
            .AddSingleton(appConfig)
            .AddHttpClient()
            .AddSingleton<RabbitMQService>()
            .AddSingleton<ApiService>()
            .AddSingleton<PatentProcessor>()
            .BuildServiceProvider();

        _processor = serviceProvider.GetRequiredService<PatentProcessor>();

        // Manejo de señales de terminación
        Console.CancelKeyPress += async (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Console.WriteLine("\n\n🛑 Deteniendo procesador...");
            if (_processor != null)
                await _processor.StopAsync();
            Environment.Exit(0);
        };

        AppDomain.CurrentDomain.ProcessExit += async (sender, eventArgs) =>
        {
            Console.WriteLine("\n\n🛑 Deteniendo procesador...");
            if (_processor != null)
                await _processor.StopAsync();
        };

        try
        {
            await _processor.StartAsync();
            
            // Mantener la aplicación ejecutándose
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fatal: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
