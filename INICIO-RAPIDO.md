# Inicio Rápido - Modulo2Web (.NET)

## Prerrequisitos

- .NET 8.0 SDK instalado
- Docker Desktop (para ejecutar RabbitMQ)

## Pasos de Inicio Rápido

### 1. Iniciar RabbitMQ con Docker

```bash
# Desde la raíz del proyecto (donde está docker-compose.yml)
docker-compose up -d rabbitmq
```

Verifica que RabbitMQ esté corriendo:
- Panel de administración: http://localhost:15672
- Usuario: `guest`
- Contraseña: `guest`

### 2. Configurar el Proyecto

#### Opción A: Usando appsettings.json (Ya configurado)

El archivo `appsettings.json` ya está configurado con los valores por defecto.

#### Opción B: Usando Variables de Entorno

En PowerShell:
```powershell
$env:RABBITMQ_URL="amqp://guest:guest@localhost:5672"
$env:QUEUE_ENTRADA="cola.entrada"
$env:QUEUE_PAGOS="cola.pagos"
$env:QUEUE_MULTAS="cola.multas"
$env:API_URL="https://rigid-raeann-johannson-systems-212c7d43.koyeb.app/api"
$env:PORT="3000"
```

### 3. Ejecutar la Aplicación

```bash
cd Modulo2Web
dotnet run
```

Deberías ver:
```
✅ Conectado a RabbitMQ
   - Cola entrada: cola.entrada
   - Cola pagos: cola.pagos
   - Cola multas: cola.multas

🚀 Procesador de patentes iniciado
📥 Esperando mensajes en cola.entrada...
```

### 4. Probar el Sistema

#### Enviar un mensaje de prueba a la cola

Puedes usar el panel de administración de RabbitMQ:

1. Ir a http://localhost:15672
2. Ir a la pestaña "Queues and Streams"
3. Click en "cola.entrada"
4. Expandir "Publish message"
5. En "Payload" poner:
```json
{
  "patente": "ABC123",
  "tipoVehiculo": "auto"
}
```
6. Click en "Publish message"

#### Verificar el procesamiento

En la consola de la aplicación .NET verás algo como:

```
📨 Mensaje recibido:
   Patente: ABC123
   Tipo de vehículo: auto
✅ Vehículo encontrado en base de datos
   Patente: ABC123
   Customer ID: xxxxx
✅ Mensaje enviado a cola.pagos
```

O si el vehículo no existe:

```
📨 Mensaje recibido:
   Patente: XYZ999
   Tipo de vehículo: auto
❌ Vehículo no encontrado en base de datos para patente: XYZ999
📤 Mensaje enviado a cola.multas
```

### 5. Verificar las Colas

Vuelve al panel de RabbitMQ y verifica que los mensajes fueron enviados a:
- `cola.pagos` (si el vehículo existe)
- `cola.multas` (si el vehículo no existe)

### 6. Detener la Aplicación

Presiona `Ctrl+C` en la consola para detener la aplicación.

### 7. Detener RabbitMQ

```bash
docker-compose down
```

## Comandos Útiles

### Compilar el proyecto
```bash
dotnet build
```

### Ejecutar en modo release
```bash
dotnet run --configuration Release
```

### Publicar para distribución
```bash
dotnet publish -c Release -o ./publish
```

### Ver logs de RabbitMQ
```bash
docker-compose logs -f rabbitmq
```

## Solución de Problemas

### Error: "No se puede conectar a RabbitMQ"
- Verifica que RabbitMQ esté corriendo: `docker ps`
- Verifica que el puerto 5672 esté disponible

### Error: "Canal no inicializado"
- Asegúrate de que RabbitMQ esté corriendo antes de iniciar la aplicación

### Los mensajes no se procesan
- Verifica que haya mensajes en `cola.entrada` en el panel de RabbitMQ
- Verifica que la aplicación esté corriendo sin errores
