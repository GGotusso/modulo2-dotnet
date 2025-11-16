using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Dapper;

namespace Shared.Services;

public interface IDatabaseService
{
    Task LogValidation(string patente, string? usuario, bool isValid, string mensaje);
    Task<Dictionary<string, string>> GetConfiguration();
}

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(string connectionString, ILogger<DatabaseService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task LogValidation(string patente, string? usuario, bool isValid, string mensaje)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO ValidationLogs (Patente, Usuario, IsValid, Mensaje, ValidationTimestamp, ModuloOrigen)
                VALUES (@Patente, @Usuario, @IsValid, @Mensaje, @ValidationTimestamp, @ModuloOrigen)";

            await connection.ExecuteAsync(sql, new
            {
                Patente = patente,
                Usuario = usuario,
                IsValid = isValid,
                Mensaje = mensaje,
                ValidationTimestamp = DateTime.UtcNow,
                ModuloOrigen = "Modulo2"
            });

            _logger.LogDebug("Validación registrada en BD: {Patente} - {IsValid}", patente, isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando log de validación en BD");
            // No lanzar excepción, el logging es opcional
        }
    }

    public async Task<Dictionary<string, string>> GetConfiguration()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT ConfigKey, ConfigValue FROM Configuration";
            var configs = await connection.QueryAsync<(string Key, string Value)>(sql);

            return configs.ToDictionary(c => c.Key, c => c.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo configuración de BD");
            return new Dictionary<string, string>();
        }
    }
}
