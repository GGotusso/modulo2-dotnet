# Migración de TypeScript/Node.js a .NET

## Resumen de la Migración

Este documento detalla la migración del proyecto `modulo2-web` de TypeScript/Node.js a .NET 8.0.

## Estructura de Archivos

### Proyecto Original (TypeScript)
```
src/
├── config/
│   └── index.ts
├── types/
│   └── index.ts
├── services/
│   ├── rabbitmq.service.ts
│   └── api.service.ts
├── processor/
│   └── patent.processor.ts
└── index.ts
```

### Proyecto Migrado (.NET)
```
Modulo2Web/
├── Config/
│   └── AppConfig.cs
├── Models/
│   └── VehicleMessage.cs
├── Services/
│   ├── RabbitMQService.cs
│   └── ApiService.cs
├── Processor/
│   └── PatentProcessor.cs
├── Program.cs
└── appsettings.json
```

## Mapeo de Tecnologías

| TypeScript/Node.js | .NET | Propósito |
|-------------------|------|-----------|
| `amqplib` | `RabbitMQ.Client 7.1.2` | Cliente RabbitMQ |
| `axios` | `HttpClient` | Cliente HTTP |
| `dotenv` | `Microsoft.Extensions.Configuration` | Configuración |
| `express` | N/A | No usado en este proyecto |
| TypeScript interfaces | C# classes/records | Modelos de datos |
| `package.json` | `.csproj` | Gestión de dependencias |
| `tsconfig.json` | `.csproj` PropertyGroup | Configuración del compilador |

## Diferencias Principales

### 1. Sistema de Tipos

**TypeScript:**
```typescript
export interface VehicleMessage {
    patente: string;
    tipoVehiculo: string;
}
```

**.NET:**
```csharp
public class VehicleMessage
{
    public string patente { get; set; } = string.Empty;
    public string tipoVehiculo { get; set; } = string.Empty;
}
```

### 2. Configuración

**TypeScript (dotenv):**
```typescript
import dotenv from 'dotenv';
dotenv.config();

export const config = {
    rabbitmq: {
        url: process.env.RABBITMQ_URL || 'amqp://...',
    }
};
```

**.NET (appsettings.json + Environment Variables):**
```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var appConfig = new AppConfig();
configuration.Bind(appConfig);
```

### 3. Inyección de Dependencias

**TypeScript (Manual):**
```typescript
export class PatentProcessor {
    private apiService: ApiService;
    private rabbitService: RabbitMQService;

    constructor() {
        this.apiService = new ApiService();
        this.rabbitService = new RabbitMQService();
    }
}
```

**.NET (DI Container):**
```csharp
var serviceProvider = new ServiceCollection()
    .AddSingleton<RabbitMQService>()
    .AddSingleton<ApiService>()
    .AddSingleton<PatentProcessor>()
    .BuildServiceProvider();

var processor = serviceProvider.GetRequiredService<PatentProcessor>();
```

### 4. Async/Await

**TypeScript:**
```typescript
async function processMessage(message: VehicleMessage): Promise<void> {
    const vehiculo = await this.apiService.buscarVehiculoPorPatente(message.patente);
}
```

**.NET:**
```csharp
private async Task ProcessMessageAsync(VehicleMessage message)
{
    var vehiculo = await _apiService.BuscarVehiculoPorPatenteAsync(message.patente);
}
```

### 5. RabbitMQ Client

**TypeScript (amqplib - callback-based):**
```typescript
amqplib.connect(url, (error, connection) => {
    connection.createChannel((channelError, channel) => {
        channel.assertQueue(queueName, { durable: true });
    });
});
```

**.NET (RabbitMQ.Client 7.x - async):**
```csharp
var factory = new ConnectionFactory { Uri = new Uri(url) };
_connection = await factory.CreateConnectionAsync();
_channel = await _connection.CreateChannelAsync();
await _channel.QueueDeclareAsync(queueName, durable: true, ...);
```

### 6. HTTP Client

**TypeScript (axios):**
```typescript
const response = await axios.get<VehicleResponse>(`${this.baseUrl}/vehicles`, {
    params: { plate: patente }
});
```

**.NET (HttpClient):**
```csharp
var response = await _httpClient.GetAsync($"{_baseUrl}/vehicles?plate={patente}");
var content = await response.Content.ReadAsStringAsync();
var vehicleResponse = JsonConvert.DeserializeObject<VehicleResponse>(content);
```

### 7. Serialización JSON

**TypeScript (nativo):**
```typescript
const content = JSON.parse(msg.content.toString());
const json = JSON.stringify(message);
```

**.NET (Newtonsoft.Json):**
```csharp
var content = JsonConvert.DeserializeObject<T>(message);
var json = JsonConvert.SerializeObject(message);
```

## Ventajas de la Versión .NET

1. **Tipado fuerte en tiempo de compilación**: Errores detectados antes de ejecutar
2. **Mejor rendimiento**: .NET generalmente más rápido que Node.js
3. **Gestión de memoria**: GC más eficiente para aplicaciones long-running
4. **IntelliSense mejorado**: Mejor autocompletado en VS Code
5. **Inyección de dependencias nativa**: Patrón más robusto y mantenible
6. **Async/Await más maduro**: Mejor manejo de excepciones en contextos async

## Equivalencias de Comandos

| Operación | TypeScript/Node.js | .NET |
|-----------|-------------------|------|
| Instalar dependencias | `npm install` | `dotnet restore` |
| Ejecutar desarrollo | `npm run dev` | `dotnet run` |
| Compilar | `npm run build` | `dotnet build` |
| Ejecutar producción | `npm start` | `dotnet run --configuration Release` |
| Ejecutar tests | `npm test` | `dotnet test` |

## Paquetes NuGet Instalados

```xml
<PackageReference Include="RabbitMQ.Client" Version="7.1.2" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.10" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.10" />
<PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.10" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.10" />
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.10" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

## Notas Importantes

1. **RabbitMQ.Client 7.x**: Esta versión usa una API completamente async. Las versiones antiguas usaban `IModel`, ahora se usa `IChannel`.

2. **Null Safety**: En .NET 8.0 con nullable reference types habilitado, es necesario inicializar todas las propiedades o marcarlas como nullable (`?`).

3. **Naming Conventions**: 
   - .NET usa PascalCase para propiedades públicas
   - Mantenemos los nombres de JSON en minúsculas para compatibilidad con la API

4. **Dispose Pattern**: .NET implementa `IDisposable` para liberar recursos de manera determinista.

## Migración de Código Existente

Si tienes código TypeScript adicional que migrar, sigue estos pasos:

1. **Clases/Interfaces** → `Models/NombreModelo.cs`
2. **Servicios** → `Services/NombreServicio.cs`
3. **Lógica de negocio** → `Processor/` o carpeta apropiada
4. **Configuración** → `appsettings.json` + `Config/`
5. **Dependencias npm** → Buscar equivalente en NuGet

## Compatibilidad

La versión .NET mantiene **100% de compatibilidad funcional** con la versión TypeScript:
- ✅ Mismas colas de RabbitMQ
- ✅ Mismo formato de mensajes
- ✅ Misma API consumida
- ✅ Misma lógica de procesamiento
- ✅ Mismos nombres de configuración
