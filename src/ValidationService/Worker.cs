using Shared.Services;

namespace ValidationService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IGlobalDbApiClient _apiClient;
    private readonly IDatabaseService _databaseService;
    private DateTime? _lastProcessedTime;
    private readonly int _pollingIntervalSeconds = 10;

    public Worker(
        ILogger<Worker> logger,
        IGlobalDbApiClient apiClient,
        IDatabaseService databaseService,
        IConfiguration configuration)
    {
        _logger = logger;
        _apiClient = apiClient;
        _databaseService = databaseService;
        
        // Intervalo de polling configurable
        _pollingIntervalSeconds = configuration.GetValue<int>("PollingIntervalSeconds", 10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ValidationService iniciado. Polling cada {Interval} segundos", _pollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNewTransits(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando tránsitos");
            }

            await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessNewTransits(CancellationToken cancellationToken)
    {
        try
        {
            // Obtener nuevos tránsitos desde la API
            var transits = await _apiClient.GetNewTransits(_lastProcessedTime);

            if (transits == null || !transits.Any())
            {
                _logger.LogDebug("No hay nuevos tránsitos para procesar");
                return;
            }

            _logger.LogInformation("Procesando {Count} nuevo(s) transito(s)", transits.Count());

            foreach (var transit in transits)
            {
                await ProcessTransit(transit, cancellationToken);
            }

            // Actualizar timestamp del último procesado
            _lastProcessedTime = transits
                .Select(t => t.GetOccurredAtDateTime())
                .Where(dt => dt.HasValue)
                .DefaultIfEmpty(DateTime.UtcNow)
                .Max() ?? DateTime.UtcNow;
            _logger.LogDebug("Último procesamiento: {Time}", _lastProcessedTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo tránsitos de la API");
        }
    }

    private async Task ProcessTransit(Transit transit, CancellationToken cancellationToken)
    {
        string plate = transit.vehicle_plate ?? "UNKNOWN";
        string vehicleType = transit.vehicle_type ?? "unknown";

        try
        {
            _logger.LogInformation("Procesando tránsito: Patente={Plate}, Tipo={Type}", plate, vehicleType);

            // Verificar si el vehículo está registrado
            bool isRegistered = await _apiClient.IsVehicleRegistered(plate);

            if (isRegistered)
            {
                _logger.LogInformation("Vehículo {Plate} registrado - Creando pago", plate);

                // Crear pago para vehículo registrado
                bool paymentCreated = await _apiClient.CreatePayment(plate, vehicleType);

                if (paymentCreated)
                {
                    await LogToDatabase(
                        plate,
                        null,
                        true,
                        $"Pago creado para vehículo registrado tipo {vehicleType}"
                    );
                }
                else
                {
                    await LogToDatabase(
                        plate,
                        null,
                        false,
                        "Error al crear el pago"
                    );
                }
            }
            else
            {
                _logger.LogInformation("Vehículo {Plate} NO registrado - Creando multa", plate);

                // Crear multa para vehículo no registrado
                bool fineCreated = await _apiClient.CreateFine(plate, vehicleType);

                if (fineCreated)
                {
                    await LogToDatabase(
                        plate,
                        null,
                        false,
                        $"Multa creada para vehículo no registrado tipo {vehicleType}"
                    );
                }
                else
                {
                    await LogToDatabase(
                        plate,
                        null,
                        false,
                        "Error al crear la multa"
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando patente {Plate}", plate);

            await LogToDatabase(
                plate,
                null,
                false,
                $"Error: {ex.Message}"
            );
        }
    }

    private async Task LogToDatabase(
        string vehiclePlate,
        string? usuario,
        bool isValid,
        string details)
    {
        try
        {
            await _databaseService.LogValidation(
                vehiclePlate,
                usuario,
                isValid,
                details
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando en base de datos local para {Plate}", vehiclePlate);
        }
    }
}
