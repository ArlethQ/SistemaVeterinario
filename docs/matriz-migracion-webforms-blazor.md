# Matriz de migración WebForms → Blazor

> Inventario generado desde código fuente. Estado conservador: ninguna página se marca completa sin prueba funcional contra la misma base de datos.

- WebForms inventariadas: **93**.
- Con code-behind localizado: **92**.
- Crystal Reports encontrados en el legado: **36**.
- Generado: **2026-10-29 10:26**.

## Resumen

| Página ASPX | Módulo | Estado | Página Blazor candidata | Servicios con objetos SQL coincidentes |
|---|---|---|---|---|
| curvaCrecimiento.aspx | Clientes y pacientes | parcial | HistorialPaciente.razor | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, KardexService, LaboratorioImagenService |
| Default.aspx | Órdenes y recepción | parcial | Home.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| FormasReportes\rep_baserecordatorio.aspx | Seguimiento clínico | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| FormasReportes\rep_basereport.aspx | Operaciones clínicas | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| FormasReportes\rep_i_stat.aspx | Seguimiento clínico | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| FormasReportes\rep_ListaAlertas.aspx | Seguimiento clínico | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| FormasReportes\rep_ListaAtenciones.aspx | Operaciones clínicas | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| FormasReportes\rep_ListaFallecidos.aspx | Seguimiento clínico | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, KardexService, LaboratorioImagenService |
| FormasReportes\rep_parteoperatorio.aspx | Quirófano y anestesia | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| FormasReportes\rep_TotalDiagnosticos.aspx | Operaciones clínicas | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| FormasReportes\rep_TotalDiagnosticosCondensado.aspx | Operaciones clínicas | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| Hosp_FichaHospitalizacion.aspx | Hospitalización | parcial | FichaHospitalizacion.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| Hosp_Monitoreo.aspx | Hospitalización | parcial | MonitoreoHospitalario.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| Hosp_PreFicha.aspx | Hospitalización | parcial | FichaHospitalizacion.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| Hosp_PreMonitoreo.aspx | Hospitalización | parcial | MonitoreoHospitalario.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| Hosp_PreProformas.aspx | Hospitalización | parcial | ProformasFacturacion.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| landing.aspx | Operaciones clínicas | parcial | Landing.razor | — |
| ReportesPaginas\vt_fichasanteriores.aspx | Historia clínica | parcial | HistorialPaciente.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_administracionCitasnew.aspx | Agenda y horarios | parcial | AgendaCitas.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_antecedentes.aspx | Historia clínica | parcial | FichaMedica.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_antFinal.aspx | Operaciones clínicas | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_apuntesahistorias.aspx | Historia clínica | parcial | ApuntesHistorias.razor | ConsultaExternaService |
| vt_autorizaciones.aspx | Operaciones clínicas | parcial | AutorizacionesAlta.razor | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, KardexService, LaboratorioImagenService |
| vt_baseRecordatorios.aspx | Seguimiento clínico | parcial | SeguimientoClinico.razor | — |
| vt_basereport.aspx | Operaciones clínicas | faltante | — | — |
| vt_Calendarizacion.aspx | Agenda y horarios | parcial | ProgramacionClinica.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_calendarizarhorariosmedicos.aspx | Agenda y horarios | parcial | ProgramacionClinica.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_categoria_ingreso.aspx | Clientes y pacientes | parcial | CatalogosClinicos.razor | AdministracionClinicaService, QuirofanoService |
| vt_clase.aspx | Clientes y pacientes | parcial | CatalogosClinicos.razor | AdministracionClinicaService, AdmisionFacturacionService |
| vt_ControlIngreso.aspx | Seguimiento clínico | parcial | OperacionesClinicas.razor | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService |
| vt_despacholab.aspx | Laboratorio e imagenología | parcial | OrdenesLaboratorio.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_despachorx.aspx | Laboratorio e imagenología | parcial | OrdenesImagenologia.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_diagnosticos.aspx | Operaciones clínicas | parcial | BandejaResultadosDiagnosticos.razor | AdministracionClinicaService, ConsultaExternaService, GestionMigradaService |
| vt_EnfermeriaKardex.aspx | Kardex y enfermería | parcial | KardexEnfermeria.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_EnfermeriaSignos.aspx | Kardex y enfermería | parcial | EnfermeriaSignos.razor | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService |
| vt_especialidades_localidades.aspx | Administración | parcial | CatalogosOperativos.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, KardexService, LaboratorioImagenService |
| vt_especialidades.aspx | Administración | parcial | CatalogosOperativos.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, KardexService, LaboratorioImagenService |
| vt_especies.aspx | Clientes y pacientes | parcial | CatalogosClinicos.razor | AdministracionClinicaService, AdmisionFacturacionService, KardexService, LaboratorioImagenService |
| vt_evoluciones.aspx | Historia clínica | parcial | EvolucionMedica.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService, ReportExportService |
| vt_EvolucionImp.aspx | Historia clínica | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService, ReportExportService |
| vt_fallecidos.aspx | Seguimiento clínico | parcial | SeguimientoClinico.razor | — |
| vt_genero.aspx | Clientes y pacientes | parcial | CatalogosClinicos.razor | AdministracionClinicaService, AdmisionFacturacionService, LaboratorioImagenService |
| vt_grupos_diagnosticos.aspx | Operaciones clínicas | faltante | — | ConsultaExternaService, GestionMigradaService |
| vt_hisDatMedicos.aspx | Historia clínica | parcial | FichaMedica.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_hisdatmedicospre.aspx | Historia clínica | parcial | FichaMedica.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_hisInfoMed.aspx | Historia clínica | parcial | FichaMedica.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_hisInfoMedPre.aspx | Historia clínica | parcial | FichaMedica.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_historialPaciente.aspx | Historia clínica | parcial | HistorialPaciente.razor | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_hojaanestesia.aspx | Quirófano y anestesia | parcial | HojaAnestesia.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_i_stat.aspx | Seguimiento clínico | parcial | OperacionesClinicas.razor | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, KardexService, LaboratorioImagenService |
| vt_imprimirfichas.aspx | Historia clínica | parcial | HistorialPaciente.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_labmedicospre.aspx | Laboratorio e imagenología | parcial | BandejaResultadosDiagnosticos.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_listaalertas.aspx | Seguimiento clínico | parcial | SeguimientoClinico.razor | — |
| vt_listadiagnosticos.aspx | Operaciones clínicas | faltante | — | AdministracionClinicaService, AuthService, ConsultaExternaService, GestionMigradaService |
| vt_listafichasmedicas.aspx | Historia clínica | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_localidades.aspx | Administración | parcial | LocalidadesAdministracion.razor | AdministracionClinicaService, AuthService, GestionMigradaService |
| vt_notas.aspx | Historia clínica | parcial | FichaMedica.razor | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService |
| vt_obtension.aspx | Clientes y pacientes | parcial | CatalogosClinicos.razor | — |
| vt_OrdenesDespacho.aspx | Órdenes y recepción | parcial | RecepcionOrdenes.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_OrdenesMenu.aspx | Órdenes y recepción | parcial | RecepcionOrdenes.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_parteoperatorio.aspx | Quirófano y anestesia | parcial | ProtocoloQuirurgico.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_permisos.aspx | Administración | parcial | RolesPermisos.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_personales.aspx | Clientes y pacientes | parcial | ClientesPacientes.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_Plan.aspx | Operaciones clínicas | parcial | SalaRecuperacion.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService, ReportExportService |
| vt_postquirurgico.aspx | Quirófano y anestesia | parcial | SalaRecuperacion.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_PreAnestesia.aspx | Quirófano y anestesia | parcial | PreAnestesia.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_preenfermeria.aspx | Kardex y enfermería | parcial | BandejaOrdenes.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_prequirurgico.aspx | Quirófano y anestesia | parcial | PreAnestesia.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_principal.aspx | Órdenes y recepción | parcial | Home.razor | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, KardexService, LaboratorioImagenService |
| vt_protocolo.aspx | Quirófano y anestesia | parcial | ProtocoloQuirurgico.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_quirArchivos.aspx | Quirófano y anestesia | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, KardexService, LaboratorioImagenService |
| vt_quirenfermeria.aspx | Quirófano y anestesia | parcial | KardexEnfermeria.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_QuirofanoIngreso.aspx | Quirófano y anestesia | parcial | OperacionesClinicas.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_QuirofanoPrincipal.aspx | Quirófano y anestesia | parcial | ProgramacionClinica.razor | GestionMigradaService, KardexService, QuirofanoService |
| vt_quirpersonales.aspx | Quirófano y anestesia | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, ConsultaExternaService, GestionMigradaService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_QuirProformas.aspx | Quirófano y anestesia | parcial | ProformasFacturacion.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_quirTeam.aspx | Quirófano y anestesia | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_razas.aspx | Clientes y pacientes | parcial | CatalogosClinicos.razor | AdministracionClinicaService, AdmisionFacturacionService, KardexService, LaboratorioImagenService |
| vt_RecepcionPaciente.aspx | Quirófano y anestesia | parcial | ChecklistPreoperatorio.razor | KardexService, QuirofanoService |
| vt_recetasnew.aspx | Historia clínica | faltante | — | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_RecetassinOrden.aspx | Historia clínica | parcial | RecetaSinOrden.razor | ConsultaExternaService, EFactCatalogService, ReportExportService |
| vt_Recuperacion.aspx | Quirófano y anestesia | parcial | SalaRecuperacion.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_RecuperacionPrincipal.aspx | Quirófano y anestesia | parcial | SalaRecuperacion.razor | QuirofanoService |
| vt_RegistroIngreso.aspx | Operaciones clínicas | parcial | OperacionesClinicas.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_tipo_cliente.aspx | Clientes y pacientes | parcial | ClientesPacientes.razor | AdministracionClinicaService |
| vt_tipo_paciente.aspx | Clientes y pacientes | parcial | ClientesPacientes.razor | AdministracionClinicaService |
| vt_UniProformas.aspx | Proformas y facturación | parcial | ProformasFacturacion.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_usuarios.aspx | Administración | parcial | UsuariosAdministracion.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_vacunas.aspx | Historia clínica | parcial | FichaMedica.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_vacunasfinal.aspx | Historia clínica | parcial | FichaMedica.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, InventarioService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_vercitas.aspx | Agenda y horarios | parcial | AgendaCitas.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_verOrdenes.aspx | Órdenes y recepción | parcial | RecepcionOrdenes.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |
| vt_vitacora.aspx | Administración | parcial | BitacoraAuditoria.razor | AdministracionClinicaService, AdmisionFacturacionService, AuthService, ConsultaExternaService, GestionMigradaService, HospitalizacionService, KardexService, LaboratorioImagenService, QuirofanoService |

## Evidencia por página

### `curvaCrecimiento.aspx`

- **Code-behind:** `curvaCrecimiento.aspx.vb`.
- **Propósito funcional:** Módulo Clientes y pacientes; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** 1033, cmbgrafico, form1, GridView1, Lblcliente, lblDiasTotal, Lblespecie, lblFechaNac, Lblnhclx, Lblnronhcl, lblPaciente, Lblsexo, Panel1, Panel2, Panel3, RadAjaxLoadingPanel1, RadAjaxPanel1, RadHtmlChart1, RadHtmlChart2, RadHtmlChart3, RadScriptManager1, SqlDataSource1, SqlDataSource2, SqlDataSource3.
- **Eventos y operaciones:** cmbgrafico_SelectedIndexChanged, Page_Load.
- **Tablas/vistas/procedimientos:** vt_clientes, vt_especie, vt_fichamedica, vt_pacientes.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/HistorialPaciente.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/HistorialPaciente.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

## Evidencia de validación — Fase 6 (2026-10-29)

| Alcance legado | Equivalente Blazor | Evidencia verificada | Estado / brecha |
|---|---|---|---|
| `vt_administracionCitasnew.aspx`, `vt_vercitas.aspx` | `AgendaCitas.razor` | Consulta multifecha con filtros de médico y texto; alta, detalle, confirmación, reagendamiento y cancelación motivada/auditada. Los slots se construyen desde `vt_horariosMedicos` por médico/localidad/intervalo y aplican excepciones. Se previene la superposición activa. | **Implementado en Fase 6.** El reagendamiento conserva la localidad de la cita, normaliza la hora y vuelve a consultar la fila antes de confirmar éxito; la cancelación usa `ci_refeliminacion` y `ci_delete` solo cuando existen. |
| `vt_calendarizarhorariosmedicos.aspx` | `ProgramacionClinica.razor` | CRUD de horarios por médico/localidad/día, validación de rango y superposición; CRUD lógico de excepciones con motivo y localidad. Se verifica `sys.columns` antes de ejecutar consultas dependientes del esquema. | **Implementado en Fase 6.** Si falta una columna requerida, se expone el error de esquema sin ocultarlo. |
| `vt_Calendarizacion.aspx` | `ProgramacionClinica.razor` | Alta/edición/cancelación de cirugía, filtro temporal, paciente/procedimiento, localidad, control de cruce de médico/localidad y bloqueo por excepción; validación previa de columnas. | **Implementado en Fase 6.** Requiere prueba funcional en una base no productiva con datos representativos. |
| `vt_OrdenesMenu.aspx`, `vt_OrdenesDespacho.aspx` | `RecepcionOrdenes.razor` | Alta de orden, búsqueda de paciente, especialidad/médico, triaje y destinos clínicos locales. | **Implementado en Fase 6.** No consume Business Central ni tablas de bodega asociadas; las integraciones externas se limitan a las APIs configuradas de E-FACT. |
| `vt_verOrdenes.aspx` | `RecepcionOrdenes.razor` | Tablero filtrable, cambio auditable de médico/especialidad, anulación con usuario, y exportación real de las órdenes mostradas a PDF y Excel. | **Implementado en Fase 6.** Bitácora registra los cambios de asignación, anulación, reagendamiento y cancelación. |
| `vt_principal.aspx`, `Default.aspx` | `Home.razor` | Accesos a agenda/órdenes, indicadores, órdenes activas y navegación directa por orden/NHCL. | **Implementado en Fase 6 para el alcance clínico.** Las integraciones externas del proyecto se configuran mediante E-FACT. |

### Pruebas y límites de la validación

- Compilación: `dotnet build HosvetKardex.Web.csproj -o .codex-build/phase6 --no-restore` — correcta, 0 advertencias y 0 errores.
- Revisión estructural del code-behind de las nueve pantallas solicitadas y de los cuatro equivalentes Blazor.
- No se ejecutaron escrituras contra base de datos, producción ni E-FACT.
- La detección de columnas agregada es de solo lectura (`sys.columns`); si faltan columnas obligatorias, el módulo detiene esa operación con un error de esquema explícito.

### `Default.aspx`

- **Code-behind:** `Default.aspx.vb`.
- **Propósito funcional:** Módulo Órdenes y recepción; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** chkRemember, claves, cmblocalidad, datosuser, eyeIcon, form1, Ingresar, lb_nombre, Lb_usuario, lbclave, lberror1, lberror2, lblHostName, lblIPAddress, Lbltoken, Panel2, Password, RadAjaxPanel1, RadButton1, RadButton2, ScriptManager1, ShowAntigua, ShowConfirmacion, ShowNueva, txt_antigua.
- **Eventos y operaciones:** Ingresar_Click, Page_Load, RadButton1_Click, RadButton2_Click.
- **Tablas/vistas/procedimientos:** vt_bodegasBC, vt_ingresodiario, vt_parametros, vt_parametrosBC, vt_principal, vt_usuarioLocalidades, vt_usuarios.
- **Archivos físicos:** infoEForVets/login_vet_hero.jpg, login.jpeg.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/Home.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/Home.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `FormasReportes\rep_baserecordatorio.aspx`

- **Code-behind:** `rep_baserecordatorio.aspx.vb`.
- **Propósito funcional:** Módulo Seguimiento clínico; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** CrystalReportSource1, CrystalReportViewer1, form1.
- **Eventos y operaciones:** CrystalReportViewer1_Unload, Page_Load.
- **Tablas/vistas/procedimientos:** vt_baserecordatorio, vt_clientes, vt_especialidades, vt_pacientes, vt_usuarios, vt_usuarios_1, vt_vitacoravacuna.
- **Archivos físicos:** /reports/vt_baserecordatorio.rpt, reports\vt_baserecordatorio.rpt.
- **Reportes Crystal:** /reports/vt_baserecordatorio.rpt, reports\vt_baserecordatorio.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `FormasReportes\rep_basereport.aspx`

- **Code-behind:** `rep_basereport.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** CrystalReportSource1, CrystalReportViewer1, form1.
- **Eventos y operaciones:** CrystalReportViewer1_Unload, Page_Load.
- **Tablas/vistas/procedimientos:** vt_basereport, vt_clientes, vt_especialidades, vt_ordenAtencion, vt_usuarios.
- **Archivos físicos:** /reports/vt_basereport.rpt, reports\vt_basereport.rpt.
- **Reportes Crystal:** /reports/vt_basereport.rpt, reports\vt_basereport.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `FormasReportes\rep_i_stat.aspx`

- **Code-behind:** `rep_i_stat.aspx.vb`.
- **Propósito funcional:** Módulo Seguimiento clínico; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** CrystalReportSource1, CrystalReportViewer1, form1.
- **Eventos y operaciones:** Page_Load.
- **Tablas/vistas/procedimientos:** vt_clientes, vt_especialidades, vt_istat, vt_pacientes, vt_usuarios, vt_usuarios_1.
- **Archivos físicos:** /reports/vt_istat.rpt, reports\vt_istat.rpt.
- **Reportes Crystal:** /reports/vt_istat.rpt, reports\vt_istat.rpt.
- **Correo:** No detectado.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `FormasReportes\rep_ListaAlertas.aspx`

- **Code-behind:** `rep_ListaAlertas.aspx.vb`.
- **Propósito funcional:** Módulo Seguimiento clínico; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** 1033, CrystalReportSource1, CrystalReportViewer1, form1.
- **Eventos y operaciones:** CrystalReportViewer1_Unload, Page_Load.
- **Tablas/vistas/procedimientos:** vt_clientes, vt_especialidades, vt_fichamedica, vt_listaAlertasrpt, vt_pacientes, vt_usuarios.
- **Archivos físicos:** /reports/vt_listaAlertasrpt.rpt, reports\vt_listaAlertasrpt.rpt.
- **Reportes Crystal:** /reports/vt_listaAlertasrpt.rpt, reports\vt_listaAlertasrpt.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `FormasReportes\rep_ListaAtenciones.aspx`

- **Code-behind:** `rep_ListaAtenciones.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** CrystalReportSource1, CrystalReportViewer1, form1.
- **Eventos y operaciones:** CrystalReportViewer1_Unload, Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_clientes, vt_diagnosticos, vt_especialidades, vt_fichamedica, vt_listafichasmedicarpt, vt_listafichasmedicarptcondensado, vt_localidades, vt_ordenAtencion, vt_pacientes, vt_usuarios, vt_usuarios_1.
- **Archivos físicos:** /reports/vt_listafichasmedicarpt.rpt, /reports/vt_listafichasmedicarptcondensado.rpt, reports\vt_listafichasmedicarpt.rpt.
- **Reportes Crystal:** /reports/vt_listafichasmedicarpt.rpt, /reports/vt_listafichasmedicarptcondensado.rpt, reports\vt_listafichasmedicarpt.rpt.
- **Correo:** No detectado.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `FormasReportes\rep_ListaFallecidos.aspx`

- **Code-behind:** `rep_ListaFallecidos.aspx.vb`.
- **Propósito funcional:** Módulo Seguimiento clínico; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** CrystalReportSource1, CrystalReportViewer1, form1.
- **Eventos y operaciones:** CrystalReportViewer1_Unload, Page_Load.
- **Tablas/vistas/procedimientos:** vt_clientes, vt_listafallecidos, vt_motivomuerte, vt_pacientes.
- **Archivos físicos:** /reports/vt_listafallecidos.rpt, reports\vt_listafallecidos.rpt.
- **Reportes Crystal:** /reports/vt_listafallecidos.rpt, reports\vt_listafallecidos.rpt.
- **Correo:** No detectado.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `FormasReportes\rep_parteoperatorio.aspx`

- **Code-behind:** `rep_parteoperatorio.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** CrystalReportSource1, CrystalReportViewer1, form1.
- **Eventos y operaciones:** CrystalReportViewer1_Unload, Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_DiagExtras, vt_parteoperatorio, vt_ProcExtras, vt_productos, vt_usuarios.
- **Archivos físicos:** /reports/vt_parteoperatorio.rpt, reports\vt_parteoperatorio.rpt.
- **Reportes Crystal:** /reports/vt_parteoperatorio.rpt, reports\vt_parteoperatorio.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `FormasReportes\rep_TotalDiagnosticos.aspx`

- **Code-behind:** `rep_TotalDiagnosticos.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** CrystalReportSource1, CrystalReportViewer1, form1.
- **Eventos y operaciones:** Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_clientes, vt_diagnosticos, vt_fichamedica, vt_ordenAtencion, vt_pacientes, vt_totaldiagnosticosrpt, vt_usuarios.
- **Archivos físicos:** /reports/vt_totaldiagnosticosrpt.rpt, reports\vt_totaldiagnosticosrpt.rpt.
- **Reportes Crystal:** /reports/vt_totaldiagnosticosrpt.rpt, reports\vt_totaldiagnosticosrpt.rpt.
- **Correo:** No detectado.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `FormasReportes\rep_TotalDiagnosticosCondensado.aspx`

- **Code-behind:** `rep_TotalDiagnosticosCondensado.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** CrystalReportSource1, CrystalReportViewer1, form1.
- **Eventos y operaciones:** Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_clientes, vt_diagnosticos, vt_fichamedica, vt_ordenAtencion, vt_pacientes, vt_totaldiagnosticoscondensadorpt, vt_usuarios.
- **Archivos físicos:** /reports/vt_totaldiagnosticoscondensadorpt.rpt, reports\vt_totaldiagnosticoscondensadorpt.rpt.
- **Reportes Crystal:** /reports/vt_totaldiagnosticoscondensadorpt.rpt, reports\vt_totaldiagnosticoscondensadorpt.rpt.
- **Correo:** No detectado.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `Hosp_FichaHospitalizacion.aspx`

- **Code-behind:** `Hosp_FichaHospitalizacion.aspx.vb`.
- **Propósito funcional:** Módulo Hospitalización; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** 1033, ali11, ali110, ali111, ali112, ali113, ali114, ali115, ali116, ali117, ali118, ali119, ali12, ali120, ali121, ali122, ali123, ali124, ali13, ali14, ali15, ali16, ali17, ali18, ali19.
- **Eventos y operaciones:** No detectado.
- **Tablas/vistas/procedimientos:** Hosp_FichaHospitalizacion, qvt_via, vt_bodegasBC, vt_cie10, vt_clientes, vt_diagnosticos, vt_especialidades, vt_especie, vt_estudiantes, vt_estudiantesorden, vt_facturasBChospi, vt_labOrdenes, vt_motivomuerte, vt_ordenAtencion, vt_pacientes, vt_pruebas_laboratorio, vt_reservaProductosStockBC, vt_usuarios.
- **Archivos físicos:** /imagenes/buscar0.png, /imagenes/Delete.png, /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/FichaHospitalizacion.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/FichaHospitalizacion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `Hosp_Monitoreo.aspx`

- **Code-behind:** `Hosp_Monitoreo.aspx.vb`.
- **Propósito funcional:** Módulo Hospitalización; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** 1033, agregarproducto, btnafactura, btnbusquedamedicamento, btncerrarficha, BtninsertarObs, btnNuevo, btnnuevo1, btnPrint, btnSave, chkfallecido, cmbmotivomuerte, cmbvia, Content1, Content2, ContentPlaceHolder1, ddlNumFormulario, funcioncargo, Grdmed, grdMedicinas, head, imp1, Iniciales, Inicialesx, Label1.
- **Eventos y operaciones:** agregarproducto_Click, btnafactura_Click, btnbusquedamedicamento_Click, btncerrarficha_Click, BtninsertarObs_Click, btnNuevo_Click, btnSave_Click, ddlNumFormulario_SelectedIndexChanged, Grdmed_SelectedIndexChanging, grdMedicinas_ItemCommand.
- **Tablas/vistas/procedimientos:** Hosp_Monitoreo, qvt_via, vt_bodegasBC, vt_clientes, vt_diagnosticos, vt_especialidades, vt_especie, vt_facturasBChospi, vt_motivomuerte, vt_ordenAtencion, vt_pacientes, vt_reservaProductosStockBC, vt_tipos_usuarios, vt_usuarios.
- **Archivos físicos:** /imagenes/agregar.png, /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/MonitoreoHospitalario.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/MonitoreoHospitalario.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `Hosp_PreFicha.aspx`

- **Code-behind:** `Hosp_PreFicha.aspx.vb`.
- **Propósito funcional:** Módulo Hospitalización; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Buscar, BuscarC, Content1, Content2, ContentPlaceHolder1, GridView1, GridView2, GridView3, GridView4, GridView5, GridView6, GridView7, GridView8, grv_Ordenes_Atencion, head, imgPacienteGrd, Label1, Label2, Label3, Label4, Label5, Lbespecialidad, Lbiduser, lbl_Cliente, lbl_esp_numero.
- **Eventos y operaciones:** Buscar_Click, BuscarC_Click, GridView1_SelectedIndexChanging, GridView2_SelectedIndexChanging, GridView3_SelectedIndexChanging, GridView4_SelectedIndexChanging, GridView5_SelectedIndexChanging, GridView7_SelectedIndexChanging, GridView8_SelectedIndexChanging, grv_Ordenes_Atencion_RowCommand, Page_Load.
- **Tablas/vistas/procedimientos:** Hosp_FichaHospitalizacion, Hosp_PreFicha, vt_cie10, vt_clase, vt_clientes, vt_clientes_1, vt_diagnosticos, vt_especialidades, vt_especie, vt_fichamedica, vt_hisDatMedicos, vt_labOrdenes, vt_localidades, vt_nombres_cientificos, vt_ordenAtencion, vt_pacientes, vt_pacientes_1, vt_productos, vt_pruebas_laboratorio, vt_razas, vt_rxOrdenes, vt_sexo, vt_usuarios.
- **Archivos físicos:** /imagenes/select20.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/FichaHospitalizacion.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/FichaHospitalizacion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `Hosp_PreMonitoreo.aspx`

- **Code-behind:** `Hosp_PreMonitoreo.aspx.vb`.
- **Propósito funcional:** Módulo Hospitalización; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Buscar, BuscarC, Content1, Content2, ContentPlaceHolder1, GridView1, GridView2, GridView3, GridView4, GridView5, GridView6, GridView7, GridView8, grv_Ordenes_Atencion, head, imgPacienteGrd, Label1, Label2, Label3, Label4, Label5, Lbespecialidad, Lbiduser, lbl_Cliente, lbl_esp_numero.
- **Eventos y operaciones:** Buscar_Click, BuscarC_Click, GridView1_SelectedIndexChanging, GridView2_SelectedIndexChanging, GridView3_SelectedIndexChanging, GridView4_SelectedIndexChanging, GridView5_SelectedIndexChanging, GridView7_SelectedIndexChanging, GridView8_SelectedIndexChanging, grv_Ordenes_Atencion_RowCommand, Page_Load.
- **Tablas/vistas/procedimientos:** Hosp_Monitoreo, Hosp_PreMonitoreo, vt_cie10, vt_clase, vt_clientes, vt_clientes_1, vt_diagnosticos, vt_especialidades, vt_especie, vt_fichamedica, vt_hisDatMedicos, vt_labOrdenes, vt_localidades, vt_nombres_cientificos, vt_ordenAtencion, vt_pacientes, vt_pacientes_1, vt_productos, vt_pruebas_laboratorio, vt_razas, vt_rxOrdenes, vt_sexo, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/MonitoreoHospitalario.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/MonitoreoHospitalario.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `Hosp_PreProformas.aspx`

- **Code-behind:** `Hosp_PreProformas.aspx.vb`.
- **Propósito funcional:** Módulo Hospitalización; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** BuscarC, Content1, Content2, ContentPlaceHolder1, GridView7, GridView8, grv_Ordenes_Atencion, head, Image1, imgPacienteGrd, Label1, Label5, LbCodCli, Lbespecialidad, Lbiduser, lbl_cedula, lbl_Cliente, lbl_esp_numero, lbl_etapa, lbl_fecha, lbl_hora, lbl_hospi, lbl_id_medico, lbl_idBC, lbl_idcliente.
- **Eventos y operaciones:** BuscarC_Click, EliminaReservas, GridView7_SelectedIndexChanging, GridView8_SelectedIndexChanging, grv_Ordenes_Atencion_RowCommand, Page_Load, refrescar_Click.
- **Tablas/vistas/procedimientos:** Hosp_PreProformas, vt_bodegasBC, vt_clase, vt_clientes, vt_clientes_1, vt_diagnosticos, vt_especialidades, vt_especie, vt_facturasBChospi, vt_labOrdenes, vt_nombres_cientificos, vt_ordenAtencion, vt_pacientes, vt_pacientes_1, vt_razas, vt_reservaProductosStockBC, vt_sexo, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/ProformasFacturacion.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ProformasFacturacion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `landing.aspx`

- **Code-behind:** No localizado.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** contacto, funciones, modulos, petModal, planes, videoModal.
- **Eventos y operaciones:** No detectado.
- **Tablas/vistas/procedimientos:** No detectado.
- **Archivos físicos:** infoEForVets/eForPetsMovil.jpeg, infoEForVets/footer_numerica.png, infoEForVets/landing_hero_mockup.png, infoEForVets/logo_eforpets_card.png, infoEForVets/logo_eforvets_card.png, infoEForVets/logo_eforvets.png, landingPage.jpeg.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/Landing.razor`.
- **Servicio equivalente:** `Services/.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/Landing.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `ReportesPaginas\vt_fichasanteriores.aspx`

- **Code-behind:** `vt_fichasanteriores.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Buscar, Content1, Content2, ContentPlaceHolder1, GridView1, GridView2, GridView3, GridView5, head, Label1, Label2, Label3, Lbespecialidad, Lbiduser, lblHostName, lblIPAddress, Lblnhcl, Lblocalidad, Lbltipoanterior, Lbltipoemergente, Lbsesion, Lbtipouser, Panel2, Panel4, RadAjaxLoadingPanel1.
- **Eventos y operaciones:** Buscar_Click, GridView1_SelectedIndexChanging, GridView2_SelectedIndexChanging, GridView3_SelectedIndexChanging, GridView5_SelectedIndexChanging, Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_clase, vt_clientes, vt_clientes_1, vt_diagnosticos, vt_especialidades, vt_especie, vt_fichamedica, vt_fichasanteriores, vt_hisDatMedicos, vt_localidades, vt_nombres_cientificos, vt_pacientes, vt_pacientes_1, vt_razas, vt_sexo, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/HistorialPaciente.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/HistorialPaciente.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_administracionCitasnew.aspx`

- **Code-behind:** `vt_administracionCitasnew.aspx.vb`.
- **Propósito funcional:** Módulo Agenda y horarios; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnAgendar, btnCerrarReagendar, BtnGrabar, Btnnuevo, Buscar, Button9, Content1, Content2, ContentPlaceHolder1, ddlAgendar, ddlEspecialidad, ddlEstadoGrid, ddlLocalidad, ddlMedico, ddlNumCitas, GridView1, GridView5, head, LbApellidosNombres, Lbespecialidad, Lbiduser, lblFechaCita, lblFechaLimite, lblFechaSelec, lblFechaSeleccionada.
- **Eventos y operaciones:** ActualizarAgenda, btnAgendar_Click, BtnCerrarReagendarClick, BtnGrabar_Click, Btnnuevo_Click, Buscar_Click, Button9_Click, ddlEspecialidad_SelectedIndexChanged, ddlLocalidad_SelectedIndexChanged, ddlMedico_SelectedIndexChanged, enviarCita, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging.
- **Tablas/vistas/procedimientos:** vt_administracionCitasnew, vt_calendarizacionHorariosMedicos, vt_citas, vt_clase, vt_clientes, vt_clientes_1, vt_eliminaCitaOp, vt_especialidades, vt_especie, vt_horariosMedicos, vt_localidades, vt_nombres_cientificos, vt_pacientes, vt_pacientes_1, vt_razas, vt_sexo, vt_usuarios.
- **Archivos físicos:** imagenes/Delete.png, imagenes/Disponible.png, imagenes/Edit.png, imagenes/NoDisponible.png, imagenes/rojo.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/AgendaCitas.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/AgendaCitas.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_antecedentes.aspx`

- **Code-behind:** `vt_antecedentes.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnIngreso0, Buscar, Content1, Content2, ContentPlaceHolder1, GridView1, GridView5, head, LbCodCli, Lbespecialidad, Lbidcli, Lbidpac, Lbiduser, lblHostName, lblIdPac, lblIPAddress, Lblocalidad, Lbnhcl, Lbtipo, Lbtipouser, Panel1, Panel2, Panel3, RadAjaxLoadingPanel1, RadAjaxPanel1.
- **Eventos y operaciones:** btnIngreso0_Click, Buscar_Click, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging, Page_Load, RadCheckBox2_Click, RadCheckBox3_Click, RadGrid1_ItemCommand.
- **Tablas/vistas/procedimientos:** vt_antecedentes, vt_clase, vt_clientes, vt_clientes_1, vt_especie, vt_nombres_cientificos, vt_pacientes, vt_pacientes_1, vt_razas, vt_sexo, vt_usuarios.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/FichaMedica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/FichaMedica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_antFinal.aspx`

- **Code-behind:** `vt_antFinal.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnIngreso0, form1, Label11, Label12, Label14, Label16, Label18, Label9, LbCodCli, Lbespecialidad, Lbiduser, lbl_cliente, lbl_hncl, lbl_nombre_pac, lbl_tipo, lblanios, lblHostName, lblIPAddress, lblmeses, Lblocalidad, Lbtipouser, Panel2, Panel3, RadAjaxLoadingPanel1, RadAjaxPanel1.
- **Eventos y operaciones:** btnIngreso0_Click, Page_Load, RadCheckBox2_Click, RadCheckBox3_Click, RadGrid1_ItemCommand.
- **Tablas/vistas/procedimientos:** vt_antecedentes, vt_antFinal, vt_clientes, vt_pacientes, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `vt_apuntesahistorias.aspx`

- **Code-behind:** `vt_apuntesahistorias.aspx.vb`.
- **Propósito funcional:** Buscar un paciente y mantener apuntes longitudinales en su historia clínica, independientes de una orden de atención.
- **Campos principales:** btnIngreso0, Buscar, Content1, Content2, ContentPlaceHolder1, DefaultSetOfTools, fechaingreso, GridView1, GridView5, head, LbCodCli, Lbespecialidad, Lbiduser, lblAnios, lblcliente, lblHostName, lblIdPac, lblIPAddress, lblMeses, lblNHCL, lblNombre, Lblocalidad, lbltipo, Lbtipouser, RadAjaxLoadingPanel1.
- **Eventos y operaciones:** btnIngreso0_Click, Buscar_Click, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging, Page_Load, RadGrid1_ItemCommand.
- **Tablas/vistas/procedimientos:** vt_apuntesahistorias, vt_clase, vt_clientes, vt_clientes_1, vt_especie, vt_nombres_cientificos, vt_notas, vt_pacientes, vt_pacientes_1, vt_razas, vt_sexo, vt_usuarios.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No; el correo del propietario solo se consultaba como dato de búsqueda.
- **Blazor equivalente:** `Components/Pages/ApuntesHistorias.razor`.
- **Servicio equivalente:** `Services/ConsultaExternaService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Búsqueda por nombres, apellidos, cédula, paciente o NHCL; alta en `vt_notas`, consulta de apuntes activos y anulación lógica con usuario y fecha.
- **Diferencias encontradas:** La interfaz Blazor unifica en una sola tabla la selección propietario/paciente y usa un editor de texto seguro en lugar del editor HTML Telerik.
- **Trabajo requerido:** Probar altas y anulaciones contra una base de datos de pruebas y validar con usuarios clínicos si requieren conservar formato enriquecido.

### `vt_autorizaciones.aspx`

- **Code-behind:** `vt_autorizaciones.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnIngreso0, DefaultSetOfTools, form1, Label1, Label11, Label12, Label14, Label16, Label18, Label9, LbCodCli, Lbespecialidad, Lbiduser, lbl_cliente, lbl_nhcl, lbl_nombre_pac, lbl_tipo, lblanios, lblHostName, lblIPAddress, lblmeses, Lblocalidad, Lbtipouser, RadAjaxLoadingPanel1, RadAjaxPanel1.
- **Eventos y operaciones:** btnIngreso0_Click, Page_Load, Radautorizacion_SelectedIndexChanged.
- **Tablas/vistas/procedimientos:** vt_autorizaciones, vt_clientes, vt_formatos, vt_pacientes.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/AutorizacionesAlta.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/AutorizacionesAlta.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_baseRecordatorios.aspx`

- **Code-behind:** `vt_baseRecordatorios.aspx.vb`.
- **Propósito funcional:** Módulo Seguimiento clínico; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnimprimir, btnimprimirrev, CompareValidator1, CompareValidator2, CompareValidator3, CompareValidator4, Content1, Content2, ContentPlaceHolder1, head, Literal1, RadScriptManager1, TextBox1, TextBox2, TextBox3, TextBox4.
- **Eventos y operaciones:** btnimprimir_Click, btnimprimirrev_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_baseRecordatorios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/SeguimientoClinico.razor`.
- **Servicio equivalente:** `Services/.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/SeguimientoClinico.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_basereport.aspx`

- **Code-behind:** `vt_basereport.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnimprimir, CompareValidator1, CompareValidator2, Content1, Content2, ContentPlaceHolder1, head, Literal1, RadScriptManager1, TextBox1, TextBox2.
- **Eventos y operaciones:** btnimprimir_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_basereport.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `vt_Calendarizacion.aspx`

- **Code-behind:** `vt_Calendarizacion.aspx.vb`.
- **Propósito funcional:** Módulo Agenda y horarios; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnactualizar, btncancelar, btnguardar, Buscar, calendario, Content1, Content2, ContentPlaceHolder1, DataProcedimientos, GridView2, GridView5, head, Lbclienteinformativo, Lbespecialidad, lbfechainformativo, lbhoradeinicio, lbhorafinalizacion, Lbidmodifica, Lbiduser, Lblcliente, lblHostName, lblidcliente, lblIdPac, lblIdProd, lblIPAddress.
- **Eventos y operaciones:** btnactualizar_Click, btncancelar_Click, btnguardar_Click, Buscar_Click, GridView2_SelectedIndexChanging, GridView5_SelectedIndexChanging, Page_Load, Radbtnfind_Click, Radbtnrefresh_Click, RadButton4_Click, RadGrid1_ItemCommand, RadScheduler1_AppointmentClick.
- **Tablas/vistas/procedimientos:** qvt_calendario_cirugias, vt_Calendarizacion, vt_clase, vt_clientes, vt_clientes_1, vt_especie, vt_nombres_cientificos, vt_pacientes, vt_pacientes_1, vt_productos, vt_razas, vt_sexo, vt_usuarioLocalidades, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/ProgramacionClinica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ProgramacionClinica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_calendarizarhorariosmedicos.aspx`

- **Code-behind:** `vt_calendarizarhorariosmedicos.aspx.vb`.
- **Propósito funcional:** Módulo Agenda y horarios; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** AppointmentsDataSource, btn_cancelarlocalidad, btn_consgeneral, btn_consindividual, btn_eliminartodos, btn_guardar, Content1, Content2, ContentPlaceHolder1, CrystalReportSource1, ddlespecialidad, ddllocalidad, ddlmedico, ddrmotivos, grd_calendarizacion, head, Label1, Label2, Lbespecialidad, Lbiduser, Lblespecialidadmensajes, lblFechaSelec, lblHostName, lblIPAddress, Lblmedicomensajes.
- **Eventos y operaciones:** btn_cancelarlocalidad_Click, btn_consgeneral_Click, btn_consindividual_Click, btn_eliminartodos_Click, btn_guardar_Click, ddlespecialidad_SelectedIndexChanged, ddllocalidad_SelectedIndexChanged, ddlmedico_SelectedIndexChanged, grd_calendarizacion_ItemCommand, Page_Load, txtFechaFin_SelectedDateChanged, txtFechaInicio_SelectedDateChanged.
- **Tablas/vistas/procedimientos:** vt_calendarizacionHorariosMedicos, vt_calendarizarhorariosmedicos, vt_especialidad_localidad, vt_especialidades, vt_localidades, vt_motivos, vt_usuarioLocalidades, vt_usuarios.
- **Archivos físicos:** imagenes/Delete.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/ProgramacionClinica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ProgramacionClinica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_categoria_ingreso.aspx`

- **Code-behind:** `vt_categoria_ingreso.aspx.vb`.
- **Propósito funcional:** Módulo Clientes y pacientes; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radestado, RadScriptManager1, txtcategoria.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_categoria_ingreso, vt_ingreso.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/CatalogosClinicos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/CatalogosClinicos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_clase.aspx`

- **Code-behind:** `vt_clase.aspx.vb`.
- **Propósito funcional:** Módulo Clientes y pacientes; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radestado, RadScriptManager1, txtclase.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_clase.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/CatalogosClinicos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/CatalogosClinicos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_ControlIngreso.aspx`

- **Code-behind:** `vt_ControlIngreso.aspx.vb`.
- **Propósito funcional:** Módulo Seguimiento clínico; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnPrint, form1, imp1, induccion, Label1, LbCodCli, Lbespecialidad, lbficha, Lbiduser, lbl_idesp, lbl_iduser, lblAnios, lblCliente, lblespecie, lblFecha, lblHostName, lblIdFichaNueva, Lblidorden, lblInicia, lblIPAddress, lblMedico, lblMeses, lblNHCL, lblNombre, Lblocalidad.
- **Eventos y operaciones:** induccion_Click, Page_Load.
- **Tablas/vistas/procedimientos:** qvt_verificacion, vt_clientes, vt_ControlIngreso, vt_especie, vt_ordenAtencion, vt_pacientes.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/OperacionesClinicas.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/OperacionesClinicas.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_despacholab.aspx`

- **Code-behind:** `vt_despacholab.aspx.vb`.
- **Propósito funcional:** Módulo Laboratorio e imagenología; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnEnviarFactura, btnGrabar, btnImprimir6, btnLaboratorio, BuscaLab, chkLaboratorio, cmbtipoamb, Content1, Content2, ContentPlaceHolder1, CrystalReportSource1, grvLaboratorio, head, Image1, LbCodCli, Lbespecialidad, Lbiduser, lbl_idesp, lbl_iduser, lblAnios, lblCliente, lblespecie, lblFecha, lblHostName, lblidbc.
- **Eventos y operaciones:** btnEnviarFactura_Click, btnGrabar_Click, btnImprimir6_Click, btnLaboratorio_Click, BuscaLab_Click, enviarAdjuntoMail, ImprimirLaboratorioEmail, Page_Load.
- **Tablas/vistas/procedimientos:** vt_bodegasBC, vt_cie10, vt_clientes, vt_despacholab, vt_diagnosticos, vt_especie, vt_facturasBC, vt_labOrdenes, vt_localidades, vt_ordenAtencion, vt_pacientes, vt_pruebas_laboratorio, vt_usuarios.
- **Archivos físicos:** /imagenes/Delete.png, /Reporteria/HDEV/rptLaboratorio.rpt, /Reporteria/HDEV/rptLaboratorioMail.rpt.
- **Reportes Crystal:** /Reporteria/HDEV/rptLaboratorio.rpt, /Reporteria/HDEV/rptLaboratorioMail.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/OrdenesLaboratorio.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/OrdenesLaboratorio.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_despachorx.aspx`

- **Code-behind:** `vt_despachorx.aspx.vb`.
- **Propósito funcional:** Módulo Laboratorio e imagenología; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnEnviarFactura, btnGrabar, btnImprimir5, btnRX, BuscaRx, chkrx, Content1, Content2, ContentPlaceHolder1, CrystalReportSource1, GrvRX2, head, Image1, Label6, LbCodCli, Lbespecialidad, Lbiduser, lbl_idesp, lbl_iduser, lblAnios, lblCliente, lblespecie, lblFecha, lblHostName, lblidbc.
- **Eventos y operaciones:** btnEnviarFactura_Click, btnGrabar_Click, btnImprimir5_Click, btnRX_Click, BuscaRx_Click, enviarAdjuntoMail, Page_Load.
- **Tablas/vistas/procedimientos:** vt_bodegasBC, vt_cie10, vt_clientes, vt_despachorx, vt_diagnosticos, vt_especie, vt_facturasBC, vt_localidades, vt_ordenAtencion, vt_pacientes, vt_rxOrdenes, vt_usuarios.
- **Archivos físicos:** /imagenes/Delete.png, /Reporteria/HDEV/rptRxImagen.rpt, /Reporteria/HDEV/rptRxImagenEmail.rpt.
- **Reportes Crystal:** /Reporteria/HDEV/rptRxImagen.rpt, /Reporteria/HDEV/rptRxImagenEmail.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/OrdenesImagenologia.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/OrdenesImagenologia.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_diagnosticos.aspx`

- **Code-behind:** `vt_diagnosticos.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radgrupos, RadScriptManager1, Txtdiagnostico.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_diagnosticos, vt_grupodes, vt_grupodiag, vt_grupoestado, vt_grupos_diagnosticos.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/BandejaResultadosDiagnosticos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/BandejaResultadosDiagnosticos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_EnfermeriaKardex.aspx`

- **Code-behind:** `vt_EnfermeriaKardex.aspx.vb`.
- **Propósito funcional:** Módulo Kardex y enfermería; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** 1033, agregarproducto, btnbusquedamedicamento, BtnImprimir, cmbvia, evo, Fec1Imp1, Fec2Imp1, Fec3Imp1, Fec4Imp1, FecImp1, form1, funcioncargo, Grdmed, grdMedicinas, GridView1, GridView2, GridView3, GridView4, GridView5, GridView6, Imp, imp1, Iniciales, Label1.
- **Eventos y operaciones:** agregarproducto_Click, btnbusquedamedicamento_Click, Grdmed_SelectedIndexChanging, grdMedicinas_ItemCommand, Page_Load, RadTabStrip1_TabClick.
- **Tablas/vistas/procedimientos:** qvt_productosUtilizados, qvt_via, vt_bodegasBC, vt_clientes, vt_EnfermeriaKardex, vt_ordenAtencion, vt_pacientes, vt_reservaProductosStockBC, vt_tipos_usuarios, vt_usuarios.
- **Archivos físicos:** /imagenes/agregar.png, /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/KardexEnfermeria.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/KardexEnfermeria.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_EnfermeriaSignos.aspx`

- **Code-behind:** `vt_EnfermeriaSignos.aspx.vb`.
- **Propósito funcional:** Módulo Kardex y enfermería; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** BtnImprimir, btnNuevo, ddlNumFormulario, form1, imp1, Lblcliente, lblEstablecimiento, lblIdOrden, lblNhcl, lblNombre, lblSexo, Panel2, RadAjaxLoadingPanel1, RadAjaxManager1, RadFec1, RadFec2, RadFec3, RadFec4, RadFec5, RadFec6, RadFec7, RadFec8, RadFec9, RadHtmlChart1, RadScriptManager1.
- **Eventos y operaciones:** btnNuevo_Click, ddlNumFormulario_SelectedIndexChanged, Page_Load, RadFec1_SelectedDateChanged, RadFec2_SelectedDateChanged, RadFec3_SelectedDateChanged, RadFec4_SelectedDateChanged, RadFec5_SelectedDateChanged, RadFec6_SelectedDateChanged, RadFec7_SelectedDateChanged, RadFec8_SelectedDateChanged, RadFec9_SelectedDateChanged.
- **Tablas/vistas/procedimientos:** qvt_enfBalanceHidrico, qvt_enfFechas, qvt_enfMediciones, qvt_enfSignosVitales, vt_clientes, vt_EnfermeriaSignos, vt_ordenAtencion, vt_pacientes.
- **Archivos físicos:** imagenes/presion.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/EnfermeriaSignos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/EnfermeriaSignos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_especialidades_localidades.aspx`

- **Code-behind:** `vt_especialidades_localidades.aspx.vb`.
- **Propósito funcional:** Módulo Administración; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** cmbespecialidad, cmblocalidad, Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radestado, RadScriptManager1.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_especialidad_localidad, vt_especialidades, vt_especialidades_localidades, vt_localidades.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/CatalogosOperativos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/CatalogosOperativos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_especialidades.aspx`

- **Code-behind:** `vt_especialidades.aspx.vb`.
- **Propósito funcional:** Módulo Administración; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radestado, RadScriptManager1, Txtespecialidad.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_especialidades.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/CatalogosOperativos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/CatalogosOperativos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_especies.aspx`

- **Code-behind:** `vt_especies.aspx.vb`.
- **Propósito funcional:** Módulo Clientes y pacientes; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radestado, RadScriptManager1, txtespecie.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_especie, vt_especies.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/CatalogosClinicos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/CatalogosClinicos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_evoluciones.aspx`

- **Code-behind:** `vt_evoluciones.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** agregarproducto, btn_filtrar, btnAgregar, btnbusquedamedicamento, btnGrabar, btnNuevo, BuscaLab, BuscaRx, chkFarmaco, chkLaboratorio, chkrx, cmbtipoamb, cmbvia, ddlUsuario, evo, form1, funcioncargo, Grdmed, grdMedicinas, GridView1, grvLaboratorio, GrvRX2, idEvolucionGrd, ImageButton2, ImageButton3.
- **Eventos y operaciones:** agregarproducto_Click, btn_filtrar_Click, btnAgregar_Click, btnbusquedamedicamento_Click, btnGrabar_Click, btnNuevo_Click, BuscaLab_Click, BuscaRx_Click.
- **Tablas/vistas/procedimientos:** qvt_productosUtilizados, qvt_via, vt_bodegasBC, vt_clientes, vt_especialidades, vt_especie, vt_evoluciones, vt_EvolucionImp, vt_EvolucionMedico, vt_fichaMedica, vt_labOrdenes, vt_objetivo, vt_ordenAtencion, vt_pacientes, vt_productos, vt_pruebas_laboratorio, vt_reservaProductosStockBC, vt_rxOrdenes, vt_subjetivo, vt_tipos_usuarios, vt_usuarios.
- **Archivos físicos:** /imagenes/agregar.png, /imagenes/Delete.png, /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/EvolucionMedica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`, `Services/ReportExportService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/EvolucionMedica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_EvolucionImp.aspx`

- **Code-behind:** `vt_EvolucionImp.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnImprimir, chkFarmaco, form1, GridView1, idEvolucionGrd, imp1, Label6, Label7, Label8, Label9, LabelIndicaciones, LbCodCli, Lbespecialidad, lbficha, Lbiduser, lbl_idesp, lbl_iduser, lblAnios, lblCliente, lblespecie, lblFecha, lblHostName, lblIdEvolucion, lblIdFichaNueva, Lblidorden.
- **Eventos y operaciones:** Page_Load.
- **Tablas/vistas/procedimientos:** qvt_productosUtilizados, vt_clientes, vt_especialidades, vt_especie, vt_EvolucionImp, vt_EvolucionMedico, vt_fichamedica, vt_labOrdenes, vt_objetivo, vt_pacientes, vt_rxOrdenes, vt_subjetivo, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`, `Services/ReportExportService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `vt_fallecidos.aspx`

- **Code-behind:** `vt_fallecidos.aspx.vb`.
- **Propósito funcional:** Módulo Seguimiento clínico; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnejecutar, Content1, Content2, ContentPlaceHolder1, head, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, Literal1, RadAjaxLoadingPanel1, RadAjaxPanel1, RadMonthYearPicker1, RadMonthYearPicker2, ScriptManager1.
- **Eventos y operaciones:** btnejecutar_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_fallecidos.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/SeguimientoClinico.razor`.
- **Servicio equivalente:** `Services/.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/SeguimientoClinico.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_genero.aspx`

- **Code-behind:** `vt_genero.aspx.vb`.
- **Propósito funcional:** Módulo Clientes y pacientes; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbaccion, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radestado, RadScriptManager1, txtgenero.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_genero, vt_sexo.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/CatalogosClinicos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/CatalogosClinicos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_grupos_diagnosticos.aspx`

- **Code-behind:** `vt_grupos_diagnosticos.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radestado, RadScriptManager1, txtgrupo.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_grupodes, vt_grupodiag, vt_grupoestado, vt_grupos_diagnosticos.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `vt_hisDatMedicos.aspx`

- **Code-behind:** `vt_hisDatMedicos.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** abdominal, antabdominal, antanamnesis, antanios, antaniostueri, antapuntes, antauscultacion, antauscultaciontueri, antcavidadoral, antcliente, antclientetueri, antcp, antCPtue, antdescripcion, antdeshidratacion, antdeshidrataciontue, Antecedentes, Anteriores, antespecialidad, antespecialidadtueri, antespecie, antespecietueri, antesterilizado, antfctue, antfecha.
- **Eventos y operaciones:** btnafacturatueri_Click, btnbuscaaccesorios_Click, btnDiagnosticostueri_Click, btnEnviarFactura_Click, btnGrabar_Click, btnImpAcc_Click, btnImpProc_Click, btnImprimir_Click, btnImprimir2_Click, btnImprimir4_Click, btnImprimir5_Click, btnImprimir6_Click, btnimprimirfichalectura_Click, btnimprimirfichalecturatueri_Click, btnImprIns_Click, btnLaboratoriotueri_Click, btnMedicinastueri_Click, btnProctueri_Click, BtnresetHDEV_Click, btnRXtueri_Click, btntraemedtueri_Click, btnVariostueri_Click, BuscaLab_Click, BuscaLabtueri_Click, BuscaProc_Click, BuscaProctueri_Click, BuscaRx_Click, BuscaRxtueri_Click.
- **Tablas/vistas/procedimientos:** vt_accesorios, vt_antecedentes, vt_antFinal, vt_autorizaciones, vt_bodegasBC, vt_calificacion, vt_cie10, vt_ciudad, vt_clase, vt_clientes, vt_consultas, vt_consultas_menores, vt_diagnosticos, vt_ecoInformes, vt_endoInformes, vt_especialidades, vt_especie, vt_estudiantes, vt_estudiantesorden, vt_facturasBC, vt_facturasBCsinOrden, vt_fichamedica, vt_fisioterapia_ordenes, vt_formatos, vt_hisDatMedicos, vt_hisDatMedicosPre, vt_historialPaciente, vt_IndicacionesGenerales, vt_ingreso, vt_labInformes, vt_labOrdenes, vt_localidades, vt_medicinas, vt_medicinas_1, vt_medInformes, vt_nombres_cientificos, vt_obtension, vt_ordenAtencion, vt_pacientes, vt_parametros, vt_procedimientos, vt_productos, vt_provincias, vt_pruebas_laboratorio, vt_reservaProductosStockBC, vt_rxInformes, vt_rxOrdenes, vt_tiempoDuracionFicha, vt_tiempoDuracionOrden, vt_tipos_usuarios.
- **Archivos físicos:** /imagenes/add_icons1.png, /imagenes/anfibiotueri.jpg, /imagenes/avetueri.jpg, /imagenes/b_report.png, /imagenes/botonimagen.png, /imagenes/buscar0.png, /imagenes/Delete.png, /imagenes/delgado.png, /imagenes/dorsal.jpg, /imagenes/dorsalgato.jpg, /imagenes/Edit.png, /imagenes/equinos.jpg, /imagenes/ideal.png, /imagenes/mamiferotueri.jpg, /imagenes/muydelgado.png, /imagenes/obeso.png, /imagenes/pulsofuerte.png, /imagenes/pulsoleve.png, /imagenes/pulsomoderado.png, /imagenes/reptiltueri.jpg, /imagenes/sobrepeso.png, /Reporteria/HDEV/FichaHDEV.rpt, /Reporteria/HDEV/rptAccesorios.rpt, /Reporteria/HDEV/rptAccesoriosMail.rpt, /Reporteria/HDEV/rptFisioterapia.rpt.
- **Reportes Crystal:** /Reporteria/HDEV/FichaHDEV.rpt, /Reporteria/HDEV/rptAccesorios.rpt, /Reporteria/HDEV/rptAccesoriosMail.rpt, /Reporteria/HDEV/rptFisioterapia.rpt, /Reporteria/HDEV/rptFisioterapiaMail.rpt, /Reporteria/HDEV/rptInsumos.rpt, /Reporteria/HDEV/rptInsumosMail.rpt, /Reporteria/HDEV/rptLaboratorio.rpt, /Reporteria/HDEV/rptLaboratorioMail.rpt, /Reporteria/HDEV/rptProcedimientos.rpt, /Reporteria/HDEV/rptProcedimientosMail.rpt, /Reporteria/HDEV/rptRecetaEmail.rpt, /Reporteria/HDEV/RptrecetaHDEV.rpt, /Reporteria/HDEV/rptRecetaIndicaciones.rpt, /Reporteria/HDEV/rptRxImagen.rpt, /Reporteria/HDEV/rptRxImagenEmail.rpt, /Reporteria/TUERI/FichaTUERI.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/FichaMedica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/FichaMedica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_hisdatmedicospre.aspx`

- **Code-behind:** `vt_hisdatmedicospre.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Buscar, Content1, Content2, ContentPlaceHolder1, CrystalReportSource1, GridView1, GridView2, GridView3, GridView4, GridView5, GridView6, GridView7, grv_Ordenes_Atencion, grv_Ordenes_Atencionmonitoreo, grv_Ordenes_Atencionmuestra, grv_Ordenes_Atencionquirurgico, head, imgColor, imgPacienteGrd, Label1, Label2, Label3, Label4, Label5, Lbespecialidad.
- **Eventos y operaciones:** Buscar_Click, GridView1_SelectedIndexChanging, GridView2_SelectedIndexChanging, GridView3_SelectedIndexChanging, GridView4_SelectedIndexChanging, GridView5_SelectedIndexChanging, grv_Ordenes_Atencion_RowCommand, grv_Ordenes_Atencionmonitoreo_RowCommand, grv_Ordenes_Atencionmonitoreo_SelectedIndexChanging, grv_Ordenes_Atencionmuestra_RowCommand, grv_Ordenes_Atencionmuestra_SelectedIndexChanging, grv_Ordenes_Atencionquirurgico_RowCommand, grv_Ordenes_Atencionquirurgico_SelectedIndexChanging.
- **Tablas/vistas/procedimientos:** Hosp_FichaHospitalizacion, Hosp_Monitoreo, vt_cabecerareceta, vt_cie10, vt_clase, vt_clientes, vt_clientes_1, vt_despacholab, vt_despachorx, vt_diagnosticos, vt_diagnosticosexternos, vt_especialidades, vt_especie, vt_fichamedica, vt_hisDatMedicos, vt_hisdatmedicospre, vt_IndicacionesGenerales, vt_labOrdenes, vt_localidades, vt_medicinas, vt_medicinasexterno, vt_nombres_cientificos, vt_notas, vt_ordenAtencion, vt_pacientes, vt_pacientes_1, vt_QuirofanoPrincipal, vt_razas, vt_rxOrdenes, vt_sexo, vt_usuarios.
- **Archivos físicos:** /imagenes/b_report.png, imagenes/temperamento/amarillo.png, imagenes/temperamento/blanco.png, imagenes/temperamento/nodefinido.png, imagenes/temperamento/rojo.png, imagenes/temperamento/verde.png, Reporteria/HDEV/RptrecetaHDEV.rpt.
- **Reportes Crystal:** Reporteria/HDEV/RptrecetaHDEV.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/FichaMedica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/FichaMedica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_hisInfoMed.aspx`

- **Code-behind:** `vt_hisInfoMed.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnEliminar, btnGrabar, btnGrabar1, Content1, Content2, ContentPlaceHolder1, ddlExamenes, ddlExamenes1, DefaultSetOfTools, FileUpload1, FileUpload2, head, Label1, Lbespecialidad, Lbiduser, lblAnios, lblAnios1, lblcliente, lblcliente1, lblFecha, lblFecha1, lblHostName, lblID, lblIPAddress, lblMedico.
- **Eventos y operaciones:** btnEliminar_Click, btnGrabar_Click, btnGrabar1_Click, ddlExamenes_SelectedIndexChanged, ddlExamenes1_SelectedIndexChanged, enviarAdjuntoMail, Page_Load.
- **Tablas/vistas/procedimientos:** vt_clientes, vt_ecoInformes, vt_endoInformes, vt_especie, vt_formatos, vt_hisInfoMed, vt_hisInfoMedPre, vt_labInformes, vt_medInformes, vt_pacientes, vt_razas, vt_rxInformes, vt_sexo, vt_transforma, vt_usuarios.
- **Archivos físicos:** imagenes/logoestudiante\.jpg, imagenes/logousfq\.jpg, imagenes/numerica_veterinaria_logo.png, imagenes/veterinaria\.jpg, Telerik.Windows.Documents.Spreadsheet.FormatProviders.OpenXml.Xlsx, Telerik.Windows.Documents.Spreadsheet.FormatProviders.Pdf.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/FichaMedica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/FichaMedica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_hisInfoMedPre.aspx`

- **Code-behind:** `vt_hisInfoMedPre.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnNuevo, Buscar, Content1, Content2, ContentPlaceHolder1, DefaultSetOfTools, GridView1, GridView2, GridView5, head, ImageButton1, Label1, Label2, Label3, Label4, Label5, Lbespecialidad, Lbiduser, lblAnios, lblcliente, lblHostName, lblIdPac, lblIPAddress, lblMeses, lblNHCL.
- **Eventos y operaciones:** btnNuevo_Click, Buscar_Click, GridView1_SelectedIndexChanging, GridView2_RowCommand, GridView2_SelectedIndexChanged, GridView5_SelectedIndexChanging, Page_Load.
- **Tablas/vistas/procedimientos:** vt_clase, vt_clientes, vt_clientes_1, vt_ecoInformes, vt_endoInformes, vt_especie, vt_formatos, vt_hisInfoMed, vt_hisInfoMedPre, vt_labInformes, vt_medInformes, vt_nombres_cientificos, vt_pacientes, vt_pacientes_1, vt_razas, vt_rxInformes, vt_sexo, vt_tipos_usuarios, vt_usuarios.
- **Archivos físicos:** /imagenes/Informe.png, imagenes/logoestudiante\.jpg, imagenes/logousfq\.jpg, imagenes/numerica_veterinaria_logo.png, imagenes/veterinaria\.jpg.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/FichaMedica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/FichaMedica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_historialPaciente.aspx`

- **Code-behind:** `vt_historialPaciente.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** form1, GridView1, GridView2, GridView3, GridView4, GridView5, GridView6, GridView7, Label1, Label2, Label3, Label4, Label5, Label6, Label7, Lbespecialidad, Lbiduser, lblHostName, lblindicacion, lblIPAddress, lblLabCod, lblLabDes, lblNHCL, Lblocalidad, Lbtipouser.
- **Eventos y operaciones:** Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_diagnosticos, vt_fisioterapia_ordenes, vt_historialPaciente, vt_labOrdenes, vt_medicinas, vt_procedimientos, vt_productos, vt_pruebas_laboratorio, vt_rxOrdenes.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/HistorialPaciente.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/HistorialPaciente.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_hojaanestesia.aspx`

- **Code-behind:** `vt_hojaanestesia.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** 1033, btnCirculantes, btnInsertaAnestesiologo, btnInsertaAyudante, btnInsertaCirujano, btnInsertaProcedimiento, btninsertdiag, btnInstrumentistas, btnNuevo, btnPrint, btnSave, Button1, chartContainer, CheckBoxArterial, CheckBoxCentral, CheckBoxEcografica, CheckBoxPeriferica1, CheckBoxPeriférica2, CheckBoxPresion, chkAsist, Chkbalon, chkcateter, chkCaudal, chkcircExtr, chkCO2ET.
- **Eventos y operaciones:** btnCirculantes_Click, btnInsertaAnestesiologo_Click, btnInsertaAyudante_Click, btnInsertaCirujano_Click, btnInsertaProcedimiento_Click, btninsertdiag_Click, btnInstrumentistas_Click, btnNuevo_Click, btnPrint_Click, btnSave_Click.
- **Tablas/vistas/procedimientos:** qvt_condicionesAnestesia, qvt_duracionAnestesia, qvt_egresosAnestesia, qvt_graficaAnestesia, qvt_graficaAnestesiaTiempo, qvt_graficaBC, qvt_graficaDescripcion, qvt_IngresosAnestesia, qvt_ManejoViaAnestesia, qvt_MonitoreoAnestesia, qvt_posicionAnestesia, qvt_preanestesia, qvt_productosUtilizados, qvt_RegistroAnestesia, qvt_team, qvt_tecnicaAnestesia, qvt_tecnicas_especialesAnestesia, qvt_valoracionAnestesia, qvt_ventilacionAnestesia, vt_anestesia, vt_bodegasBC, vt_cie10, vt_clientes, vt_especialidades, vt_especie, vt_hojaanestesia, vt_ordenAtencion, vt_pacientes, vt_productos, vt_reservaProductosStockBC, vt_tipos_usuarios, vt_usuarioLocalidades, vt_usuarios.
- **Archivos físicos:** ../imagenes/diastolica.png, ../imagenes/equis.png, ../imagenes/oxigeno.png, ../imagenes/punto.png, ../imagenes/sistolica.png, /imagenes/agregar.png, /imagenes/buscar0.png, /imagenes/Delete.png, /imagenes/diastolica.png, /imagenes/Edit.png, /imagenes/Equis.png, /imagenes/feto.png, /imagenes/finAnestesia.png, /imagenes/Induccion.png, /imagenes/oxigeno.png, /imagenes/punto.png, /imagenes/respAsistida.png, /imagenes/respCOntrolada.png, /imagenes/respEspontanea.png, /imagenes/sistolica.png, /imagenes/temperatura.png, /imagenes/torniquete.png, /imagenes/venosa.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/HojaAnestesia.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/HojaAnestesia.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_i_stat.aspx`

- **Code-behind:** `vt_i_stat.aspx.vb`.
- **Propósito funcional:** Módulo Seguimiento clínico; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** baseexcess, bicarbonato, btnGrabar, Buscar, carbon, chloride, Content1, Content2, ContentPlaceHolder1, creatinina, fechaingreso, gap, glucosa, GridView1, GridView5, head, hematocrito, hemoglobina, ionized, lactato, LbCodCli, Lbespecialidad, Lbiduser, lblAnios, lblcliente.
- **Eventos y operaciones:** btnGrabar_Click, Buscar_Click, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging, Page_Load, RadGrid1_ItemCommand.
- **Tablas/vistas/procedimientos:** vt_clase, vt_clientes, vt_clientes_1, vt_especie, vt_i_stat, vt_istat, vt_nombres_cientificos, vt_pacientes, vt_pacientes_1, vt_razas, vt_sexo.
- **Archivos físicos:** /imagenes/Edit.png, assets/img/print.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/OperacionesClinicas.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/OperacionesClinicas.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_imprimirfichas.aspx`

- **Code-behind:** `vt_imprimirfichas.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** 1033, btnconsultar, btnvisualizar, Buscar, checkbox, CheckBox1, Content1, Content2, ContentPlaceHolder1, CrystalReportSource1, grdSeguros, GridView1, GridView5, head, imgArchivo, LbCodCli, Lbespecialidad, Lbidcli, Lbidpac, Lbiduser, lblcliente, lblDiagnosticos, Lblemail, lblespecialidad, Lblespecie.
- **Eventos y operaciones:** btnconsultar_Click, btnvisualizar_Click, Buscar_Click, grdSeguros_ItemCommand, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging, Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_clase, vt_clientes, vt_clientes_1, vt_consultas, vt_diagnosticos, vt_especialidades, vt_especie, vt_fichaMedica, vt_fisioterapia_ordenes, vt_imprimirfichas, vt_labOrdenes, vt_medicinas, vt_medicinas_1, vt_nombres_cientificos, vt_pacientes, vt_pacientes_1, vt_procedimientos, vt_razas, vt_rxOrdenes, vt_sexo, vt_usuarios.
- **Archivos físicos:** /imagenes/anfibiotueri.jpg, /imagenes/avetueri.jpg, /imagenes/dorsal.jpg, /imagenes/dorsalgato.jpg, /imagenes/equinos.jpg, /imagenes/Informe.png, /imagenes/mamiferotueri.jpg, /imagenes/reptiltueri.jpg, /Reporteria/HDEV/FichaHDEV.rpt.
- **Reportes Crystal:** /Reporteria/HDEV/FichaHDEV.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/HistorialPaciente.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/HistorialPaciente.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_labmedicospre.aspx`

- **Code-behind:** `vt_labmedicospre.aspx.vb`.
- **Propósito funcional:** Módulo Laboratorio e imagenología; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnGrabar, btnNuevo, Btnsubir, Buscar, Content1, Content2, ContentPlaceHolder1, FileUpload1, GridView2, grvRx, head, ID, ImageButton1, Label1, Label2, Label3, Label4, Label5, lbahora, Lbespecialidad, lbexamen, Lbiduser, lbl_cliente, lbl_especialidad, lbl_estado.
- **Eventos y operaciones:** btnGrabar_Click, Buscar_Click, enviarAdjuntoMail, enviarAdjuntoMailCopia, GridView2_RowCommand, grvRx_RowCommand, Page_Load.
- **Tablas/vistas/procedimientos:** vt_clientes, vt_especialidades, vt_fichamedica, vt_labInformes, vt_labmedicospre, vt_labOrdenes, vt_ordenAtencion, vt_pacientes, vt_usuarios.
- **Archivos físicos:** /imagenes/Informe.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/BandejaResultadosDiagnosticos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/BandejaResultadosDiagnosticos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_listaalertas.aspx`

- **Code-behind:** `vt_listaalertas.aspx.vb`.
- **Propósito funcional:** Módulo Seguimiento clínico; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** 1033, btnimprimir, cmbestadoparte, Content1, Content2, ContentPlaceHolder1, head, Literal1, RadScriptManager1, TextBox1, TextBox2.
- **Eventos y operaciones:** btnimprimir_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_listaalertas.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/SeguimientoClinico.razor`.
- **Servicio equivalente:** `Services/.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/SeguimientoClinico.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_listadiagnosticos.aspx`

- **Code-behind:** `vt_listadiagnosticos.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnimprimir, cmblocalidad, CompareValidator1, CompareValidator2, Content1, Content2, ContentPlaceHolder1, head, Label1, Literal1, RadAjaxLoadingPanel1, RadAjaxPanel1, RadScriptManager1, Rdcondensado, Rddetallado, SqlDataDiag, TextBox1, TextBox2, txtDiagnosticos.
- **Eventos y operaciones:** btnimprimir_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_listadiagnosticos, vt_localidades.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `vt_listafichasmedicas.aspx`

- **Code-behind:** `vt_listafichasmedicas.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnimprimir, cmblocalidad, CompareValidator1, CompareValidator2, Condensado, Content1, Content2, ContentPlaceHolder1, ddrespecialidad, ddrmedicos, head, Label1, Literal1, RadScriptManager1, SqlDataDiag, TextBox1, TextBox2, txtDiagnosticos.
- **Eventos y operaciones:** btnimprimir_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_especialidades, vt_listafichasmedicas, vt_localidades, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `vt_localidades.aspx`

- **Code-behind:** `vt_localidades.aspx.vb`.
- **Propósito funcional:** Módulo Administración; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbprefijo, Lbregion, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, RadCiudad, Radestado, RadProvincia, Radruc, RadScriptManager1, Radtelefono, Txtadministrador.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load, RadProvincia_SelectedIndexChanged.
- **Tablas/vistas/procedimientos:** vt_ciudad, vt_localidades, vt_provincias.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/LocalidadesAdministracion.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AuthService.cs`, `Services/GestionMigradaService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/LocalidadesAdministracion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_notas.aspx`

- **Code-behind:** `vt_notas.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnImprimir, btnObjetivo, btnSubjetivo, cmbtipodiagnostico, ddlObjetivo, ddlSubjetivo, form1, GrabaCabecera, grvDiagnosticos, grvObjetivo, grvSubjetivo, ImageButton1, imp1, LbCodCli, Lbespecialidad, Lbiduser, lbl_idesp, lbl_iduser, lblAnios, lblCliente, lblespecie, lblFecha, lblHostName, lblIdFichaNueva, Lblidorden.
- **Eventos y operaciones:** btnObjetivo_Click, btnSubjetivo_Click, GrabaCabecera_Click, ImageButton1_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_clientes, vt_diagnosticos, vt_especie, vt_fichamedica, vt_notas, vt_objetivo, vt_ordenAtencion, vt_pacientes, vt_ParamObjetivo, vt_ParamSubjetivo, vt_subjetivo.
- **Archivos físicos:** /imagenes/agregar.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/FichaMedica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/FichaMedica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_obtension.aspx`

- **Code-behind:** `vt_obtension.aspx.vb`.
- **Propósito funcional:** Módulo Clientes y pacientes; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radestado, RadScriptManager1, txtobtension.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_obtension.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/CatalogosClinicos.razor`.
- **Servicio equivalente:** `Services/.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/CatalogosClinicos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_OrdenesDespacho.aspx`

- **Code-behind:** `vt_OrdenesDespacho.aspx.vb`.
- **Propósito funcional:** Módulo Órdenes y recepción; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnIngreso0, btnNuevo, Buscar, cmbestadocivil, cmbformapago, cmbgenero, cmbgrupocliente, cmbtemperamento, cmbterminospago, cmbtipoidentificacion, cmbtipopersona, Content1, Content2, ContentPlaceHolder1, ddrespecialidad, ddrmedicos, GridView1, GridView5, head, Lbespecialidad, Lbid, lbidcliente, Lbiduser, LblaccionSys, LblBC.
- **Eventos y operaciones:** btnIngreso0_Click, btnNuevo_Click, Buscar_Click, cmbtemperamento_SelectedIndexChanged, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging, Page_Load.
- **Tablas/vistas/procedimientos:** vt_bodegasBC, vt_clase, vt_clientes, vt_clientes_1, vt_especialidades, vt_especie, vt_EstadoCivil, vt_FormasPagoVigentes, vt_GeneroClientes, vt_nombres_cientificos, vt_ordenAtencion, vt_OrdenesDespacho, vt_pacientes, vt_pacientes_1, vt_razas, vt_sexo, vt_tipo_paciente_clasifica, vt_TipoIdentificacion, vt_tipoPaciente, vt_TipoPersonas, vt_usuarios.
- **Archivos físicos:** /imagenes/Edit.png, /imagenes/temperamento/amarillo.png, /imagenes/temperamento/blanco.png, /imagenes/temperamento/rojo.png, /imagenes/temperamento/verde.png, imagenes/temperamento/amarillo.png, imagenes/temperamento/blanco.png, imagenes/temperamento/rojo.png, imagenes/temperamento/verde.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/RecepcionOrdenes.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/RecepcionOrdenes.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_OrdenesMenu.aspx`

- **Code-behind:** `vt_OrdenesMenu.aspx.vb`.
- **Propósito funcional:** Módulo Órdenes y recepción; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnIngreso0, btnNuevo, Buscar, ChHospitalizacion, ChQuirurgico, cmbestadocivil, cmbformapago, cmbgenero, cmbgrupocliente, cmbtemperamento, cmbterminospago, cmbtipoidentificacion, cmbtipopersona, contenedorModal, Content1, Content2, ContentPlaceHolder1, conteo, ddrespecialidad, ddrmedicos, GridView1, GridView5, head, Lbespecialidad, Lbid.
- **Eventos y operaciones:** btnIngreso0_Click, btnNuevo_Click, Buscar_Click, cmbtemperamento_SelectedIndexChanged, ddrespecialidad_SelectedIndexChanged, GridView1_SelectedIndexChanging.
- **Tablas/vistas/procedimientos:** vt_bodegasBC, vt_clase, vt_clientes, vt_clientes_1, vt_especialidades, vt_especie, vt_EstadoCivil, vt_FormasPagoVigentes, vt_GeneroClientes, vt_nombres_cientificos, vt_ordenAtencion, vt_OrdenesMenu, vt_pacientes, vt_pacientes_1, vt_razas, vt_sexo, vt_tipo_paciente_clasifica, vt_TipoIdentificacion, vt_tipoPaciente, vt_TipoPersonas, vt_usuarios, vt_verOrdenes.
- **Archivos físicos:** /imagenes/Edit.png, /imagenes/temperamento/amarillo.png, /imagenes/temperamento/blanco.png, /imagenes/temperamento/rojo.png, /imagenes/temperamento/verde.png, imagenes/temperamento/amarillo.png, imagenes/temperamento/blanco.png, imagenes/temperamento/rojo.png, imagenes/temperamento/verde.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/RecepcionOrdenes.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/RecepcionOrdenes.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_parteoperatorio.aspx`

- **Code-behind:** `vt_parteoperatorio.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** 1033, Btnbusquedatabla, btncambiaaRealizado, Btndeshacer, btnDiagnostico, btnIngreso1, btnSubjetivo, Buscar, chksn, cmbanios, cmbdias, cmbmeses, cmbtipocirugia, Content1, Content2, ContentPlaceHolder1, DataProcedimientos, Dsayudante, dsrealizados, dtrealizacion, GridView1, GridView5, grvOtrosDiagnosticos, grvOtrosProcedimientos, head.
- **Eventos y operaciones:** Btnbusquedatabla_Click, btncambiaaRealizado_Click, Btndeshacer_Click, btnDiagnostico_Click, btnIngreso1_Click, btnSubjetivo_Click, Buscar_Click, dtrealizacion_SelectedDateChanged, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging, Page_Load, RadGrid1_ItemCommand, RadGrid2_ItemCommand, RadGrid3_ItemCommand, RadGrid4_ItemCommand, RadTabStrip1_TabClick.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_clase, vt_clientes, vt_clientes_1, vt_DiagExtras, vt_especialidades, vt_especie, vt_fichamedica, vt_nombres_cientificos, vt_pacientes, vt_pacientes_1, vt_parteoperatorio, vt_ProcExtras, vt_productos, vt_razas, vt_sexo, vt_tipocirugia, vt_usuarioLocalidades, vt_usuarios.
- **Archivos físicos:** /imagenes/agregar.png, /imagenes/delete.png, /imagenes/informe.png, assets/img/refresh.png, assets/img/update.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/ProtocoloQuirurgico.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ProtocoloQuirurgico.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_permisos.aspx`

- **Code-behind:** `vt_permisos.aspx.vb`.
- **Propósito funcional:** Módulo Administración; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, head, Lbespecialidad, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, ListBox1, Panel1, RadAjaxLoadingPanel1, RadAjaxPanel1, RadButton1, RadScriptManager1, TreeView1.
- **Eventos y operaciones:** ListBox1_SelectedIndexChanged, Page_Load, RadButton1_Click.
- **Tablas/vistas/procedimientos:** vt_accesos, vt_menudinamico, vt_permisos, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/RolesPermisos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/RolesPermisos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_personales.aspx`

- **Code-behind:** `vt_personales.aspx.vb`.
- **Propósito funcional:** Módulo Clientes y pacientes; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** analizador, btnIngreso0, btnIngreso1, btnIngreso3, btnNuevo, btnnuevo0, btnnuevo1, btnnuevo3, Buscar, ChHospitalizacion, chkfallecido, ChQuirurgico, Clientes, cmbanios, cmbcolor, cmbdias, cmbestadocivil, cmbestadocivil2, cmbformapago, cmbformapago2, cmbgenero, cmbgenero2, cmbgrupocliente, cmbgrupocliente2, cmbmeses.
- **Eventos y operaciones:** Actualizarcli, btnIngreso0_Click, btnIngreso1_Click, btnIngreso3_Click, btnNuevo_Click, btnnuevo0_Click, btnnuevo1_Click, btnnuevo3_Click, Buscar_Click.
- **Tablas/vistas/procedimientos:** vt_bodegasBC, vt_ciudad, vt_clase, vt_clientes, vt_clientes_1, vt_color, vt_especialidades, vt_especie, vt_EstadoCivil, vt_FormasPagoVigentes, vt_GeneroClientes, vt_ingreso, vt_motivomuerte, vt_nombres_cientificos, vt_obtension, vt_ordenAtencion, vt_pacientes, vt_pacientes_1, vt_personales, vt_provincias, vt_razas, vt_sexo, vt_tipo_paciente_clasifica, vt_TipoIdentificacion, vt_tipoPaciente, vt_TipoPersonas, vt_usuarios, vt_verOrdenes.
- **Archivos físicos:** /imagenes/avatar_paciente_default.png, /imagenes/Edit.png, /imagenes/numerica_fauna_silvestre_logo.png, /imagenes/numerica_veterinaria_logo.png, /imagenes/temperamento/amarillo.png, /imagenes/temperamento/blanco.png, /imagenes/temperamento/rojo.png, /imagenes/temperamento/verde.png, /imagenes/userlogo.png, imagenes/temperamento/amarillo.png, imagenes/temperamento/blanco.png, imagenes/temperamento/rojo.png, imagenes/temperamento/verde.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/ClientesPacientes.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ClientesPacientes.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_Plan.aspx`

- **Code-behind:** `vt_Plan.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** ddlTipoAnestesia, form1, grdOperacion, imp1, Label1, Label2, Label3, Label4, LabelIdRegistro, LbCodCli, Lbespecialidad, lbficha, Lbiduser, lbl_idesp, lbl_iduser, lblAnios, lblCliente, lblespecie, Lblfecha, lblHostName, lblIdFichaNueva, LblIdorden, lblInicia, lblIPAddress, lblMedico.
- **Eventos y operaciones:** GuardarDesc, Page_Load, RadGrabar_Click.
- **Tablas/vistas/procedimientos:** qvt_formulario_alta, qvt_productosUtilizados, qvt_RegistroAnestesia, qvt_team, vt_anestesia, vt_clientes, vt_especie, vt_ordenAtencion, vt_pacientes, vt_Plan, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/SalaRecuperacion.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`, `Services/ReportExportService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/SalaRecuperacion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_postquirurgico.aspx`

- **Code-behind:** `vt_postquirurgico.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Buscar, Content1, Content2, ContentPlaceHolder1, GridView1, GridView5, grv_Ordenes_Atencion, head, imgPacienteGrd, Label1, Lbespecialidad, Lbiduser, lbl_Cliente, lbl_esp_numero, lbl_etapa, lbl_fecha, lbl_hora, lbl_id_medico, lbl_medico, lbl_nhcl, lbl_ordenAtem_Id, lbl_ordenAtem_nombre, lbl_registra, lbl_tipo, lbl_tipoorden.
- **Eventos y operaciones:** Buscar_Click, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging, grv_Ordenes_Atencion_RowCommand, Page_Load, refrescar_Click.
- **Tablas/vistas/procedimientos:** vt_clase, vt_clientes, vt_clientes_1, vt_especialidades, vt_especie, vt_nombres_cientificos, vt_ordenAtencion, vt_pacientes, vt_pacientes_1, vt_postquirurgico, vt_razas, vt_RecuperacionPrincipal, vt_sexo, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/SalaRecuperacion.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/SalaRecuperacion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_PreAnestesia.aspx`

- **Code-behind:** `vt_PreAnestesia.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** agregarproducto, btnbusquedamedicamento, Bueno, chkAbdomen, chkCabeza, chkCuello, chkEndoscopia, chkExtradural, chkExtratoracica, chkExtremidades, chkIntradural, chkIntratoracica, chkLaboratorio, chkObstetricas, chkOdeS, chkPerineales, cmbtipoamb, cmbvia, Deprimido, form1, funcioncargo, GrabaCabecera, Grdmed, grdMedicinas, GridView2.
- **Eventos y operaciones:** agregarproducto_Click, btnbusquedamedicamento_Click, GrabaCabecera_Click, Grdmed_SelectedIndexChanging, grdMedicinas_ItemCommand, ImageButton7_Click, Page_Load.
- **Tablas/vistas/procedimientos:** qvt_preanestesia, qvt_productosUtilizados, qvt_via, vt_bodegasBC, vt_clientes, vt_especie, vt_labOrdenes, vt_ordenAtencion, vt_pacientes, vt_PreAnestesia, vt_pruebas_laboratorio, vt_reservaProductosStockBC, vt_tipos_usuarios, vt_usuarios.
- **Archivos físicos:** /imagenes/add_icons1.png, /imagenes/agregar.png, /imagenes/Delete.png, /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/PreAnestesia.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/PreAnestesia.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_preenfermeria.aspx`

- **Code-behind:** `vt_preenfermeria.aspx.vb`.
- **Propósito funcional:** Módulo Kardex y enfermería; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grv_Ordenes_Atencion, head, imgPacienteGrd, Label1, Lbespecialidad, Lbiduser, lbl_Cliente, lbl_esp_numero, lbl_etapa, lbl_fecha, lbl_hora, lbl_id_medico, lbl_medico, lbl_nhcl, lbl_ordenAtem_Id, lbl_ordenAtem_nombre, lbl_registra, lbl_tipo, lbl_tipoorden, Lblespecialidad, lblFecha, lblHostName.
- **Eventos y operaciones:** grv_Ordenes_Atencion_RowCommand, Page_Load.
- **Tablas/vistas/procedimientos:** vt_clientes, vt_especialidades, vt_ordenAtencion, vt_pacientes, vt_preenfermeria, vt_quirenfermeria, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/BandejaOrdenes.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/BandejaOrdenes.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_prequirurgico.aspx`

- **Code-behind:** `vt_prequirurgico.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Buscar, BuscarC, Content1, Content2, ContentPlaceHolder1, GridView1, GridView2, GridView3, GridView4, GridView5, GridView6, GridView7, GridView8, grv_Ordenes_Atencion, head, imgPacienteGrd, Label1, Label2, Label3, Label4, Label5, Lbespecialidad, Lbiduser, lbl_Cliente, lbl_esp_numero.
- **Eventos y operaciones:** Buscar_Click, BuscarC_Click, GridView1_SelectedIndexChanging, GridView2_SelectedIndexChanging, GridView3_SelectedIndexChanging, GridView4_SelectedIndexChanging, GridView5_SelectedIndexChanging, GridView7_SelectedIndexChanging, GridView8_SelectedIndexChanging, grv_Ordenes_Atencion_RowCommand, Page_Load.
- **Tablas/vistas/procedimientos:** vt_cie10, vt_clase, vt_clientes, vt_clientes_1, vt_diagnosticos, vt_especialidades, vt_especie, vt_fichamedica, vt_hisDatMedicos, vt_labOrdenes, vt_localidades, vt_nombres_cientificos, vt_ordenAtencion, vt_pacientes, vt_pacientes_1, vt_prequirurgico, vt_productos, vt_pruebas_laboratorio, vt_quirpersonales, vt_razas, vt_rxOrdenes, vt_sexo, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/PreAnestesia.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/PreAnestesia.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_principal.aspx`

- **Code-behind:** `vt_principal.aspx.vb`.
- **Propósito funcional:** Módulo Órdenes y recepción; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grvqry, head, Imgmostrar, lblAgendadosveterinaria, modalPopup, RadAjaxPanel1, RadGrid1, RadScriptManager1.
- **Eventos y operaciones:** Imgmostrar_Click, Page_Load, RadGrid1_ItemCommand.
- **Tablas/vistas/procedimientos:** vt_citas, vt_clientes, vt_principal.
- **Archivos físicos:** /imagenes/selected.png, imagenes/imgPrincipalnueva.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/Home.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/Home.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_protocolo.aspx`

- **Code-behind:** `vt_protocolo.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnSave, form1, Grdestudiantes, Imgagregaestudiante, imp1, Label1, LbCodCli, Lbespecialidad, lbficha, Lbiduser, lbl_idesp, lbl_iduser, lblAnios, lblCliente, lblespecie, lblFecha, lblHostName, lblIdFichaNueva, Lblidorden, lblInicia, lblIPAddress, lblMedico, lblMeses, lblNHCL, lblNombre.
- **Eventos y operaciones:** btnSave_Click, GuardarDesc, Imgagregaestudiante_Click, Page_Load.
- **Tablas/vistas/procedimientos:** qvt_Protocolo, qvt_team, vt_clientes, vt_especie, vt_estudiantes, vt_estudiantesorden, vt_ordenAtencion, vt_pacientes, vt_protocolo, vt_usuarios.
- **Archivos físicos:** /imagenes/add_icons1.png, /imagenes/Delete.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/ProtocoloQuirurgico.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ProtocoloQuirurgico.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_quirArchivos.aspx`

- **Code-behind:** `vt_quirArchivos.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** form1, lblCliente, lblNhcl, lblNombre, RadFileExplorer1, ScriptManager1.
- **Eventos y operaciones:** vt_quirArchivos_Load.
- **Tablas/vistas/procedimientos:** vt_clientes, vt_pacientes, vt_quirArchivos, vt_quirArchivos_Load.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `vt_quirenfermeria.aspx`

- **Code-behind:** `vt_quirenfermeria.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** antabdominal, antanamnesis, antanios, antapuntes, antauscultacion, antcavidadoral, antcliente, antcp, antdescripcion, antdeshidratacion, antespecialidad, antespecie, antesterilizado, antfecha, antfrecuenciacardiaca, antfrecuenciarespiratoria, anticc, antimagen, antlinfonodos, antmedico, antmeses, antmotivo, antmucosas, antnhcl, antpaciente.
- **Eventos y operaciones:** GridView1_RowCommand, GridView1_SelectedIndexChanged, GridView2_RowCommand, GridView2_SelectedIndexChanged, GridView3_RowCommand, GridView3_SelectedIndexChanged, GridView4_RowCommand, GridView4_SelectedIndexChanged, GridView5_RowCommand, GridView5_SelectedIndexChanged, GridView6_SelectedIndexChanged, Page_Load.
- **Tablas/vistas/procedimientos:** vt_accesorios, vt_cie10, vt_clientes, vt_consultas, vt_diagnosticos, vt_ecoInformes, vt_endoInformes, vt_EnfermeriaKardex, vt_EnfermeriaSignos, vt_especialidades, vt_especie, vt_estudiantes, vt_estudiantesorden, vt_evoluciones, vt_fichamedica, vt_fisioterapia_ordenes, vt_formatos, vt_labInformes, vt_labOrdenes, vt_localidades, vt_medicinas, vt_medInformes, vt_notas, vt_pacientes, vt_PreAnestesia, vt_procedimientos, vt_quirenfermeria, vt_RecepcionPaciente, vt_rxInformes, vt_rxOrdenes, vt_tipos_usuarios, vt_usuarios.
- **Archivos físicos:** /imagenes/b_report.png, /imagenes/dorsal.jpg, /imagenes/dorsalgato.jpg, /imagenes/equinos.jpg.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/KardexEnfermeria.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/KardexEnfermeria.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_QuirofanoIngreso.aspx`

- **Code-behind:** `vt_QuirofanoIngreso.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Buscar, Content1, Content2, ContentPlaceHolder1, GridView1, GridView5, grv_Ordenes_Atencion, head, imgPacienteGrd, Label1, Lbespecialidad, Lbiduser, lbl_Cliente, lbl_esp_numero, lbl_etapa, lbl_fecha, lbl_hora, lbl_id_medico, lbl_medico, lbl_nhcl, lbl_ordenAtem_Id, lbl_ordenAtem_nombre, lbl_registra, lbl_tipo, lbl_tipoorden.
- **Eventos y operaciones:** Buscar_Click, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging, grv_Ordenes_Atencion_RowCommand, Page_Load, refrescar_Click.
- **Tablas/vistas/procedimientos:** vt_clase, vt_clientes, vt_clientes_1, vt_especialidades, vt_especie, vt_nombres_cientificos, vt_ordenAtencion, vt_pacientes, vt_pacientes_1, vt_QuirofanoIngreso, vt_QuirofanoPrincipal, vt_razas, vt_sexo, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/OperacionesClinicas.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/OperacionesClinicas.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_QuirofanoPrincipal.aspx`

- **Code-behind:** `vt_QuirofanoPrincipal.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** anestesia, Content1, Content2, ContentPlaceHolder1, evolucion, head, lblIdOrden, lblNhcl, LtrAnestesia, LtrEvolucion, LtrPausa, LtrProtocolo, ltrRecepcionEnfermeria, LtrRecepcionPaciente, ltrRegistroEnfermeria, pausa, protocolo, RadMultiPage1, RadTabStrip1, recepcionEnfermeria, recepcionPaciente, registroEnfermeria, ScriptManager1.
- **Eventos y operaciones:** RadTabStrip1_TabClick, vt_QuirofanoPrincipal_Load.
- **Tablas/vistas/procedimientos:** vt_ControlIngreso, vt_EnfermeriaKardex, vt_evoluciones, vt_hojaanestesia, vt_protocolo, vt_QuirofanoPrincipal, vt_QuirofanoPrincipal_Load, vt_RecepcionPaciente, vt_RegistroIngreso.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/ProgramacionClinica.razor`.
- **Servicio equivalente:** `Services/GestionMigradaService.cs`, `Services/KardexService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ProgramacionClinica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_quirpersonales.aspx`

- **Code-behind:** `vt_quirpersonales.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Archivos, Cirugia, Content1, Content2, ContentPlaceHolder1, head, LbCodCli, Lbespecialidad, Lbiduser, lbl_idesp, lbl_iduser, lblAnios, lblCliente, lblespecie, lblFecha, lblHostName, lblIdFichaNueva, Lblidorden, lblInicia, lblIPAddress, lblMedico, lblMeses, lblNHCL, lblNombre, Lblocalidad.
- **Eventos y operaciones:** Page_Load, RadTabStrip1_TabClick.
- **Tablas/vistas/procedimientos:** vt_autorizaciones, vt_clientes, vt_especie, vt_pacientes, vt_quirArchivos, vt_quirpersonales, vt_quirTeam.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `vt_QuirProformas.aspx`

- **Code-behind:** `vt_QuirProformas.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** BuscarC, Content1, Content2, ContentPlaceHolder1, GridView7, GridView8, grv_Ordenes_Atencion, head, Image1, imgPacienteGrd, Label1, Label5, LbCodCli, Lbespecialidad, Lbiduser, lbl_cedula, lbl_cierre, lbl_Cliente, lbl_esp_numero, lbl_etapa, lbl_fecha, lbl_hora, lbl_id_medico, lbl_idBC, lbl_idcliente.
- **Eventos y operaciones:** BuscarC_Click, EliminaReservas, GridView7_SelectedIndexChanging, GridView8_SelectedIndexChanging, grv_Ordenes_Atencion_RowCommand, Page_Load, refrescar_Click.
- **Tablas/vistas/procedimientos:** qvt_graficaBC, qvt_ingliqadm, qvt_productosUtilizados, vt_bodegasBC, vt_clase, vt_clientes, vt_clientes_1, vt_diagnosticos, vt_especialidades, vt_especie, vt_facturasBCquirofano, vt_labOrdenes, vt_nombres_cientificos, vt_ordenAtencion, vt_pacientes, vt_pacientes_1, vt_QuirProformas, vt_razas, vt_reservaProductosStockBC, vt_rxOrdenes, vt_sexo, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/ProformasFacturacion.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ProformasFacturacion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_quirTeam.aspx`

- **Code-behind:** `vt_quirTeam.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btn_grabar, btnCirculantes, btnInserta, btnInsertaAnestesiologo, btnInsertaAyudante, btnInsertaCirujano, btnInsertaProcedimiento, btninsertdiag, btnInstrumentistas, chkPrincipal0, chkPrincipal1, chkPrincipal2, DataProcedimientos, DropDownList1, form1, Grddiagnosticos, Grdprocedimiento, GridView6, GridView7, GridView8, GridView9, grvCirculantes, grvInstrumentistas, Label1, Label2.
- **Eventos y operaciones:** btn_grabar_Click, btnCirculantes_Click, btnInserta_Click, btnInsertaAnestesiologo_Click, btnInsertaAyudante_Click, btnInsertaCirujano_Click, btnInsertaProcedimiento_Click, btninsertdiag_Click, btnInstrumentistas_Click, GridView6_SelectedIndexChanging.
- **Tablas/vistas/procedimientos:** qvt_procedimientosQuirofanos, qvt_team, vt_cie10, vt_clientes, vt_diagnosticos, vt_especialidades, vt_especie, vt_ordenAtencion, vt_pacientes, vt_productos, vt_quirTeam, vt_tipos_usuarios, vt_usuarioLocalidades, vt_usuarios.
- **Archivos físicos:** /imagenes/agregar.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `vt_razas.aspx`

- **Code-behind:** `vt_razas.aspx.vb`.
- **Propósito funcional:** Módulo Clientes y pacientes; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radespecie, Radestado, RadScriptManager1, Txtraza.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_especie, vt_razas.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/CatalogosClinicos.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/CatalogosClinicos.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_RecepcionPaciente.aspx`

- **Code-behind:** `vt_RecepcionPaciente.aspx.vb`.
- **Propósito funcional:** Recepción preoperatoria del paciente: consentimientos, exámenes, alergias, signos vitales, vía endovenosa, premedicación, prótesis y observaciones antes del ingreso a quirófano.
- **Campos principales:** btnPrint, enviar, form1, imp1, Label1, Label2, Label3, Label4, Lbespecialidad, Lbiduser, lblCirugiaProgramada, lblHostName, lblId, Lblidcliente, lblIPAddress, lblNhcl, lblnombres, Lblocalidad, lblpaciente, lblsexo, lborden, Lbtipouser, numdiastolicaedit, nummiccioedit, numpesoedit.
- **Eventos y operaciones:** enviar_Click, Page_Load.
- **Tablas/vistas/procedimientos:** qvt_checkpreoperatorio, vt_antecedentes, vt_clientes, vt_ordenAtencion, vt_pacientes, vt_RecepcionPaciente.
- **Archivos físicos:** p.rpt.
- **Reportes Crystal:** p.rpt.
- **Correo:** No; el código solo consulta el correo del propietario como dato de cabecera.
- **Blazor equivalente:** `Components/Pages/ChecklistPreoperatorio.razor`.
- **Servicio equivalente:** `Services/QuirofanoService.cs`, `Services/KardexService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Vista y servicio implementados; conserva `qvt_checkpreoperatorio`, sincroniza alergias con `vt_antecedentes`, bloquea órdenes cerradas, valida campos obligatorios, calcula IMC e imprime desde el navegador.
- **Diferencias encontradas:** El archivo `p.rpt` existe en el sistema antiguo, pero esta página imprime el HTML con `window.print()`; Blazor conserva ese comportamiento y moderniza la presentación.
- **Trabajo requerido:** Ejecutar una prueba funcional contra una base de datos de pruebas y validar el resultado impreso con el personal clínico antes de marcar la migración como completa.

### `vt_recetasnew.aspx`

- **Code-behind:** `vt_recetasnew.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnGenerar, Content1, Content2, ContentPlaceHolder1, CrystalReportSource1, DateTimePicker1, DateTimePicker2, head, ltrDocs, RadGrid1, RadGrid2, RadNhcl, RadScriptManager1, RadWinDoc.
- **Eventos y operaciones:** btnGenerar_Click, Page_Load, RadGrid1_ItemCommand, RadGrid2_ItemCommand.
- **Tablas/vistas/procedimientos:** vt_cabecerareceta, vt_cie10, vt_clientes, vt_diagnosticos, vt_diagnosticosexternos, vt_especialidades, vt_fichamedica, vt_IndicacionesGenerales, vt_localidades, vt_medicinas, vt_medicinasexterno, vt_ordenAtencion, vt_pacientes, vt_recetasnew, vt_usuarios.
- **Archivos físicos:** /imagenes/b_report.png, Reporteria/HDEV/RptrecetaHDEV.rpt.
- **Reportes Crystal:** Reporteria/HDEV/RptrecetaHDEV.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** No identificado.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **faltante**.
- **Evidencia:** Sin página Razor equivalente demostrable por nombre o mapeo funcional.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Definir equivalencia vigente e implementar o justificar obsolescencia.

### `vt_RecetassinOrden.aspx`

- **Code-behind:** `vt_RecetassinOrden.aspx.vb`.
- **Propósito funcional:** Emitir una receta médica directamente desde la historia del paciente, sin requerir una orden de atención.
- **Campos principales:** btnDiagnosticos, btnEnviarFactura, btnGenerar, btnMedicinas, btnnuevo, btntraemed, Buscar, cmbtipodiagnostico, Content1, Content2, ContentPlaceHolder1, CrystalReportSource1, Grdmed, GridView1, GridView5, grvDiagnosticos, GrvMedicinas, head, Image1, LbCodCli, Lbespecialidad, Lbiduser, lbl_iddinamics, lbl_iva, lbl_precio.
- **Eventos y operaciones:** btnDiagnosticos_Click, btnEnviarFactura_Click, btnGenerar_Click, btnMedicinas_Click, btnnuevo_Click, btntraemed_Click, Buscar_Click, EliminaReservasSO, enviarAdjuntoMail, Grdmed_SelectedIndexChanging, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging.
- **Tablas/vistas/procedimientos:** vt_bodegasBC, vt_cabecerareceta, vt_cie10, vt_clase, vt_clientes, vt_clientes_1, vt_diagnosticosexternos, vt_especialidades, vt_especie, vt_facturasBCsinOrden, vt_localidades, vt_medicinasexterno, vt_medicinasexterno_1, vt_nombres_cientificos, vt_pacientes, vt_pacientes_1, vt_razas, vt_RecetassinOrden, vt_reservaProductosStockBC, vt_sexo, vt_usuarios.
- **Archivos físicos:** /imagenes/buscar0.png, /imagenes/Delete.png, /imagenes/Edit.png, /Reporteria/HDEV/rptRecetaEmail.rpt, /Reporteria/HDEV/RptrecetaHDEV.rpt.
- **Reportes Crystal:** /Reporteria/HDEV/rptRecetaEmail.rpt, /Reporteria/HDEV/RptrecetaHDEV.rpt.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/RecetaSinOrden.razor`.
- **Servicio equivalente:** `Services/ConsultaExternaService.cs`, `Services/EFactCatalogService.cs`, `Services/ReportExportService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Búsqueda y selección del paciente, cabecera en `vt_cabecerareceta`, diagnósticos externos, medicamentos de catálogo o libres, indicaciones, serial y generación PDF.
- **Diferencias encontradas:** El envío automático por correo, la reserva de inventario y la creación directa de factura de Business Central no se ejecutan desde la receta; el usuario continúa hacia el módulo moderno de proformas y facturación.
- **Trabajo requerido:** Probar el esquema real de las tres tablas externas y definir con administración si deben reactivarse los efectos automáticos de correo, reserva y facturación.

### `vt_Recuperacion.aspx`

- **Code-behind:** `vt_Recuperacion.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Agregar, agregarproducto, btnactualizaingresos, btnbusquedamedicamento, BtnImprimir, btninsertapost, btninsertarinforme, btninsertliqadm, btninsertliqelim, cmbvia, form1, funcioncargo, Grdmed, grdMedicinas, Grdmedx, Image1, Image2, Image3, Image4, Image5, Image6, Image7, ImageButton10, imgSistolica, imp1.
- **Eventos y operaciones:** Agregar_Click, agregarproducto_Click, btnactualizaingresos_Click, btnbusquedamedicamento_Click, BtnImprimir_Click, btninsertapost_Click, btninsertarinforme_Click, btninsertliqadm_Click, btninsertliqelim_Click, Grdmed_SelectedIndexChanging, grdMedicinas_ItemCommand, Grdmedx_SelectedIndexChanging, ImageButton10_Click, Page_Load, RadGrid1_ItemCommand.
- **Tablas/vistas/procedimientos:** qvt_ingliqadm, qvt_observaciones_recuperacion, qvt_post_operatorio, qvt_productosUtilizados, qvt_recuperacion, qvt_via, vt_bodegasBC, vt_clientes, vt_especie, vt_ordenAtencion, vt_pacientes, vt_Recuperacion, vt_reservaProductosStockBC, vt_tipos_usuarios, vt_usuarios.
- **Archivos físicos:** ../imagenes/diastolica.png, ../imagenes/equis.png, ../imagenes/oxigeno.png, ../imagenes/oxigeno1.png, ../imagenes/oxigeno2.png, ../imagenes/punto.png, ../imagenes/sistolica.png, ../imagenes/temperatura.png, /imagenes/agregar.png, /imagenes/buscar0.png, /imagenes/diastolica.png, /imagenes/Edit.png, /imagenes/Equis.png, /imagenes/oxigeno.png, /imagenes/oxigeno1.png, /imagenes/oxigeno2.png, /imagenes/punto.png, /imagenes/sistolica.png, /imagenes/temperatura.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/SalaRecuperacion.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/SalaRecuperacion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_RecuperacionPrincipal.aspx`

- **Code-behind:** `vt_RecuperacionPrincipal.aspx.vb`.
- **Propósito funcional:** Módulo Quirófano y anestesia; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, head, lblIdOrden, lblNhcl, LtrPlan, LtrRecuperacion, Plan, RadMultiPage1, RadTabStrip1, Recuperacion, ScriptManager1.
- **Eventos y operaciones:** RadTabStrip1_TabClick, vt_RecuperacionPrincipal_Load.
- **Tablas/vistas/procedimientos:** vt_Plan, vt_Recuperacion, vt_RecuperacionPrincipal, vt_RecuperacionPrincipal_Load.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/SalaRecuperacion.razor`.
- **Servicio equivalente:** `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/SalaRecuperacion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_RegistroIngreso.aspx`

- **Code-behind:** `vt_RegistroIngreso.aspx.vb`.
- **Propósito funcional:** Módulo Operaciones clínicas; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** agregarproducto, btnbusquedamedicamento, btnCirculantes, btnInsertaAnestesiologo, btnInsertaAyudante, btnInsertaCirujano, btnInsertaProcedimiento, btninsertdiag, btnInstrumentistas, btnPrint, btnSave, CheckBox1, CheckBoxColocaUQ, CheckBoxDesfl, CheckBoxEsmer, CheckBoxfentanyl, CheckBoxinten, CheckBoxOxi, CheckBoxPropofol, CheckBoxRx, CheckBoxSevorane, CheckBoxunidadElec, CheckBoxVesical, chkPielIntegra, cmbvia.
- **Eventos y operaciones:** agregarproducto_Click, btnbusquedamedicamento_Click, btnCirculantes_Click, btnInsertaAnestesiologo_Click, btnInsertaAyudante_Click, btnInsertaCirujano_Click, btnInsertaProcedimiento_Click, btninsertdiag_Click, btnInstrumentistas_Click, btnSave_Click, DiagPost_Click.
- **Tablas/vistas/procedimientos:** qvt_ingresocirugia, qvt_productosUtilizados, qvt_team, qvt_via, vt_anestesia, vt_bodegasBC, vt_cie10, vt_clientes, vt_especialidades, vt_especie, vt_ordenAtencion, vt_pacientes, vt_productos, vt_RegistroIngreso, vt_reservaProductosStockBC, vt_tipos_usuarios, vt_usuarioLocalidades, vt_usuarios.
- **Archivos físicos:** /imagenes/agregar.png, /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/OperacionesClinicas.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/OperacionesClinicas.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_tipo_cliente.aspx`

- **Code-behind:** `vt_tipo_cliente.aspx.vb`.
- **Propósito funcional:** Módulo Clientes y pacientes; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radestado, RadScriptManager1, txttipopersonal.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_tipo_cliente, vt_tipo_paciente_clasifica.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/ClientesPacientes.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ClientesPacientes.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_tipo_paciente.aspx`

- **Code-behind:** `vt_tipo_paciente.aspx.vb`.
- **Propósito funcional:** Módulo Clientes y pacientes; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, grabar, GridView1, head, Lbespecialidad, Lbid, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, nuevo, RadAjaxLoadingPanel1, RadAjaxPanel1, Radestado, RadScriptManager1, Txttipo.
- **Eventos y operaciones:** grabar_Click, GridView1_SelectedIndexChanging, nuevo_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_tipo_paciente, vt_tipoPaciente.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/ClientesPacientes.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ClientesPacientes.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_UniProformas.aspx`

- **Code-behind:** `vt_UniProformas.aspx.vb`.
- **Propósito funcional:** Módulo Proformas y facturación; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btngenerarproforma, btnnueva, btnpreliminares, Buscar, chkRxItem1, chkRxItem2, chkRxItem3, Content1, Content2, ContentPlaceHolder1, diagnostico, GridView1, GridView5, GrvAmbulatorias, Grvhospitalario, Grvquirurgico, head, Image1, Label1, Label2, Label4, LbCodCli, Lbespecialidad, Lbiduser, lblaleatorio.
- **Eventos y operaciones:** btngenerarproforma_Click, btnnueva_Click, btnpreliminares_Click, Buscar_Click, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging, Page_Load, RadGrid1_ItemCommand.
- **Tablas/vistas/procedimientos:** vt_clase, vt_clientes, vt_clientes_1, vt_diagnosticos, vt_especialidades, vt_especie, vt_facturasBC, vt_facturasBChospi, vt_facturasBCquirofano, vt_facturasBCunificadas, vt_nombres_cientificos, vt_ordenAtencion, vt_pacientes, vt_pacientes_1, vt_razas, vt_secuencialUnificadasBC, vt_sexo, vt_UniProformas, vt_usuarios.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/ProformasFacturacion.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/ProformasFacturacion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_usuarios.aspx`

- **Code-behind:** `vt_usuarios.aspx.vb`.
- **Propósito funcional:** Módulo Administración; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnmas0, cmblocalidad, Content1, Content2, ContentPlaceHolder1, DdldimensionMedico, ddlespecialidad, DdlproveedorMedico, ddltipo, DropDownList1, GRABAR, GridView1, grvusuarios0, head, lbcodigousu, lblHostName, lblIPAddress, lbsesion, Nuevo, opcion, Panel1, Panel2, Panel3, Permisos, RadAjaxLoadingPanel1.
- **Eventos y operaciones:** btnmas0_Click, ddltipo_SelectedIndexChanged, GRABAR_Click, GridView1_SelectedIndexChanged, Nuevo_Click, Page_Load, Permisos_Click, RadButton1_Click.
- **Tablas/vistas/procedimientos:** vt_especialidades, vt_horariosmedicos, vt_localidades, vt_permisos, vt_tipos_usuarios, vt_usuariolocalidades, vt_usuarios.
- **Archivos físicos:** /imagenes/agregar.png, /imagenes/Delete.png, /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/UsuariosAdministracion.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/UsuariosAdministracion.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_vacunas.aspx`

- **Code-behind:** `vt_vacunas.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnIngreso0, DefaultSetOfTools, form1, Label11, Label12, Label14, Label16, Label18, Label9, LbCodCli, Lbespecialidad, Lbiduser, lbl_cliente, lbl_nhcl, lbl_nombre_pac, lbl_tipo, lblanios, lblHostName, lblIPAddress, lblmeses, Lblocalidad, Lbtipouser, RadAjaxLoadingPanel1, RadAjaxPanel1, RadDateDia.
- **Eventos y operaciones:** btnIngreso0_Click, Page_Load, RadGrid1_ItemCommand.
- **Tablas/vistas/procedimientos:** vt_clientes, vt_pacientes, vt_productos, vt_usuarios, vt_vacunas.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/FichaMedica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/FichaMedica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_vacunasfinal.aspx`

- **Code-behind:** `vt_vacunasfinal.aspx.vb`.
- **Propósito funcional:** Módulo Historia clínica; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btnIngreso0, Buscar, Content1, Content2, ContentPlaceHolder1, DefaultSetOfTools, GridView1, GridView5, head, LbCodCli, Lbespecialidad, Lbiduser, lblAnios, lblcliente, Lblemail, lblHostName, lblIdPac, lblIPAddress, lblMeses, lblNHCL, lblNombre, Lblocalidad, lbltipo, Lbtipouser, RadAjaxLoadingPanel1.
- **Eventos y operaciones:** btnIngreso0_Click, Buscar_Click, enviarRecordatorio, GridView1_SelectedIndexChanging, GridView5_SelectedIndexChanging, Page_Load, RadGrid1_ItemCommand.
- **Tablas/vistas/procedimientos:** vt_clase, vt_clientes, vt_clientes_1, vt_especie, vt_nombres_cientificos, vt_pacientes, vt_pacientes_1, vt_productos, vt_razas, vt_sexo, vt_usuarios, vt_vacunas, vt_vacunasbc, vt_vacunasfinal, vt_vitacoravacuna.
- **Archivos físicos:** /imagenes/Edit.png.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/FichaMedica.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/InventarioService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/FichaMedica.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_vercitas.aspx`

- **Code-behind:** `vt_vercitas.aspx.vb`.
- **Propósito funcional:** Módulo Agenda y horarios; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** btn_consultar, Content1, Content2, ContentPlaceHolder1, ddlLocalidad, DropDownList1, DropDownList2, DropDownList3, grd_citas, head, Lbespecialidad, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, RadAjaxLoadingPanel1, RadAjaxPanel1, RadScriptManager1, SqlDataSource1, SqlDataSource2, SqlDataSource3, SqlDataSource4, SqlDataSource5, txt_cliente_apellidos.
- **Eventos y operaciones:** btn_consultar_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_citas, vt_clientes, vt_eliminaCitaOp, vt_especialidades, vt_localidades, vt_pacientes, vt_usuariolocalidades, vt_usuarios, vt_vercitas.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** Sí; revisar implementación y destinatarios en code-behind.
- **Blazor equivalente:** `Components/Pages/AgendaCitas.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/AgendaCitas.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_verOrdenes.aspx`

- **Code-behind:** `vt_verOrdenes.aspx.vb`.
- **Propósito funcional:** Módulo Órdenes y recepción; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, conteo, ddrespecialidad, ddrLocalidad, ddrmedicos, dsapellidos, dsnombres, dspacientes, head, Lbclientecambio, Lbespecialidad, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbnhclpac, Lbpacientecambio, LbsecOrden, Lbtipouser, listaordenes, modalPopup, Panel4, RadAjaxLoadingPanel1.
- **Eventos y operaciones:** ddrespecialidad_SelectedIndexChanged, ddrLocalidad_SelectedIndexChanged, ddrmedicos_SelectedIndexChanged, RadCambioAcepta_Click, RadCambioespecialidad_SelectedIndexChanged, RadCancelaCambio_Click, RadGrid1_ItemCommand, RadGrid1_SortCommand, txt_desdefec_SelectedDateChanged, txt_hastafec_SelectedDateChanged, ver_ordenesAtencion_aspx_Load.
- **Tablas/vistas/procedimientos:** vt_clientes, vt_especialidades, vt_fichamedica, vt_localidades, vt_ordenAtencion, vt_pacientes, vt_usuarios, vt_usuarios_1, vt_verOrdenes.
- **Archivos físicos:** RadGrid1.ExportSettings.Pdf.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/RecepcionOrdenes.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/RecepcionOrdenes.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

### `vt_vitacora.aspx`

- **Code-behind:** `vt_vitacora.aspx.vb`.
- **Propósito funcional:** Módulo Administración; confirmar el propósito exacto durante la validación funcional.
- **Campos principales:** Content1, Content2, ContentPlaceHolder1, Ejecutar, head, Lbespecialidad, Lbiduser, lblHostName, lblIPAddress, Lblocalidad, Lbtipouser, RadAjaxLoadingPanel1, RadAjaxPanel1, RadGrid1, RadScriptManager1, TextBox1, TextBox2.
- **Eventos y operaciones:** Ejecutar_Click, Page_Load.
- **Tablas/vistas/procedimientos:** vt_usuarios, vt_vitacora.
- **Archivos físicos:** No detectado.
- **Reportes Crystal:** No detectado.
- **Correo:** No detectado.
- **Blazor equivalente:** `Components/Pages/BitacoraAuditoria.razor`.
- **Servicio equivalente:** `Services/AdministracionClinicaService.cs`, `Services/AdmisionFacturacionService.cs`, `Services/AuthService.cs`, `Services/ConsultaExternaService.cs`, `Services/GestionMigradaService.cs`, `Services/HospitalizacionService.cs`, `Services/KardexService.cs`, `Services/LaboratorioImagenService.cs`, `Services/QuirofanoService.cs`.
- **Estado:** **parcial**.
- **Evidencia:** Página Components/Pages/BitacoraAuditoria.razor localizada; equivalencia funcional aún requiere pruebas CRUD, permisos y efectos secundarios.
- **Diferencias encontradas:** La presencia de pantalla/servicio no demuestra paridad; quedan por contrastar validaciones, eliminación/anulación, impresión, permisos y efectos secundarios.
- **Trabajo requerido:** Comparar campo/evento, validar CRUD, permisos, reportes, archivos y efectos secundarios con base de pruebas.

