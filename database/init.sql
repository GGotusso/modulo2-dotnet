-- Script de inicialización de base de datos para Módulo 2
-- Este script se ejecuta automáticamente al iniciar SQL Server

USE master;
GO

-- Crear base de datos si no existe
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Modulo2DB')
BEGIN
    CREATE DATABASE Modulo2DB;
    PRINT 'Base de datos Modulo2DB creada exitosamente';
END
GO

USE Modulo2DB;
GO

-- Tabla de logs de validación (opcional, para auditoría)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ValidationLogs')
BEGIN
    CREATE TABLE ValidationLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Patente NVARCHAR(20) NOT NULL,
        Usuario NVARCHAR(100),
        IsValid BIT NOT NULL,
        Mensaje NVARCHAR(500),
        ValidationTimestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModuloOrigen NVARCHAR(50),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE INDEX IX_ValidationLogs_Patente ON ValidationLogs(Patente);
    CREATE INDEX IX_ValidationLogs_ValidationTimestamp ON ValidationLogs(ValidationTimestamp);
    
    PRINT 'Tabla ValidationLogs creada exitosamente';
END
GO

-- Tabla de configuración (opcional)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Configuration')
BEGIN
    CREATE TABLE Configuration (
        ConfigKey NVARCHAR(100) PRIMARY KEY,
        ConfigValue NVARCHAR(500) NOT NULL,
        Description NVARCHAR(500),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    -- Insertar configuraciones por defecto
    INSERT INTO Configuration (ConfigKey, ConfigValue, Description)
    VALUES 
        ('RabbitMQ_InputQueue', 'cola.patente', 'Cola de entrada de patentes'),
        ('RabbitMQ_OutputQueueOK', 'cola.patenteOK', 'Cola de salida para pagos'),
        ('RabbitMQ_OutputQueueMulta', 'cola.multa', 'Cola de salida para multas'),
        ('GlobalDbApi_BaseUrl', 'https://fun-bernetta-johannson-systems-v2-ba75677f.koyeb.app', 'URL de la API de DB global');
    
    PRINT 'Tabla Configuration creada e inicializada';
END
GO

PRINT 'Inicialización de base de datos completada';
GO
