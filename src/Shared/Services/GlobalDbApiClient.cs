using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Shared.Services;

/// <summary>
/// Interface que define los métodos para comunicarse con la API externa de tránsitos
/// </summary>
public interface IGlobalDbApiClient
{
    /// <summary>
    /// Obtiene el último tránsito desde la API
    /// </summary>
    Task<List<Transit>> GetNewTransits(DateTime? since = null);
    
    /// <summary>
    /// Verifica si un vehículo está registrado en el sistema
    /// </summary>
    Task<bool> IsVehicleRegistered(string plate);
    
    /// <summary>
    /// Crea un registro de pago para un vehículo registrado
    /// </summary>
    Task<bool> CreatePayment(string plate, string? vehicleType = null);
    
    /// <summary>
    /// Crea una multa para un vehículo no registrado
    /// </summary>
    Task<bool> CreateFine(string plate, string? vehicleType = null);
}

/// <summary>
/// Cliente HTTP para comunicarse con la API externa de Koyeb.
/// Maneja todas las operaciones HTTP: GET (consultas) y POST (crear registros).
/// </summary>
public class GlobalDbApiClient : IGlobalDbApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GlobalDbApiClient> _logger;

    public GlobalDbApiClient(HttpClient httpClient, ILogger<GlobalDbApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el último tránsito desde la API externa.
    /// Consulta: GET /api/transits?order_by=occurred_at&order_dir=desc&limit=1
    /// Retorna solo el vehículo más reciente que pasó por el toll.
    /// </summary>
    public async Task<List<Transit>> GetNewTransits(DateTime? since = null)
    {
        try
        {
            // Construir URL para obtener solo el último tránsito (limit=1)
            // Ordenado por fecha descendente para obtener el más reciente
            var url = "/api/transits?order_by=occurred_at&order_dir=desc&limit=1";
            
            _logger.LogInformation("Consultando ultimo transito");
            
            // Hacer petición HTTP GET
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                // Deserializar respuesta JSON a objeto C#
                // La API devuelve: { "data": [...], "limit": 1, "offset": 0 }
                var apiResponse = await response.Content.ReadFromJsonAsync<TransitListResponse>();
                var transits = apiResponse?.data ?? new List<Transit>();
                
                if (transits.Any())
                {
                    _logger.LogInformation("Ultimo transito obtenido: Patente={Plate}, Fecha={Date}", 
                        transits[0].vehicle_plate, 
                        transits[0].occurred_at);
                }
                else
                {
                    _logger.LogDebug("No hay transitos disponibles");
                }
                
                return transits;
            }
            else
            {
                _logger.LogWarning("Error consultando transitos - StatusCode: {StatusCode}", response.StatusCode);
                return new List<Transit>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepcion consultando transitos");
            return new List<Transit>();
        }
    }

    /// <summary>
    /// Verifica si un vehículo está registrado en el sistema.
    /// Consulta: GET /api/vehicles?plate={plate}
    /// Retorna true si el vehículo existe, false si no está registrado.
    /// </summary>
    /// <param name="plate">Patente del vehículo a verificar</param>
    public async Task<bool> IsVehicleRegistered(string plate)
    {
        try
        {
            _logger.LogInformation("Verificando si vehiculo esta registrado - Plate: {Plate}", plate);
            
            // Hacer petición HTTP GET con la patente como parámetro
            var response = await _httpClient.GetAsync($"/api/vehicles?plate={plate}");
            
            if (response.IsSuccessStatusCode)
            {
                // Deserializar respuesta JSON
                // La API devuelve: { "data": [...], "limit": 10, "offset": 0 }
                var apiResponse = await response.Content.ReadFromJsonAsync<VehicleListResponse>();
                var vehicles = apiResponse?.data ?? new List<Vehicle>();
                
                // Vehículo está registrado si:
                // 1. La lista NO está vacía (existe en la BD)
                // 2. Tiene un customer_id asociado (pertenece a un cliente)
                bool isRegistered = vehicles.Count > 0 && vehicles[0].customer_id != null;
                
                _logger.LogInformation("Vehiculo {Plate} - Registrado: {IsRegistered}", plate, isRegistered);
                return isRegistered;
            }
            else
            {
                _logger.LogWarning("Error consultando vehiculo - StatusCode: {StatusCode}", response.StatusCode);
                return false; // Asumimos no registrado en caso de error
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepcion consultando vehiculo");
            return false; // Asumimos no registrado en caso de error
        }
    }

    /// <summary>
    /// Crea un registro de pago para un vehículo registrado.
    /// Endpoint: POST /api/payments
    /// NOTA: amount=0 es placeholder - será actualizado por el equipo de pagos posteriormente.
    /// </summary>
    /// <param name="plate">Patente del vehículo</param>
    /// <param name="vehicleType">Tipo de vehículo (opcional)</param>
    public async Task<bool> CreatePayment(string plate, string? vehicleType = null)
    {
        try
        {
            _logger.LogInformation("Creando pago para plate: {Plate}, tipo: {Type}", plate, vehicleType ?? "N/A");
            
            // Crear objeto de pago con estructura requerida por la API
            // amount=0: Placeholder - otro equipo calculará el monto real según tarifas
            var payment = new
            {
                plate = plate,
                amount = 0,  // Placeholder - será actualizado por el equipo de pagos
                currency = "ARS",
                status = "pending",
                method = "automatic",
                requested_at = DateTime.UtcNow
            };
            
            // Hacer petición HTTP POST con el objeto serializado a JSON
            var response = await _httpClient.PostAsJsonAsync("/api/payments", payment);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ Pago creado exitosamente para plate: {Plate}", plate);
                return true;
            }
            else
            {
                // Leer el cuerpo de la respuesta para diagnóstico
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error creando pago - StatusCode: {StatusCode}, Error: {Error}", 
                    response.StatusCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepcion creando pago");
            return false;
        }
    }

    /// <summary>
    /// Crea un registro de multa para un vehículo NO registrado.
    /// Endpoint: POST /api/fines
    /// NOTA: amount=0 es placeholder - será actualizado por el equipo de multas posteriormente.
    /// </summary>
    /// <param name="plate">Patente del vehículo</param>
    /// <param name="vehicleType">Tipo de vehículo (opcional)</param>
    public async Task<bool> CreateFine(string plate, string? vehicleType = null)
    {
        try
        {
            _logger.LogInformation("Creando multa para plate: {Plate}, tipo: {Type}", plate, vehicleType ?? "N/A");
            
            // Crear objeto de multa con estructura requerida por la API
            // amount=0: Placeholder - otro equipo calculará el monto real según infracciones
            var fine = new
            {
                plate = plate,
                amount = 0,  // Placeholder - será actualizado por el equipo de multas
                currency = "ARS",
                reason = $"Vehiculo no registrado - Transito sin autorizacion{(vehicleType != null ? $" (Tipo: {vehicleType})" : "")}",
                status = "pending",
                issued_at = DateTime.UtcNow
            };
            
            // Hacer petición HTTP POST con el objeto serializado a JSON
            var response = await _httpClient.PostAsJsonAsync("/api/fines", fine);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ Multa creada exitosamente para plate: {Plate}", plate);
                return true;
            }
            else
            {
                // Leer el cuerpo de la respuesta para diagnóstico
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error creando multa - StatusCode: {StatusCode}, Error: {Error}", 
                    response.StatusCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepcion creando multa");
            return false;
        }
    }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepcion creando multa");
            return false;
        }
    }
}

// Modelos
public class TransitListResponse
{
    public List<Transit>? data { get; set; }
    public int limit { get; set; }
    public int offset { get; set; }
}

public class VehicleListResponse
{
    public List<Vehicle>? data { get; set; }
    public int limit { get; set; }
    public int offset { get; set; }
}

public class Transit
{
    public string? transit_id { get; set; }
    public string? vehicle_plate { get; set; }
    public string? vehicle_type { get; set; }
    public string? occurred_at { get; set; }  // Formato: "Tue, 21 Oct 2025 23:00:00 GMT"
    public string? gate_id { get; set; }
    public double? speed_kmh { get; set; }
    public string? capture_ref { get; set; }
    
    public DateTime? GetOccurredAtDateTime()
    {
        if (string.IsNullOrEmpty(occurred_at))
            return null;
        
        // Parse RFC1123 date format
        if (DateTime.TryParse(occurred_at, out DateTime result))
            return result;
        
        return null;
    }
}

public class Vehicle
{
    public string? vehicle_id { get; set; }
    public string? plate { get; set; }
    public string? customer_id { get; set; }
    public string? make { get; set; }
    public string? model { get; set; }
    public int? year { get; set; }
}
