SET XACT_ABORT ON;
BEGIN TRANSACTION;

UPDATE vt_menuDinamico SET Descripcion='Programación clínica', Url='programacion/clinica', Habilitado=1 WHERE id=14;
UPDATE vt_menuDinamico SET Descripcion='Catálogos diagnósticos, productos y laboratorio', Url='administracion/catalogos-operativos', Habilitado=1 WHERE id=16;
UPDATE vt_menuDinamico SET Descripcion='Bitácora de auditoría', Url='administracion/bitacora', Habilitado=1 WHERE id=24;
UPDATE vt_menuDinamico SET Descripcion='Reportes clínicos', Url='reportes/clinicos', Habilitado=1 WHERE id=43;

-- Funciones ahora consolidadas en las opciones anteriores.
UPDATE vt_menuDinamico SET Habilitado=0, Url='' WHERE id IN (
    35, -- categorías de productos
    38, -- productos e insumos
    41, -- grupos de laboratorio
    42, -- pruebas clínicas
    44, -- diagnósticos consolidados dentro de Reportes clínicos
    52  -- programación quirúrgica dentro de Programación clínica
);

COMMIT TRANSACTION;
