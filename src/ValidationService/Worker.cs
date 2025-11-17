using Shared.Services;

namespace ValidationService;

/// <summary>
/// Worker principal que ejecuta el servicio de validación en segundo plano.
/// Realiza polling cada 5 segundos para procesar el último tránsito de vehículos.
/// </summary>
public class Worker : BackgroundService
{
    // Logger para registrar eventos y errores
    private readonly ILogger<Worker> _logger;
    
    // Cliente HTTP para comunicarse con la API externa de tránsitos
    private readonly IGlobalDbApiClient _apiClient;
    
    // Servicio para guardar logs en la base de datos local (SQL Server)
    private readonly IDatabaseService _databaseService;
    
    // ID del último tránsito procesado (para evitar reprocesar el mismo)
    private string? _lastProcessedTransitId;
    
    // Intervalo de tiempo entre cada consulta a la API (por defecto 5 segundos)
    private readonly int _pollingIntervalSeconds = 5;

    /// <summary>
    /// Constructor con inyección de dependencias
    /// </summary>
    public Worker(
        ILogger<Worker> logger,
        IGlobalDbApiClient apiClient,
        IDatabaseService databaseService,
        IConfiguration configuration)
    {
        _logger = logger;
        _apiClient = apiClient;
        _databaseService = databaseService;
        
        // Lee el intervalo de polling desde appsettings.json (default: 10, pero usamos 5)
        _pollingIntervalSeconds = configuration.GetValue<int>("PollingIntervalSeconds", 10);
    }

    /// <summary>
    /// Método principal que se ejecuta cuando el servicio inicia.
    /// Corre en un loop infinito hasta que se detiene el servicio.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ValidationService iniciado. Polling cada {Interval} segundos (procesando ultimo transito)", _pollingIntervalSeconds);

        // Loop infinito mientras el servicio esté activo
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Procesar nuevos tránsitos
                await ProcessNewTransits(stoppingToken);
            }
            catch (Exception ex)
            {
                // Si hay error, registrarlo pero continuar el loop
                _logger.LogError(ex, "Error procesando tránsitos");
            }

            // Esperar X segundos antes de la próxima consulta (default: 5 segundos)
            await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
        }
    }

    /// <summary>
    /// Consulta la API para obtener el último tránsito y procesarlo.
    /// Solo procesa tránsitos nuevos (que no se hayan procesado antes).
    /// </summary>
    private async Task ProcessNewTransits(CancellationToken cancellationToken)
    {
        try
        {
            // Consultar el último tránsito desde la API externa
            // GET /api/transits?order_by=occurred_at&order_dir=desc&limit=1
            var transits = await _apiClient.GetNewTransits(null);

            // Si no hay tránsitos disponibles, salir
            if (transits == null || !transits.Any())
            {
                _logger.LogInformation("No hay transitos disponibles en la API");
                return;
            }

            // Obtener el tránsito más reciente (solo viene 1 porque limit=1)
            var lastTransit = transits.First();
            
            // Si ya procesamos este tránsito antes, no hacer nada (evitar reprocesar)
            if (_lastProcessedTransitId == lastTransit.transit_id)
            {
                _logger.LogInformation("✓ Ultimo transito ya fue procesado (ID: {TransitId}, Patente: {Plate})", 
                    lastTransit.transit_id?.Substring(0, 8), 
                    lastTransit.vehicle_plate);
                return;
            }

            // Es un tránsito nuevo, procesarlo
            _logger.LogInformation("Procesando nuevo transito: ID={TransitId}, Patente={Plate}", 
                lastTransit.transit_id, 
                lastTransit.vehicle_plate);

            await ProcessTransit(lastTransit, cancellationToken);

            // Guardar el ID del último tránsito procesado para no reprocesarlo
            _lastProcessedTransitId = lastTransit.transit_id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo tránsitos de la API");
        }
    }

    /// <summary>
    /// Procesa un tránsito individual:
    /// 1. Verifica si el vehículo está registrado en la API
    /// 2. Si está registrado → crea un PAGO
    /// 3. Si NO está registrado → crea una MULTA
    /// </summary>
    private async Task ProcessTransit(Transit transit, CancellationToken cancellationToken)
    {
        string plate = transit.vehicle_plate ?? "UNKNOWN";
        string vehicleType = transit.vehicle_type ?? "unknown";

        try
        {
            _logger.LogInformation("Procesando tránsito: Patente={Plate}, Tipo={Type}", plate, vehicleType);

            // PASO 1: Verificar si el vehículo está registrado en la base de datos
            // GET /api/vehicles?plate={plate}
            bool isRegistered = await _apiClient.IsVehicleRegistered(plate);

            if (isRegistered)
            {
                // CASO 1: Vehículo REGISTRADO → Crear PAGO
                _logger.LogInformation("Vehículo {Plate} registrado - Creando pago", plate);

                // POST /api/payments con amount: 0 (placeholder)
                bool paymentCreated = await _apiClient.CreatePayment(plate, vehicleType);

                if (paymentCreated)
                {
                    // Guardar log exitoso en base de datos local
                    await LogToDatabase(
                        plate,
                        null,
                        true,
                        $"Pago creado para vehículo registrado tipo {vehicleType}"
                    );
                }
                else
                {
                    // Error creando pago
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
                // CASO 2: Vehículo NO REGISTRADO → Crear MULTA
                _logger.LogInformation("Vehículo {Plate} NO registrado - Creando multa", plate);

                // POST /api/fines con amount: 0 (placeholder)
                bool fineCreated = await _apiClient.CreateFine(plate, vehicleType);

                if (fineCreated)
                {
                    // Guardar log exitoso en base de datos local
                    await LogToDatabase(
                        plate,
                        null,
                        false,
                        $"Multa creada para vehículo no registrado tipo {vehicleType}"
                    );
                }
                else
                {
                    // Error creando multa
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

            // Guardar log de error en base de datos local
            await LogToDatabase(
                plate,
                null,
                false,
                $"Error: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Guarda un registro de log en la base de datos SQL Server local.
    /// Nota: Esta BD es opcional, solo para auditoría local. Si falla, no afecta el funcionamiento.
    /// </summary>
    /// <param name="vehiclePlate">Patente del vehículo</param>
    /// <param name="usuario">Usuario asociado (null en este caso)</param>
    /// <param name="isValid">True si creó pago, False si creó multa o error</param>
    /// <param name="details">Mensaje descriptivo del resultado</param>
    private async Task LogToDatabase(
        string vehiclePlate,
        string? usuario,
        bool isValid,
        string details)
    {
        try
        {
            // Intentar guardar en SQL Server local
            await _databaseService.LogValidation(
                vehiclePlate,
                usuario,
                isValid,
                details
            );
        }
        catch (Exception ex)
        {
            // Si falla, solo registrar el error pero NO detener el servicio
            // El funcionamiento principal (crear pagos/multas) no depende de esta BD
            _logger.LogError(ex, "Error guardando en base de datos local para {Plate}", vehiclePlate);
        }
    }
}
