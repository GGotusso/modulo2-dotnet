# Modulo2Web - Versión .NET

Sistema de procesamiento de patentes con RabbitMQ migrado a .NET Core.

## Descripción

Este proyecto es una migración del sistema Node.js/TypeScript original a .NET. Mantiene la misma funcionalidad:
- Procesa mensajes de patentes desde RabbitMQ
- Consulta una API para verificar vehículos
- Enruta mensajes a diferentes colas según el estado del vehículo

## Requisitos

- .NET 8.0 SDK o superior
- RabbitMQ Server
- Visual Studio Code o Visual Studio

## Estructura del Proyecto

```
Modulo2Web/
├── Config/           # Configuración de la aplicación
├── Models/           # Modelos de datos (DTOs)
├── Services/         # Servicios (RabbitMQ y API)
├── Processor/        # Lógica de procesamiento de patentes
├── Program.cs        # Punto de entrada de la aplicación
└── appsettings.json  # Configuración
```

## Configuración

### Archivo appsettings.json

```json
{
  "RabbitMQ": {
    "Url": "amqp://guest:guest@localhost:5672",
    "Queues": {
      "Entrada": "cola.entrada",
      "Pagos": "cola.pagos",
      "Multas": "cola.multas"
    }
  },
  "Api": {
    "Url": "https://rigid-raeann-johannson-systems-212c7d43.koyeb.app/api"
  },
  "Server": {
    "Port": 3000
  }
}
```

### Variables de Entorno

También puedes configurar la aplicación usando variables de entorno:

- `RABBITMQ_URL`: URL de conexión a RabbitMQ
- `QUEUE_ENTRADA`: Nombre de la cola de entrada
- `QUEUE_PAGOS`: Nombre de la cola de pagos
- `QUEUE_MULTAS`: Nombre de la cola de multas
- `API_URL`: URL de la API
- `PORT`: Puerto del servidor

## Instalación

1. Clonar el repositorio:
```bash
git clone https://github.com/nscanga/modulo2-web
cd modulo2-web/Modulo2Web
```

2. Restaurar paquetes NuGet:
```bash
dotnet restore
```

3. Configurar las variables de entorno o editar `appsettings.json`

## Ejecución

### Modo Desarrollo

```bash
dotnet run
```

### Compilar para Producción

```bash
dotnet build --configuration Release
```

### Ejecutar compilado

```bash
dotnet bin/Release/net8.0/Modulo2Web.dll
```

## Docker

Para ejecutar con Docker, asegúrate de que RabbitMQ esté corriendo:

```bash
docker-compose up -d rabbitmq
```

## Funcionalidad

1. La aplicación se conecta a RabbitMQ y escucha mensajes en `cola.entrada`
2. Por cada mensaje recibido:
   - Busca el vehículo en la API por patente
   - Si el vehículo existe y tiene `customer_id`: envía mensaje a `cola.pagos`
   - Si el vehículo no existe o no tiene `customer_id`: envía mensaje a `cola.multas`

## Diferencias con la versión Node.js

- Tipado fuerte con C#
- Inyección de dependencias nativa de .NET
- Manejo de configuración con el sistema de configuración de .NET
- Uso de async/await con Task en lugar de Promise
- RabbitMQ.Client en lugar de amqplib

## Paquetes NuGet Utilizados

- `RabbitMQ.Client` - Cliente de RabbitMQ para .NET
- `Microsoft.Extensions.Configuration` - Sistema de configuración
- `Microsoft.Extensions.Configuration.Json` - Soporte para archivos JSON
- `Microsoft.Extensions.Configuration.EnvironmentVariables` - Variables de entorno
- `Microsoft.Extensions.DependencyInjection` - Inyección de dependencias
- `Microsoft.Extensions.Http` - Cliente HTTP
- `Newtonsoft.Json` - Serialización/deserialización JSON

## Licencia

ISC
