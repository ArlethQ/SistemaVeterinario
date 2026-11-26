namespace HosvetKardex.Web.Models;

public sealed record SeccionFichaHospitalizacion(string Titulo, string[] Campos, int Filas = 1, bool Horaria = false, bool TextoLargo = false)
{
    public string[] Encabezados => Horaria
        ? [.. Campos, .. Matriz24HorasBaseDto.HorasOrden.Select(hora => hora.ToString())]
        : Campos;

    public string Clave(int fila, int columna)
    {
        // Preserve existing hourly entries when adding flow columns.
        if (Titulo == "Plan de fluidoterapia" && columna >= 3)
            columna = columna < Campos.Length ? columna + 24 : columna - (Campos.Length - 3);

        return $"{Titulo}/{fila}/{columna}";
    }
}

public sealed class DetalleFichaHospitalizacion
{
    public Dictionary<string, string> Valores { get; set; } = [];

    public int FilasDisponibles(SeccionFichaHospitalizacion seccion) =>
        seccion == PlanFluidoterapia ? FilasPlanFluidoterapia : seccion.Filas;

    public int FilasPlanFluidoterapia { get; set; } = 7;
    public bool FluidoterapiaIntegrada { get; set; }

    public void IntegrarFluidoterapia(IEnumerable<FichaBloque3FluidoterapiaDto> anteriores)
    {
        if (FluidoterapiaIntegrada) return;

        var plan = PlanFluidoterapia;
        var encabezados = plan.Encabezados;
        foreach (var anterior in anteriores)
        {
            string?[] valores = [anterior.Fluidoterapia, null, null, anterior.MlDia, anterior.MlH,
                anterior.Gotas, anterior.Mpd, .. Matriz24HorasBaseDto.HorasOrden.Select(anterior.GetValorHora)];
            if (valores.All(string.IsNullOrWhiteSpace)) continue;

            int fila = 0;
            while (fila < FilasPlanFluidoterapia &&
                   Enumerable.Range(0, encabezados.Length).Any(columna => Obtener(plan.Clave(fila, columna)).Length > 0))
                fila++;

            FilasPlanFluidoterapia = Math.Max(FilasPlanFluidoterapia, fila + 1);
            for (int columna = 0; columna < valores.Length; columna++)
                Registrar(plan.Clave(fila, columna), valores[columna]);
        }
        FluidoterapiaIntegrada = true;
    }

    public string Obtener(string clave) => Valores.GetValueOrDefault(clave, "");

    public void Registrar(string clave, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            Valores.Remove(clave);
        else
            Valores[clave] = valor.Trim();
    }

    public static readonly SeccionFichaHospitalizacion PlanFluidoterapia =
        new("Plan de fluidoterapia", ["Fluido", "Mantenimiento / bolos de rescate", "Reposición por deshidratación (%)", "ml/día", "ml/h", "gotas/min", "M+P+D"], 7, Horaria: true);

    public static readonly SeccionFichaHospitalizacion[] Secciones =
    [
        new("Datos complementarios del paciente",
            ["Temperamento", "ALF", "Médico general", "Médico secundario", "Médico nocturno"]),
        new("Diagnóstico y prediagnósticos", ["Diagnóstico / prediagnósticos"], TextoLargo: true),
        new("Infusiones", ["Infusión", "Dosis", "Dosis (ml/h)", "Bureta (ml)", "Velocidad de fluidoterapia"], 4, Horaria: true),
        PlanFluidoterapia,
        new("Plan nutricional", ["Alimento", "Kcal/d", "Kcal/1000 g", "g/día", "Preparación", "% RER",
            "ml/día", "N.º de raciones", "ml o g por ración", "Tipo de alimentación", "Voluntaria",
            "Asistida por tubo", "Mixta", "Con tutor"], 4),
        new("Administración de agua", ["Agua", "ml", "Frecuencia"], 3, Horaria: true),
        new("Colocación de sonda nasoesofágica / nasofaríngea", ["Sonda", "Hora", "Médico", "Calibre y ubicación"], 3, Horaria: true),
        new("Colocación de vía", ["Vía", "Fecha y hora", "Médico", "Ubicación"], 3),
        new("Paseos", ["Paseo"], 3, Horaria: true),
        new("Monitoreo clínico", ["Parámetro"], Horaria: true),
        new("Examen clínico", ["Auscultación cardíaca", "Auscultación respiratoria", "Palpación abdominal", "Linfonodos"], TextoLargo: true),
        new("Control ecográfico torácico", ["Zona"], Horaria: true),
        new("Control ecográfico y observaciones", ["Abdominal", "Cardíaco", "Observaciones adicionales del examen clínico"], TextoLargo: true)
    ];

    public static string[] EtiquetasDeFila(SeccionFichaHospitalizacion seccion) => seccion.Titulo switch
    {
        "Monitoreo clínico" =>
        [
            "FC (lpm)", "FR (rpm)", "Calidad del pulso (F/M/D)", "Concordancia del pulso (C/NC)",
            "TLLC (s)", "Color de mucosas (R/C/P)", "Pliegue cutáneo (s)", "Deshidratación (%)",
            "SpO₂ con oxígeno", "SpO₂ sin oxígeno", "Conciencia (AVDN)", "Temperatura (°C)",
            "Escala del dolor", "Palpación abdominal", "PAS/PAD/PAM", "Miembro (presión arterial)",
            "Manguito (n.º)", "Hematocrito", "Glucosa (mg/dl)", "Lactato", "Sonda urinaria",
            "Débito urinario", "Jackson Pratt", "Producción Jackson Pratt", "Micción",
            "Defecación (grado)", "Vómitos", "Diarrea", "Consumo de agua", "Apetito"
        ],
        "Control ecográfico torácico" =>
        [
            "Izquierdo · Craneal", "Izquierdo · Medial", "Izquierdo · Perihiliar", "Izquierdo · Caudal",
            "Derecho · Craneal", "Derecho · Medial", "Derecho · Perihiliar", "Derecho · Caudal"
        ],
        _ => []
    };

    public IEnumerable<string[]> ObtenerFilas(SeccionFichaHospitalizacion seccion)
    {
        var encabezados = seccion.Encabezados;
        var etiquetas = EtiquetasDeFila(seccion);
        for (int fila = 0; fila < Math.Max(FilasDisponibles(seccion), etiquetas.Length); fila++)
        {
            var valores = encabezados.Select((_, columna) => Obtener(seccion.Clave(fila, columna))).ToArray();
            if (!valores.Any(valor => valor.Length > 0))
                continue;
            if (etiquetas.Length > 0)
                valores[0] = etiquetas[fila];
            yield return valores;
        }
    }
}
