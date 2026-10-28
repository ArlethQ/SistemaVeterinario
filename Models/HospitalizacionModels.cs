namespace HosvetKardex.Web.Models;

// ============================================================================
// 1. MODELOS DE SIGNOS VITALES Y BALANCE HÍDRICO (vt_EnfermeriaSignos.aspx)
// ============================================================================

public class EnfFechaDiaDto
{
    public int Id { get; set; }
    public int Orden { get; set; } // 1..9 (Días)
    public DateTime? Fecha { get; set; }
    public int? DiasInternacion { get; set; }
    public int? DiasPostQuirurgico { get; set; }
}

public class SignoVitalSlotDto
{
    public int Id { get; set; }
    public int Orden { get; set; } // 1..18 (9 días x AM/PM)
    public int? Pulso { get; set; }
    public decimal? Temperatura { get; set; }
    public int? FrecuenciaRespiratoria { get; set; }
    public int? PresionSistolica { get; set; }
    public int? PresionDiastolica { get; set; }
    public string Turno => Orden % 2 == 1 ? "AM" : "PM";
    public int DiaIndice => ((Orden - 1) / 2) + 1;
}

public class EnfBalanceHidricoDiaDto
{
    public int Id { get; set; }
    public int Orden { get; set; } // 1..9 (Días)
    public decimal? Parenteral { get; set; }
    public decimal? ViaOral { get; set; }
    public decimal? Orina { get; set; }
    public decimal? Drenaje { get; set; }
    public decimal? Otros { get; set; }

    public decimal TotalIngresos => (Parenteral ?? 0m) + (ViaOral ?? 0m);
    public decimal TotalEliminaciones => (Orina ?? 0m) + (Drenaje ?? 0m) + (Otros ?? 0m);
    public decimal BalanceNeto => TotalIngresos - TotalEliminaciones;
}

public class EnfMedicionDiaDto
{
    public int Id { get; set; }
    public int Orden { get; set; } // 1..18 (1..9 para campos diarios, 1..18 para Aseo AM/PM)
    public decimal? Peso { get; set; }
    public string? Talla { get; set; }
    public string? PerimetroAbdominal { get; set; }
    public string? DietaIndicada { get; set; }
    public string? NumeroComidas { get; set; }
    public string? Aseo { get; set; }
    public string? Banio { get; set; }
    public string? Movilizacion { get; set; }
    public string? Vendajes { get; set; }
    public string? CambioSonda { get; set; }
    public string? Responsable { get; set; }
}

public class FormularioSignosEnfermeriaDto
{
    public int IdOrdenAtencion { get; set; }
    public int Nhcl { get; set; }
    public int NumFormulario { get; set; } = 1;
    public List<int> FormulariosDisponibles { get; set; } = [1];
    public List<EnfFechaDiaDto> Fechas { get; set; } = [];
    public List<SignoVitalSlotDto> SignosVitales { get; set; } = [];
    public List<EnfBalanceHidricoDiaDto> BalanceHidrico { get; set; } = [];
    public List<EnfMedicionDiaDto> Mediciones { get; set; } = [];
}

// ============================================================================
// 2. CLASE BASE MATRIZ 24 HORAS (txt7..txt24, txt1..txt6)
// ============================================================================

public abstract class Matriz24HorasBaseDto
{
    public int Id { get; set; }
    public int Fila { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public int Hoja { get; set; } = 1;
    public DateTime? Fecha { get; set; }
    public int? Responsable { get; set; }

    public string? Txt7 { get; set; }
    public string? Txt8 { get; set; }
    public string? Txt9 { get; set; }
    public string? Txt10 { get; set; }
    public string? Txt11 { get; set; }
    public string? Txt12 { get; set; }
    public string? Txt13 { get; set; }
    public string? Txt14 { get; set; }
    public string? Txt15 { get; set; }
    public string? Txt16 { get; set; }
    public string? Txt17 { get; set; }
    public string? Txt18 { get; set; }
    public string? Txt19 { get; set; }
    public string? Txt20 { get; set; }
    public string? Txt21 { get; set; }
    public string? Txt22 { get; set; }
    public string? Txt23 { get; set; }
    public string? Txt24 { get; set; }
    public string? Txt1 { get; set; }
    public string? Txt2 { get; set; }
    public string? Txt3 { get; set; }
    public string? Txt4 { get; set; }
    public string? Txt5 { get; set; }
    public string? Txt6 { get; set; }

    // Orden cronológico usado en Hospitalización: 07:00..24:00 (cols 1..18) y 01:00..06:00 (cols 19..24)
    public static readonly int[] HorasOrden = [7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 1, 2, 3, 4, 5, 6];

    public string? GetValorHora(int horaDb) => horaDb switch
    {
        7 => Txt7, 8 => Txt8, 9 => Txt9, 10 => Txt10, 11 => Txt11, 12 => Txt12,
        13 => Txt13, 14 => Txt14, 15 => Txt15, 16 => Txt16, 17 => Txt17, 18 => Txt18,
        19 => Txt19, 20 => Txt20, 21 => Txt21, 22 => Txt22, 23 => Txt23, 24 => Txt24,
        1 => Txt1, 2 => Txt2, 3 => Txt3, 4 => Txt4, 5 => Txt5, 6 => Txt6,
        _ => null
    };

    public void SetValorHora(int horaDb, string? valor)
    {
        var limpio = string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToUpperInvariant();
        switch (horaDb)
        {
            case 7: Txt7 = limpio; break;
            case 8: Txt8 = limpio; break;
            case 9: Txt9 = limpio; break;
            case 10: Txt10 = limpio; break;
            case 11: Txt11 = limpio; break;
            case 12: Txt12 = limpio; break;
            case 13: Txt13 = limpio; break;
            case 14: Txt14 = limpio; break;
            case 15: Txt15 = limpio; break;
            case 16: Txt16 = limpio; break;
            case 17: Txt17 = limpio; break;
            case 18: Txt18 = limpio; break;
            case 19: Txt19 = limpio; break;
            case 20: Txt20 = limpio; break;
            case 21: Txt21 = limpio; break;
            case 22: Txt22 = limpio; break;
            case 23: Txt23 = limpio; break;
            case 24: Txt24 = limpio; break;
            case 1: Txt1 = limpio; break;
            case 2: Txt2 = limpio; break;
            case 3: Txt3 = limpio; break;
            case 4: Txt4 = limpio; break;
            case 5: Txt5 = limpio; break;
            case 6: Txt6 = limpio; break;
        }
    }
}

// ============================================================================
// 3. FICHA DE HOSPITALIZACIÓN (Hosp_FichaHospitalizacion.aspx)
// ============================================================================

public class CondicionHospitalDto
{
    public int Id { get; set; }
    public string Condicion { get; set; } = string.Empty;
}

public class RitmoHorarioDto
{
    public int Id { get; set; }
    public string Des_Ritmo { get; set; } = string.Empty;
}

public class MotivoMuerteDto
{
    public int Id { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public class FichaBloque1MedicacionDto : Matriz24HorasBaseDto
{
    public string? Medicacion { get; set; }
    public decimal? MgKg { get; set; }
    public decimal? Ml { get; set; }
    public int? Via { get; set; }
    public int? Ritmo { get; set; }
    public int? Condicion { get; set; }
    public decimal? Peso { get; set; }
    public int? Dia { get; set; }
}

public class FichaBloque2AlimentacionDto : Matriz24HorasBaseDto
{
    public string? Alimentacion { get; set; }
    public decimal? Cald { get; set; }
    public decimal? Calv { get; set; }
    public string? Gr { get; set; }
    public int? Ritmo { get; set; }
    public int? Condicion { get; set; }
    public decimal? Peso { get; set; }
    public int? Dia { get; set; }
}

public class FichaBloque3FluidoterapiaDto : Matriz24HorasBaseDto
{
    public string? Fluidoterapia { get; set; }
    public string? Mpd { get; set; }
    public string? MlDia { get; set; }
    public string? MlH { get; set; }
    public string? Gotas { get; set; }
    public int? Condicion { get; set; }
    public decimal? Peso { get; set; }
    public int? Dia { get; set; }
}

public class NotaImportanteDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string Hora { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public string Tipo { get; set; } = "I"; // 'I' = Importante, 'O' = Observación, 'N' = Nota Chequeo Quirúrgico
}

public class FichaHospitalizacionSheetDto
{
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public int Hoja { get; set; } = 1;
    public DateTime Fecha { get; set; } = DateTime.Today;
    public int? CondicionId { get; set; }
    public decimal? Peso { get; set; }
    public int DiasHospitalizacion { get; set; } = 1;
    public string MedicoResponsable { get; set; } = string.Empty;
    public bool Cerrada { get; set; }
    public List<int> HojasDisponibles { get; set; } = [1];

    public List<FichaBloque1MedicacionDto> Bloque1Medicacion { get; set; } = [];
    public List<FichaBloque2AlimentacionDto> Bloque2Alimentacion { get; set; } = [];
    public List<FichaBloque3FluidoterapiaDto> Bloque3Fluidoterapia { get; set; } = [];
    public List<NotaImportanteDto> NotasImportantes { get; set; } = [];
    public List<NotaImportanteDto> ObservacionesGenerales { get; set; } = [];
}

// ============================================================================
// 4. MONITOREO HOSPITALARIO 24 HORAS (Hosp_Monitoreo.aspx)
// ============================================================================

public class MonitoreoFilaDto : Matriz24HorasBaseDto
{
    public string? Item { get; set; }
    public string ParametroLabel { get; set; } = string.Empty;
    public string UnidadLabel { get; set; } = string.Empty;
    public bool EsParametroPersonalizado => Fila >= 22;
}

public class ObservacionHoraDto
{
    public int Id { get; set; }
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public string Hora { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
    public string? Responsable { get; set; }
}

public class MonitoreoMedInputModel
{
    public int OrdenId { get; set; }
    public int Nhcl { get; set; }
    public string CodigoProducto { get; set; } = string.Empty;
    public string NombreProducto { get; set; } = string.Empty;
    public string Indicaciones { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
    public string Via { get; set; } = "IV";
    public DateTime FechaAdministracion { get; set; } = DateTime.Today;
    public TimeSpan HoraAdministracion { get; set; } = DateTime.Now.TimeOfDay;
    public string Iniciales { get; set; } = "MED";
    public string Funcion { get; set; } = "M";
    public string Funcionario { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public decimal StockDisponible { get; set; }
    public string Iva { get; set; } = string.Empty;
    public string IdBC { get; set; } = string.Empty;
    public string Medida { get; set; } = string.Empty;
    public string Bodega { get; set; } = string.Empty;
}

public class MonitoreoMedicamentoDto
{
    public int Id { get; set; }
    public int OrdenId { get; set; }
    public int Nhcl { get; set; }
    public string Producto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string Indicaciones { get; set; } = string.Empty;
    public string Via { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string Hora { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string? Iniciales { get; set; }
    public string? Funcion { get; set; }
}

public class MonitoreoHospitalarioSheetDto
{
    public int IdOrden { get; set; }
    public int Nhcl { get; set; }
    public int Hoja { get; set; } = 1;
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string MedicoResponsable { get; set; } = string.Empty;
    public bool Cerrada { get; set; }
    public List<int> HojasDisponibles { get; set; } = [1];

    public List<MonitoreoFilaDto> FilasMonitoreo { get; set; } = [];
    public List<MonitoreoMedicamentoDto> MedicamentosRegistrados { get; set; } = [];
    public List<NotaImportanteDto> NotasChequeoQuirurgico { get; set; } = [];
    public List<ObservacionHoraDto> ObservacionesPorHora { get; set; } = [];

    public static readonly (int Fila, string Parametro, string Unidad)[] CatalogoParametros =
    [
        (1, "Fr. Cardíaca", "lpm"),
        (2, "Pulso", "Fu,Mo,De"),
        (3, "Fr. Respiratoria", "rpm"),
        (4, "Patrón Respiratorio", "Eu,Ta,Dis"),
        (5, "SaO2", "%"),
        (6, "TLLC", "Seg"),
        (7, "Pliegue Cutáneo", "Seg"),
        (8, "Deshidratación", "%"),
        (9, "Temperatura", "°C"),
        (10, "PAS", "mmHg"),
        (11, "Glasgow (Dolor)", "Escala"),
        (12, "Glasgow (Conciencia)", "Escala"),
        (13, "Estado de Conciencia", "Al,Dep,Com"),
        (14, "Micción", "ml"),
        (15, "Producción de Orina", "ml/kg/h"),
        (16, "Heces", "Grado"),
        (17, "Emesis", "Caract."),
        (18, "Hematocrito", "%"),
        (19, "Proteínas Totales", "g/dL"),
        (20, "Glicemia", "mg/dL"),
        (21, "Lactato", "mmol/L"),
        (22, "Peso / Otro 1", "kg"),
        (23, "Parámetro Extra 2", ""),
        (24, "Parámetro Extra 3", "")
    ];

    public static readonly IReadOnlyDictionary<int, MonitoreoParametroReglaDto> ReglasParametros =
        new Dictionary<int, MonitoreoParametroReglaDto>
        {
            [1] = new(true, 0, 800, 1, "0–800 lpm"),
            [2] = new(false, null, null, null, "Fu, Mo o De", ["FU", "MO", "DE"]),
            [3] = new(true, 0, 300, 1, "0–300 rpm"),
            [4] = new(false, null, null, null, "Eu, Ta o Dis", ["EU", "TA", "DIS"]),
            [5] = new(true, 0, 100, 1, "0–100 %"),
            [6] = new(true, 0, 30, 0.1m, "0–30 segundos"),
            [7] = new(true, 0, 60, 0.1m, "0–60 segundos"),
            [8] = new(true, 0, 100, 0.1m, "0–100 %"),
            [9] = new(true, 20, 45, 0.1m, "20–45 °C"),
            [10] = new(true, 0, 350, 1, "0–350 mmHg"),
            [11] = new(true, 0, 24, 1, "Glasgow dolor: 0–24"),
            [12] = new(true, 3, 18, 1, "Glasgow conciencia: 3–18"),
            [13] = new(false, null, null, null, "Al, Dep o Com", ["AL", "DEP", "COM"]),
            [14] = new(true, 0, 10000, 0.1m, "0–10000 ml"),
            [15] = new(true, 0, 100, 0.1m, "0–100 ml/kg/h"),
            [16] = new(true, 0, 7, 1, "Grado 0–7"),
            [17] = new(false, null, null, null, "Característica clínica"),
            [18] = new(true, 0, 100, 0.1m, "0–100 %"),
            [19] = new(true, 0, 20, 0.1m, "0–20 g/dL"),
            [20] = new(true, 0, 1500, 1, "0–1500 mg/dL"),
            [21] = new(true, 0, 30, 0.1m, "0–30 mmol/L"),
            [22] = new(true, 0, 2000, 0.1m, "0–2000 kg")
        };

    public static MonitoreoParametroReglaDto ObtenerRegla(int fila) =>
        ReglasParametros.TryGetValue(fila, out var regla)
            ? regla
            : new(false, null, null, null, "Texto libre");
}

public sealed record MonitoreoParametroReglaDto(
    bool EsNumerico,
    decimal? Minimo,
    decimal? Maximo,
    decimal? Paso,
    string Ayuda,
    string[]? ValoresPermitidos = null);
