# Script para enviar mensajes de prueba a RabbitMQ
param(
    [string]$Patente = "ABC123",
    [string]$TipoVehiculo = "auto"
)

Write-Host "Enviando mensaje de prueba a RabbitMQ..." -ForegroundColor Cyan
Write-Host "   Patente: $Patente" -ForegroundColor Yellow
Write-Host "   Tipo: $TipoVehiculo" -ForegroundColor Yellow
Write-Host ""

# Construir el mensaje JSON
$mensaje = @{
    patente = $Patente
    tipoVehiculo = $TipoVehiculo
} | ConvertTo-Json

Write-Host "Mensaje JSON:" -ForegroundColor Green
Write-Host $mensaje
Write-Host ""
Write-Host "COMO ENVIAR EL MENSAJE:" -ForegroundColor Cyan
Write-Host ""
Write-Host "Panel Web de RabbitMQ (Recomendado):" -ForegroundColor Yellow
Write-Host "1. Abre http://localhost:15672" -ForegroundColor White
Write-Host "2. Login: guest / guest" -ForegroundColor White
Write-Host "3. Ve a Queues -> cola.entrada -> Publish message" -ForegroundColor White
Write-Host "4. Pega el JSON de arriba en Payload" -ForegroundColor White
Write-Host "5. Click en Publish message" -ForegroundColor White
Write-Host ""
Write-Host "Luego ve a la consola donde corre 'dotnet run' para ver el resultado!" -ForegroundColor Green
