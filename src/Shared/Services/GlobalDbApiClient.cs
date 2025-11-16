using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Shared.Services;

public interface IGlobalDbApiClient
{
    Task<List<Transit>> GetNewTransits(DateTime? since = null);
    Task<bool> IsVehicleRegistered(string plate);
    Task<bool> CreatePayment(string plate, string? vehicleType = null);
    Task<bool> CreateFine(string plate, string? vehicleType = null);
}

public class GlobalDbApiClient : IGlobalDbApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GlobalDbApiClient> _logger;

    public GlobalDbApiClient(HttpClient httpClient, ILogger<GlobalDbApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<Transit>> GetNewTransits(DateTime? since = null)
    {
        try
        {
            var url = "/api/transits?order_by=occurred_at&order_dir=desc&limit=100";
            
            if (since.HasValue)
            {
                url += $"&occurred_at_gte={since.Value:yyyy-MM-ddTHH:mm:ss}";
            }
            
            _logger.LogInformation("Consultando nuevos transitos desde: {Since}", since);
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<TransitListResponse>();
                var transits = apiResponse?.data ?? new List<Transit>();
                _logger.LogInformation("Transitos obtenidos: {Count}", transits.Count);
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

    public async Task<bool> IsVehicleRegistered(string plate)
    {
        try
        {
            _logger.LogInformation("Verificando si vehiculo esta registrado - Plate: {Plate}", plate);
            
            var response = await _httpClient.GetAsync($"/api/vehicles?plate={plate}");
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<VehicleListResponse>();
                var vehicles = apiResponse?.data ?? new List<Vehicle>();
                bool isRegistered = vehicles.Count > 0 && vehicles[0].customer_id != null;
                
                _logger.LogInformation("Vehiculo {Plate} - Registrado: {IsRegistered}", plate, isRegistered);
                return isRegistered;
            }
            else
            {
                _logger.LogWarning("Error consultando vehiculo - StatusCode: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepcion consultando vehiculo");
            return false;
        }
    }

    public async Task<bool> CreatePayment(string plate, string? vehicleType = null)
    {
        try
        {
            _logger.LogInformation("Creando pago para plate: {Plate}, tipo: {Type}", plate, vehicleType ?? "N/A");
            
            var payment = new
            {
                plate = plate,
                amount = 0,  // Placeholder - será actualizado por el equipo de pagos
                currency = "ARS",
                status = "pending",
                method = "automatic",
                requested_at = DateTime.UtcNow
            };
            
            var response = await _httpClient.PostAsJsonAsync("/api/payments", payment);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ Pago creado exitosamente para plate: {Plate}", plate);
                return true;
            }
            else
            {
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

    public async Task<bool> CreateFine(string plate, string? vehicleType = null)
    {
        try
        {
            _logger.LogInformation("Creando multa para plate: {Plate}, tipo: {Type}", plate, vehicleType ?? "N/A");
            
            var fine = new
            {
                plate = plate,
                amount = 0,  // Placeholder - será actualizado por el equipo de multas
                currency = "ARS",
                reason = $"Vehiculo no registrado - Transito sin autorizacion{(vehicleType != null ? $" (Tipo: {vehicleType})" : "")}",
                status = "pending",
                issued_at = DateTime.UtcNow
            };
            
            var response = await _httpClient.PostAsJsonAsync("/api/fines", fine);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✓ Multa creada exitosamente para plate: {Plate}", plate);
                return true;
            }
            else
            {
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
