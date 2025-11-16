using Shared.Services;

namespace ValidationService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IGlobalDbApiClient _apiClient;
    private readonly IDatabaseService _databaseService;
    private string? _lastProcessedTransitId;  // Trackear por ID en lugar de timestamp
    private readonly int _pollingIntervalSeconds = 5;  // Cambiar a 5 segundos

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
        _logger.LogInformation("ValidationService iniciado. Polling cada {Interval} segundos (procesando ultimo transito)", _pollingIntervalSeconds);

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
            // Obtener el último tránsito de la API
            var transits = await _apiClient.GetNewTransits(null);

            if (transits == null || !transits.Any())
            {
                _logger.LogInformation("No hay transitos disponibles en la API");
                return;
            }

            var lastTransit = transits.First();
            
            // Si ya procesamos este tránsito, no hacer nada
            if (_lastProcessedTransitId == lastTransit.transit_id)
            {
                _logger.LogInformation("✓ Ultimo transito ya fue procesado (ID: {TransitId}, Patente: {Plate})", 
                    lastTransit.transit_id?.Substring(0, 8), 
                    lastTransit.vehicle_plate);
                return;
            }

            _logger.LogInformation("Procesando nuevo transito: ID={TransitId}, Patente={Plate}", 
                lastTransit.transit_id, 
                lastTransit.vehicle_plate);

            await ProcessTransit(lastTransit, cancellationToken);

            // Actualizar el ID del último procesado
            _lastProcessedTransitId = lastTransit.transit_id;
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
