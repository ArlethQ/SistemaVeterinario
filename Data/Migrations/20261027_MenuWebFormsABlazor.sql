SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Reemplazos directos de opciones WebForms que ya cuentan con vista Blazor.
UPDATE vt_menuDinamico SET Descripcion = 'Clientes, tutores y mascotas', Url = 'admision/clientes-pacientes' WHERE id = 8;
UPDATE vt_menuDinamico SET Descripcion = 'Recepción y triaje', Url = 'admision/ordenes' WHERE id = 9;
UPDATE vt_menuDinamico SET Descripcion = 'Agenda de citas', Url = 'admision/citas' WHERE id = 11;
UPDATE vt_menuDinamico SET Descripcion = 'Usuarios', Url = 'administracion/usuarios' WHERE id = 13;
UPDATE vt_menuDinamico SET Descripcion = 'Catálogos clínicos', Url = 'administracion/catalogos' WHERE id = 21;
UPDATE vt_menuDinamico SET Descripcion = 'Localidades y especialidades', Url = 'administracion/localidades' WHERE id = 23;
UPDATE vt_menuDinamico SET Descripcion = 'Historia clínica SOAP', Url = 'consulta/fichamedica' WHERE id = 26;
UPDATE vt_menuDinamico SET Descripcion = 'Órdenes de laboratorio', Url = 'diagnostico/laboratorio' WHERE id = 46;
UPDATE vt_menuDinamico SET Descripcion = 'Operaciones clínicas', Url = 'operaciones/clinicas' WHERE id = 47;
UPDATE vt_menuDinamico SET Descripcion = 'Protocolo y equipo quirúrgico', Url = 'quirofano/protocolo' WHERE id = 51;
UPDATE vt_menuDinamico SET Descripcion = 'Evaluación preanestésica', Url = 'quirofano/preanestesia' WHERE id = 53;
UPDATE vt_menuDinamico SET Descripcion = 'Protocolo quirúrgico', Url = 'quirofano/protocolo' WHERE id = 54;
UPDATE vt_menuDinamico SET Descripcion = 'Sala de recuperación', Url = 'quirofano/recuperacion' WHERE id = 55;
UPDATE vt_menuDinamico SET Descripcion = 'Ficha de hospitalización', Url = 'hospitalizacion/ficha' WHERE id = 56;
UPDATE vt_menuDinamico SET Descripcion = 'Monitoreo 24 horas', Url = 'hospitalizacion/monitoreo' WHERE id = 57;
UPDATE vt_menuDinamico SET Descripcion = 'Evolución médica', Url = 'consulta/evoluciones' WHERE id = 58;
UPDATE vt_menuDinamico SET Descripcion = 'Recetario médico', Url = 'consulta/recetario' WHERE id = 59;
UPDATE vt_menuDinamico SET Descripcion = 'Seguimiento clínico' WHERE LOWER(LTRIM(RTRIM(ISNULL(Url, '')))) = 'seguimiento/clinico';

-- Estas funciones quedaron integradas en una vista Blazor principal; se deshabilitan
-- para no mostrar opciones repetidas hacia la misma pantalla.
UPDATE vt_menuDinamico SET Habilitado = 0, Url = '' WHERE id IN (
    12, -- calendario incluido en Agenda de citas
    22, -- especialidades incluidas en Catálogos clínicos
    25, -- antecedentes incluidos en Historia clínica SOAP
    27, -- vacunación incluida en Historia clínica SOAP
    45, -- historial requiere seleccionar primero un NHCL
    48, -- especies y razas incluidas en Catálogos clínicos
    54, -- registro quirúrgico incluido en Protocolo y equipo quirúrgico
    60  -- recetas ambulatorias incluidas en Recetario médico
);

-- Nuevos grupos consolidados, creados solo si todavía no existen.
IF NOT EXISTS (SELECT 1 FROM vt_menuDinamico WHERE LOWER(LTRIM(RTRIM(ISNULL(Url, '')))) = 'seguimiento/clinico')
BEGIN
    INSERT INTO vt_menuDinamico (secNuevoMenu, Descripcion, PadreNuevoMenu, Habilitado, Url)
    SELECT CONVERT(varchar(50), ISNULL(MAX(TRY_CONVERT(int, secNuevoMenu)), 0) + 1),
           'Seguimiento clínico', '1', 1, 'seguimiento/clinico'
    FROM vt_menuDinamico;
END;

COMMIT TRANSACTION;
