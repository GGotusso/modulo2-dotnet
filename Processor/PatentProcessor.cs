using Modulo2Web.Config;
using Modulo2Web.Models;
using Modulo2Web.Services;

namespace Modulo2Web.Processor
{
    public class PatentProcessor
    {
        private readonly ApiService _apiService;
        private readonly RabbitMQService _rabbitService;
        private readonly AppConfig _config;

        public PatentProcessor(ApiService apiService, RabbitMQService rabbitService, AppConfig config)
        {
            _apiService = apiService;
            _rabbitService = rabbitService;
            _config = config;
        }

        public async Task StartAsync()
        {
            await _rabbitService.ConnectAsync();

            Console.WriteLine("\n🚀 Procesador de patentes iniciado");
            Console.WriteLine("📥 Esperando mensajes en cola.entrada...\n");

            await _rabbitService.ConsumeAsync<VehicleMessage>(
                _config.RabbitMQ.Queues.Entrada,
                ProcessMessageAsync
            );
        }

        private async Task ProcessMessageAsync(VehicleMessage message)
        {
            Console.WriteLine($"\n📨 Mensaje recibido:");
            Console.WriteLine($"   Patente: {message.patente}");
            Console.WriteLine($"   Tipo de vehículo: {message.tipoVehiculo}");

            try
            {
                // Buscar vehículo por patente en la base de datos
                var vehiculo = await _apiService.BuscarVehiculoPorPatenteAsync(message.patente);

                if (vehiculo != null && !string.IsNullOrEmpty(vehiculo.customer_id))
                {
                    // Vehículo encontrado con customer_id - enviar a cola.pagos
                    Console.WriteLine($"✅ Vehículo encontrado en base de datos");
                    Console.WriteLine($"   Patente: {vehiculo.plate}");
                    Console.WriteLine($"   Customer ID: {vehiculo.customer_id}");

                    var pagoMessage = new PagoMessage
                    {
                        patente = message.patente,
                        tipoVehiculo = message.tipoVehiculo
                    };

                    await _rabbitService.PublishAsync(_config.RabbitMQ.Queues.Pagos, pagoMessage);
                    Console.WriteLine($"✅ Mensaje enviado a cola.pagos");
                }
                else
                {
                    // Vehículo no encontrado o sin customer_id - enviar a cola.multas
                    if (vehiculo == null)
                    {
                        Console.WriteLine($"❌ Vehículo no encontrado en base de datos para patente: {message.patente}");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️  Vehículo encontrado pero sin customer_id asociado");
                    }

                    var multaMessage = new MultaMessage
                    {
                        patente = message.patente
                    };

                    await _rabbitService.PublishAsync(_config.RabbitMQ.Queues.Multas, multaMessage);
                    Console.WriteLine($"📤 Mensaje enviado a cola.multas");
                }
            }
            catch (Exception error)
            {
                Console.WriteLine($"❌ Error al procesar mensaje: {error.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            await _rabbitService.CloseAsync();
        }
    }
}
