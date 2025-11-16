using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Modulo2Web.Config;
using Newtonsoft.Json;
using System.Text;

namespace Modulo2Web.Services
{
    public class RabbitMQService : IDisposable
    {
        private IConnection? _connection;
        private RabbitMQ.Client.IChannel? _channel;
        private readonly AppConfig _config;

        public RabbitMQService(AppConfig config)
        {
            _config = config;
        }

        public async Task ConnectAsync()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_config.RabbitMQ.Url)
                };

                Console.WriteLine($"🔌 Conectando a RabbitMQ: {_config.RabbitMQ.Url}");
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

            // Declarar colas
            await _channel.QueueDeclareAsync(
                queue: _config.RabbitMQ.Queues.Entrada,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            await _channel.QueueDeclareAsync(
                queue: _config.RabbitMQ.Queues.Pagos,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            await _channel.QueueDeclareAsync(
                queue: _config.RabbitMQ.Queues.Multas,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            Console.WriteLine("✅ Conectado a RabbitMQ");
            Console.WriteLine($"   - Cola entrada: {_config.RabbitMQ.Queues.Entrada}");
            Console.WriteLine($"   - Cola pagos: {_config.RabbitMQ.Queues.Pagos}");
            Console.WriteLine($"   - Cola multas: {_config.RabbitMQ.Queues.Multas}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error al conectar a RabbitMQ: {ex.Message}");
            Console.WriteLine($"   URL: {_config.RabbitMQ.Url}");
            Console.WriteLine("\n💡 Asegúrate de que RabbitMQ esté corriendo:");
            Console.WriteLine("   docker-compose up -d rabbitmq");
            throw;
        }
    }

        public async Task ConsumeAsync<T>(string queue, Func<T, Task> callback)
        {
            if (_channel == null)
                throw new InvalidOperationException("Canal no inicializado");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var content = JsonConvert.DeserializeObject<T>(message);

                    if (content != null)
                    {
                        await callback(content);
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al procesar mensaje: {ex.Message}");
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await _channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer);
        }

        public async Task PublishAsync<T>(string queue, T message)
        {
            if (_channel == null)
                throw new InvalidOperationException("Canal no inicializado");

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: queue,
                mandatory: false,
                basicProperties: properties,
                body: body
            );
        }

        public async Task CloseAsync()
        {
            if (_channel != null)
                await _channel.CloseAsync();
            if (_connection != null)
                await _connection.CloseAsync();
        }

        public void Dispose()
        {
            CloseAsync().Wait();
        }
    }
}
