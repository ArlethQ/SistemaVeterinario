using System.ComponentModel.DataAnnotations;

namespace HosvetKardex.Web.Models;

// ============================================================================
// 1. CATÁLOGO DE GRUPOS Y PRUEBAS DE LABORATORIO / IMAGENOLOGÍA
// ============================================================================
public class GrupoLaboratorioCatalogoDto
{
    public int GrupoId { get; set; }
    public string NombreGrupo { get; set; } = string.Empty;
    public string Icono { get; set; } = "bi-eyedropper";

    public static List<GrupoLaboratorioCatalogoDto> ObtenerGruposOficiales() => new()
    {
        new() { GrupoId = 1, NombreGrupo = "1. Hematología", Icono = "bi-droplet-fill" },
        new() { GrupoId = 2, NombreGrupo = "2. Hemostasia y Coagulación", Icono = "bi-heart-pulse" },
        new() { GrupoId = 3, NombreGrupo = "3. Química Sanguínea", Icono = "bi-Prescription2" },
        new() { GrupoId = 4, NombreGrupo = "4. Enzimas", Icono = "bi-activity" },
        new() { GrupoId = 5, NombreGrupo = "5. Electrolitos", Icono = "bi-lightning-charge" },
        new() { GrupoId = 6, NombreGrupo = "6. Uroanálisis", Icono = "bi-cup-straw" },
        new() { GrupoId = 7, NombreGrupo = "7. Microbiología", Icono = "bi-bug" },
        new() { GrupoId = 8, NombreGrupo = "8. Coproanálisis", Icono = "bi-search" },
        new() { GrupoId = 9, NombreGrupo = "9. Citología", Icono = "bi-bounding-box" },
        new() { GrupoId = 10, NombreGrupo = "10. Hormonas / Endocrinología", Icono = "bi-diagram-3" },
        new() { GrupoId = 11, NombreGrupo = "11. Perfiles Integrales", Icono = "bi-layers" },
        new() { GrupoId = 12, NombreGrupo = "12. Análisis Especiales", Icono = "bi-stars" },
        new() { GrupoId = 13, NombreGrupo = "13. Química Seca", Icono = "bi-grid-3x3" },
        new() { GrupoId = 14, NombreGrupo = "14. Gasometría Arterial/Venosa", Icono = "bi-lungs" },
        new() { GrupoId = 15, NombreGrupo = "15. Inmunología / Serología", Icono = "bi-shield-shaded" },
        new() { GrupoId = 16, NombreGrupo = "16. Drogas / Toxicología", Icono = "bi-exclamation-octagon" },
        new() { GrupoId = 0, NombreGrupo = "Todos los Exámenes", Icono = "bi-list-check" }
    };
}

public class PruebaCatalogoDto
{
    public string CodigoProd { get; set; } = string.Empty;
    public string IdEfact { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int GrupoId { get; set; }
    public string Categoria { get; set; } = "LAB"; // LAB, RX, ECOS
    public string UnidadMedida { get; set; } = "UND";
    public string GrupoIva { get; set; } = "IVA0";
    public decimal PrecioUnitario { get; set; }
    public bool Seleccionado { get; set; }
}

// ============================================================================
// 2. ÓRDENES DE LABORATORIO (vt_labOrdenes - Reemplaza vt_despacholab.aspx)
// ============================================================================
public class OrdenLaboratorioItemDto
{
    public int Sec { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string CodigoProd { get; set; } = string.Empty;
    public string DescripcionExamen { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public string Estado { get; set; } = "POR REALIZAR"; // POR REALIZAR, REALIZADO, Por Facturar
    public string Prioridad { get; set; } = "Rutina"; // Rutina, Emergente (labO_tipo)
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Iva { get; set; } = string.Empty;
    public string IdEfact { get; set; } = string.Empty;
    public string Medida { get; set; } = "UND";
    public decimal Precio { get; set; }
    public int? IdEvolucion { get; set; }
}

// ============================================================================
// 3. ÓRDENES DE IMAGENOLOGÍA RX / ECOGRAFÍA (vt_rxOrdenes - Reemplaza vt_despachorx.aspx)
// ============================================================================
public class OrdenImagenItemDto
{
    public int Sec { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string CodigoProd { get; set; } = string.Empty;
    public string DescripcionEstudio { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public string Estado { get; set; } = "Por Facturar"; // Por Facturar, POR REALIZAR, REALIZADO
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Iva { get; set; } = string.Empty;
    public string IdEfact { get; set; } = string.Empty;
    public string Medida { get; set; } = "UND";
    public decimal Precio { get; set; }
    public int? IdEvolucion { get; set; }
}

// ============================================================================
// 4. BANDEJA CENTRAL DE TRABAJO Y RESULTADOS (Reemplaza vt_labmedicospre.aspx)
// ============================================================================
public class BandejaTrabajoDiagnosticoDto
{
    public int Secuencial { get; set; }
    public string CodigoProd { get; set; } = string.Empty;
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string Examen { get; set; } = string.Empty;
    public string Prioridad { get; set; } = "Rutina"; // Emergente o Rutina
    public string Medico { get; set; } = string.Empty;
    public string EmailMedico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Paciente { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Estado { get; set; } = "POR REALIZAR";
    public string Observaciones { get; set; } = string.Empty;
    public string FechaTexto { get; set; } = string.Empty;
    public string AreaOrigen { get; set; } = "LAB"; // LAB o RX
}

// ============================================================================
// 5. INFORMES Y RESULTADOS DIAGNÓSTICOS (vt_labInformes, vt_rxInformes, vt_ecoInformes, vt_endoInformes, vt_medInformes)
// ============================================================================
public class InformeDiagnosticoDto
{
    public int IdInforme { get; set; }
    public string TipoInforme { get; set; } = "Laboratorio"; // Laboratorio, Imagen, Ecografia, Endoscopia, Medicos
    public int Nhcl { get; set; }
    public int IdOrden { get; set; }
    public int SecOrden { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public int EdadAnios { get; set; }
    public int EdadMeses { get; set; }
    public int IdFormato { get; set; }
    public string NombreFormato { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty; // InformesUpload/Laboratorio/...
    public string ContenidoHtml { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Now;
    public DateTime FechaMod { get; set; } = DateTime.Now;
    public int IdUser { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public int Localidad { get; set; } = 1;
    public string Estado { get; set; } = "A";
}
