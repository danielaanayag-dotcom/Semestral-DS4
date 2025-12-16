
USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'GestionCitasMedicas')
BEGIN
    ALTER DATABASE GestionCitasMedicas SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE GestionCitasMedicas;
END
GO


CREATE DATABASE GestionCitasMedicas;
GO

USE GestionCitasMedicas;
GO

--CREACION DE TABLAS

CREATE TABLE Usuarios (
    UsuarioID INT PRIMARY KEY IDENTITY(1,1),
    Nombre NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Contrasena NVARCHAR(100) NOT NULL,
    Telefono NVARCHAR(20),
    Rol NVARCHAR(20) NOT NULL CHECK (Rol IN ('Usuario', 'Administrador')),
    FechaRegistro DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE ServiciosMedicos (
    ServicioID INT PRIMARY KEY IDENTITY(1,1),
    NombreServicio NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(255),
    Duracion INT NOT NULL, 
    Activo BIT DEFAULT 1
);
GO

CREATE TABLE Horarios (
    HorarioID INT PRIMARY KEY IDENTITY(1,1),
    ServicioID INT FOREIGN KEY REFERENCES ServiciosMedicos(ServicioID),
    FechaHora DATETIME NOT NULL,
    Disponible BIT DEFAULT 1
);
GO

CREATE TABLE Citas (
    CitaID INT PRIMARY KEY IDENTITY(1,1),
    UsuarioID INT FOREIGN KEY REFERENCES Usuarios(UsuarioID),
    HorarioID INT FOREIGN KEY REFERENCES Horarios(HorarioID),
    ServicioID INT FOREIGN KEY REFERENCES ServiciosMedicos(ServicioID),
    FechaCita DATETIME NOT NULL,
    Estado NVARCHAR(20) DEFAULT 'Pendiente' CHECK (Estado IN ('Pendiente', 'Confirmada', 'Cancelada', 'Completada')),
    FechaCreacion DATETIME DEFAULT GETDATE()
);
GO

-- INSERTAR DATOS DE PRUEBA

INSERT INTO Usuarios (Nombre, Email, Contrasena, Telefono, Rol) 
VALUES ('Admin Sistema', 'admin@clinica.com', 'admin123', '6000-0000', 'Administrador');

INSERT INTO Usuarios (Nombre, Email, Contrasena, Telefono, Rol) 
VALUES 
('Daniela Anaya', 'dani@email.com', 'dani123', '6123-4567', 'Usuario'),
('Santiago Ospina', 'santy@email.com', 'santy123', '6589-6251', 'Usuario'),
('Paola Herman', 'pao@email.com', 'contra456', '6245-6789', 'Usuario');

INSERT INTO ServiciosMedicos (NombreServicio, Descripcion, Duracion) 
VALUES 
('Consulta General', 'Consulta médica general', 30),
('Cardiología', 'Consulta especializada en cardiología', 45),
('Pediatría', 'Atención médica pediátrica', 30),
('Psicología', 'Consulta psicológica', 45);

INSERT INTO Horarios (ServicioID, FechaHora, Disponible) 
VALUES 
(1, '2024-12-17 09:00:00', 1),
(1, '2024-12-17 10:00:00', 1),
(1, '2024-12-17 11:00:00', 1),
(1, '2024-12-18 09:00:00', 1),
(2, '2024-12-17 14:00:00', 1),
(2, '2024-12-17 15:00:00', 1),
(2, '2024-12-18 14:00:00', 1),
(3, '2024-12-18 09:00:00', 1),
(3, '2024-12-18 10:00:00', 1),
(4, '2024-12-18 14:00:00', 1),
(4, '2024-12-19 14:00:00', 1);

GO

-- PROCEDIMIENTOS ALMACENADOS

CREATE PROCEDURE sp_LoginUsuario
    @Email NVARCHAR(100),
    @Contrasena NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT UsuarioID, Nombre, Email, Rol 
    FROM Usuarios 
    WHERE Email = @Email AND Contrasena = @Contrasena;
END
GO

-- 2. OBTENER SERVICIOS ACTIVOS
CREATE PROCEDURE sp_ObtenerServicios
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT ServicioID, NombreServicio, Descripcion, Duracion 
    FROM ServiciosMedicos 
    WHERE Activo = 1
    ORDER BY NombreServicio;
END
GO

-- 3. OBTENER HORARIOS DISPONIBLES POR SERVICIO
CREATE PROCEDURE sp_ObtenerHorariosDisponibles
    @ServicioID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT HorarioID, FechaHora 
    FROM Horarios 
    WHERE ServicioID = @ServicioID 
    AND Disponible = 1 
    AND FechaHora > GETDATE()
    ORDER BY FechaHora;
END
GO

-- 4. SOLICITAR/CREAR UNA CITA (Usuario)
CREATE PROCEDURE sp_SolicitarCita
    @UsuarioID INT,
    @HorarioID INT,
    @ServicioID INT,
    @FechaCita DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Insertar la cita con estado Pendiente
        INSERT INTO Citas (UsuarioID, HorarioID, ServicioID, FechaCita, Estado)
        VALUES (@UsuarioID, @HorarioID, @ServicioID, @FechaCita, 'Pendiente');
        
        -- Marcar el horario como no disponible
        UPDATE Horarios 
        SET Disponible = 0 
        WHERE HorarioID = @HorarioID;
        
        COMMIT TRANSACTION;
        
        SELECT SCOPE_IDENTITY() AS CitaID;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 5. OBTENER CITAS DE UN USUARIO
CREATE PROCEDURE sp_ObtenerCitasUsuario
    @UsuarioID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        c.CitaID,
        c.FechaCita,
        c.Estado,
        s.NombreServicio,
        s.Descripcion
    FROM Citas c
    INNER JOIN ServiciosMedicos s ON c.ServicioID = s.ServicioID
    WHERE c.UsuarioID = @UsuarioID
    ORDER BY c.FechaCita DESC;
END
GO

-- 6. CANCELAR CITA (Usuario - solo si está Pendiente)
CREATE PROCEDURE sp_CancelarCita
    @CitaID INT,
    @UsuarioID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @HorarioID INT;
    DECLARE @Estado NVARCHAR(20);

    -- Verificar que la cita pertenece al usuario y obtener su estado
    SELECT @HorarioID = HorarioID, @Estado = Estado
    FROM Citas 
    WHERE CitaID = @CitaID AND UsuarioID = @UsuarioID;
    
    -- Solo permitir cancelar si está en estado Pendiente
    IF @Estado = 'Pendiente'
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;
            
            -- Cambiar estado a Cancelada
            UPDATE Citas 
            SET Estado = 'Cancelada' 
            WHERE CitaID = @CitaID AND UsuarioID = @UsuarioID;
            
            -- Liberar el horario
            UPDATE Horarios 
            SET Disponible = 1 
            WHERE HorarioID = @HorarioID;
            
            COMMIT TRANSACTION;
            
            SELECT 1 AS Resultado, 'Cita cancelada exitosamente' AS Mensaje;
        END TRY
        BEGIN CATCH
            ROLLBACK TRANSACTION;
            SELECT 0 AS Resultado, ERROR_MESSAGE() AS Mensaje;
        END CATCH
    END
    ELSE
    BEGIN
        SELECT 0 AS Resultado, 'Solo se pueden cancelar citas en estado Pendiente' AS Mensaje;
    END
END
GO

-- 7. AGREGAR SERVICIO (Admin)
CREATE PROCEDURE sp_AgregarServicio
    @NombreServicio NVARCHAR(100),
    @Descripcion NVARCHAR(255),
    @Duracion INT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO ServiciosMedicos (NombreServicio, Descripcion, Duracion, Activo)
    VALUES (@NombreServicio, @Descripcion, @Duracion, 1);
    
    SELECT SCOPE_IDENTITY() AS ServicioID;
END
GO

-- 8. AGREGAR HORARIO (Admin)
CREATE PROCEDURE sp_AgregarHorario
    @ServicioID INT,
    @FechaHora DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO Horarios (ServicioID, FechaHora, Disponible)
    VALUES (@ServicioID, @FechaHora, 1);
    
    SELECT SCOPE_IDENTITY() AS HorarioID;
END
GO

-- 9. OBTENER TODAS LAS CITAS (Admin)
CREATE PROCEDURE sp_ObtenerTodasCitas
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        c.CitaID,
        u.Nombre AS NombrePaciente,
        u.Email,
        u.Telefono,
        s.NombreServicio,
        c.FechaCita,
        c.Estado,
        c.FechaCreacion
    FROM Citas c
    INNER JOIN Usuarios u ON c.UsuarioID = u.UsuarioID
    INNER JOIN ServiciosMedicos s ON c.ServicioID = s.ServicioID
    ORDER BY c.FechaCita DESC;
END
GO

-- 10. CONFIRMAR CITA (Admin)
CREATE PROCEDURE sp_ConfirmarCita
    @CitaID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @FilasAfectadas INT;
    
    -- Solo confirmar si está en estado Pendiente
    UPDATE Citas 
    SET Estado = 'Confirmada' 
    WHERE CitaID = @CitaID AND Estado = 'Pendiente';
    
    SET @FilasAfectadas = @@ROWCOUNT;
    
    SELECT @FilasAfectadas AS FilasAfectadas;
END
GO

-- 11. CANCELAR CITA (Admin - puede cancelar cualquier estado)
CREATE PROCEDURE sp_CancelarCitaAdmin
    @CitaID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @HorarioID INT;
    DECLARE @FilasAfectadas INT = 0;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Obtener el HorarioID de la cita
        SELECT @HorarioID = HorarioID 
        FROM Citas 
        WHERE CitaID = @CitaID;
        
        -- Actualizar estado de la cita a Cancelada
        UPDATE Citas 
        SET Estado = 'Cancelada' 
        WHERE CitaID = @CitaID;
        
        SET @FilasAfectadas = @@ROWCOUNT;
        
        -- Liberar el horario para que esté disponible nuevamente
        IF @HorarioID IS NOT NULL
        BEGIN
            UPDATE Horarios 
            SET Disponible = 1 
            WHERE HorarioID = @HorarioID;
        END
        
        COMMIT TRANSACTION;
        
        SELECT @FilasAfectadas AS FilasAfectadas;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SELECT 0 AS FilasAfectadas;
    END CATCH
END
GO

