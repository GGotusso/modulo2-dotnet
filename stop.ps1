# Detener servicios del Modulo 2
Write-Host "Deteniendo servicios..." -ForegroundColor Yellow
docker-compose down

Write-Host ""
Write-Host "Servicios detenidos" -ForegroundColor Green
Write-Host ""
Write-Host "Para eliminar tambien los volumenes (base de datos):" -ForegroundColor Yellow
Write-Host "docker-compose down -v" -ForegroundColor White
