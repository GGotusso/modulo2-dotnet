# Módulo 2 - Sistema de Validación de Vehículos

Sistema de validación automática de vehículos para peajes. Realiza polling a una API externa cada 5 segundos para procesar el último tránsito y genera pagos o multas según el registro del vehículo.

##  Funcionalidad

El servicio procesa **el último vehículo que pasó por el toll** cada 5 segundos:

1. **Consulta el último tránsito** desde la API externa
2. **Verifica si el vehículo está registrado** en la base de datos
3. **Si está registrado**  Crea un pago automático (amount: 0, pendiente de actualización)
4. **Si NO está registrado**  Crea una multa (amount: 0, pendiente de actualización)
5. **No reprocesa** el mismo tránsito (trackea por transit_id)

##  Inicio Rápido

```powershell
# Iniciar servicios
.\start.ps1

# Ver logs en tiempo real
docker-compose logs -f validation-service

# Detener servicios
.\stop.ps1
```

##  Requisitos

- Docker Desktop
- Windows PowerShell
- Puerto 1433 disponible (SQL Server)

##  Arquitectura

```

  API Externa (Koyeb)                            
   GET  /api/transits    (último tránsito)    
   GET  /api/vehicles    (verificar registro) 
   POST /api/payments    (crear pago)         
   POST /api/fines       (crear multa)        

                     (polling cada 5 seg)

  ValidationService (.NET 8 Worker)              
   Polling cada 5 segundos                     
   Procesa último tránsito (limit=1)           
   Trackea por transit_id (no reprocesa)      
   Valida y envía a pagos/multas              

                    

  SQL Server 2022 (Opcional)                     
   Logs de auditoría local                    

```

##  Estructura del Proyecto

```
Modulo_2/
 src/
    ValidationService/         # Worker principal
       Worker.cs             # Lógica de polling y validación
       Program.cs            # Configuración DI
       Dockerfile            # Imagen Docker
       appsettings.json      # Configuración
    Shared/
        Services/
            GlobalDbApiClient.cs    # Cliente API HTTP
            DatabaseService.cs      # Servicio SQL local
 database/
    init.sql                  # Script inicialización BD
 docker-compose.yml            # Orquestación contenedores
 start.ps1                     # Iniciar servicios
 stop.ps1                      # Detener servicios
 README.md                     # Este archivo
```

##  Tecnologías

- **.NET 8.0**: Framework principal (Worker Service)
- **Docker**: Containerización
- **SQL Server 2022**: Base de datos local para logs (opcional)
- **HttpClient**: Consumo de API REST

##  Flujo de Trabajo

### 1. Polling del Último Tránsito
Cada 5 segundos consulta:
```
GET /api/transits?order_by=occurred_at&order_dir=desc&limit=1
```

**Respuesta:**
```json
{
  "data": [{
    "transit_id": "c4984ff0-e2e3-4e2c-bf62-c26a6e71b547",
    "vehicle_plate": "SIM516",
    "vehicle_type": "Automovil",
    "occurred_at": "Sun, 16 Nov 2025 18:12:55 GMT",
    "speed_kmh": "57.00"
  }],
  "limit": 1,
  "offset": 0
}
```

### 2. Verificación de Registro
Consulta si el vehículo está registrado:
```
GET /api/vehicles?plate=SIM516
```

**Respuesta:**
```json
{
  "data": [],  // Vacío = NO registrado
  "limit": 100,
  "offset": 0
}
```

### 3. Creación de Pago (si está registrado)
```
POST /api/payments
{
  "plate": "ABC123",
  "amount": 0,
  "currency": "ARS",
  "status": "pending",
  "method": "automatic",
  "requested_at": "2025-11-16T18:30:00Z"
}
```

### 4. Creación de Multa (si NO está registrado)
```
POST /api/fines
{
  "plate": "SIM516",
  "amount": 0,
  "currency": "ARS",
  "reason": "Vehiculo no registrado - Transito sin autorizacion (Tipo: Automovil)",
  "status": "pending",
  "issued_at": "2025-11-16T18:30:00Z"
}
```

##  Configuración

### appsettings.json
```json
{
  "GlobalDbApi": {
    "BaseUrl": "https://fun-bernetta-johannson-systems-v2-ba75677f.koyeb.app"
  },
  "PollingIntervalSeconds": 5,
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Modulo2DB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  }
}
```

##  Logs Importantes

**Tránsito ya procesado:**
```
 Ultimo transito ya fue procesado (ID: c4984ff0, Patente: SIM516)
```

**Nuevo tránsito detectado:**
```
Procesando nuevo transito: ID=abc123, Patente=XYZ789
Vehículo XYZ789 NO registrado - Creando multa
 Multa creada exitosamente para plate: XYZ789
```

##  Troubleshooting

### Error: Cannot open database "Modulo2DB"
Es **normal** y **no afecta el funcionamiento**. El SQL Server local es solo para logs opcionales. El servicio funciona correctamente sin la base de datos.

### Error: Port 1433 already in use
Otro SQL Server está corriendo. Detenerlo o cambiar el puerto en docker-compose.yml.

### No procesa nuevos tránsitos
Verificar que la API externa esté respondiendo:
```powershell
Invoke-RestMethod -Uri "https://fun-bernetta-johannson-systems-v2-ba75677f.koyeb.app/api/transits?limit=1"
```

##  Soporte

Para más información sobre la API externa, consultar api-docs.json en la raíz del proyecto.

---

**Repositorio:** https://github.com/GGotusso/modulo2-dotnet
