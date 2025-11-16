# Módulo 2 - Validación de Vehículos# Módulo 2 - Sistema de Validación de Patentes



Sistema de validación automática de vehículos para peajes. Consulta tránsitos desde una API externa y genera pagos o multas según el registro del vehículo.## Descripción



## 🎯 FuncionalidadEl Módulo 2 es un **microservicio en .NET** que valida patentes vehiculares consultando la base de datos global del sistema mediante una API REST.



El servicio realiza polling cada 10 segundos a la API de tránsitos y por cada vehículo que pasa:### Funcionalidades



1. **Consulta** si el vehículo está registrado en la base de datos1. **Consume de RabbitMQ**: Recibe patentes del Módulo 1 a través de la cola `cola.patente`

2. **Si está registrado** → Crea un pago automático2. **Consulta API de DB Global**: Obtiene la información de usuario y patente registrados en el sistema

3. **Si NO está registrado** → Crea una multa3. **Valida la patente**: Corrobora si la patente está registrada y activa en el sistema

4. **Publica resultados en RabbitMQ**:

## 🚀 Inicio Rápido   - `cola.patenteOK`: Patentes validadas exitosamente → van a **PAGOS**

   - `cola.multa`: Patentes no validadas o inactivas → van a **MULTAS**

```powershell

# Iniciar servicios## Arquitectura

.\start.ps1

```

# Ver logs en tiempo real┌─────────────┐         ┌──────────────────┐

docker-compose logs -f validation-service│  Módulo 1   │────────▶│ cola.patente     │

└─────────────┘         └──────────────────┘

# Detener servicios                                │

.\stop.ps1                                │ Consume

```                                ▼

                   ┌────────────────────────────┐

## 📋 Requisitos                   │   ValidationService        │

                   │      (Módulo 2)            │

- Docker Desktop                   │                            │

- Windows PowerShell                   │  ┌──────────────────────┐  │

- Puerto 1433 disponible (SQL Server)                   │  │ 1. Recibe patente    │  │

                   │  │    de cola           │  │

## 🏗️ Arquitectura                   │  └──────────┬───────────┘  │

                   │             │              │

```                   │             ▼              │

┌─────────────────────────────────────────────────┐                   │  ┌──────────────────────┐  │

│  API Externa (Koyeb)                            │                   │  │ 2. Consulta API      │◄─┼─── API DB Global

│  └─ GET  /api/transits    (tránsitos)          │                   │  │    DB Global         │  │    (Usuario + Patente)

│  └─ GET  /api/vehicles    (verificar registro) │                   │  └──────────┬───────────┘  │

│  └─ POST /api/payments    (crear pago)         │                   │             │              │

│  └─ POST /api/fines       (crear multa)        │                   │             ▼              │

└─────────────────────────────────────────────────┘                   │  ┌──────────────────────┐  │

                    ↓                   │  │ 3. Valida si patente │  │

┌─────────────────────────────────────────────────┐                   │  │    está registrada   │  │

│  ValidationService (.NET 8 Worker)              │                   │  └──────────┬───────────┘  │

│  └─ Polling cada 10 segundos                    │                   │             │              │

│  └─ Procesa nuevos tránsitos                    │                   └─────────────┼──────────────┘

│  └─ Valida y envía a pagos/multas              │                                 │

└─────────────────────────────────────────────────┘                     ┌───────────┴───────────┐

                    ↓                     │                       │

┌─────────────────────────────────────────────────┐                     ▼                       ▼

│  SQL Server 2022                                 │            ┌──────────────────┐    ┌──────────────┐

│  └─ Logs de auditoría local                    │            │ cola.patenteOK   │    │ cola.multa   │

└─────────────────────────────────────────────────┘            │                  │    │              │

```            │ → PAGOS          │    │ → MULTAS     │

            └──────────────────┘    └──────────────┘

## 📁 Estructura del Proyecto```



```## Tecnologías

Modulo_2/

├── src/- **.NET 8.0**: Framework principal

│   ├── ValidationService/         # Worker principal- **RabbitMQ**: Sistema de mensajería

│   │   ├── Worker.cs             # Lógica de polling y validación- **SQL Server**: Base de datos (disponible para uso futuro)

│   │   ├── Program.cs            # Configuración DI- **Docker**: Containerización

│   │   └── appsettings.json      # Configuración- **Azure VM**: Deployment

│   └── Shared/

│       └── Services/## Flujo de Trabajo

│           ├── GlobalDbApiClient.cs    # Cliente API HTTP

│           └── DatabaseService.cs      # Servicio SQL local### 1. Recepción de Patente

├── database/El servicio escucha la cola `cola.patente` donde el Módulo 1 publica las patentes detectadas.

│   └── init.sql                  # Script inicialización BD

├── docker-compose.yml            # Orquestación contenedores**Mensaje de entrada:**

├── start.ps1                     # Iniciar servicios```json

└── stop.ps1                      # Detener servicios{

```  "patente": "ABC123",

  "timestamp": "2024-10-12T10:30:00Z",

## ⚙️ Configuración  "modulo": "Modulo1"

}

### Variables de Entorno (docker-compose.yml)```



```yaml### 2. Consulta a API de DB Global

GlobalDbApi__BaseUrl: "https://fun-bernetta-johannson-systems-v2-ba75677f.koyeb.app"El servicio consulta la API REST de la base de datos global para obtener información del usuario y patente:

PollingIntervalSeconds: 10- **Endpoint**: `GET /api/validacion/patente/{patente}`

ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=Modulo2DB;..."- **Respuesta**: Información del usuario registrado con esa patente

```

### 3. Validación

### Ajustar Intervalo de PollingEl servicio valida:

- ✅ Si la patente está registrada en el sistema

Editar `docker-compose.yml` o `appsettings.json`:- ✅ Si la patente está activa

- ✅ Si tiene un usuario asociado

```json

{### 4. Publicación de Resultado

  "PollingIntervalSeconds": 10

}#### Caso 1: Validación Exitosa → `cola.patenteOK` (PAGOS)

``````json

{

## 📊 Monitoreo  "patente": "ABC123",

  "usuario": "USUARIO123",

### Ver logs en tiempo real  "isValid": true,

  "mensaje": "Patente validada correctamente. Usuario registrado en el sistema.",

```powershell  "validationTimestamp": "2024-10-12T10:30:05Z",

docker-compose logs -f validation-service  "moduloOrigen": "Modulo2"

```}

```

### Consultar base de datos local

#### Caso 2: Validación Fallida → `cola.multa` (MULTAS)

```powershell```json

# Conectarse a SQL Server{

docker exec -it modulo2-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd'  "patente": "XYZ999",

  "usuario": null,

# Consultar logs de validación  "isValid": false,

USE Modulo2DB  "mensaje": "Patente NO registrada en el sistema",

SELECT TOP 10 * FROM ValidationLogs ORDER BY ValidationDate DESC  "validationTimestamp": "2024-10-12T10:30:05Z",

GO  "moduloOrigen": "Modulo2"

```}

```

## 🔧 Troubleshooting

## Tecnologías

### El servicio no inicia

- **.NET 8.0**: Framework principal

```powershell- **RabbitMQ**: Sistema de mensajería

# Ver logs de todos los contenedores- **SQL Server**: Base de datos

docker-compose logs- **Docker**: Containerización

- **Azure VM**: Deployment

# Verificar estado

docker-compose ps## Estructura del Proyecto

```

```

### Error de conexión a SQL ServerModulo_2/

├── src/

```powershell│   ├── ValidationService/            # Microservicio único de validación

# Verificar que SQL Server esté iniciado│   │   ├── Worker.cs                 # Lógica de validación

docker-compose logs sqlserver│   │   ├── Program.cs                # Configuración del servicio

│   │   ├── Dockerfile                # Containerización

# Esperar 30 segundos después del inicio│   │   └── appsettings.json          # Configuración

```│   └── Shared/                       # Modelos y servicios compartidos

│       ├── Models/                   # DTOs y modelos de datos

### No procesa tránsitos│       └── Services/                 # RabbitMQ, API client y SQL

├── database/

- Verificar conectividad a la API externa│   └── init.sql                      # Script de inicialización de BD

- Revisar logs: `docker-compose logs -f validation-service`├── docker-compose.yml                # Orquestación: RabbitMQ + SQL + Service

- Verificar que haya tránsitos nuevos en la API├── Modulo2.sln                       # Solución .NET

├── deploy-azure.ps1                  # Script de deployment a Azure

## 🧹 Limpieza├── start-local.ps1                   # Script para desarrollo local

└── README.md                         # Este archivo

```powershell```

# Detener y eliminar contenedores + volúmenes

docker-compose down -v## Base de Datos SQL Server



# Eliminar imágenesEl módulo incluye SQL Server para:

docker image prune -a- **Logging de validaciones**: Auditoría de todas las validaciones realizadas

```- **Configuración**: Parámetros del sistema

- **Futura extensibilidad**: Datos propios del módulo

## 📝 API Externa

### Tablas creadas automáticamente:

**Base URL:** `https://fun-bernetta-johannson-systems-v2-ba75677f.koyeb.app`

1. **ValidationLogs**: Registra cada validación de patente

### Endpoints utilizados:   - Id, Patente, Usuario, IsValid, Mensaje, ValidationTimestamp, ModuloOrigen



- `GET /api/transits` - Obtener tránsitos2. **Configuration**: Parámetros de configuración del sistema

- `GET /api/vehicles?plate={plate}` - Verificar registro   - ConfigKey, ConfigValue, Description, UpdatedAt

- `POST /api/payments` - Crear pago

- `POST /api/fines` - Crear multa## Requisitos Previos



## 🛠️ Desarrollo- Docker Desktop instalado

- .NET 8.0 SDK (para desarrollo local)

### Ejecutar localmente sin Docker- Azure CLI (para deployment en Azure)



```powershell## Configuración Local

cd src/ValidationService

dotnet run### 1. Clonar el repositorio

```

```bash

### Reconstruir imagencd Modulo_2

```

```powershell

docker-compose up -d --build validation-service### 2. Configurar variables de entorno

```

Las configuraciones principales están en `docker-compose.yml`. Ajustar según necesidad:

## 📄 Licencia

- **RabbitMQ**:

Este proyecto es parte del sistema de peajes inteligentes.  - Usuario: `admin`

  - Contraseña: `admin123`
  - Puerto AMQP: `5672`
  - Puerto Management: `15672`

- **SQL Server**:
  - Usuario: `sa`
  - Contraseña: `YourStrong@Passw0rd`
  - Puerto: `1433`

- **Colas RabbitMQ**:
  - **Entrada** (desde Módulo 1): `cola.patente`
  - **Salida OK** (hacia pagos): `cola.patenteOK`
  - **Salida multas**: `cola.multa`

### 3. Levantar los servicios

```bash
docker-compose up --build
```

### 4. Verificar servicios

- **RabbitMQ Management**: http://localhost:15672 (admin/admin123)
- **SQL Server**: localhost:1433 (sa/YourStrong@Passw0rd)

### 5. Ver logs

```bash
# Todos los servicios
docker-compose logs -f

# Servicio de validación específicamente
docker-compose logs -f validation-service
```

## Desarrollo Local (sin Docker)

### 1. Restaurar dependencias

```bash
dotnet restore
```

### 2. Configurar `appsettings.Development.json`

Crear en cada proyecto de servicio:

```json
{
  "RabbitMQ": {
    "Host": "localhost"
  },
  "GlobalDbApi": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### 3. Ejecutar servicios

```bash
# Ejecutar el servicio de validación
cd src/ValidationService
dotnet run
```

## Deployment en Azure VM

### Prerequisitos Azure

1. VM con Ubuntu 20.04 o superior
2. Docker y Docker Compose instalados
3. Puertos abiertos: 5672, 15672, 1433

### Paso 1: Conectar a la VM

```bash
ssh azureuser@<VM_IP_ADDRESS>
```

### Paso 2: Instalar Docker

```bash
# Actualizar paquetes
sudo apt update
sudo apt upgrade -y

# Instalar Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Agregar usuario al grupo docker
sudo usermod -aG docker $USER

# Instalar Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
```

### Paso 3: Transferir archivos

```bash
# Desde tu máquina local
scp -r Modulo_2 azureuser@<VM_IP_ADDRESS>:~/
```

### Paso 4: Levantar servicios en Azure

```bash
cd ~/Modulo_2
docker-compose up -d --build
```

### Paso 5: Verificar deployment

```bash
# Ver logs
docker-compose logs -f

# Ver estado
docker-compose ps

# Verificar conexión RabbitMQ
curl http://<VM_IP_ADDRESS>:15672
```

### Paso 6: Configurar firewall Azure

En Azure Portal:
1. Ir a la VM → Networking → Inbound port rules
2. Agregar reglas:
   - Puerto 5672 (RabbitMQ AMQP)
   - Puerto 15672 (RabbitMQ Management)
   - Puerto 1433 (SQL Server) - **IMPORTANTE para acceso remoto**

### Paso 7: Verificar base de datos

```bash
# Conectarse a SQL Server desde la VM
docker exec -it modulo2-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd'

# Verificar base de datos creada
1> SELECT name FROM sys.databases;
2> GO

# Ver logs de validación
1> USE Modulo2DB;
2> GO
1> SELECT TOP 10 * FROM ValidationLogs ORDER BY ValidationTimestamp DESC;
2> GO
```

## Monitoreo y Mantenimiento

### Ver estado de contenedores

```bash
docker-compose ps
```

### Reiniciar servicios

```bash
# Todos los servicios
docker-compose restart

# Servicio de validación
docker-compose restart validation-service
```

### Detener servicios

```bash
docker-compose down
```

### Detener y eliminar volúmenes (CUIDADO: elimina datos)

```bash
docker-compose down -v
```

### Ver logs en tiempo real

```bash
docker-compose logs -f --tail=100
```

## API de Base de datos Global

Los servicios esperan que la API global tenga estos endpoints:

### Validar patente

```http
GET /api/validacion/patente/{patente}
```

**Respuesta exitosa:**
```json
{
  "success": true,
  "data": {
    "usuario": "USUARIO123",
    "patente": "ABC123",
    "fechaRegistro": "2024-01-01T00:00:00Z",
    "activo": true
  }
}
```

### Validar usuario y patente

```http
GET /api/validacion/usuario/{usuario}/patente/{patente}
```

**Respuesta exitosa:**
```json
{
  "success": true,
  "data": {
    "usuario": "USUARIO123",
    "patente": "ABC123",
    "fechaRegistro": "2024-01-01T00:00:00Z",
    "activo": true
  }
}
```

## Troubleshooting

### RabbitMQ no conecta

```bash
# Verificar que RabbitMQ esté corriendo
docker-compose ps rabbitmq

# Ver logs de RabbitMQ
docker-compose logs rabbitmq

# Reiniciar RabbitMQ
docker-compose restart rabbitmq
```

### SQL Server no conecta

```bash
# Verificar estado
docker-compose ps sqlserver

# Verificar logs
docker-compose logs sqlserver

# Probar conexión
docker exec -it modulo2-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q 'SELECT @@VERSION'

# Verificar que la BD se inicializó
docker-compose logs sqlserver-init
```

### Ver logs de validaciones en la base de datos

```bash
# Conectarse a SQL Server
docker exec -it modulo2-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd'

# Consultar logs
1> USE Modulo2DB;
2> GO
1> SELECT * FROM ValidationLogs ORDER BY ValidationTimestamp DESC;
2> GO

# Ver estadísticas
1> SELECT IsValid, COUNT(*) as Total FROM ValidationLogs GROUP BY IsValid;
2> GO
```

### Servicio no procesa mensajes

```bash
# Verificar logs del servicio
docker-compose logs -f validation-service

# Verificar colas en RabbitMQ Management
# http://localhost:15672 → Queues

# Reiniciar servicio
docker-compose restart validation-service
```

### Problemas de memoria en Azure

```bash
# Ver uso de recursos
docker stats

# Ajustar límites en docker-compose.yml
# Agregar bajo cada servicio:
resources:
  limits:
    memory: 512M
```

## Seguridad

### Cambiar contraseñas en producción

Editar `docker-compose.yml`:

1. **RabbitMQ**: Cambiar `RABBITMQ_DEFAULT_PASS`
2. **SQL Server**: Cambiar `SA_PASSWORD`
3. Reiniciar: `docker-compose up -d --force-recreate`

### Usar variables de entorno

```bash
# Crear archivo .env
cat > .env << EOF
RABBITMQ_USER=admin
RABBITMQ_PASS=SecurePassword123!
SQL_SA_PASSWORD=VeryStrong@Pass456!
EOF

# Actualizar docker-compose.yml para usar ${VARIABLE}
```

## Contacto y Soporte

Para problemas o consultas sobre el Módulo 2, contactar al equipo de desarrollo.

## Licencia

Proyecto académico - Universidad [Nombre]
