using System.ComponentModel.DataAnnotations;

namespace HosvetKardex.Web.Models;

// =========================================================================
// 1. CLIENTES Y PACIENTES (HDEV Mascotas Domésticas + TUERI Fauna Silvestre)
// Reemplaza: vt_personales.aspx.vb (3,427 líneas)
// =========================================================================

public class ClienteDto
{
    public int PaCodigo { get; set; }

    [Required(ErrorMessage = "Los nombres del propietario son obligatorios.")]
    public string PaNombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos del propietario son obligatorios.")]
    public string PaApellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de Cédula/RUC/Pasaporte es obligatorio.")]
    public string PaCedula { get; set; } = string.Empty

    public string PaTelefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono móvil es obligatorio.")]
    public string PaMovil { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Formato de correo electrónico inválido.")]
    public string PaEmail { get; set; } = string.Empty;

    public string PaEmailFactura { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección domiciliaria es obligatoria.")]
    public string PaDireccion { get; set; } = string.Empty;

    public string PaObservaciones { get; set; } = string.Empty;
    public DateTime PaFechaIngreso { get; set; } = DateTime.Now;
    public string PaEstado { get; set; } = "A";
    public string PaTipoCli { get; set; } = "R"; // R=Regular, C=Convenio, E=Empleado
    public string PaDocumento { get; set; } = "CEDULA"; // CEDULA, RUC, PASAPORTE
    public string PaNotas { get; set; } = string.Empty;
    public string PaGrupoCliente { get; set; } = "NAC"; // NAC=Nacionales
    public string PaTerminosPago { get; set; } = "CONTADO";
    public string PaTipoPersona { get; set; } = "NATURAL"; // NATURAL, JURIDICA
    public string PaGenero { get; set; } = "M";
    public string PaFormaPago { get; set; } = "EFECTIVO";
    public string PaEstadoCivil { get; set; } = "SOLTERO";

    public string NombreCompleto => $"{PaApellidos} {PaNombres}".Trim();
}

public class PacienteMascotaDto
{
    public int MasNhcl { get; set; }
    public int MasIdCliente { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteCedula { get; set; } = string.Empty;
    public string ClienteTelefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del paciente es obligatorio.")]
    public string MasNombre { get; set; } = string.Empty;

    // Origen: 'M' = Mascotas Domésticas (HDEV), 'T' = Fauna Silvestre (TUERI)
    public string MasOrigen { get; set; } = "M";

    public int MasSexo { get; set; } = 1;
    public string SexoDescripcion { get; set; } = string.Empty;
    public string MasCastrado { get; set; } = "NO";

    public int MasEspecie { get; set; } = 1;
    public string EspecieDescripcion { get; set; } = string.Empty;

    public int MasRaza { get; set; } = 1;
    public string RazaDescripcion { get; set; } = string.Empty;

    public int MasColor { get; set; } = 1;
    public string ColorDescripcion { get; set; } = string.Empty;

    public string MasOtros { get; set; } = string.Empty;
    public DateTime MasFechaNace { get; set; } = DateTime.Today.AddYears(-1);
    public string MasObservacion { get; set; } = string.Empty;
    public string MasEstado { get; set; } = "A";

    // Triaje / Temperamento ('green', 'yellow', 'red')
    public string MasColorTriaje { get; set; } = "green";

    // Campos exclusivos TUERI (Fauna Silvestre)
    public int? TueClase { get; set; }
    public string TueOtraClase { get; set; } = string.Empty;
    public string TuePeso { get; set; } = string.Empty;
    public string TueCientifico { get; set; } = string.Empty;
    public int? TueCategoria { get; set; }
    public string TueOtraCategoria { get; set; } = string.Empty;
    public int? TueFormaObtencion { get; set; }
    public string TueOtraObtencion { get; set; } = string.Empty;
    public int? TueRegion { get; set; }
    public string TueSectorBarrio { get; set; } = string.Empty;
    public int? TueProvincia { get; set; }
    public int? TueCiudad { get; set; }

    public string EdadCalculada
    {
        get
        {
            var hoy = DateTime.Today;
            int anios = hoy.Year - MasFechaNace.Year;
            if (MasFechaNace.Date > hoy.AddYears(-anios)) anios--;
            int meses = ((hoy.Year - MasFechaNace.Year) * 12) + hoy.Month - MasFechaNace.Month;
            if (anios > 0) return $"{anios} año(s)";
            return $"{Math.Max(0, meses)} mes(es)";
        }
    }
}

public class CatalogoItemGenericoDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int? IdPadre { get; set; }
}

// =========================================================================
// 2. RECEPCIÓN Y ÓRDENES DE ATENCIÓN
// Reemplaza: vt_OrdenesMenu.aspx.vb, vt_RecepcionPaciente.aspx.vb, vt_verOrdenes.aspx.vb
// =========================================================================

public class NuevaOrdenAtencionDto
{
    public int Nhcl { get; set; }
    public int IdCliente { get; set; }
    public string NombrePropietario { get; set; } = string.Empty;
    public string NombreMascota { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una especialidad.")]
    public int IdEspecialidad { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un médico tratante.")]
    public int IdMedico { get; set; }

    public int IdLocalidad { get; set; } = 1;
    public DateTime FechaOrden { get; set; } = DateTime.Today;
    public string HoraOrden { get; set; } = DateTime.Now.ToString("HH:mm");

    // 'M' = Mascotas HDEV, 'T' = TUERI
    public string TipoOrigen { get; set; } = "M";

    // 0=Ambulatorio, 1=Despacho Imagen, 2=Despacho Laboratorio, 3=Hospitalización, 4=Quirúrgico
    public int TipoDespacho { get; set; } = 0;

    public string ColorTriaje { get; set; } = "green";
    public string ObservacionIngreso { get; set; } = string.Empty;
}

public class OrdenAtencionResumenDto
{
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public int IdCliente { get; set; }
    public string Propietario { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public string Mascota { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public int IdMedico { get; set; }
    public string Medico { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Hora { get; set; } = string.Empty;
    public string Estado { get; set; } = "A"; // A=Activa/Espera, T=Terminada, P=Pre-alta, I=Anulada
    public string Etapa { get; set; } = "Consulta";
    public string TipoOrigen { get; set; } = "M";
    public int TipoDespacho { get; set; }
    public string ColorTriaje { get; set; } = "green";
    public string Facturado { get; set; } = "N";
    public string NumFacturaBc { get; set; } = string.Empty;

    public string DespachoDescripcion => TipoDespacho switch
    {
        1 => "Despacho Imagen",
        2 => "Despacho Laboratorio",
        3 => "Hospitalización",
        4 => "Quirófano",
        _ => "Ambulatorio / Consulta"
    };
}

public class ConteoMedicoEsperaDto
{
    public int IdMedico { get; set; }
    public string Medico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public int EnEspera { get; set; }
}

// =========================================================================
// 3. AGENDA MÉDICA Y CALENDARIZACIÓN DE CITAS
// Reemplaza: vt_administracionCitasnew.aspx.vb, vt_Calendarizacion.aspx.vb
// =========================================================================

public class CitaMedicaDto
{
    public int CiId { get; set; }

    [Required(ErrorMessage = "El nombre del propietario es obligatorio.")]
    public string CiNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    public string CiNombreMas { get; set; } = string.Empty;

    public int CiNhcl { get; set; }
    public DateTime CiFecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Seleccione la hora de la cita.")]
    public string CiHora { get; set; } = "08:00";

    public string CiEstadoCita { get; set; } = "N"; // N=Normal/Agendada, C=Confirmada, A=Atendida, X=Cancelada
    public string CiTelefono { get; set; } = string.Empty;
    public string CiEmail { get; set; } = string.Empty;
    public string CiObservacion { get; set; } = string.Empty;
    public int CiLocalidad { get; set; } = 1;
    public int CiUsuario { get; set; } // Médico asignado
    public string NombreMedico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public int CiUsuarioEjecuta { get; set; }
    public string CiEstado { get; set; } = "A";
    public int CiTipoConsulta { get; set; } = 1; // 1=Consulta General, 2=Especialidad, 3=Cirugía, 4=Control, 5=Imagen
    public string CiTipo { get; set; } = "M"; // M=Mascota, T=TUERI
}

public class SlotHorarioMedicoDto
{
    public string Hora { get; set; } = string.Empty;
    public bool Disponible { get; set; } = true;
    public bool Bloqueado { get; set; }
    public CitaMedicaDto? CitaExistente { get; set; }
    public string MotivoEstado { get; set; } = "Disponible";
}

// =========================================================================
// 4. FACTURACIÓN CONSOLIDADA / PROFORMAS BUSINESS CENTRAL
// Reemplaza: Hosp_PreProformas.aspx.vb, vt_QuirProformas.aspx.vb, vt_UniProformas.aspx.vb
// =========================================================================

public class ItemFacturableProformaDto
{
    public int IdRegistro { get; set; }
    public int IdOrden { get; set; }
    public string OrigenModulo { get; set; } = string.Empty; // HOSPITALIZACION, QUIROFANO, LABORATORIO, IMAGEN, CONSULTA
    public string CodigoItemBc { get; set; } = string.Empty;
    public string DescripcionItem { get; set; } = string.Empty;
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Math.Round(Cantidad * PrecioUnitario, 2);
    public DateTime FechaCargo { get; set; } = DateTime.Now;
    public string EstadoFacturacion { get; set; } = "PENDIENTE"; // PENDIENTE, ENVIADO_BC, ANULADO
    public bool Seleccionado { get; set; } = true;
}

public class OrdenProformaResumenDto
{
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public int IdCliente { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteCedula { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public string ClienteTelefono { get; set; } = string.Empty;
    public string MascotaNombre { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string MedicoTratante { get; set; } = string.Empty;
    public DateTime FechaOrden { get; set; }
    public string EtapaActual { get; set; } = string.Empty;
    public string FacturadoBc { get; set; } = "N";
    public string NumeroFacturaBc { get; set; } = string.Empty;
    public string IdFacturaBc { get; set; } = string.Empty;
    public List<ItemFacturableProformaDto> Items { get; set; } = new();

    public decimal TotalHospitalizacion => Items.Where(i => i.Seleccionado && i.OrigenModulo == "HOSPITALIZACION").Sum(i => i.Subtotal);
    public decimal TotalQuirofano => Items.Where(i => i.Seleccionado && i.OrigenModulo == "QUIROFANO").Sum(i => i.Subtotal);
    public decimal TotalLaboratorio => Items.Where(i => i.Seleccionado && i.OrigenModulo == "LABORATORIO").Sum(i => i.Subtotal);
    public decimal TotalImagen => Items.Where(i => i.Seleccionado && i.OrigenModulo == "IMAGEN").Sum(i => i.Subtotal);
    public decimal TotalGeneral => Items.Where(i => i.Seleccionado).Sum(i => i.Subtotal);
}

// =========================================================================
// 5. ADMINISTRACIÓN DE USUARIOS Y BITÁCORA DE AUDITORÍA
// Reemplaza: vt_usuarios.aspx.vb, vt_vitacora.aspx.vb
// =========================================================================

public class UsuarioSistemaDto
{
    public int UserNumero { get; set; }

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    public string UserNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El usuario (login) es obligatorio.")]
    public string UserLogin { get; set; } = string.Empty;

    public string UserClave { get; set; } = string.Empty;
    public string UserEstado { get; set; } = "A";
    public int UserTipoUserNumero { get; set; } = 2;
    public string TipoDescripcion { get; set; } = string.Empty;
    public int UserEspNumero { get; set; } = 1;
    public string EspecialidadDescripcion { get; set; } = string.Empty;
    public string UserObservaciones { get; set; } = string.Empty;
    public string UserTipoMedico { get; set; } = "P"; // P=Planta, L=Llamada, N=Ninguno
    public string UserCedula { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserConvencional { get; set; } = string.Empty;
    public string UserCelular { get; set; } = string.Empty;
    public string UserIniciales { get; set; } = string.Empty;
}

public class BitacoraAuditoriaDto
{
    public int VitId { get; set; }
    public string VitDetalle { get; set; } = string.Empty;
    public string UserLogin { get; set; } = string.Empty;
    public DateTime VitFechaMod { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
}
