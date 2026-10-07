/* =====================================================================
   BibliotecaDB - Laboratorio 07
   Ejecutar en SSMS (o con sqlcmd -S . -E -i BibliotecaDB.sql).
   El script se puede ejecutar varias veces: recrea las tablas y los datos.
   ===================================================================== */
USE master;
GO
IF DB_ID(N'BibliotecaDB') IS NULL
    CREATE DATABASE BibliotecaDB;
GO
USE BibliotecaDB;
GO

IF OBJECT_ID(N'dbo.DetallePrestamo', N'U') IS NOT NULL DROP TABLE dbo.DetallePrestamo;
IF OBJECT_ID(N'dbo.Prestamos', N'U')       IS NOT NULL DROP TABLE dbo.Prestamos;
IF OBJECT_ID(N'dbo.Libros', N'U')          IS NOT NULL DROP TABLE dbo.Libros;
IF OBJECT_ID(N'dbo.Socios', N'U')          IS NOT NULL DROP TABLE dbo.Socios;
IF OBJECT_ID(N'dbo.Autores', N'U')         IS NOT NULL DROP TABLE dbo.Autores;
GO

CREATE TABLE dbo.Autores (
    AutorId      INT IDENTITY(1,1) CONSTRAINT PK_Autores PRIMARY KEY,
    Nombre       NVARCHAR(100) NOT NULL,
    Nacionalidad NVARCHAR(60)  NULL,
    Activo       BIT NOT NULL CONSTRAINT DF_Autores_Activo DEFAULT 1
);

CREATE TABLE dbo.Libros (
    LibroId    INT IDENTITY(1,1) CONSTRAINT PK_Libros PRIMARY KEY,
    Titulo     NVARCHAR(200) NOT NULL,
    ISBN       NVARCHAR(20)  NOT NULL CONSTRAINT UQ_Libros_ISBN UNIQUE,
    AutorId    INT NOT NULL CONSTRAINT FK_Libros_Autores REFERENCES dbo.Autores(AutorId),
    Ejemplares INT NOT NULL CONSTRAINT CK_Libros_Ejemplares CHECK (Ejemplares >= 0),
    Activo     BIT NOT NULL CONSTRAINT DF_Libros_Activo DEFAULT 1
);

CREATE TABLE dbo.Socios (
    SocioId INT IDENTITY(1,1) CONSTRAINT PK_Socios PRIMARY KEY,
    DNI     NVARCHAR(8)   NOT NULL CONSTRAINT UQ_Socios_DNI UNIQUE,
    Nombre  NVARCHAR(100) NOT NULL,
    Email   NVARCHAR(150) NULL,
    Activo  BIT NOT NULL CONSTRAINT DF_Socios_Activo DEFAULT 1
);

CREATE TABLE dbo.Prestamos (
    PrestamoId    INT IDENTITY(1,1) CONSTRAINT PK_Prestamos PRIMARY KEY,
    SocioId       INT NOT NULL CONSTRAINT FK_Prestamos_Socios REFERENCES dbo.Socios(SocioId),
    FechaPrestamo DATE NOT NULL CONSTRAINT DF_Prestamos_Fecha DEFAULT (CAST(GETDATE() AS DATE)),
    FechaLimite   DATE NOT NULL,
    Estado        NVARCHAR(20) NOT NULL CONSTRAINT DF_Prestamos_Estado DEFAULT N'Pendiente',
    CONSTRAINT CK_Prestamos_Estado CHECK (Estado IN (N'Pendiente', N'Devuelto')),
    CONSTRAINT CK_Prestamos_Fechas CHECK (FechaLimite >= FechaPrestamo)
);

CREATE TABLE dbo.DetallePrestamo (
    PrestamoId      INT NOT NULL CONSTRAINT FK_Detalle_Prestamos REFERENCES dbo.Prestamos(PrestamoId),
    LibroId         INT NOT NULL CONSTRAINT FK_Detalle_Libros    REFERENCES dbo.Libros(LibroId),
    FechaDevolucion DATE NULL,
    CONSTRAINT PK_DetallePrestamo PRIMARY KEY (PrestamoId, LibroId)
);
GO

/* ------------------------------ DATOS DE PRUEBA ------------------------------ */
INSERT INTO dbo.Autores (Nombre, Nacionalidad) VALUES
 (N'Gabriel Garcia Marquez', N'Colombiana'),
 (N'Mario Vargas Llosa',     N'Peruana'),
 (N'Isabel Allende',         N'Chilena'),
 (N'Julio Cortazar',         N'Argentina'),
 (N'Jorge Luis Borges',      N'Argentina'),
 (N'Miguel de Cervantes',    N'Espanola'),
 (N'Ricardo Palma',          N'Peruana'),
 (N'Cesar Vallejo',          N'Peruana');

INSERT INTO dbo.Libros (Titulo, ISBN, AutorId, Ejemplares) VALUES
 (N'Cien anos de soledad',                   N'9789000000001', 1, 4),
 (N'El amor en los tiempos del colera',      N'9789000000002', 1, 4),
 (N'Cronica de una muerte anunciada',        N'9789000000003', 1, 4),
 (N'La ciudad y los perros',                N'9789000000004', 2, 4),
 (N'Conversacion en La Catedral',            N'9789000000005', 2, 4),
 (N'La fiesta del Chivo',                   N'9789000000006', 2, 4),
 (N'La casa de los espiritus',               N'9789000000007', 3, 4),
 (N'Paula',                                 N'9789000000008', 3, 4),
 (N'Rayuela',                               N'9789000000009', 4, 4),
 (N'Bestiario',                             N'9789000000010', 4, 1),
 (N'Ficciones',                             N'9789000000011', 5, 4),
 (N'El Aleph',                              N'9789000000012', 5, 4),
 (N'Don Quijote de la Mancha',              N'9789000000013', 6, 4),
 (N'Novelas ejemplares',                    N'9789000000014', 6, 4),
 (N'Tradiciones peruanas',                  N'9789000000015', 7, 4),
 (N'Trilce',                                N'9789000000016', 8, 4),
 (N'Los heraldos negros',                   N'9789000000017', 8, 4),
 (N'Poemas humanos',                        N'9789000000018', 8, 4),
 (N'El coronel no tiene quien le escriba',  N'9789000000019', 1, 4),
 (N'Eva Luna',                              N'9789000000020', 3, 4);

INSERT INTO dbo.Socios (DNI, Nombre, Email) VALUES
 (N'70000001', N'Ana Torres Quispe',      N'ana.torres@correo.com'),
 (N'70000002', N'Luis Ramirez Soto',       N'luis.ramirez@correo.com'),
 (N'70000003', N'Maria Flores Vega',       N'maria.flores@correo.com'),
 (N'70000004', N'Carlos Mendoza Ruiz',    N'carlos.mendoza@correo.com'),
 (N'70000005', N'Lucia Paredes Diaz',      N'lucia.paredes@correo.com'),
 (N'70000006', N'Jorge Castillo Rojas',   N'jorge.castillo@correo.com'),
 (N'70000007', N'Rosa Salazar Leon',       N'rosa.salazar@correo.com'),
 (N'70000008', N'Pedro Huaman Chavez',     N'pedro.huaman@correo.com'),
 (N'70000009', N'Elena Vargas Nunez',      N'elena.vargas@correo.com'),
 (N'70000010', N'Miguel Ortiz Campos',    N'miguel.ortiz@correo.com');

DECLARE @hoy DATE = CAST(GETDATE() AS DATE);

INSERT INTO dbo.Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) VALUES
 (1, DATEADD(DAY,  -5, @hoy), DATEADD(DAY,   9, @hoy), N'Pendiente'),  -- Socio 1: 3 libros pendientes (tope)
 (2, DATEADD(DAY, -20, @hoy), DATEADD(DAY,  -6, @hoy), N'Pendiente'),  -- Con retraso de 6 días (multa S/ 9.00)
 (3, DATEADD(DAY, -16, @hoy), DATEADD(DAY,  -2, @hoy), N'Pendiente'),  -- Con retraso de 2 días (multa S/ 3.00)
 (4, DATEADD(DAY, -30, @hoy), DATEADD(DAY, -16, @hoy), N'Devuelto'),
 (5, DATEADD(DAY, -12, @hoy), DATEADD(DAY,   2, @hoy), N'Devuelto');

INSERT INTO dbo.DetallePrestamo (PrestamoId, LibroId, FechaDevolucion) VALUES
 (1, 1, NULL), (1, 2, NULL), (1, 3, NULL),
 (2, 4, DATEADD(DAY, -10, @hoy)), (2, 5, NULL),
 (3, 6, NULL),
 (4, 7, DATEADD(DAY, -20, @hoy)),
 (5, 8, DATEADD(DAY,  -4, @hoy)), (5, 9, DATEADD(DAY, -3, @hoy));

-- Ejemplares disponibles = stock - libros actualmente prestados (pendientes)
UPDATE dbo.Libros SET Ejemplares = Ejemplares - 1 WHERE LibroId IN (1, 2, 3, 5, 6);
GO
