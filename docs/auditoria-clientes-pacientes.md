# Auditoría: clientes, propietarios y pacientes

## Evidencia revisada

- `vt_personales.aspx` y `vt_personales.aspx.vb`.
- Catálogos WebForms de tipo de cliente, tipo de paciente, especie, raza, género, clase, categoría de ingreso y forma de obtención.
- `curvaCrecimiento.aspx`.
- `ClientesPacientes.razor`, `CatalogosClinicos.razor`, `HistorialPaciente.razor`.
- `AdmisionFacturacionService.cs` y `AdministracionClinicaService.cs`.

## Corregido

- Lectura y persistencia real del correo de facturación.
- Lectura y persistencia de grupo de cliente, términos de pago, tipo de persona, género, forma de pago y estado civil.
- Persistencia de tipo de cliente, estado y apuntes durante edición.
- Compatibilidad de códigos de identificación históricos `04`, `05` y `06`, manteniendo lectura de códigos abreviados anteriores.
- Edición visual de los campos comerciales y personales que antes se reemplazaban por constantes.
- Lectura, edición y persistencia de microchip, fallecimiento, fecha y motivo de muerte.
- Campos TUERI conectados: clase, nombre científico, peso, categoría, forma de obtención, región, provincia, ciudad y sector.
- Nombres científicos filtrados por clase y ciudades filtradas por provincia.

## Equivalencias confirmadas parcialmente

- Especies, razas, géneros, clases y categorías disponen de mantenimiento CRUD en `CatalogosClinicos.razor`.
- Clientes y pacientes permiten búsqueda, alta, edición, validación de documento y control de duplicados.
- La fotografía dispone de validación de formato y tamaño, pero aún debe migrarse el histórico y endurecerse la validación del contenido binario.

## Brechas pendientes

- La base conectada no contiene siete columnas de propietario presentes en el WebForms histórico. El servicio funciona en ambos esquemas y registra la diferencia; `scripts/sql/001-paridad-vt-clientes.sql` permite agregarlas de forma idempotente después de validar respaldo y ambiente.
- Verificar y migrar la curva de crecimiento con las mismas series y filtros del WebForms.
- Comparar contra una base de pruebas los identificadores reales de género y estado civil antes de considerarlos completos.
- Validar la integración histórica con el sistema de facturación externo; no se ejecutó contra producción.
