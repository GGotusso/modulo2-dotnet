# Script para iniciar Modulo 2 - Validacion de Vehiculos
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Modulo 2 - Iniciando servicios..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar Docker
Write-Host "1. Verificando Docker..." -ForegroundColor Yellow
docker --version
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Docker no esta instalado o no esta corriendo" -ForegroundColor Red
    exit 1
}
Write-Host "   Docker OK" -ForegroundColor Green
Write-Host ""

# Limpiar contenedores anteriores
Write-Host "2. Limpiando contenedores anteriores..." -ForegroundColor Yellow
docker-compose down -v 2>$null
Write-Host "   Limpieza completa" -ForegroundColor Green
Write-Host ""

# Iniciar servicios
Write-Host "3. Iniciando servicios..." -ForegroundColor Yellow
Write-Host "   - SQL Server (puerto 1433)" -ForegroundColor Gray
Write-Host "   - ValidationService (polling API cada 10s)" -ForegroundColor Gray
Write-Host ""

docker-compose up -d --build

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Servicios iniciados correctamente!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "El servicio esta procesando transitos de:" -ForegroundColor Cyan
    Write-Host "https://fun-bernetta-johannson-systems-v2-ba75677f.koyeb.app" -ForegroundColor White
    Write-Host ""
    Write-Host "Para ver los logs en tiempo real:" -ForegroundColor Yellow
    Write-Host "docker-compose logs -f validation-service" -ForegroundColor White
    Write-Host ""
    Write-Host "Para detener los servicios:" -ForegroundColor Yellow
    Write-Host ".\stop-services.ps1" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "ERROR: Hubo un problema al iniciar los servicios" -ForegroundColor Red
    Write-Host "Revisa los logs con: docker-compose logs" -ForegroundColor Yellow
}
