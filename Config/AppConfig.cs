namespace Modulo2Web.Config
{
    public class RabbitMQConfig
    {
        public string Url { get; set; } = "amqp://guest:guest@localhost:5672";
        public QueueConfig Queues { get; set; } = new();
    }

    public class QueueConfig
    {
        public string Entrada { get; set; } = "cola.entrada";
        public string Pagos { get; set; } = "cola.pagos";
        public string Multas { get; set; } = "cola.multas";
    }

    public class ApiConfig
    {
        public string Url { get; set; } = "http://localhost:3001/api";
    }

    public class ServerConfig
    {
        public int Port { get; set; } = 3000;
    }

    public class AppConfig
    {
        public RabbitMQConfig RabbitMQ { get; set; } = new();
        public ApiConfig Api { get; set; } = new();
        public ServerConfig Server { get; set; } = new();
    }
}
