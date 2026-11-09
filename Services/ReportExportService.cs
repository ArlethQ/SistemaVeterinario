using ClosedXML.Excel;
using HosvetKardex.Web.Models;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HosvetKardex.Web.Services;

public interface IReportExportService
{
    byte[] GenerarKardexPdf(PacienteHeaderDto paciente, List<KardexSabanaItemDto> sabana, List<ProductoUtilizadoDto> detalleRegistros);
    byte[] GenerarKardexExcel(PacienteHeaderDto paciente, List<KardexSabanaItemDto> sabana, List<ProductoUtilizadoDto> detalleRegistros);
    byte[] GenerarSignosVitalesPdf(PacienteHeaderDto paciente, FormularioSignosEnfermeriaDto form);
    byte[] GenerarSignosVitalesExcel(PacienteHeaderDto paciente, FormularioSignosEnfermeriaDto form);
    byte[] GenerarFichaHospitalizacionPdf(PacienteHeaderDto paciente, FichaHospitalizacionSheetDto sheet, List<ViaAdministracionDto> vias, List<RitmoHorarioDto> ritmos);
    byte[] GenerarFichaHospitalizacionExcel(PacienteHeaderDto paciente, FichaHospitalizacionSheetDto sheet, List<ViaAdministracionDto> vias, List<RitmoHorarioDto> ritmos);
    byte[] GenerarMonitoreoHospitalarioPdf(PacienteHeaderDto paciente, MonitoreoHospitalarioSheetDto sheet);
    byte[] GenerarMonitoreoHospitalarioExcel(PacienteHeaderDto paciente, MonitoreoHospitalarioSheetDto sheet);
    byte[] GenerarPreAnestesiaPdf(PacienteHeaderDto paciente, PreAnestesiaDto pre, List<ProductoQuirofanoDto> premedicacion);
    byte[] GenerarHojaAnestesiaPdf(PacienteHeaderDto paciente, HojaAnestesiaCompletaDto hoja);
    byte[] GenerarHojaAnestesiaExcel(PacienteHeaderDto paciente, HojaAnestesiaCompletaDto hoja);
    byte[] GenerarProtocoloQuirurgicoPdf(PacienteHeaderDto paciente, ProtocoloQuirurgicoCompletoDto prot);
    byte[] GenerarRecuperacionPdf(PacienteHeaderDto paciente, List<RecuperacionSignoVitalDto> signos, PostOperatorioChecklistDto post, List<LiquidoRecuperacionDto> ing, List<LiquidoRecuperacionDto> egr, List<ObservacionRecuperacionDto> obs);
    byte[] GenerarFichaMedicaPdf(PacienteHeaderDto paciente, FichaMedicaCompletaDto ficha, List<DiagnosticoCie10Dto> diagnosticos, List<RecetaMedicamentoItemDto> receta);
    byte[] GenerarRecetaMedicaPdf(PacienteHeaderDto paciente, List<RecetaMedicamentoItemDto> items, List<DiagnosticoCie10Dto> diagnosticos, string indicacionesGenerales, string? identificador = null, DateTime? fechaEmision = null);
    byte[] GenerarEvolucionesPdf(PacienteHeaderDto paciente, List<EvolucionMedicaDto> evoluciones);
    byte[] GenerarAltaMedicaPdf(PacienteHeaderDto paciente, FormularioAltaMedicaDto alta);
    byte[] GenerarOrdenLaboratorioPdf(PacienteHeaderDto paciente, List<OrdenLaboratorioItemDto> ordenes, List<DiagnosticoCie10Dto> diagnosticos);
    byte[] GenerarOrdenImagenPdf(PacienteHeaderDto paciente, List<OrdenImagenItemDto> ordenes, List<DiagnosticoCie10Dto> diagnosticos);
    byte[] GenerarInformeDiagnosticoPdf(PacienteHeaderDto paciente, InformeDiagnosticoDto informe);
    byte[] GenerarBandejaLaboratorioExcel(List<BandejaTrabajoDiagnosticoDto> bandeja, DateTime desde, DateTime hasta);
    byte[] GenerarProformaConsolidadaPdf(OrdenProformaResumenDto proforma);
    byte[] GenerarProformaConsolidadaExcel(OrdenProformaResumenDto proforma);
    byte[] GenerarAgendaCitasPdf(List<CitaMedicaDto> citas, DateTime desde, DateTime hasta);
    byte[] GenerarPartesOperatoriosPdf(List<ParteOperatorioResumenDto> partes, DateTime desde, DateTime hasta);
    byte[] GenerarOrdenesAtencionPdf(List<OrdenAtencionResumenDto> ordenes, DateTime desde, DateTime hasta);
    byte[] GenerarOrdenesAtencionExcel(List<OrdenAtencionResumenDto> ordenes, DateTime desde, DateTime hasta);
    byte[] GenerarReporteAtencionesPdf(List<ReporteAtencionDto> atenciones, DateTime desde, DateTime hasta, bool condensado);
    byte[] GenerarReporteAtencionesExcel(List<ReporteAtencionDto> atenciones, DateTime desde, DateTime hasta, bool condensado);
    byte[] GenerarReporteDiagnosticosPdf(List<ReporteDiagnosticoDto> diagnosticos, DateTime desde, DateTime hasta, bool condensado);
    byte[] GenerarReporteDiagnosticosExcel(List<ReporteDiagnosticoDto> diagnosticos, DateTime desde, DateTime hasta, bool condensado);
    byte[] GenerarRecordatoriosSeguimientoPdf(List<RecordatorioVacunaDto> recordatorios, DateTime desde, DateTime hasta, bool porRevision);
    byte[] GenerarRecordatoriosSeguimientoExcel(List<RecordatorioVacunaDto> recordatorios, DateTime desde, DateTime hasta, bool porRevision);
    byte[] GenerarAlertasSeguimientoPdf(List<AlertaClinicaDto> alertas, DateTime desde, DateTime hasta, string estado);
    byte[] GenerarAlertasSeguimientoExcel(List<AlertaClinicaDto> alertas, DateTime desde, DateTime hasta, string estado);
    byte[] GenerarFallecidosPdf(List<PacienteFallecidoDto> fallecidos, DateTime desde, DateTime hasta);
    byte[] GenerarFallecidosExcel(List<PacienteFallecidoDto> fallecidos, DateTime desde, DateTime hasta);
    byte[] GenerarIStatPdf(PacienteHeaderDto paciente, IStatDto istat, string? nombreMedico = null);
    byte[] GenerarFichasMedicasLotePdf(PacienteHeaderDto paciente, List<FichaLoteItemDto> lote);
    byte[] GenerarCodigoQrPng(string texto);
}

public class ReportExportService : IReportExportService
{
    public ReportExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public byte[] GenerarCodigoQrPng(string texto)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(texto, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(8);
    }

    public byte[] GenerarOrdenesAtencionPdf(List<OrdenAtencionResumenDto> ordenes, DateTime desde, DateTime hasta)
    {
        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape()); page.Margin(24); page.DefaultTextStyle(x => x.FontSize(8));
            page.Header().Text($"Órdenes de atención · {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}").Bold().FontSize(15);
            page.Content().PaddingTop(10).Table(t =>
            {
                t.ColumnsDefinition(c => { c.ConstantColumn(55); c.ConstantColumn(60); c.RelativeColumn(1.4f); c.RelativeColumn(1.4f); c.RelativeColumn(1.3f); c.RelativeColumn(1.3f); c.RelativeColumn(1); });
                t.Header(h => { foreach (var x in new[] { "Orden", "Fecha", "Paciente", "Propietario", "Médico", "Especialidad", "Etapa" }) h.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(x).Bold(); });
                foreach (var o in ordenes) foreach (var x in new[] { o.IdOrden.ToString(), o.Fecha.ToString("dd/MM/yyyy"), $"{o.Mascota} ({o.Nhcl})", o.Propietario, o.Medico, o.Especialidad, o.Etapa }) t.Cell().BorderBottom(.5f).Padding(3).Text(x ?? "");
            });
            page.Footer().AlignCenter().Text(x => { x.Span("Generado "); x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")); });
        })).GeneratePdf();
    }

    public byte[] GenerarOrdenesAtencionExcel(List<OrdenAtencionResumenDto> ordenes, DateTime desde, DateTime hasta)
    {
        using var wb = new XLWorkbook(); var ws = wb.Worksheets.Add("Órdenes");
        string[] headers = ["Orden", "Fecha", "Hora", "NHCL", "Paciente", "Propietario", "Médico", "Especialidad", "Etapa", "Estado", "Despacho"];
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true; ws.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.LightBlue;
        int row = 2; foreach (var o in ordenes) { object[] values = [o.IdOrden, o.Fecha, o.Hora, o.Nhcl, o.Mascota, o.Propietario, o.Medico, o.Especialidad, o.Etapa, o.Estado, o.DespachoDescripcion]; for (int i = 0; i < values.Length; i++) ws.Cell(row, i + 1).Value = XLCellValue.FromObject(values[i]); row++; }
        ws.Column(2).Style.DateFormat.Format = "dd/mm/yyyy"; ws.Columns().AdjustToContents(); ws.SheetView.FreezeRows(1); ws.RangeUsed()?.SetAutoFilter();
        using var stream = new MemoryStream(); wb.SaveAs(stream); return stream.ToArray();
    }

    public byte[] GenerarReporteAtencionesPdf(
        List<ReporteAtencionDto> atenciones, DateTime desde, DateTime hasta, bool condensado)
    {
        var resumen = atenciones
            .GroupBy(x => new { x.IdMedico, x.Medico, x.IdEspecialidad, x.Especialidad, x.IdLocalidad, x.Localidad })
            .Select(g => new
            {
                g.Key.Medico,
                g.Key.Especialidad,
                g.Key.Localidad,
                Total = g.Select(x => x.Orden).Distinct().Count()
            })
            .OrderBy(x => x.Medico).ThenBy(x => x.Especialidad).ThenBy(x => x.Localidad)
            .ToList();

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(22);
            page.DefaultTextStyle(x => x.FontSize(condensado ? 9 : 7.5f));
            page.Header().Column(col =>
            {
                col.Item().Text(condensado ? "REPORTE CONDENSADO DE ATENCIONES" : "LISTA DETALLADA DE ATENCIONES POR MÉDICO")
                    .Bold().FontSize(14).FontColor(Colors.Blue.Darken3);
                col.Item().Text($"Período: {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy} · {atenciones.Select(x => x.Orden).Distinct().Count()} atención(es)")
                    .FontColor(Colors.Grey.Darken2);
            });
            page.Content().PaddingTop(10).Table(table =>
            {
                if (condensado)
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1.6f); c.RelativeColumn(1.6f); c.ConstantColumn(70); });
                    table.Header(h => { foreach (var texto in new[] { "Médico", "Especialidad", "Localidad", "Atenciones" }) h.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text(texto).Bold(); });
                    foreach (var item in resumen)
                    {
                        foreach (var valor in new[] { item.Medico, item.Especialidad, item.Localidad, item.Total.ToString() })
                            table.Cell().BorderBottom(.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(valor);
                    }
                }
                else
                {
                    table.ColumnsDefinition(c => { c.ConstantColumn(55); c.ConstantColumn(70); c.RelativeColumn(1.3f); c.RelativeColumn(1.5f); c.RelativeColumn(1.4f); c.RelativeColumn(1.5f); c.RelativeColumn(1.1f); });
                    table.Header(h => { foreach (var texto in new[] { "Fecha", "Orden/NHCL", "Paciente", "Propietario", "Médico", "Diagnóstico", "Sede / Especialidad" }) h.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text(texto).Bold(); });
                    foreach (var item in atenciones)
                    {
                        var diagnostico = item.CodigoDiagnostico == "0" ? item.OtrosDiagnostico : $"{item.CodigoDiagnostico} · {item.Diagnostico}";
                        foreach (var valor in new[]
                        {
                            item.Fecha.ToString("dd/MM/yyyy"), $"{item.Orden} / {item.Nhcl}", item.Paciente,
                            item.Cliente, item.Medico, diagnostico, $"{item.Localidad}\n{item.Especialidad}"
                        }) table.Cell().BorderBottom(.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(valor ?? string.Empty);
                    }
                }
            });
            page.Footer().AlignCenter().Text($"Generado por e-ForVets · {DateTime.Now:dd/MM/yyyy HH:mm}");
        })).GeneratePdf();
    }

    public byte[] GenerarReporteAtencionesExcel(
        List<ReporteAtencionDto> atenciones, DateTime desde, DateTime hasta, bool condensado)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(condensado ? "Atenciones condensadas" : "Atenciones");
        if (condensado)
        {
            string[] headers = ["Médico", "Especialidad", "Localidad", "Total atenciones"];
            for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
            var grupos = atenciones
                .GroupBy(x => new { x.IdMedico, x.Medico, x.IdEspecialidad, x.Especialidad, x.IdLocalidad, x.Localidad })
                .Select(g => new { g.Key.Medico, g.Key.Especialidad, g.Key.Localidad, Total = g.Select(x => x.Orden).Distinct().Count() })
                .OrderBy(x => x.Medico).ThenBy(x => x.Especialidad).ThenBy(x => x.Localidad);
            var fila = 2;
            foreach (var g in grupos)
            {
                ws.Cell(fila, 1).Value = g.Medico; ws.Cell(fila, 2).Value = g.Especialidad;
                ws.Cell(fila, 3).Value = g.Localidad; ws.Cell(fila, 4).Value = g.Total; fila++;
            }
        }
        else
        {
            string[] headers = ["Fecha", "Orden", "NHCL", "Paciente", "Propietario", "Dirección", "Médico", "Especialidad", "Código diagnóstico", "Diagnóstico", "Localidad", "Registrado por"];
            for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
            var fila = 2;
            foreach (var x in atenciones)
            {
                object[] valores = [x.Fecha, x.Orden, x.Nhcl, x.Paciente, x.Cliente, x.Direccion, x.Medico, x.Especialidad, x.CodigoDiagnostico, x.CodigoDiagnostico == "0" ? x.OtrosDiagnostico : x.Diagnostico, x.Localidad, x.RegistradoPor];
                for (var i = 0; i < valores.Length; i++) ws.Cell(fila, i + 1).Value = XLCellValue.FromObject(valores[i]);
                fila++;
            }
            ws.Column(1).Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
        }
        var ultimaColumna = condensado ? 4 : 12;
        ws.Range(1, 1, 1, ultimaColumna).Style.Font.Bold = true;
        ws.Range(1, 1, 1, ultimaColumna).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.SheetView.FreezeRows(1); ws.RangeUsed()?.SetAutoFilter(); ws.Columns().AdjustToContents();
        ws.Cell(ws.LastRowUsed()?.RowNumber() + 2 ?? 3, 1).Value = $"Período: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}";
        using var stream = new MemoryStream(); wb.SaveAs(stream); return stream.ToArray();
    }

    public byte[] GenerarReporteDiagnosticosPdf(
        List<ReporteDiagnosticoDto> diagnosticos, DateTime desde, DateTime hasta, bool condensado)
    {
        var resumen = diagnosticos
            .GroupBy(x => new { x.CodigoDiagnostico, x.Diagnostico })
            .Select(g => new { g.Key.CodigoDiagnostico, g.Key.Diagnostico, Total = g.Count() })
            .OrderBy(x => x.Diagnostico)
            .ToList();

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(22);
            page.DefaultTextStyle(x => x.FontSize(condensado ? 9 : 7.5f));
            page.Header().Column(col =>
            {
                col.Item().Text(condensado ? "REPORTE CONDENSADO DE DIAGNÓSTICOS" : "REPORTE DETALLADO DE DIAGNÓSTICOS")
                    .Bold().FontSize(14).FontColor(Colors.Blue.Darken3);
                col.Item().Text($"Período: {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy} · {diagnosticos.Count} diagnóstico(s)")
                    .FontColor(Colors.Grey.Darken2);
            });
            page.Content().PaddingTop(10).Table(table =>
            {
                if (condensado)
                {
                    table.ColumnsDefinition(c => { c.ConstantColumn(95); c.RelativeColumn(4); c.ConstantColumn(80); });
                    table.Header(h => { foreach (var texto in new[] { "Código", "Diagnóstico", "Cantidad" }) h.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text(texto).Bold(); });
                    foreach (var item in resumen)
                    {
                        foreach (var valor in new[] { item.CodigoDiagnostico, item.Diagnostico, item.Total.ToString() })
                            table.Cell().BorderBottom(.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(valor);
                    }
                }
                else
                {
                    table.ColumnsDefinition(c => { c.ConstantColumn(52); c.ConstantColumn(72); c.RelativeColumn(1.2f); c.RelativeColumn(1.3f); c.RelativeColumn(1.2f); c.RelativeColumn(1.6f); c.ConstantColumn(62); c.ConstantColumn(72); });
                    table.Header(h => { foreach (var texto in new[] { "Fecha", "Orden/NHCL", "Paciente", "Propietario", "Médico", "Diagnóstico", "Tipo", "Edad / Sexo" }) h.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text(texto).Bold(); });
                    foreach (var item in diagnosticos)
                    {
                        foreach (var valor in new[]
                        {
                            item.Fecha.ToString("dd/MM/yyyy"), $"{item.Orden} / {item.Nhcl}", item.Paciente,
                            item.Cliente, item.Medico, $"{item.CodigoDiagnostico} · {item.Diagnostico}", item.Tipo,
                            $"{item.EdadAnios}a {item.EdadMeses}m · {item.Sexo}"
                        }) table.Cell().BorderBottom(.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(valor ?? string.Empty);
                    }
                }
            });
            page.Footer().AlignCenter().Text($"Generado por e-ForVets · {DateTime.Now:dd/MM/yyyy HH:mm}");
        })).GeneratePdf();
    }

    public byte[] GenerarReporteDiagnosticosExcel(
        List<ReporteDiagnosticoDto> diagnosticos, DateTime desde, DateTime hasta, bool condensado)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(condensado ? "Diagnósticos condensados" : "Diagnósticos");
        if (condensado)
        {
            string[] headers = ["Código", "Diagnóstico", "Cantidad"];
            for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
            var grupos = diagnosticos.GroupBy(x => new { x.CodigoDiagnostico, x.Diagnostico })
                .Select(g => new { g.Key.CodigoDiagnostico, g.Key.Diagnostico, Total = g.Count() })
                .OrderBy(x => x.Diagnostico);
            var fila = 2;
            foreach (var g in grupos)
            {
                ws.Cell(fila, 1).Value = g.CodigoDiagnostico; ws.Cell(fila, 2).Value = g.Diagnostico;
                ws.Cell(fila, 3).Value = g.Total; fila++;
            }
        }
        else
        {
            string[] headers = ["Fecha", "Orden", "NHCL", "Paciente", "Propietario", "Médico", "Código diagnóstico", "Diagnóstico", "Tipo", "Edad años", "Edad meses", "Sexo"];
            for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
            var fila = 2;
            foreach (var x in diagnosticos)
            {
                object[] valores = [x.Fecha, x.Orden, x.Nhcl, x.Paciente, x.Cliente, x.Medico, x.CodigoDiagnostico, x.Diagnostico, x.Tipo, x.EdadAnios, x.EdadMeses, x.Sexo];
                for (var i = 0; i < valores.Length; i++) ws.Cell(fila, i + 1).Value = XLCellValue.FromObject(valores[i]);
                fila++;
            }
            ws.Column(1).Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
        }
        var ultimaColumna = condensado ? 3 : 12;
        ws.Range(1, 1, 1, ultimaColumna).Style.Font.Bold = true;
        ws.Range(1, 1, 1, ultimaColumna).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.SheetView.FreezeRows(1); ws.RangeUsed()?.SetAutoFilter(); ws.Columns().AdjustToContents();
        ws.Cell(ws.LastRowUsed()?.RowNumber() + 2 ?? 3, 1).Value = $"Período: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}";
        using var stream = new MemoryStream(); wb.SaveAs(stream); return stream.ToArray();
    }

    public byte[] GenerarRecordatoriosSeguimientoPdf(
        List<RecordatorioVacunaDto> recordatorios, DateTime desde, DateTime hasta, bool porRevision)
    {
        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape()); page.Margin(22); page.DefaultTextStyle(x => x.FontSize(8));
            page.Header().Column(col =>
            {
                col.Item().Text("REPORTE DE RECORDATORIOS DE VACUNAS A PACIENTES").Bold().FontSize(14).FontColor(Colors.Blue.Darken3);
                col.Item().Text($"Período: {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy} · Fecha de referencia: {(porRevision ? "próxima revisión" : "envío")} · {recordatorios.Count} registro(s)").FontColor(Colors.Grey.Darken2);
            });
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(60); c.RelativeColumn(1.2f); c.RelativeColumn(1.4f); c.RelativeColumn(1.3f); c.ConstantColumn(65); c.ConstantColumn(65); c.RelativeColumn(1.4f); });
                table.Header(h => { foreach (var texto in new[] { "NHCL", "Paciente", "Propietario", "Enviado por", "Envío", "Revisión", "Contacto" }) h.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text(texto).Bold(); });
                foreach (var item in recordatorios)
                {
                    foreach (var valor in new[]
                    {
                        item.Nhcl.ToString(), item.Paciente, item.Propietario, $"{item.QuienManda}\n{item.Especialidad}",
                        item.FechaEnvio.ToString("dd/MM/yyyy"), item.ProximaRevision?.ToString("dd/MM/yyyy") ?? "",
                        $"{item.Telefono} / {item.Movil}\n{item.CorreoEnviado}"
                    }) table.Cell().BorderBottom(.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(valor ?? string.Empty);
                }
            });
            page.Footer().AlignCenter().Text($"Generado por e-ForVets · {DateTime.Now:dd/MM/yyyy HH:mm}");
        })).GeneratePdf();
    }

    public byte[] GenerarRecordatoriosSeguimientoExcel(
        List<RecordatorioVacunaDto> recordatorios, DateTime desde, DateTime hasta, bool porRevision)
    {
        using var wb = new XLWorkbook(); var ws = wb.Worksheets.Add("Recordatorios");
        string[] headers = ["NHCL", "Paciente", "Propietario", "Enviado por", "Especialidad", "Correo enviado", "Fecha envío", "Próxima revisión", "Teléfono", "Móvil", "Dirección"];
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        var fila = 2;
        foreach (var x in recordatorios)
        {
            object[] valores = [x.Nhcl, x.Paciente, x.Propietario, x.QuienManda, x.Especialidad, x.CorreoEnviado, x.FechaEnvio, x.ProximaRevision.HasValue ? x.ProximaRevision.Value : string.Empty, x.Telefono, x.Movil, x.Direccion];
            for (var i = 0; i < valores.Length; i++) ws.Cell(fila, i + 1).Value = XLCellValue.FromObject(valores[i]);
            fila++;
        }
        ws.Columns(7, 8).Style.DateFormat.Format = "dd/mm/yyyy";
        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true; ws.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.SheetView.FreezeRows(1); ws.RangeUsed()?.SetAutoFilter(); ws.Columns().AdjustToContents();
        ws.Cell(ws.LastRowUsed()?.RowNumber() + 2 ?? 3, 1).Value = $"Período: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}; referencia: {(porRevision ? "próxima revisión" : "envío")}";
        using var stream = new MemoryStream(); wb.SaveAs(stream); return stream.ToArray();
    }

    public byte[] GenerarAlertasSeguimientoPdf(
        List<AlertaClinicaDto> alertas, DateTime desde, DateTime hasta, string estado)
    {
        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape()); page.Margin(22); page.DefaultTextStyle(x => x.FontSize(7.5f));
            page.Header().Column(col =>
            {
                col.Item().Text("LISTA DE PROCEDIMIENTOS QUIRÚRGICOS POR ESTADO").Bold().FontSize(14).FontColor(Colors.Blue.Darken3);
                col.Item().Text($"Período: {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy} · Estado: {(string.IsNullOrWhiteSpace(estado) ? "Todos" : estado)} · {alertas.Count} registro(s)").FontColor(Colors.Grey.Darken2);
            });
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(62); c.ConstantColumn(58); c.RelativeColumn(1.2f); c.RelativeColumn(1.4f); c.RelativeColumn(1.3f); c.RelativeColumn(1.1f); c.RelativeColumn(1.2f); c.ConstantColumn(65); });
                table.Header(h => { foreach (var texto in new[] { "Orden/NHCL", "Fecha", "Paciente", "Propietario", "Médico", "Especie/Edad", "Pronóstico", "Estado" }) h.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text(texto).Bold(); });
                foreach (var item in alertas)
                {
                    foreach (var valor in new[]
                    {
                        $"{item.Orden} / {item.Nhcl}", item.FechaEnvio.ToString("dd/MM/yyyy"), item.Paciente, item.Cliente,
                        $"{item.Medico}\n{item.Especialidad}", $"{item.Especie}\n{item.Anios} año(s)", item.Pronostico, item.Estado
                    }) table.Cell().BorderBottom(.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(valor ?? string.Empty);
                }
            });
            page.Footer().AlignCenter().Text($"Generado por e-ForVets · {DateTime.Now:dd/MM/yyyy HH:mm}");
        })).GeneratePdf();
    }

    public byte[] GenerarAlertasSeguimientoExcel(
        List<AlertaClinicaDto> alertas, DateTime desde, DateTime hasta, string estado)
    {
        using var wb = new XLWorkbook(); var ws = wb.Worksheets.Add("Alertas quirúrgicas");
        string[] headers = ["Orden", "NHCL", "Fecha", "Paciente", "Propietario", "Médico", "Especialidad", "Especie", "Edad años", "Pronóstico", "Estado", "Teléfono", "Móvil", "Correo"];
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        var fila = 2;
        foreach (var x in alertas)
        {
            object[] valores = [x.Orden, x.Nhcl, x.FechaEnvio, x.Paciente, x.Cliente, x.Medico, x.Especialidad, x.Especie, x.Anios, x.Pronostico, x.Estado, x.Telefono, x.Movil, x.Email];
            for (var i = 0; i < valores.Length; i++) ws.Cell(fila, i + 1).Value = XLCellValue.FromObject(valores[i]);
            fila++;
        }
        ws.Column(3).Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true; ws.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.SheetView.FreezeRows(1); ws.RangeUsed()?.SetAutoFilter(); ws.Columns().AdjustToContents();
        ws.Cell(ws.LastRowUsed()?.RowNumber() + 2 ?? 3, 1).Value = $"Período: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}; estado: {(string.IsNullOrWhiteSpace(estado) ? "Todos" : estado)}";
        using var stream = new MemoryStream(); wb.SaveAs(stream); return stream.ToArray();
    }

    public byte[] GenerarFallecidosPdf(
        List<PacienteFallecidoDto> fallecidos, DateTime desde, DateTime hasta)
    {
        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4); page.Margin(28); page.DefaultTextStyle(x => x.FontSize(9));
            page.Header().Column(col =>
            {
                col.Item().Text("REPORTE DE PACIENTES FALLECIDOS").Bold().FontSize(14).FontColor(Colors.Blue.Darken3);
                col.Item().Text($"Período: {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy} · {fallecidos.Count} registro(s)").FontColor(Colors.Grey.Darken2);
            });
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(c => { c.ConstantColumn(70); c.ConstantColumn(60); c.RelativeColumn(1.2f); c.RelativeColumn(1.5f); c.RelativeColumn(1.8f); });
                table.Header(h => { foreach (var texto in new[] { "Fecha", "NHCL", "Paciente", "Propietario", "Motivo" }) h.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text(texto).Bold(); });
                foreach (var item in fallecidos)
                {
                    foreach (var valor in new[] { item.Fecha.ToString("dd/MM/yyyy"), item.Nhcl.ToString(), item.Paciente, item.Propietario, item.Motivo })
                        table.Cell().BorderBottom(.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(valor ?? string.Empty);
                }
            });
            page.Footer().AlignCenter().Text($"Generado por e-ForVets · {DateTime.Now:dd/MM/yyyy HH:mm}");
        })).GeneratePdf();
    }

    public byte[] GenerarFallecidosExcel(
        List<PacienteFallecidoDto> fallecidos, DateTime desde, DateTime hasta)
    {
        using var wb = new XLWorkbook(); var ws = wb.Worksheets.Add("Pacientes fallecidos");
        string[] headers = ["Fecha", "NHCL", "Paciente", "Propietario", "Motivo"];
        for (var i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        var fila = 2;
        foreach (var x in fallecidos)
        {
            object[] valores = [x.Fecha, x.Nhcl, x.Paciente, x.Propietario, x.Motivo];
            for (var i = 0; i < valores.Length; i++) ws.Cell(fila, i + 1).Value = XLCellValue.FromObject(valores[i]);
            fila++;
        }
        ws.Column(1).Style.DateFormat.Format = "dd/mm/yyyy";
        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true; ws.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.SheetView.FreezeRows(1); ws.RangeUsed()?.SetAutoFilter(); ws.Columns().AdjustToContents();
        ws.Cell(ws.LastRowUsed()?.RowNumber() + 2 ?? 3, 1).Value = $"Período: {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}";
        using var stream = new MemoryStream(); wb.SaveAs(stream); return stream.ToArray();
    }

    // ========================================================================
    // 1. KARDEX DE ENFERMERÍA (PDF & EXCEL)
    // ========================================================================

    public byte[] GenerarKardexPdf(
        PacienteHeaderDto paciente,
        List<KardexSabanaItemDto> sabana,
        List<ProductoUtilizadoDto> detalleRegistros)
    {
        var fechas = sabana.FirstOrDefault()?.FechasColumnas
                     ?? Enumerable.Range(0, 5).Select(i => DateTime.Today.AddDays(i)).ToList();

        var qrBytes = GenerarCodigoQrPng($"EFORVETS-ORDEN:{paciente.OrdenAtencionId}|NHCL:{paciente.Nhcl}|PAC:{paciente.PacienteNombre}");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                            c.Item().Text("HOJA DE KARDEX DE ENFERMERÍA — ADMINISTRACIÓN DE MEDICAMENTOS").Bold().FontSize(10).FontColor(Colors.Grey.Darken3);
                            c.Item().Text($"Fecha de Emisión: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(52).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Blue.Darken2);

                    col.Item().Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Blue.Lighten3).Padding(6).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("N° Orden: ").Bold();
                            t.Span($"{paciente.OrdenAtencionId}    ");
                            t.Span("N° Historia (NHCL): ").Bold();
                            t.Span($"{paciente.Nhcl}").FontColor(Colors.Red.Darken2).Bold();
                        });
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Paciente: ").Bold();
                            t.Span($"{paciente.PacienteNombre} ({paciente.Especie} - {paciente.Sexo})");
                        });
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Propietario: ").Bold();
                            t.Span($"{paciente.ClienteNombre}");
                        });
                    });

                    col.Item().PaddingBottom(6);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.6f);
                            for (int d = 0; d < 5; d++)
                                columns.RelativeColumn(1.48f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Indigo.Lighten4).Padding(4).Column(c =>
                            {
                                c.Item().AlignCenter().Text("1.- MEDICAMENTO").Bold().FontSize(8.5f);
                                c.Item().AlignCenter().Text("PRESENTACIÓN, VÍA, DOSIS UNITARIA, FRECUENCIA").FontSize(7);
                            });

                            foreach (var fec in fechas)
                            {
                                header.Cell().Border(1).BorderColor(Colors.Grey.Medium).Column(c =>
                                {
                                    c.Item().Background(Colors.Green.Lighten4).Padding(2).AlignCenter().Text($"DÍA: {fec:yyyy-MM-dd}").Bold().FontSize(8);
                                    c.Item().Background(Colors.Yellow.Lighten3).Row(r =>
                                    {
                                        r.RelativeItem(4).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(1).AlignCenter().Text("HORA").Bold().FontSize(7);
                                        r.RelativeItem(3).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(1).AlignCenter().Text("INI").Bold().FontSize(7);
                                        r.RelativeItem(3).Padding(1).AlignCenter().Text("FUN").Bold().FontSize(7);
                                    });
                                });
                            }
                        });

                        if (sabana.Count == 0)
                        {
                            table.Cell().ColumnSpan(6).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(12).AlignCenter()
                                .Text("No existen medicamentos registrados en el Kardex para esta orden.").Italic();
                        }
                        else
                        {
                            foreach (var med in sabana)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignMiddle().Column(c =>
                                {
                                    c.Item().Text(med.DescripcionCompleta).Bold().FontSize(8);
                                    c.Item().Text($"Cant: {med.Cantidad:N2} | Código: {med.IdProd}").FontSize(7).FontColor(Colors.Grey.Darken2);
                                });

                                for (int diaIdx = 0; diaIdx < 5; diaIdx++)
                                {
                                    var tomas = diaIdx < med.AdministracionesPorDia.Count
                                        ? med.AdministracionesPorDia[diaIdx]
                                        : [];

                                    table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Column(c =>
                                    {
                                        foreach (var toma in tomas)
                                        {
                                            var colorTexto = toma.EsTurnoNocturno ? Colors.Red.Darken2 : Colors.Blue.Darken2;
                                            c.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Row(r =>
                                            {
                                                r.RelativeItem(4).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(1).AlignCenter()
                                                    .Text(string.IsNullOrWhiteSpace(toma.Hora) ? " " : toma.Hora).FontColor(colorTexto).FontSize(7.5f);
                                                r.RelativeItem(3).BorderRight(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(1).AlignCenter()
                                                    .Text(string.IsNullOrWhiteSpace(toma.Iniciales) ? " " : toma.Iniciales).FontColor(colorTexto).FontSize(7.5f);
                                                r.RelativeItem(3).PaddingVertical(1).AlignCenter()
                                                    .Text(string.IsNullOrWhiteSpace(toma.Funcion) ? " " : toma.Funcion).FontColor(colorTexto).FontSize(7.5f);
                                            });
                                        }
                                    });
                                }
                            }
                        }
                    });

                    col.Item().PaddingTop(6).Text("Nota: Turno Diurno (00:00 - 17:59) en Azul | Turno Nocturno (18:00 - 23:59) en Rojo.")
                        .FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken2);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("e-ForVets Kardex — Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerarKardexExcel(
        PacienteHeaderDto paciente,
        List<KardexSabanaItemDto> sabana,
        List<ProductoUtilizadoDto> detalleRegistros)
    {
        using var workbook = new XLWorkbook();

        var wsSabana = workbook.Worksheets.Add("Sabana Kardex 5 Dias");
        wsSabana.Cell(1, 1).Value = "HOSPITAL DE ESPECIALIDADES VETERINARIAS - KARDEX DE ENFERMERÍA";
        wsSabana.Range(1, 1, 1, 16).Merge().Style
            .Font.SetBold(true).Font.SetFontSize(13)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A"))
            .Font.SetFontColor(XLColor.White);

        wsSabana.Cell(2, 1).Value = $"Orden: {paciente.OrdenAtencionId} | NHCL: {paciente.Nhcl} | Paciente: {paciente.PacienteNombre} ({paciente.Sexo}) | Cliente: {paciente.ClienteNombre}";
        wsSabana.Range(2, 1, 2, 16).Merge().Style
            .Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#DBEAFE"));

        var fechas = sabana.FirstOrDefault()?.FechasColumnas
                     ?? Enumerable.Range(0, 5).Select(i => DateTime.Today.AddDays(i)).ToList();

        wsSabana.Cell(4, 1).Value = "MEDICAMENTO / PRESENTACIÓN / VÍA / FRECUENCIA";
        wsSabana.Range(4, 1, 5, 1).Merge().Style
            .Font.SetBold(true).Alignment.SetVertical(XLAlignmentVerticalValues.Center)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#C7D2FE"));

        int colBase = 2;
        foreach (var fec in fechas)
        {
            wsSabana.Cell(4, colBase).Value = fec.ToString("yyyy-MM-dd");
            wsSabana.Range(4, colBase, 4, colBase + 2).Merge().Style
                .Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#BBF7D0"));

            wsSabana.Cell(5, colBase).Value = "HORA";
            wsSabana.Cell(5, colBase + 1).Value = "INI";
            wsSabana.Cell(5, colBase + 2).Value = "FUN";
            wsSabana.Range(5, colBase, 5, colBase + 2).Style
                .Font.SetBold(true).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#FEF08A"));

            colBase += 3;
        }

        int currentRow = 6;
        foreach (var med in sabana)
        {
            int startRow = currentRow;
            int endRow = currentRow + 5;
            wsSabana.Cell(startRow, 1).Value = med.DescripcionCompleta;
            wsSabana.Range(startRow, 1, endRow, 1).Merge().Style
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true)
                .Font.SetBold(true);

            for (int d = 0; d < 5; d++)
            {
                int c = 2 + (d * 3);
                var tomas = d < med.AdministracionesPorDia.Count ? med.AdministracionesPorDia[d] : [];
                for (int t = 0; t < 6; t++)
                {
                    var toma = t < tomas.Count ? tomas[t] : new HoraInicialesFuncionDto();
                    var fontColor = toma.EsTurnoNocturno ? XLColor.Red : XLColor.DarkBlue;

                    wsSabana.Cell(startRow + t, c).Value = toma.Hora;
                    wsSabana.Cell(startRow + t, c + 1).Value = toma.Iniciales;
                    wsSabana.Cell(startRow + t, c + 2).Value = toma.Funcion;

                    wsSabana.Range(startRow + t, c, startRow + t, c + 2).Style
                        .Font.SetFontColor(fontColor)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }
            }

            wsSabana.Range(startRow, 1, endRow, 16).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Hair);

            currentRow += 6;
        }

        wsSabana.Column(1).Width = 42;
        for (int c = 2; c <= 16; c++) wsSabana.Column(c).Width = 9;

        var wsDetalle = workbook.Worksheets.Add("Detalle Administraciones");
        string[] headers = ["ID", "Orden", "NHCL", "Código", "Producto", "Cantidad", "Precio Unit.", "Vía", "Fecha", "Hora", "Iniciales", "Función", "Funcionario", "Bodega", "Indicaciones"];
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = wsDetalle.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A")).Font.SetFontColor(XLColor.White);
        }

        int rowDet = 2;
        foreach (var item in detalleRegistros)
        {
            wsDetalle.Cell(rowDet, 1).Value = item.Id;
            wsDetalle.Cell(rowDet, 2).Value = item.Orden;
            wsDetalle.Cell(rowDet, 3).Value = item.Nhcl;
            wsDetalle.Cell(rowDet, 4).Value = item.Codigo;
            wsDetalle.Cell(rowDet, 5).Value = item.Producto;
            wsDetalle.Cell(rowDet, 6).Value = item.Cantidad;
            wsDetalle.Cell(rowDet, 7).Value = item.Precio;
            wsDetalle.Cell(rowDet, 8).Value = item.Via;
            wsDetalle.Cell(rowDet, 9).Value = item.Fecha;
            wsDetalle.Cell(rowDet, 10).Value = item.Hora;
            wsDetalle.Cell(rowDet, 11).Value = item.Iniciales;
            wsDetalle.Cell(rowDet, 12).Value = item.Funcion;
            wsDetalle.Cell(rowDet, 13).Value = item.Funcionario;
            wsDetalle.Cell(rowDet, 14).Value = item.Bodega;
            wsDetalle.Cell(rowDet, 15).Value = item.Indicaciones;
            rowDet++;
        }

        wsDetalle.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ========================================================================
    // 2. SIGNOS VITALES Y BALANCE HÍDRICO (PDF & EXCEL)
    // ========================================================================

    public byte[] GenerarSignosVitalesPdf(PacienteHeaderDto paciente, FormularioSignosEnfermeriaDto form)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-SIGNOS|ORDEN:{paciente.OrdenAtencionId}|NHCL:{paciente.Nhcl}|FORM:{form.NumFormulario}");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(14);
                page.DefaultTextStyle(x => x.FontSize(7.2f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"SIGNOS VITALES, BALANCE HÍDRICO Y CUIDADOS DE ENFERMERÍA — HOJA #{form.NumFormulario}").Bold().FontSize(9.5f);
                        });
                        row.ConstantItem(44).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(2).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text($"Orden: {paciente.OrdenAtencionId} | NHCL: {paciente.Nhcl}").Bold();
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie} - {paciente.Sexo})");
                        r.RelativeItem().Text($"Cliente: {paciente.ClienteNombre}");
                    });
                });

                page.Content().Column(col =>
                {
                    // 1. SIGNOS VITALES
                    col.Item().PaddingTop(3).PaddingBottom(2).Text($"1. SIGNOS VITALES — HOJA #{form.NumFormulario} (9 DÍAS — TURNOS AM / PM)").Bold().FontSize(8f).FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2.5f);
                            for (int i = 0; i < 18; i++) c.RelativeColumn(1f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(1.5f).Text("Parámetro").Bold();
                            for (int d = 1; d <= 9; d++)
                            {
                                var fec = form.Fechas.FirstOrDefault(x => x.Orden == d)?.Fecha?.ToString("dd/MM") ?? $"Día {d}";
                                h.Cell().ColumnSpan(2).Border(0.5f).Background(Colors.Blue.Lighten4).Padding(1.5f).AlignCenter().Text(fec).Bold();
                            }

                            h.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(1).Text("Turno").Bold();
                            for (int s = 1; s <= 18; s++)
                            {
                                h.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text(s % 2 == 1 ? "AM" : "PM").Bold();
                            }
                        });

                        void AddSignoRow(string label, Func<SignoVitalSlotDto, string> selector)
                        {
                            t.Cell().Border(0.5f).Padding(1.5f).Text(label).Bold();
                            foreach (var slot in form.SignosVitales.OrderBy(x => x.Orden))
                            {
                                t.Cell().Border(0.5f).Padding(1.5f).AlignCenter().Text(selector(slot));
                            }
                        }

                        AddSignoRow("Pulso (lpm)", s => s.Pulso?.ToString() ?? "");
                        AddSignoRow("Temperatura (°C)", s => s.Temperatura?.ToString("N1") ?? "");
                        AddSignoRow("Frec. Resp. (rpm)", s => s.FrecuenciaRespiratoria?.ToString() ?? "");
                        AddSignoRow("P. Sistólica", s => s.PresionSistolica?.ToString() ?? "");
                        AddSignoRow("P. Diastólica", s => s.PresionDiastolica?.ToString() ?? "");
                    });

                    // 2. BALANCE HÍDRICO
                    col.Item().PaddingTop(5).PaddingBottom(2).Text($"2. BALANCE HÍDRICO DIARIO — HOJA #{form.NumFormulario} (cc / ml)").Bold().FontSize(8f).FontColor(Colors.Green.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2.5f);
                            for (int i = 0; i < 9; i++) c.RelativeColumn(1f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(1.5f).Text("Concepto (cc / ml)").Bold();
                            for (int d = 1; d <= 9; d++)
                            {
                                var fec = form.Fechas.FirstOrDefault(x => x.Orden == d)?.Fecha?.ToString("yyyy-MM-dd") ?? $"Día {d}";
                                h.Cell().Border(0.5f).Background(Colors.Green.Lighten4).Padding(1.5f).AlignCenter().Text(fec).Bold();
                            }
                        });

                        void AddBalanceRow(string label, Func<EnfBalanceHidricoDiaDto, string> selector, bool bold = false)
                        {
                            var c0 = t.Cell().Border(0.5f).Padding(1.5f).Text(label);
                            if (bold) c0.Bold();
                            foreach (var b in form.BalanceHidrico.OrderBy(x => x.Orden))
                            {
                                var cell = t.Cell().Border(0.5f).Padding(1.5f).AlignCenter().Text(selector(b));
                                if (bold) cell.Bold();
                            }
                        }

                        AddBalanceRow("Ingreso Parenteral", b => b.Parenteral?.ToString("N1") ?? "");
                        AddBalanceRow("Ingreso Vía Oral", b => b.ViaOral?.ToString("N1") ?? "");
                        AddBalanceRow("TOTAL INGRESOS", b => b.TotalIngresos.ToString("N1"), true);
                        AddBalanceRow("Eliminación Orina", b => b.Orina?.ToString("N1") ?? "");
                        AddBalanceRow("Eliminación Drenaje", b => b.Drenaje?.ToString("N1") ?? "");
                        AddBalanceRow("Eliminación Otros", b => b.Otros?.ToString("N1") ?? "");
                        AddBalanceRow("TOTAL ELIMINACIONES", b => b.TotalEliminaciones.ToString("N1"), true);
                        AddBalanceRow("BALANCE NETO", b => b.BalanceNeto.ToString("N1"), true);
                    });

                    // 3. MEDICIONES Y CUIDADOS DIARIOS
                    col.Item().PaddingTop(5).PaddingBottom(2).Text($"3. MEDICIONES Y CUIDADOS DIARIOS — HOJA #{form.NumFormulario} (9 DÍAS)").Bold().FontSize(8f).FontColor(Colors.Teal.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2.5f);
                            for (int i = 0; i < 9; i++) c.RelativeColumn(1f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(1.5f).Text("Actividad / Medición").Bold();
                            for (int d = 1; d <= 9; d++)
                            {
                                var fec = form.Fechas.FirstOrDefault(x => x.Orden == d)?.Fecha?.ToString("yyyy-MM-dd") ?? $"Día {d}";
                                h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(1.5f).AlignCenter().Text(fec).Bold();
                            }
                        });

                        void AddMedicionRow(string label, Func<EnfMedicionDiaDto, string> selector)
                        {
                            t.Cell().Border(0.5f).Padding(1.5f).Text(label).Bold();
                            for (int d = 1; d <= 9; d++)
                            {
                                var m = form.Mediciones.FirstOrDefault(x => x.Orden == d);
                                var val = m is not null ? selector(m) : "";
                                t.Cell().Border(0.5f).Padding(1.5f).AlignCenter().Text(val);
                            }
                        }

                        AddMedicionRow("Peso (kg)", m => m.Peso?.ToString("N2") ?? "");
                        AddMedicionRow("Dieta Indicada", m => m.DietaIndicada ?? "");
                        AddMedicionRow("N° Comidas", m => m.NumeroComidas ?? "");
                        AddMedicionRow("Baño / Aseo", m => (!string.IsNullOrWhiteSpace(m.Banio) ? m.Banio : m.Aseo) ?? "");
                        AddMedicionRow("Movilización", m => m.Movilizacion ?? "");
                        AddMedicionRow("Vendajes / Curación", m => m.Vendajes ?? "");
                        AddMedicionRow("Cambio de Sonda", m => m.CambioSonda ?? "");
                        AddMedicionRow("Responsable (Inic.)", m => m.Responsable ?? "");
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("e-ForVets Signos Vitales — Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenerarSignosVitalesExcel(PacienteHeaderDto paciente, FormularioSignosEnfermeriaDto form)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Signos Hoja {form.NumFormulario}");

        ws.Cell(1, 1).Value = $"e-ForVets - SIGNOS VITALES, BALANCE HÍDRICO Y MEDICIONES (HOJA #{form.NumFormulario})";
        ws.Range(1, 1, 1, 19).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A")).Font.SetFontColor(XLColor.White);
        ws.Cell(2, 1).Value = $"Orden: {paciente.OrdenAtencionId} | NHCL: {paciente.Nhcl} | Paciente: {paciente.PacienteNombre} | Cliente: {paciente.ClienteNombre}";
        ws.Range(2, 1, 2, 19).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#DBEAFE"));

        // 1. SIGNOS VITALES
        ws.Cell(4, 1).Value = "Parámetro";
        for (int s = 1; s <= 18; s++)
        {
            int dia = ((s - 1) / 2) + 1;
            ws.Cell(4, s + 1).Value = $"D{dia} {(s % 2 == 1 ? "AM" : "PM")}";
        }
        ws.Range(4, 1, 4, 19).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0"));

        ws.Cell(5, 1).Value = "Pulso (lpm)";
        ws.Cell(6, 1).Value = "Temperatura (°C)";
        ws.Cell(7, 1).Value = "Frec. Respiratoria (rpm)";
        ws.Cell(8, 1).Value = "Presión Sistólica";
        ws.Cell(9, 1).Value = "Presión Diastólica";

        for (int i = 0; i < form.SignosVitales.Count && i < 18; i++)
        {
            var s = form.SignosVitales[i];
            int col = i + 2;
            ws.Cell(5, col).Value = s.Pulso;
            ws.Cell(6, col).Value = s.Temperatura;
            ws.Cell(7, col).Value = s.FrecuenciaRespiratoria;
            ws.Cell(8, col).Value = s.PresionSistolica;
            ws.Cell(9, col).Value = s.PresionDiastolica;
        }

        // 2. BALANCE HÍDRICO
        ws.Cell(11, 1).Value = $"2. Balance Hídrico Diario — Hoja #{form.NumFormulario} (9 Días)";
        for (int d = 1; d <= 9; d++)
        {
            var fec = form.Fechas.FirstOrDefault(x => x.Orden == d)?.Fecha?.ToString("yyyy-MM-dd") ?? $"Día {d}";
            ws.Cell(11, d + 1).Value = fec;
        }
        ws.Range(11, 1, 11, 10).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#BBF7D0"));

        string[] labelsBal = ["Parenteral", "Vía Oral", "Total Ingresos", "Orina", "Drenaje", "Otros", "Total Eliminaciones", "Balance Neto"];
        for (int r = 0; r < labelsBal.Length; r++) ws.Cell(12 + r, 1).Value = labelsBal[r];

        for (int d = 0; d < form.BalanceHidrico.Count && d < 9; d++)
        {
            var b = form.BalanceHidrico[d];
            int col = d + 2;
            ws.Cell(12, col).Value = b.Parenteral;
            ws.Cell(13, col).Value = b.ViaOral;
            ws.Cell(14, col).Value = b.TotalIngresos;
            ws.Cell(15, col).Value = b.Orina;
            ws.Cell(16, col).Value = b.Drenaje;
            ws.Cell(17, col).Value = b.Otros;
            ws.Cell(18, col).Value = b.TotalEliminaciones;
            ws.Cell(19, col).Value = b.BalanceNeto;
        }

        // 3. MEDICIONES Y CUIDADOS DIARIOS
        ws.Cell(21, 1).Value = $"3. Mediciones y Cuidados Diarios — Hoja #{form.NumFormulario} (9 Días)";
        for (int d = 1; d <= 9; d++)
        {
            var fec = form.Fechas.FirstOrDefault(x => x.Orden == d)?.Fecha?.ToString("yyyy-MM-dd") ?? $"Día {d}";
            ws.Cell(21, d + 1).Value = fec;
        }
        ws.Range(21, 1, 21, 10).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#BAE6FD"));

        string[] labelsMed = ["Peso (kg)", "Dieta Indicada", "N° Comidas", "Baño / Aseo", "Movilización", "Vendajes / Curación", "Cambio de Sonda", "Responsable (Inic.)"];
        for (int r = 0; r < labelsMed.Length; r++) ws.Cell(22 + r, 1).Value = labelsMed[r];

        for (int d = 1; d <= 9; d++)
        {
            var m = form.Mediciones.FirstOrDefault(x => x.Orden == d);
            int col = d + 1;
            if (m is not null)
            {
                if (m.Peso.HasValue) ws.Cell(22, col).Value = m.Peso.Value;
                ws.Cell(23, col).Value = m.DietaIndicada ?? "";
                ws.Cell(24, col).Value = m.NumeroComidas ?? "";
                ws.Cell(25, col).Value = (!string.IsNullOrWhiteSpace(m.Banio) ? m.Banio : m.Aseo) ?? "";
                ws.Cell(26, col).Value = m.Movilizacion ?? "";
                ws.Cell(27, col).Value = m.Vendajes ?? "";
                ws.Cell(28, col).Value = m.CambioSonda ?? "";
                ws.Cell(29, col).Value = m.Responsable ?? "";
            }
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ========================================================================
    // 3. FICHA DE HOSPITALIZACIÓN 24 HORAS (PDF & EXCEL)
    // ========================================================================

    public byte[] GenerarFichaHospitalizacionPdf(
        PacienteHeaderDto paciente,
        FichaHospitalizacionSheetDto sheet,
        List<ViaAdministracionDto> vias,
        List<RitmoHorarioDto> ritmos)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-FICHA|ORDEN:{sheet.IdOrden}|NHCL:{sheet.Nhcl}|HOJA:{sheet.Hoja}");
        var viasMap = vias.ToDictionary(v => v.Id, v => v.Via);
        var ritmosMap = ritmos.ToDictionary(r => r.Id, r => r.Des_Ritmo);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(14);
                page.DefaultTextStyle(x => x.FontSize(7f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"FICHA DE HOSPITALIZACIÓN 24 HORAS — HOJA #{sheet.Hoja} | FECHA: {sheet.Fecha:yyyy-MM-dd}").Bold().FontSize(9);
                        });
                        row.ConstantItem(42).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(3).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text($"Orden: {sheet.IdOrden} | NHCL: {sheet.Nhcl} | Día Hosp: {sheet.DiasHospitalizacion}").Bold();
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie}) | Peso: {sheet.Peso ?? 0:N2} kg");
                        r.RelativeItem().Text($"Médico Tratante: {sheet.MedicoResponsable}");
                    });
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(2).Text("BLOQUE 1: MEDICACIÓN INTRAHOSPITALARIA (24 HORAS: 07:00 A 06:00)").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3.2f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            for (int h = 0; h < 24; h++) c.RelativeColumn(0.75f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).Text("Principio Activo").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text("mg/kg").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text("ml").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text("Vía").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text("Ritmo").Bold();
                            foreach (var hr in Matriz24HorasBaseDto.HorasOrden)
                                h.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text($"{hr}h").Bold();
                        });

                        foreach (var r in sheet.Bloque1Medicacion.Where(x => !string.IsNullOrWhiteSpace(x.Medicacion)))
                        {
                            t.Cell().Border(0.5f).Padding(2).Text(r.Medicacion ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.MgKg?.ToString("N2") ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Ml?.ToString("N2") ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Via.HasValue && viasMap.TryGetValue(r.Via.Value, out var v) ? v : "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Ritmo.HasValue && ritmosMap.TryGetValue(r.Ritmo.Value, out var rt) ? rt : "");
                            foreach (var hr in Matriz24HorasBaseDto.HorasOrden)
                                t.Cell().Border(0.5f).Padding(1).AlignCenter().Text(r.GetValorHora(hr) ?? "");
                        }
                    });

                    col.Item().PaddingTop(5).PaddingBottom(2).Text("BLOQUE 2: ALIMENTACIÓN (24 HORAS)").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3.2f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            for (int h = 0; h < 24; h++) c.RelativeColumn(0.75f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.Green.Lighten4).Padding(2).Text("Alimentación").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Green.Lighten4).Padding(2).AlignCenter().Text("Kcal/d").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Green.Lighten4).Padding(2).AlignCenter().Text("Kcal/v").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Green.Lighten4).Padding(2).AlignCenter().Text("gr").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Green.Lighten4).Padding(2).AlignCenter().Text("Ritmo").Bold();
                            foreach (var hr in Matriz24HorasBaseDto.HorasOrden)
                                h.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text($"{hr}h").Bold();
                        });

                        foreach (var r in sheet.Bloque2Alimentacion.Where(x => !string.IsNullOrWhiteSpace(x.Alimentacion)))
                        {
                            t.Cell().Border(0.5f).Padding(2).Text(r.Alimentacion ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Cald?.ToString("N1") ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Calv?.ToString("N1") ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Gr ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Ritmo.HasValue && ritmosMap.TryGetValue(r.Ritmo.Value, out var rt) ? rt : "");
                            foreach (var hr in Matriz24HorasBaseDto.HorasOrden)
                                t.Cell().Border(0.5f).Padding(1).AlignCenter().Text(r.GetValorHora(hr) ?? "");
                        }
                    });

                    col.Item().PaddingTop(5).PaddingBottom(2).Text("BLOQUE 3: FLUIDOTERAPIA (24 HORAS)").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3.2f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            for (int h = 0; h < 24; h++) c.RelativeColumn(0.75f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(2).Text("Fluido").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(2).AlignCenter().Text("M+P+D").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(2).AlignCenter().Text("ml/día").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(2).AlignCenter().Text("ml/h").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(2).AlignCenter().Text("gotas").Bold();
                            foreach (var hr in Matriz24HorasBaseDto.HorasOrden)
                                h.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text($"{hr}h").Bold();
                        });

                        foreach (var r in sheet.Bloque3Fluidoterapia.Where(x => !string.IsNullOrWhiteSpace(x.Fluidoterapia)))
                        {
                            t.Cell().Border(0.5f).Padding(2).Text(r.Fluidoterapia ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Mpd ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.MlDia ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.MlH ?? "");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Gotas ?? "");
                            foreach (var hr in Matriz24HorasBaseDto.HorasOrden)
                                t.Cell().Border(0.5f).Padding(1).AlignCenter().Text(r.GetValorHora(hr) ?? "");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("e-ForVets Ficha de Hospitalización — Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenerarFichaHospitalizacionExcel(
        PacienteHeaderDto paciente,
        FichaHospitalizacionSheetDto sheet,
        List<ViaAdministracionDto> vias,
        List<RitmoHorarioDto> ritmos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Ficha Hoja {sheet.Hoja}");

        ws.Cell(1, 1).Value = $"e-ForVets - FICHA DE HOSPITALIZACIÓN (Hoja #{sheet.Hoja} - Fecha: {sheet.Fecha:yyyy-MM-dd})";
        ws.Range(1, 1, 1, 29).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A")).Font.SetFontColor(XLColor.White);
        ws.Cell(2, 1).Value = $"Orden: {sheet.IdOrden} | NHCL: {sheet.Nhcl} | Paciente: {paciente.PacienteNombre} | Peso: {sheet.Peso ?? 0:N2} kg | Médico: {sheet.MedicoResponsable}";
        ws.Range(2, 1, 2, 29).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#DBEAFE"));

        int row = 4;
        ws.Cell(row, 1).Value = "MEDICACIÓN";
        ws.Cell(row, 2).Value = "mg/kg";
        ws.Cell(row, 3).Value = "ml";
        ws.Cell(row, 4).Value = "Vía";
        ws.Cell(row, 5).Value = "Ritmo";
        for (int i = 0; i < 24; i++)
            ws.Cell(row, 6 + i).Value = $"{Matriz24HorasBaseDto.HorasOrden[i]}h";
        ws.Range(row, 1, row, 29).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#C7D2FE"));
        row++;

        foreach (var r in sheet.Bloque1Medicacion)
        {
            ws.Cell(row, 1).Value = r.Medicacion;
            ws.Cell(row, 2).Value = r.MgKg;
            ws.Cell(row, 3).Value = r.Ml;
            ws.Cell(row, 4).Value = vias.FirstOrDefault(v => v.Id == r.Via)?.Via ?? "";
            ws.Cell(row, 5).Value = ritmos.FirstOrDefault(rt => rt.Id == r.Ritmo)?.Des_Ritmo ?? "";
            for (int i = 0; i < 24; i++)
                ws.Cell(row, 6 + i).Value = r.GetValorHora(Matriz24HorasBaseDto.HorasOrden[i]);
            row++;
        }

        row++;
        ws.Cell(row, 1).Value = "ALIMENTACIÓN";
        ws.Cell(row, 2).Value = "Kcal/d";
        ws.Cell(row, 3).Value = "Kcal/v";
        ws.Cell(row, 4).Value = "gr";
        ws.Cell(row, 5).Value = "Ritmo";
        for (int i = 0; i < 24; i++)
            ws.Cell(row, 6 + i).Value = $"{Matriz24HorasBaseDto.HorasOrden[i]}h";
        ws.Range(row, 1, row, 29).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#BBF7D0"));
        row++;

        foreach (var r in sheet.Bloque2Alimentacion)
        {
            ws.Cell(row, 1).Value = r.Alimentacion;
            ws.Cell(row, 2).Value = r.Cald;
            ws.Cell(row, 3).Value = r.Calv;
            ws.Cell(row, 4).Value = r.Gr;
            ws.Cell(row, 5).Value = ritmos.FirstOrDefault(rt => rt.Id == r.Ritmo)?.Des_Ritmo ?? "";
            for (int i = 0; i < 24; i++)
                ws.Cell(row, 6 + i).Value = r.GetValorHora(Matriz24HorasBaseDto.HorasOrden[i]);
            row++;
        }

        row++;
        ws.Cell(row, 1).Value = "FLUIDOTERAPIA";
        ws.Cell(row, 2).Value = "M+P+D";
        ws.Cell(row, 3).Value = "ml/día";
        ws.Cell(row, 4).Value = "ml/h";
        ws.Cell(row, 5).Value = "gotas";
        for (int i = 0; i < 24; i++)
            ws.Cell(row, 6 + i).Value = $"{Matriz24HorasBaseDto.HorasOrden[i]}h";
        ws.Range(row, 1, row, 29).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#A5F3FC"));
        row++;

        foreach (var r in sheet.Bloque3Fluidoterapia)
        {
            ws.Cell(row, 1).Value = r.Fluidoterapia;
            ws.Cell(row, 2).Value = r.Mpd;
            ws.Cell(row, 3).Value = r.MlDia;
            ws.Cell(row, 4).Value = r.MlH;
            ws.Cell(row, 5).Value = r.Gotas;
            for (int i = 0; i < 24; i++)
                ws.Cell(row, 6 + i).Value = r.GetValorHora(Matriz24HorasBaseDto.HorasOrden[i]);
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ========================================================================
    // 4. MONITOREO HOSPITALARIO 24 HORAS (PDF & EXCEL)
    // ========================================================================

    public byte[] GenerarMonitoreoHospitalarioPdf(PacienteHeaderDto paciente, MonitoreoHospitalarioSheetDto sheet)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-MONITOREO|ORDEN:{sheet.IdOrden}|NHCL:{sheet.Nhcl}|HOJA:{sheet.Hoja}");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(14);
                page.DefaultTextStyle(x => x.FontSize(7f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"MONITOREO PACIENTE HOSPITALIZADO 24 HORAS — HOJA #{sheet.Hoja} | FECHA: {sheet.Fecha:yyyy-MM-dd}").Bold().FontSize(9);
                        });
                        row.ConstantItem(42).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(3).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text($"Orden: {sheet.IdOrden} | NHCL: {sheet.Nhcl}").Bold();
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie} - {paciente.Sexo})");
                        r.RelativeItem().Text($"Médico Tratante: {sheet.MedicoResponsable}");
                    });
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2.5f);
                            c.RelativeColumn(1.2f);
                            for (int h = 0; h < 24; h++) c.RelativeColumn(0.75f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(2).Text("Parámetro Clínico").Bold();
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(2).AlignCenter().Text("Unidad").Bold();
                            foreach (var hr in Matriz24HorasBaseDto.HorasOrden)
                                h.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(1).AlignCenter().Text($"{hr}h").Bold();
                        });

                        foreach (var f in sheet.FilasMonitoreo.Where(x => x.Fila <= 22 || !string.IsNullOrWhiteSpace(x.Item)))
                        {
                            var nombreParam = f.EsParametroPersonalizado && !string.IsNullOrWhiteSpace(f.Item) ? f.Item : f.ParametroLabel;
                            t.Cell().Border(0.5f).Padding(1.5f).Text(nombreParam).Bold();
                            t.Cell().Border(0.5f).Padding(1.5f).AlignCenter().Text(f.UnidadLabel);
                            foreach (var hr in Matriz24HorasBaseDto.HorasOrden)
                                t.Cell().Border(0.5f).Padding(1.5f).AlignCenter().Text(f.GetValorHora(hr) ?? "");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("e-ForVets Monitoreo Hospitalario 24h — Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenerarMonitoreoHospitalarioExcel(PacienteHeaderDto paciente, MonitoreoHospitalarioSheetDto sheet)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Monitoreo Hoja {sheet.Hoja}");

        ws.Cell(1, 1).Value = $"e-ForVets - MONITOREO PACIENTE HOSPITALIZADO 24H (Hoja #{sheet.Hoja} - Fecha: {sheet.Fecha:yyyy-MM-dd})";
        ws.Range(1, 1, 1, 26).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A")).Font.SetFontColor(XLColor.White);
        ws.Cell(2, 1).Value = $"Orden: {sheet.IdOrden} | NHCL: {sheet.Nhcl} | Paciente: {paciente.PacienteNombre} | Médico: {sheet.MedicoResponsable}";
        ws.Range(2, 1, 2, 26).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#DBEAFE"));

        ws.Cell(4, 1).Value = "Parámetro";
        ws.Cell(4, 2).Value = "Unidad";
        for (int i = 0; i < 24; i++)
            ws.Cell(4, 3 + i).Value = $"{Matriz24HorasBaseDto.HorasOrden[i]}:00";
        ws.Range(4, 1, 4, 26).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0"));

        int row = 5;
        foreach (var f in sheet.FilasMonitoreo)
        {
            ws.Cell(row, 1).Value = f.EsParametroPersonalizado && !string.IsNullOrWhiteSpace(f.Item) ? f.Item : f.ParametroLabel;
            ws.Cell(row, 2).Value = f.UnidadLabel;
            for (int i = 0; i < 24; i++)
                ws.Cell(row, 3 + i).Value = f.GetValorHora(Matriz24HorasBaseDto.HorasOrden[i]);
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ========================================================================
    // 5. EVALUACIÓN PRE-ANESTÉSICA (PDF - Reemplaza vt_reportepreanestesia.rpt)
    // ========================================================================
    public byte[] GenerarPreAnestesiaPdf(PacienteHeaderDto paciente, PreAnestesiaDto pre, List<ProductoQuirofanoDto> premedicacion)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-PREANESTESIA|ORDEN:{pre.Orden}|NHCL:{pre.Nhcl}|ASA:{pre.Riesgo}");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"EVALUACIÓN PRE-ANESTÉSICA — RIESGO ASA {pre.Riesgo} | FECHA: {pre.FechaIngreso:yyyy-MM-dd HH:mm}").Bold().FontSize(9.5f);
                        });
                        row.ConstantItem(45).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(4).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text($"Orden: {pre.Orden} | NHCL: {pre.Nhcl}").Bold();
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie} - {paciente.Raza})");
                        r.RelativeItem().Text($"Propietario: {paciente.PropietarioNombre}");
                    });
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(4).Text("1. CLASIFICACIÓN ASA Y ESTADO CLÍNICO").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Border(0.5f).Padding(5).Column(c =>
                    {
                        c.Item().Text($"Clasificación ASA: ASA {pre.Riesgo} | Grupo Sanguíneo: {pre.GrupoSanguineo} | Última Ingestión: {pre.UltimaIngestion:yyyy-MM-dd HH:mm}");
                        c.Item().Text($"Información Especial / Alertas: {pre.InformacionEspecial}");
                    });

                    col.Item().PaddingTop(6).PaddingBottom(4).Text("2. EVALUACIÓN POR SISTEMAS Y LABORATORIO").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2.5f); });
                        void AddRow(string label, string val)
                        {
                            t.Cell().Border(0.5f).Background(Colors.Grey.Lighten4).Padding(3).Text(label).Bold();
                            t.Cell().Border(0.5f).Padding(3).Text(val ?? "");
                        }
                        AddRow("Hematológico", pre.Hematologico);
                        AddRow("Química Sanguínea", pre.QuimicaSanguinea);
                        AddRow("Uroanálisis", pre.Uroanalisis);
                        AddRow("Sistema Respiratorio", pre.SistemaRespiratorio);
                        AddRow("Sistema Circulatorio", pre.SistemaCirculatorio);
                        AddRow("Sistema Nervioso", pre.SistemaNervioso);
                        AddRow("Otros Sistemas", pre.SistemasOtros);
                        AddRow("Anestesias Anteriores", pre.AnestesiasAnteriores);
                        AddRow("Terapia Farmacológica Previa", pre.TerapiaAnterior);
                    });

                    col.Item().PaddingTop(8).PaddingBottom(4).Text("3. MEDICACIÓN PRE-ANESTÉSICA ADMINISTRADA").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(3f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(2.5f);
                        });
                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).Text("Fecha/Hora").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).Text("Fármaco / Insumo").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).AlignCenter().Text("Cant.").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).AlignCenter().Text("Vía").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).Text("Dosis / Indicaciones").Bold();
                        });
                        foreach (var m in premedicacion)
                        {
                            t.Cell().Border(0.5f).Padding(2.5f).Text($"{m.Fecha:dd/MM} {m.Hora}");
                            t.Cell().Border(0.5f).Padding(2.5f).Text(m.ProdDescripcion);
                            t.Cell().Border(0.5f).Padding(2.5f).AlignCenter().Text($"{m.CantProd:N2}");
                            t.Cell().Border(0.5f).Padding(2.5f).AlignCenter().Text(m.Via);
                            t.Cell().Border(0.5f).Padding(2.5f).Text(m.IndicacionesProd);
                        }
                    });

                    col.Item().PaddingTop(14).Text($"Anestesiólogo / Responsable: {pre.Firma}").Bold();
                });

                page.Footer().AlignCenter().Text("e-ForVets — Evaluación Pre-Anestésica");
            });
        }).GeneratePdf();
    }

    // ========================================================================
    // 6. HOJA DE ANESTESIA INTRAOPERATORIA (PDF & EXCEL - Reemplaza vt_reporteAnestesia.rpt)
    // ========================================================================
    public byte[] GenerarHojaAnestesiaPdf(PacienteHeaderDto paciente, HojaAnestesiaCompletaDto hoja)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-ANESTESIA|ORDEN:{hoja.IdOrden}|NHCL:{hoja.Nhcl}|REG:{hoja.IdRegistro}");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(14);
                page.DefaultTextStyle(x => x.FontSize(7f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"REGISTRO INTRAOPERATORIO DE ANESTESIA — ASA {hoja.Asa} | FECHA: {hoja.Fecha:yyyy-MM-dd} ({hoja.HoraInicio} - {hoja.HoraFin})").Bold().FontSize(9);
                        });
                        row.ConstantItem(42).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(3).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text($"Orden: {hoja.IdOrden} | NHCL: {hoja.Nhcl} | Paciente: {paciente.PacienteNombre} ({paciente.Especie})").Bold();
                        r.RelativeItem().Text($"Vía Aérea: Tubo #{hoja.NumTubo} | O2: {hoja.O2}% | Vol Tidal: {hoja.VolTidal} ml | FR: {hoja.FR}");
                        r.RelativeItem().Text($"Balance Hídrico: Ingresos {hoja.TotalIngresos:N0} ml | Egresos {hoja.TotalEgresos:N0} ml | Balance: {hoja.BalanceTotal:N0} ml");
                    });
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(2).Text("MONITORIZACIÓN HEMODINÁMICA Y AGENTES ANESTÉSICOS (16 INTERVALOS)").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2.6f);
                            for (int i = 0; i < 16; i++) c.RelativeColumn(1f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).Text("Parámetro / Agente").Bold();
                            foreach (var s in hoja.SlotsTiempo)
                                h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text(s.HoraLabel).Bold();
                        });

                        void AddMetricRow(string nombre, Func<SlotGraficaAnestesiaDto, string> selector, string bgColor = "#FFFFFF")
                        {
                            t.Cell().Border(0.5f).Background(bgColor).Padding(1.5f).Text(nombre).Bold();
                            foreach (var s in hoja.SlotsTiempo)
                                t.Cell().Border(0.5f).Background(bgColor).Padding(1.5f).AlignCenter().Text(selector(s));
                        }

                        AddMetricRow("Frec. Cardíaca (lpm)", s => s.FrecuenciaCardiaca?.ToString() ?? "", "#FEF2F2");
                        AddMetricRow("P. Sistólica (mmHg)", s => s.PresionSistolica?.ToString() ?? "", "#EFF6FF");
                        AddMetricRow("P. Diastólica (mmHg)", s => s.PresionDiastolica?.ToString() ?? "", "#EFF6FF");
                        AddMetricRow("P. Media TAM (mmHg)", s => s.TensionArterialMedia ?? "", "#EFF6FF");
                        AddMetricRow("SpO2 (%)", s => s.O2L?.ToString() ?? "", "#F0FDF4");
                        AddMetricRow("EtCO2 (mmHg)", s => s.EtCO2?.ToString() ?? "", "#FEFCE8");
                        AddMetricRow("Temperatura (°C)", s => s.Temperatura?.ToString() ?? "");

                        for (int a = 0; a < 15; a++)
                        {
                            int idx = a;
                            if (string.IsNullOrWhiteSpace(hoja.NombresAgentes[idx]) && hoja.SlotsTiempo.All(s => string.IsNullOrWhiteSpace(s.ValoresAgentes[idx])))
                                continue;
                            AddMetricRow(hoja.NombresAgentes[idx], s => s.ValoresAgentes[idx]);
                        }
                    });

                    col.Item().PaddingTop(4).Text($"Observaciones / Incidentes Intraoperatorios: {hoja.Comentarios} | Anestesiólogo: {hoja.Firma}");
                });

                page.Footer().AlignCenter().Text("e-ForVets — Hoja de Anestesia Intraoperatoria");
            });
        }).GeneratePdf();
    }

    public byte[] GenerarHojaAnestesiaExcel(PacienteHeaderDto paciente, HojaAnestesiaCompletaDto hoja)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Hoja Anestesia");

        ws.Cell(1, 1).Value = $"e-ForVets - HOJA DE ANESTESIA INTRAOPERATORIA (Orden #{hoja.IdOrden} - {hoja.Fecha:yyyy-MM-dd})";
        ws.Range(1, 1, 1, 17).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A")).Font.SetFontColor(XLColor.White);
        ws.Cell(2, 1).Value = $"Paciente: {paciente.PacienteNombre} | ASA: {hoja.Asa} | Inicio: {hoja.HoraInicio} | Fin: {hoja.HoraFin} | Balance: {hoja.BalanceTotal:N0} ml";
        ws.Range(2, 1, 2, 17).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#DBEAFE"));

        ws.Cell(4, 1).Value = "Parámetro / Agente";
        for (int i = 0; i < 16; i++)
            ws.Cell(4, 2 + i).Value = hoja.SlotsTiempo[i].HoraLabel;
        ws.Range(4, 1, 4, 17).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0"));

        int row = 5;
        void WriteRow(string label, Func<SlotGraficaAnestesiaDto, object?> getter)
        {
            ws.Cell(row, 1).Value = label;
            for (int i = 0; i < 16; i++)
                ws.Cell(row, 2 + i).Value = getter(hoja.SlotsTiempo[i])?.ToString() ?? "";
            row++;
        }

        WriteRow("Frecuencia Cardíaca (lpm)", s => s.FrecuenciaCardiaca);
        WriteRow("Presión Sistólica (mmHg)", s => s.PresionSistolica);
        WriteRow("Presión Diastólica (mmHg)", s => s.PresionDiastolica);
        WriteRow("TAM (mmHg)", s => s.TensionArterialMedia);
        WriteRow("SpO2 (%)", s => s.O2L);
        WriteRow("EtCO2 (mmHg)", s => s.EtCO2);
        WriteRow("Temperatura (°C)", s => s.Temperatura);

        for (int a = 0; a < 15; a++)
        {
            int idx = a;
            WriteRow(hoja.NombresAgentes[idx], s => s.ValoresAgentes[idx]);
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ========================================================================
    // 7. PROTOCOLO QUIRÚRGICO (PDF - Reemplaza vt_reporteProtocolo.rpt)
    // ========================================================================
    public byte[] GenerarProtocoloQuirurgicoPdf(PacienteHeaderDto paciente, ProtocoloQuirurgicoCompletoDto prot)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-PROTOCOLO|ORDEN:{prot.IdOrden}|NHCL:{prot.Nhcl}|FECHA:{prot.Fecha:yyyyMMdd}");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"PROTOCOLO OPERATORIO Y CHEQUEO DE CIRUGÍA | FECHA: {prot.Fecha:yyyy-MM-dd}").Bold().FontSize(10);
                        });
                        row.ConstantItem(46).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(4).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text($"Orden: {prot.IdOrden} | NHCL: {prot.Nhcl}").Bold();
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie} - {paciente.Raza})");
                        r.RelativeItem().Text($"Tipo Cirugía: {prot.TipoCirugia} | Sala #{prot.Sala}");
                    });
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(3).Text("1. EQUIPO QUIRÚRGICO INTERVINIENTE").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(2.5f); c.RelativeColumn(2f); c.RelativeColumn(1f); });
                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(3).Text("Profesional").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(3).Text("Función Quirúrgica").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(3).AlignCenter().Text("Principal").Bold();
                        });
                        foreach (var m in prot.Equipo)
                        {
                            string profesional = m.NombreProfesional;
                            if (m.EsExterno)
                            {
                                profesional += " (Externo";
                                if (!string.IsNullOrWhiteSpace(m.Cedula))
                                {
                                    profesional += $" - C.I.: {m.Cedula}";
                                }
                                profesional += ")";
                            }
                            t.Cell().Border(0.5f).Padding(3).Text(profesional);
                            t.Cell().Border(0.5f).Padding(3).Text(m.RolFuncion);
                            t.Cell().Border(0.5f).Padding(3).AlignCenter().Text(m.Principal ? "SÍ" : "NO");
                        }
                    });

                    col.Item().PaddingTop(8).PaddingBottom(3).Text("2. DESCRIPCIÓN DEL ACTO QUIRÚRGICO").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(1.2f); c.RelativeColumn(3.5f); });
                        void RowProt(string k, string v)
                        {
                            t.Cell().Border(0.5f).Background(Colors.Grey.Lighten4).Padding(4).Text(k).Bold();
                            t.Cell().Border(0.5f).Padding(4).Text(v ?? "");
                        }
                        string horarioStr = "No registrado";
                        if (!string.IsNullOrWhiteSpace(prot.HoraInicio) || !string.IsNullOrWhiteSpace(prot.HoraFin))
                        {
                            horarioStr = $"Inicio: {(string.IsNullOrWhiteSpace(prot.HoraInicio) ? "--:--" : prot.HoraInicio)} | Fin: {(string.IsNullOrWhiteSpace(prot.HoraFin) ? "--:--" : prot.HoraFin)}";
                            if (prot.DuracionMinutos > 0)
                            {
                                int durH = prot.DuracionMinutos / 60;
                                int durM = prot.DuracionMinutos % 60;
                                horarioStr += $" (Duración: {durH}h {durM}m)";
                            }
                        }
                        RowProt("Horario y Duración", horarioStr);
                        RowProt("Procedimiento Principal", prot.Principal);
                        RowProt("Diéresis / Incisión", prot.Incision);
                        RowProt("Hallazgos Quirúrgicos", prot.Hallazgos);
                        RowProt("Técnica / Procedimiento", prot.ProcedimientoQuir);
                        RowProt("Síntesis / Cierre", prot.Sintesis);
                        RowProt("Complicaciones", prot.Complicaciones);
                        RowProt("Sangrado Estimado /uestras", $"Sangrado: {prot.Sangrado} ml | Muestra Histopatológica: {prot.Muestra} | Drenajes: {prot.Drenaje}");
                    });

                    col.Item().PaddingTop(8).PaddingBottom(3).Text("3. CHEQUEO DE ENFERMERÍA QUIRÚRGICA Y CONTEO DE TEXTILES").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Border(0.5f).Padding(5).Column(c =>
                    {
                        c.Item().Text($"Posición: {prot.Posicion} | Piel: {prot.CondicionPiel} | Antisepsia: {prot.Lavado} | Electrobisturí: {prot.UnidadElectro}");
                        c.Item().Text($"Gasas Iniciales: {prot.GasasIniciales} | Gasas Adicionales: {prot.GasasAdicionales} | Recuento Final: {prot.GasasRecuentoFinal} | Compresas: {prot.Compresas}").Bold();
                        c.Item().Text($"Clasificación Herida: {prot.CasoContaminado} | Transferido a: {prot.TransferidoA}");
                    });
                });

                page.Footer().AlignCenter().Text("e-ForVets — Protocolo Operatorio");
            });
        }).GeneratePdf();
    }

    // ========================================================================
    // 8. SALA DE RECUPERACIÓN POST-ANESTÉSICA (PDF - Reemplaza vt_reporteRecuperacion.rpt)
    // ========================================================================
    public byte[] GenerarRecuperacionPdf(
        PacienteHeaderDto paciente,
        List<RecuperacionSignoVitalDto> signos,
        PostOperatorioChecklistDto post,
        List<LiquidoRecuperacionDto> ing,
        List<LiquidoRecuperacionDto> egr,
        List<ObservacionRecuperacionDto> obs)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-RECUPERACION|ORDEN:{paciente.OrdenAtencionId}|NHCL:{paciente.Nhcl}");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"SALA DE RECUPERACIÓN POST-ANESTÉSICA Y ENTREGA DE PACIENTE").Bold().FontSize(9.5f);
                        });
                        row.ConstantItem(42).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(3).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text($"Orden: {paciente.OrdenAtencionId} | NHCL: {paciente.Nhcl}").Bold();
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie})");
                        r.RelativeItem().Text($"Destino Alta Recup.: {post.Destino} ({post.FechaHoraSalida:yyyy-MM-dd HH:mm})");
                    });
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(2).Text("1. MONITORIZACIÓN DE SIGNOS VITALES EN RECUPERACIÓN").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(2f);
                        });
                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).Text("Hora").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text("Pulso").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text("PA (S/D)").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text("PAM").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text("FR").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text("Temp °C").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).AlignCenter().Text("SpO2 %").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(2).Text("Responsable").Bold();
                        });
                        foreach (var s in signos)
                        {
                            t.Cell().Border(0.5f).Padding(2).Text($"{s.Fecha:dd/MM} {s.Hora}");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text($"{s.Pulso}");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text($"{s.Sistolica}/{s.Diastolica}");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text($"{s.PresionMedia}");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text($"{s.Respiracion}");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text($"{s.Temperatura:N1}");
                            t.Cell().Border(0.5f).Padding(2).AlignCenter().Text($"{s.Saturacion}%");
                            t.Cell().Border(0.5f).Padding(2).Text(s.NombreUsuario);
                        }
                    });

                    col.Item().PaddingTop(6).PaddingBottom(2).Text("2. CHECKLIST DE ENTREGA POST-OPERATORIA").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Border(0.5f).Padding(4).Column(c =>
                    {
                        c.Item().Text($"Indicaciones Médicas: {post.Ind} ({post.ObsInd}) | Signos Vitales Estables: {post.Sig} ({post.ObsSig})");
                        c.Item().Text($"Apósitos / Herida: {post.Apo} ({post.ObsApo}) | Líquidos Parenterales: {post.Liq} ({post.ObsLiq})");
                        c.Item().Text($"Sonda Vesical: {post.Son} ({post.ObsSon}) | Drenajes: {post.Drenes} ({post.ObsDrenes}) | SNG: {post.Sng} ({post.ObsSng})");
                    });

                    col.Item().PaddingTop(6).PaddingBottom(2).Text("3. NOTAS DE EVOLUCIÓN EN RECUPERACIÓN").Bold().FontColor(Colors.Blue.Darken3);
                    foreach (var n in obs)
                    {
                        col.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2)
                            .Text($"[{n.Fecha:dd/MM/yyyy} {n.Hora}] ({n.NombreUsuario}): {n.Observacion}");
                    }
                });

                page.Footer().AlignCenter().Text("e-ForVets — Sala de Recuperación Post-Anestésica");
            });
        }).GeneratePdf();
    }
    // ========================================================================
    // 9. FICHA MÉDICA / HISTORIA CLÍNICA SOAP (PDF - Reemplaza RptFichaMedicaHDEV.rpt)
    // ========================================================================
    public byte[] GenerarFichaMedicaPdf(
        PacienteHeaderDto paciente,
        FichaMedicaCompletaDto ficha,
        List<DiagnosticoCie10Dto> diagnosticos,
        List<RecetaMedicamentoItemDto> receta)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-FICHAMEDICA|ORDEN:{ficha.MedOrden}|NHCL:{ficha.MedNhcl}|FECHA:{ficha.MedFecha:yyyyMMdd}");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ComponerPaginaFichaMedica(page, paciente, ficha, diagnosticos, receta, qrBytes, "HISTORIA CLÍNICA VETERINARIA — EVALUACIÓN SOAP");
            });
        }).GeneratePdf();
    }

    private static void ComponerPaginaFichaMedica(
        PageDescriptor page,
        PacienteHeaderDto paciente,
        FichaMedicaCompletaDto ficha,
        List<DiagnosticoCie10Dto> diagnosticos,
        List<RecetaMedicamentoItemDto> receta,
        byte[] qrBytes,
        string? etiquetaDocumento = null)
    {
        page.Size(PageSizes.A4);
        page.Margin(18);
        page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Lato"));

        page.Header().Column(header =>
        {
            // Barra superior institucional
            header.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("HOSPITAL DE ESPECIALIDADES VETERINARIAS - e-ForVets")
                        .Bold().FontSize(12.5f).FontColor(Colors.Blue.Darken4);
                    c.Item().Text("SISTEMA DE GESTIÓN HOSPITALARIA Y EXPEDIENTE CLÍNICO ELECTRÓNICO")
                        .FontSize(7.5f).FontColor(Colors.Grey.Darken1);

                    c.Item().PaddingTop(3).Row(badgeRow =>
                    {
                        badgeRow.AutoItem().Background(Colors.Blue.Darken3).PaddingHorizontal(6).PaddingVertical(2)
                            .Text(etiquetaDocumento ?? "HISTORIA CLÍNICA VETERINARIA — EVALUACIÓN SOAP")
                            .Bold().FontSize(7.8f).FontColor(Colors.White);

                        var modalidad = string.IsNullOrWhiteSpace(ficha.MedTipoConsulta) ? "CONSULTA EXTERNA" : ficha.MedTipoConsulta.ToUpper();
                        badgeRow.AutoItem().PaddingLeft(6).Background(Colors.Blue.Lighten4).PaddingHorizontal(6).PaddingVertical(2)
                            .Text($"MODALIDAD: {modalidad}")
                            .Bold().FontSize(7.5f).FontColor(Colors.Blue.Darken4);
                    });
                });

                row.ConstantItem(50).AlignRight().Column(colQr =>
                {
                    colQr.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Image(qrBytes);
                    colQr.Item().AlignCenter().Text($"ORDEN #{ficha.MedOrden}").Bold().FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                });
            });

            // Tarjeta de Identificación de Paciente y Tutor
            header.Item().PaddingTop(4).Background(Colors.Blue.Lighten5).Border(0.6f).BorderColor(Colors.Blue.Lighten3).Padding(5).Row(r =>
            {
                // Columna 1: Paciente
                r.RelativeItem(2.2f).Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("PACIENTE: ").Bold().FontSize(7.5f).FontColor(Colors.Blue.Darken4);
                        t.Span(paciente.PacienteNombre).Bold().FontSize(8.5f).FontColor(Colors.Grey.Darken4);
                    });
                    c.Item().Text(t =>
                    {
                        t.Span("NHCL: ").Bold().FontSize(7.5f);
                        t.Span($"#{paciente.Nhcl}").Bold().FontSize(8f).FontColor(Colors.Blue.Darken3);
                        t.Span("   |   Orden: ").Bold().FontSize(7.5f);
                        t.Span($"#{ficha.MedOrden}").Bold().FontSize(8f);
                    });
                    c.Item().Text(t =>
                    {
                        t.Span("Especie / Raza: ").Bold().FontSize(7.5f);
                        t.Span($"{paciente.Especie} / {paciente.Raza}").FontSize(7.5f);
                    });
                    c.Item().Text(t =>
                    {
                        t.Span("Sexo / Edad: ").Bold().FontSize(7.5f);
                        t.Span($"{paciente.Sexo} / {paciente.EdadTexto}").FontSize(7.5f);
                    });
                });

                // Columna 2: Tutor / Asegurado
                r.RelativeItem(2f).Column(c =>
                {
                    c.Item().Text("TUTOR / ASEGURADO:").Bold().FontSize(7.5f).FontColor(Colors.Blue.Darken4);
                    c.Item().Text(string.IsNullOrWhiteSpace(paciente.ClienteNombre) ? paciente.PropietarioNombre : paciente.ClienteNombre).Bold().FontSize(8f);
                    c.Item().Text(t =>
                    {
                        t.Span("Cédula / RUC: ").Bold().FontSize(7.5f);
                        t.Span(string.IsNullOrWhiteSpace(paciente.ClienteCedula) ? "Sin registrar" : paciente.ClienteCedula).FontSize(7.5f);
                    });
                    c.Item().Text(t =>
                    {
                        t.Span("Origen Paciente: ").Bold().FontSize(7.5f);
                        t.Span(paciente.Origen).FontSize(7.5f);
                    });
                });

                // Columna 3: Médico & Atención
                r.RelativeItem(1.8f).Column(c =>
                {
                    c.Item().Text("MÉDICO TRATANTE:").Bold().FontSize(7.5f).FontColor(Colors.Blue.Darken4);
                    var med = !string.IsNullOrWhiteSpace(ficha.UserNombre) 
                        ? ficha.UserNombre 
                        : (!string.IsNullOrWhiteSpace(ficha.NombreMedico) ? ficha.NombreMedico : paciente.MedicoResponsable);
                    c.Item().Text(string.IsNullOrWhiteSpace(med) ? "Médico Especialista" : med).Bold().FontSize(8f);
                    c.Item().Text(t =>
                    {
                        t.Span("Fecha: ").Bold().FontSize(7.5f);
                        t.Span($"{ficha.MedFecha:dd/MM/yyyy HH:mm}").FontSize(7.5f);
                    });
                    c.Item().Text(t =>
                    {
                        t.Span("Pronóstico: ").Bold().FontSize(7.5f);
                        t.Span(string.IsNullOrWhiteSpace(ficha.MedPronostico) ? "Favorable" : ficha.MedPronostico).Bold().FontSize(7.5f).FontColor(Colors.Green.Darken3);
                    });
                });
            });
        });

        page.Content().PaddingTop(4).Column(col =>
        {
            // ====================================================================
            // 1. CONSTANTES FISIOLÓGICAS Y SIGNOS VITALES
            // ====================================================================
            col.Item().PaddingTop(2).PaddingBottom(2).Row(r =>
            {
                r.RelativeItem().Text("1. CONSTANTES FISIOLÓGICAS Y SIGNOS VITALES").Bold().FontSize(8.5f).FontColor(Colors.Blue.Darken4);
            });

            // Grid de 7 tiles de signos vitales
            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                void AddVitalTile(string label, string valor, string unidad)
                {
                    t.Cell().Background(Colors.Grey.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(2.5f).Column(cell =>
                    {
                        cell.Item().AlignCenter().Text(label).Bold().FontSize(6.2f).FontColor(Colors.Grey.Darken2);
                        cell.Item().AlignCenter().Text(tText =>
                        {
                            tText.Span(valor).Bold().FontSize(8.5f).FontColor(Colors.Blue.Darken4);
                            if (!string.IsNullOrWhiteSpace(unidad))
                            {
                                tText.Span($" {unidad}").FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                            }
                        });
                    });
                }

                AddVitalTile("PESO", ficha.Peso > 0 ? ficha.Peso.ToString("N2") : "---", "kg");
                AddVitalTile("TEMPERATURA", ficha.Temperatura > 0 ? ficha.Temperatura.ToString("N1") : "---", "°C");
                AddVitalTile("F. CARDÍACA", ficha.FrecuenciaCardiaca > 0 ? ficha.FrecuenciaCardiaca.ToString("N0") : "---", "lpm");
                AddVitalTile("F. RESPIRAT.", ficha.FrecuenciaRespiratoria > 0 ? ficha.FrecuenciaRespiratoria.ToString("N0") : "---", "rpm");
                AddVitalTile("T.LL. CAPILAR", ficha.TiempoLlenadoCapilar > 0 ? ficha.TiempoLlenadoCapilar.ToString() : "---", "seg");
                AddVitalTile("DESHIDRAT.", $"{ficha.Deshidratacion}", "%");
                AddVitalTile("COND. CORP.", $"{ficha.IndiceCondicionCorporal}", "/9");
            });

            // Examen Físico Segmentario
            col.Item().PaddingTop(2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(85);
                    c.RelativeColumn();
                    c.ConstantColumn(85);
                    c.RelativeColumn();
                });

                void AddExamRow(string k1, string v1, string k2, string v2)
                {
                    t.Cell().Background(Colors.Grey.Lighten4).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(k1).Bold().FontSize(7f).FontColor(Colors.Grey.Darken3);
                    t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(string.IsNullOrWhiteSpace(v1) ? "Normal / Sin hallazgos" : v1).FontSize(7.2f);
                    t.Cell().Background(Colors.Grey.Lighten4).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(k2).Bold().FontSize(7f).FontColor(Colors.Grey.Darken3);
                    t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(string.IsNullOrWhiteSpace(v2) ? "Normal / Sin hallazgos" : v2).FontSize(7.2f);
                }

                AddExamRow("Mucosas:", ficha.Mucosas, "Reflejo Tusígeno:", ficha.ReflejoTusigeno);
                AddExamRow("Auscultación:", ficha.Auscultacion, "Palpación Abdominal:", ficha.PalpacionAbdominal);
                AddExamRow("Linfonodos:", ficha.Linfonodos, "Cavidad Oral:", ficha.CavidadOral);
                var detalleTiroides = string.IsNullOrWhiteSpace(ficha.Otros) ? $"{ficha.PalpacionTiroides} · {ficha.Cp}" : $"{ficha.PalpacionTiroides} · {ficha.Cp} · {ficha.Otros}";
                AddExamRow("Piel y Anexos:", ficha.PielYAnexos, "Tiroides / CP:", detalleTiroides);
            });

            // ====================================================================
            // 2. MOTIVO DE CONSULTA Y EVALUACIÓN CLÍNICA SOAP
            // ====================================================================
            col.Item().PaddingTop(4).PaddingBottom(2).Row(r =>
            {
                r.RelativeItem().Text("2. ANAMNESIS Y EVALUACIÓN CLÍNICA SOAP").Bold().FontSize(8.5f).FontColor(Colors.Blue.Darken4);
            });

            // Caja destacada de Motivo de Consulta
            col.Item().Background(Colors.Blue.Lighten5).Border(0.6f).BorderColor(Colors.Blue.Lighten3).Padding(4).Row(r =>
            {
                r.AutoItem().Text("MOTIVO DE LA CONSULTA: ").Bold().FontSize(7.5f).FontColor(Colors.Blue.Darken4);
                r.RelativeItem().Text(string.IsNullOrWhiteSpace(ficha.MedMotivo) ? "Consulta médica veterinaria de rutina" : ficha.MedMotivo).Bold().FontSize(8f).FontColor(Colors.Grey.Darken4);
            });

            // Tabla SOAP
            col.Item().PaddingTop(2).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(110);
                    c.RelativeColumn();
                });

                void AddSoapRow(string label, string valor, bool isSoapMain = false)
                {
                    var bgHeader = isSoapMain ? Colors.Blue.Lighten4 : Colors.Grey.Lighten4;
                    var textHeaderColor = isSoapMain ? Colors.Blue.Darken4 : Colors.Grey.Darken3;

                    t.Cell().Background(bgHeader).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                        .Text(label).Bold().FontSize(7.2f).FontColor(textHeaderColor);

                    t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3)
                        .Text(string.IsNullOrWhiteSpace(valor) ? "—" : valor).FontSize(7.5f);
                }

                AddSoapRow("[S] SUBJETIVO / ANAMNESIS", ficha.MedAnamnesis, true);
                AddSoapRow("ANTECEDENTES CLÍNICOS", ficha.MedAntecedente);
                AddSoapRow("[O] OBJETIVO / HALLAZGOS", ficha.MedObjetivo, true);
                AddSoapRow("[A] ANÁLISIS / EVALUACIÓN", ficha.MedAnalisis, true);
                AddSoapRow("[P] PLAN DIAGNÓSTICO", ficha.MedPlan, true);
                AddSoapRow("PLAN TERAPÉUTICO", ficha.Tratamiento);
                var altaPronostico = string.IsNullOrWhiteSpace(ficha.MedPronostico) 
                    ? ficha.ObservacionesYRecomendaciones 
                    : $"{ficha.ObservacionesYRecomendaciones} [Pronóstico: {ficha.MedPronostico}]";
                AddSoapRow("RECOMENDACIONES / ALTA", altaPronostico);
            });

            // ====================================================================
            // 3. DIAGNÓSTICOS CIE-10
            // ====================================================================
            col.Item().PaddingTop(4).PaddingBottom(2).Row(r =>
            {
                r.RelativeItem().Text("3. DIAGNÓSTICOS CLÍNICOS CODIFICADOS (CIE-10)").Bold().FontSize(8.5f).FontColor(Colors.Blue.Darken4);
            });

            if (diagnosticos != null && diagnosticos.Any())
            {
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(55);
                        c.RelativeColumn(3f);
                        c.ConstantColumn(75);
                        c.RelativeColumn(3.5f);
                    });

                    t.Header(h =>
                    {
                        h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).AlignCenter().Text("CÓDIGO").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                        h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).Text("DIAGNÓSTICO").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                        h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).AlignCenter().Text("TIPO").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                        h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).Text("OBSERVACIONES / HALLAZGOS").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                    });

                    foreach (var d in diagnosticos)
                    {
                        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).AlignCenter().Text($"[{d.CodigoDiag}]").Bold().FontSize(7.2f).FontColor(Colors.Blue.Darken3);
                        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(d.NombreCie10).Bold().FontSize(7.5f);
                        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).AlignCenter().Text(d.TipoDiag).FontSize(7.2f);
                        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(string.IsNullOrWhiteSpace(d.DescripcionObservacion) ? "—" : d.DescripcionObservacion).FontSize(7.2f);
                    }
                });
            }
            else
            {
                col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text("Sin diagnósticos CIE-10 codificados registrados en esta atención.").Italic().FontSize(7.2f).FontColor(Colors.Grey.Darken1);
            }

            // ====================================================================
            // 4. PRESCRIPCIÓN FARMACOLÓGICA / RECETA
            // ====================================================================
            col.Item().PaddingTop(4).PaddingBottom(2).Row(r =>
            {
                r.RelativeItem().Text("4. PLAN FARMACOLÓGICO / MEDICAMENTOS PRESCRITOS").Bold().FontSize(8.5f).FontColor(Colors.Blue.Darken4);
            });

            if (receta != null && receta.Any())
            {
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3f);
                        c.ConstantColumn(50);
                        c.RelativeColumn(4.5f);
                    });

                    t.Header(h =>
                    {
                        h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).Text("MEDICAMENTO / PRESENTACIÓN").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                        h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).AlignCenter().Text("CANT.").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                        h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).Text("INDICACIONES / POSOLOGÍA / FRECUENCIA").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                    });

                    foreach (var m in receta)
                    {
                        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(m.Descripcionamento).Bold().FontSize(7.5f);
                        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).AlignCenter().Text($"{m.Cantidad:N0}").Bold().FontSize(7.5f);
                        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(string.IsNullOrWhiteSpace(m.Indicaciones) ? "Según prescripción médica" : m.Indicaciones).FontSize(7.2f);
                    }
                });
            }
            else
            {
                col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text("No se registraron medicamentos ambulatorios prescritos en esta atención.").Italic().FontSize(7.2f).FontColor(Colors.Grey.Darken1);
            }

            // ====================================================================
            // 5. VALIDACIÓN Y FIRMA DEL MÉDICO
            // ====================================================================
            col.Item().PaddingTop(8).Row(r =>
            {
                r.RelativeItem(1.4f).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Column(c =>
                {
                    c.Item().Text("VALIDEZ CLÍNICA E INSTITUCIONAL:").Bold().FontSize(6.8f).FontColor(Colors.Grey.Darken3);
                    c.Item().Text("Registro clínico electrónico emitido de conformidad con la ley de ejercicio veterinario y reglamentos de historia clínica. Documento válido para auditorías internas, coberturas de seguros y peritajes médicos.").FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(2).Text($"VERIFICACIÓN: ORDEN #{ficha.MedOrden} | NHCL #{ficha.MedNhcl} | e-ForVets").FontSize(6.2f).FontColor(Colors.Grey.Darken2);
                });

                r.ConstantItem(25);

                r.RelativeItem(1f).Column(c =>
                {
                    c.Item().Height(25);
                    c.Item().BorderTop(0.8f).BorderColor(Colors.Grey.Darken2).AlignCenter().Text(t =>
                    {
                        var med = !string.IsNullOrWhiteSpace(ficha.UserNombre) 
                            ? ficha.UserNombre 
                            : (!string.IsNullOrWhiteSpace(ficha.NombreMedico) ? ficha.NombreMedico : paciente.MedicoResponsable);
                        t.Span(string.IsNullOrWhiteSpace(med) ? "Médico Especialista" : med).Bold().FontSize(7.8f);
                    });
                    c.Item().AlignCenter().Text("Médico Veterinario Tratante").FontSize(7f).FontColor(Colors.Grey.Darken2);
                    c.Item().AlignCenter().Text("Firma y Sello Profesional").Italic().FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        page.Footer().Row(r =>
        {
            r.RelativeItem().Text($"e-ForVets · Historia Clínica Oficial | Orden #{ficha.MedOrden} | NHCL #{paciente.Nhcl}").FontSize(7f).FontColor(Colors.Grey.Darken1);
            r.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7f).FontColor(Colors.Grey.Darken1));
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });
    }

    // ========================================================================
    // 10. RECETA MÉDICA e-ForVets (PDF - Reemplaza RptrecetaHDEV.rpt)
    // ========================================================================
    public byte[] GenerarRecetaMedicaPdf(
        PacienteHeaderDto paciente,
        List<RecetaMedicamentoItemDto> items,
        List<DiagnosticoCie10Dto> diagnosticos,
        string indicacionesGenerales,
        string? identificador = null,
        DateTime? fechaEmision = null)
    {
        var qrBytes = GenerarCodigoQrPng($"NUMERICA-{paciente.Nhcl}-{paciente.OrdenAtencionId}");
        var referencia = string.IsNullOrWhiteSpace(identificador) ? $"ORDEN #{paciente.OrdenAtencionId}" : identificador;
        var fecha = fechaEmision ?? DateTime.Today;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"RECETA MÉDICA VETERINARIA E INDICACIONES AL PROPIETARIO — {referencia}").Bold().FontSize(10);
                        });
                        row.ConstantItem(48).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(4).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie} - {paciente.Sexo}) | NHCL: {paciente.Nhcl}").Bold();
                        r.RelativeItem().Text($"Propietario: {paciente.ClienteNombre} (C.I./RUC: {paciente.ClienteCedula})");
                        r.RelativeItem().Text($"Médico: {paciente.MedicoResponsable} | Fecha: {fecha:yyyy-MM-dd}");
                    });
                });

                page.Content().Row(row =>
                {
                    // Columna Izquierda: Rp./ Medicamentos Prescritos (Cantidades)
                    row.RelativeItem().PaddingRight(8).Border(0.5f).Padding(8).Column(col =>
                    {
                        col.Item().PaddingBottom(4).Text("Rp. / MEDICAMENTOS E INSUMOS PRESCRITOS").Bold().FontSize(10).FontColor(Colors.Blue.Darken3);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.5f);
                            });
                            t.Header(h =>
                            {
                                h.Cell().BorderBottom(1).Padding(3).Text("Medicamento / Presentación").Bold();
                                h.Cell().BorderBottom(1).Padding(3).AlignCenter().Text("Cant.").Bold();
                                h.Cell().BorderBottom(1).Padding(3).Text("En Letras").Bold();
                            });
                            foreach (var m in items)
                            {
                                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(m.Descripcionamento).Bold();
                                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text($"{m.Cantidad:N0}");
                                t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(m.CantidadLetras);
                            }
                        });

                        if (diagnosticos.Any())
                        {
                            col.Item().PaddingTop(10).Text("DIAGNÓSTICOS CIE-10:").Bold().FontSize(8.5f);
                            foreach (var d in diagnosticos)
                            {
                                col.Item().Text($"• [{d.CodigoDiag}] {d.NombreCie10}");
                            }
                        }
                    });

                    // Columna Derecha: Indicaciones de Administración y Cuidados Generales
                    row.RelativeItem().PaddingLeft(8).Border(0.5f).Padding(8).Column(col =>
                    {
                        col.Item().PaddingBottom(4).Text("INDICACIONES POSOLÓGICAS PARA EL PACIENTE").Bold().FontSize(10).FontColor(Colors.Blue.Darken3);
                        foreach (var m in items)
                        {
                            col.Item().PaddingVertical(3).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Column(c =>
                            {
                                c.Item().Text(m.Descripcionamento).Bold().FontColor(Colors.Indigo.Darken3);
                                c.Item().Text($"Indicaciones: {m.Indicaciones}");
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(indicacionesGenerales))
                        {
                            col.Item().PaddingTop(10).Background(Colors.Yellow.Lighten5).Border(0.5f).BorderColor(Colors.Yellow.Darken2).Padding(6).Column(c =>
                            {
                                c.Item().Text("INDICACIONES GENERALES Y SIGNOS DE ALARMA:").Bold();
                                c.Item().Text(indicacionesGenerales);
                            });
                        }
                    });
                });

                page.Footer().AlignCenter().Text($"Firma y sello médico tratante: {paciente.MedicoResponsable} — e-ForVets");
            });
        }).GeneratePdf();
    }

    // ========================================================================
    // 11. REPORTE DE EVOLUCIONES MÉDICAS (PDF - Reemplaza vt_EvolucionImp.aspx)
    // ========================================================================
    public byte[] GenerarEvolucionesPdf(PacienteHeaderDto paciente, List<EvolucionMedicaDto> evoluciones)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-EVOLUCION|ORDEN:{paciente.OrdenAtencionId}|NHCL:{paciente.Nhcl}");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"HOJA DE EVOLUCIÓN MÉDICA Y ÓRDENES COMPLEMENTARIAS").Bold().FontSize(10);
                        });
                        row.ConstantItem(45).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(4).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text($"Orden: #{paciente.OrdenAtencionId} | NHCL: {paciente.Nhcl}").Bold();
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie})");
                        r.RelativeItem().Text($"Propietario: {paciente.ClienteNombre}");
                    });
                });

                page.Content().Column(col =>
                {
                    foreach (var ev in evoluciones)
                    {
                        col.Item().PaddingBottom(6).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(c =>
                        {
                            c.Item().Background(Colors.Indigo.Lighten5).Padding(3).Row(r =>
                            {
                                r.RelativeItem().Text($"Evolución #{ev.IdEvolucion} — Fecha: {ev.Fecha:yyyy-MM-dd} {ev.Hora} (Orden #{ev.IdOrden})").Bold();
                                r.RelativeItem().AlignRight().Text($"Médico: {ev.NombreMedico}").Bold();
                            });
                            c.Item().PaddingTop(4).Text(ev.Descripcion);

                            if (ev.ExamenesLabAsociados.Any() || ev.ExamenesImagenAsociados.Any())
                            {
                                c.Item().PaddingTop(4).Text("Exámenes Solicitados en esta Evolución:").Bold().FontSize(8f).FontColor(Colors.Blue.Darken2);
                                foreach (var ex in ev.ExamenesLabAsociados.Concat(ev.ExamenesImagenAsociados))
                                {
                                    c.Item().Text($"  • [{ex.TipoCategoria}] {ex.DescripcionExamen} ({ex.Observaciones})").FontSize(8f);
                                }
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text("e-ForVets — Registro de Evoluciones Médicas");
            });
        }).GeneratePdf();
    }

    // ========================================================================
    // 12. CERTIFICADO DE ALTA MÉDICA E INSTRUCCIONES (PDF - qvt_formulario_alta)
    // ========================================================================
    public byte[] GenerarAltaMedicaPdf(PacienteHeaderDto paciente, FormularioAltaMedicaDto alta)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-ALTA|ORDEN:{alta.IdOrden}|NHCL:{alta.Nhcl}");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"PLAN DE ALTA HOSPITALARIA Y CUIDADOS EN CASA | FECHA: {alta.FechaIng:yyyy-MM-dd}").Bold().FontSize(10);
                        });
                        row.ConstantItem(45).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(4).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text($"Orden: #{alta.IdOrden} | NHCL: {alta.Nhcl}").Bold();
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie})");
                        r.RelativeItem().Text($"Propietario: {paciente.ClienteNombre}");
                    });
                });

                page.Content().Column(col =>
                {
                    void AddBlock(string title, string content)
                    {
                        col.Item().PaddingTop(6).PaddingBottom(2).Text(title).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Border(0.5f).Padding(6).Text(string.IsNullOrWhiteSpace(content) ? "Sin observaciones." : content);
                    }

                    AddBlock("1. CUIDADOS GENERALES EN DOMICILIO", alta.CuidadosGenerales);
                    AddBlock("2. ACTIVIDADES PERMITIDAS", alta.Permitido);
                    AddBlock("3. ACTIVIDADES RESTRINGIDAS / NO PERMITIDAS", alta.NoPermitido);
                    AddBlock("4. MANEJO NUTRICIONAL Y DIETA", alta.Dieta);
                    AddBlock("5. ESQUEMA DE MEDICACIÓN EN CASA", alta.Medicacion);
                });

                page.Footer().AlignCenter().Text($"Médico responsable del alta: {paciente.MedicoResponsable} — e-ForVets");
            });
        }).GeneratePdf();
    }

    // ========================================================================
    // 13. PEDIDO DE LABORATORIO CLÍNICO (PDF - Reemplaza rptLaboratorio.rpt)
    // ========================================================================
    public byte[] GenerarOrdenLaboratorioPdf(
        PacienteHeaderDto paciente,
        List<OrdenLaboratorioItemDto> ordenes,
        List<DiagnosticoCie10Dto> diagnosticos)
    {
        var qrBytes = GenerarCodigoQrPng($"NUMERICA-{paciente.Nhcl}-{paciente.OrdenAtencionId}");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"ORDEN DE PRUEBAS DE LABORATORIO CLÍNICO — ORDEN #{paciente.OrdenAtencionId}").Bold().FontSize(10);
                            c.Item().Text($"Fecha de Solicitud: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(46).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(4).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie} - {paciente.Sexo}) | NHCL: #{paciente.Nhcl}").Bold();
                        r.RelativeItem().Text($"Propietario: {paciente.PropietarioNombre}");
                        r.RelativeItem().Text($"Médico Solicitante: {paciente.MedicoResponsable}");
                    });
                });

                page.Content().Column(col =>
                {
                    if (diagnosticos.Any())
                    {
                        col.Item().PaddingBottom(4).Text("DIAGNÓSTICOS PRESUNTIVOS / DEFINITIVOS (CIE-10):").Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Border(0.5f).Padding(4).Column(c =>
                        {
                            foreach (var d in diagnosticos)
                            {
                                c.Item().Text($"• [{d.CodigoDiag}] {d.NombreCie10} ({d.TipoDiag}) — {d.DescripcionObservacion}");
                            }
                        });
                    }

                    col.Item().PaddingTop(6).PaddingBottom(3).Text("PRUEBAS DE LABORATORIO SOLICITADAS:").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(3.2f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(2.2f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).Text("Código").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).Text("Prueba / Perfil de Laboratorio").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).AlignCenter().Text("Prioridad").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).AlignCenter().Text("Cant.").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).AlignCenter().Text("Estado").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Indigo.Lighten4).Padding(3).Text("Observación / Muestra").Bold();
                        });

                        foreach (var item in ordenes)
                        {
                            string bg = item.Prioridad == "Emergente" ? "#FEF2F2" : "#FFFFFF";
                            t.Cell().Border(0.5f).Background(bg).Padding(3).Text(item.CodigoProd);
                            t.Cell().Border(0.5f).Background(bg).Padding(3).Text(item.DescripcionExamen).Bold();
                            t.Cell().Border(0.5f).Background(bg).Padding(3).AlignCenter().Text(item.Prioridad);
                            t.Cell().Border(0.5f).Background(bg).Padding(3).AlignCenter().Text($"{item.Cantidad}");
                            t.Cell().Border(0.5f).Background(bg).Padding(3).AlignCenter().Text(item.Estado);
                            t.Cell().Border(0.5f).Background(bg).Padding(3).Text(item.Observaciones);
                        }
                    });
                });

                page.Footer().AlignCenter().Text($"Firma médico solicitante: {paciente.MedicoResponsable} — Laboratorio clínico e-ForVets");
            });
        }).GeneratePdf();
    }

    // ========================================================================
    // 14. PEDIDO DE IMAGENOLOGÍA RX / ECOGRAFÍA (PDF - Reemplaza rptRxImagen.rpt)
    // ========================================================================
    public byte[] GenerarOrdenImagenPdf(
        PacienteHeaderDto paciente,
        List<OrdenImagenItemDto> ordenes,
        List<DiagnosticoCie10Dto> diagnosticos)
    {
        var qrBytes = GenerarCodigoQrPng($"NUMERICA-{paciente.Nhcl}-{paciente.OrdenAtencionId}");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"ORDEN DE ESTUDIOS DE IMAGENOLOGÍA (RAYOS X / ECOGRAFÍA) — ORDEN #{paciente.OrdenAtencionId}").Bold().FontSize(10);
                            c.Item().Text($"Fecha de Solicitud: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(46).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(4).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie} - {paciente.Sexo}) | NHCL: #{paciente.Nhcl}").Bold();
                        r.RelativeItem().Text($"Propietario: {paciente.PropietarioNombre}");
                        r.RelativeItem().Text($"Médico Solicitante: {paciente.MedicoResponsable}");
                    });
                });

                page.Content().Column(col =>
                {
                    if (diagnosticos.Any())
                    {
                        col.Item().PaddingBottom(4).Text("DIAGNÓSTICOS CIE-10 ASOCIADOS:").Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Border(0.5f).Padding(4).Column(c =>
                        {
                            foreach (var d in diagnosticos)
                            {
                                c.Item().Text($"• [{d.CodigoDiag}] {d.NombreCie10} ({d.TipoDiag}) — {d.DescripcionObservacion}");
                            }
                        });
                    }

                    col.Item().PaddingTop(6).PaddingBottom(3).Text("ESTUDIOS RADIOLÓGICOS / ECOGRÁFICOS SOLICITADOS:").Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(3.5f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(2.6f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(3).Text("Código").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(3).Text("Estudio de Imagenología").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(3).AlignCenter().Text("Cant.").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(3).AlignCenter().Text("Estado").Bold();
                            h.Cell().Border(0.5f).Background(Colors.Cyan.Lighten4).Padding(3).Text("Proyecciones / Región Anatómica / Obs.").Bold();
                        });

                        foreach (var item in ordenes)
                        {
                            t.Cell().Border(0.5f).Padding(3).Text(item.CodigoProd);
                            t.Cell().Border(0.5f).Padding(3).Text(item.DescripcionEstudio).Bold();
                            t.Cell().Border(0.5f).Padding(3).AlignCenter().Text($"{item.Cantidad}");
                            t.Cell().Border(0.5f).Padding(3).AlignCenter().Text(item.Estado);
                            t.Cell().Border(0.5f).Padding(3).Text(item.Observaciones);
                        }
                    });
                });

                page.Footer().AlignCenter().Text($"Firma médico solicitante: {paciente.MedicoResponsable} — Servicio de imagenología e-ForVets");
            });
        }).GeneratePdf();
    }

    // ========================================================================
    // 15. INFORME DE RESULTADOS DIAGNÓSTICOS (PDF - Laboratorio / RX / Eco / Endo)
    // ========================================================================
    public byte[] GenerarInformeDiagnosticoPdf(PacienteHeaderDto paciente, InformeDiagnosticoDto informe)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-INFORME:{informe.TipoInforme}|ID:{informe.IdInforme}|NHCL:{informe.Nhcl}");
        string textoPlano = System.Text.RegularExpressions.Regex.Replace(
            (informe.ContenidoHtml ?? "")
                .Replace("<br>", "\n").Replace("<br/>", "\n").Replace("</p>", "\n\n").Replace("</h4>", "\n").Replace("</h5>", "\n"),
            "<.*?>", string.Empty);
        textoPlano = System.Net.WebUtility.HtmlDecode(textoPlano).Trim();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"INFORME OFICIAL DE {informe.TipoInforme.ToUpper()} — {informe.NombreFormato}").Bold().FontSize(10);
                            c.Item().Text($"Fecha de Emisión: {informe.FechaMod:yyyy-MM-dd HH:mm}").FontSize(8);
                        });
                        row.ConstantItem(46).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(4).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text($"Paciente: {paciente.PacienteNombre} ({paciente.Especie}) | NHCL: #{informe.Nhcl}").Bold();
                        r.RelativeItem().Text($"Propietario: {paciente.PropietarioNombre}");
                        r.RelativeItem().Text($"Especialista: {informe.NombreUsuario}");
                    });
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(informe.RutaArchivo))
                    {
                        col.Item().PaddingBottom(6).Background(Colors.Grey.Lighten4).Padding(4)
                            .Text($"Archivo Adjunto Vinculado: {informe.RutaArchivo}").FontSize(8.5f).Italic();
                    }

                    col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(10)
                        .Text(string.IsNullOrWhiteSpace(textoPlano) ? "Ver documento adjunto asociado al informe." : textoPlano);
                });

                page.Footer().AlignCenter().Text($"Validado por: {informe.NombreUsuario} — e-ForVets");
            });
        }).GeneratePdf();
    }

    // ========================================================================
    // 16. EXPORTACIÓN EXCEL DE BANDEJA DE LABORATORIO E IMAGENOLOGÍA
    // ========================================================================
    public byte[] GenerarBandejaLaboratorioExcel(List<BandejaTrabajoDiagnosticoDto> bandeja, DateTime desde, DateTime hasta)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Bandeja Diagnóstica");

        ws.Cell(1, 1).Value = $"e-ForVets - BANDEJA CENTRAL DE LABORATORIO E IMAGENOLOGÍA ({desde:yyyy-MM-dd} al {hasta:yyyy-MM-dd})";
        ws.Range(1, 1, 1, 10).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A")).Font.SetFontColor(XLColor.White);

        string[] headers = ["Sec", "Área", "Fecha", "Orden", "NHCL", "Paciente", "Propietario", "Examen / Estudio", "Prioridad", "Estado", "Médico Solicitante", "Observaciones"];
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(3, i + 1).Value = headers[i];
        ws.Range(3, 1, 3, headers.Length).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#DBEAFE"));

        int row = 4;
        foreach (var item in bandeja)
        {
            ws.Cell(row, 1).Value = item.Secuencial;
            ws.Cell(row, 2).Value = item.AreaOrigen;
            ws.Cell(row, 3).Value = item.FechaTexto;
            ws.Cell(row, 4).Value = item.IdOrden;
            ws.Cell(row, 5).Value = item.Nhcl;
            ws.Cell(row, 6).Value = item.Paciente;
            ws.Cell(row, 7).Value = item.Cliente;
            ws.Cell(row, 8).Value = item.Examen;
            ws.Cell(row, 9).Value = item.Prioridad;
            ws.Cell(row, 10).Value = item.Estado;
            ws.Cell(row, 11).Value = item.Medico;
            ws.Cell(row, 12).Value = item.Observaciones;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ========================================================================
    // 17. PROFORMA CONSOLIDADA MULTI-MÓDULO (PDF & EXCEL)
    // ========================================================================
    public byte[] GenerarProformaConsolidadaPdf(OrdenProformaResumenDto proforma)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-PROFORMA:{proforma.NumeroFacturaBc}|ORDEN:{proforma.IdOrden}|NHCL:{proforma.Nhcl}|TOTAL:{proforma.TotalGeneral:F2}");
        var itemsActivos = proforma.Items.Where(i => i.Seleccionado).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                            c.Item().Text($"ESTADO DE CUENTA / PROFORMA CONSOLIDADA — {proforma.NumeroFacturaBc}").Bold().FontSize(10);
                            c.Item().Text($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(48).Image(qrBytes);
                    });

                    col.Item().PaddingVertical(4).Background(Colors.Blue.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(6).Column(info =>
                    {
                        info.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"Orden Atención: #{proforma.IdOrden} | NHCL: #{proforma.Nhcl}").Bold();
                            r.RelativeItem().Text($"Paciente: {proforma.MascotaNombre} ({proforma.Especie})").Bold();
                        });
                        info.Item().PaddingTop(2).Row(r =>
                        {
                            r.RelativeItem().Text($"Cliente / Razón Social: {proforma.ClienteNombre}");
                            r.RelativeItem().Text($"Cédula / RUC: {proforma.ClienteCedula} | Tel: {proforma.ClienteTelefono}");
                        });
                    });
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.3f);
                            c.RelativeColumn(3.6f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1.2f);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(3).Text("Módulo Origen").Bold();
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(3).Text("Código").Bold();
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(3).Text("Descripción del Servicio / Insumo").Bold();
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(3).AlignRight().Text("Cant.").Bold();
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(3).AlignRight().Text("P. Unit.").Bold();
                            h.Cell().Border(0.5f).Background(Colors.BlueGrey.Lighten4).Padding(3).AlignRight().Text("Subtotal").Bold();
                        });

                        foreach (var item in itemsActivos)
                        {
                            t.Cell().Border(0.5f).Padding(3).Text(item.OrigenModulo).FontSize(8);
                            t.Cell().Border(0.5f).Padding(3).Text(item.CodigoItemBc).FontSize(8);
                            t.Cell().Border(0.5f).Padding(3).Text(item.DescripcionItem);
                            t.Cell().Border(0.5f).Padding(3).AlignRight().Text($"{item.Cantidad:N2}");
                            t.Cell().Border(0.5f).Padding(3).AlignRight().Text($"${item.PrecioUnitario:N2}");
                            t.Cell().Border(0.5f).Padding(3).AlignRight().Text($"${item.Subtotal:N2}").Bold();
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Width(240).Border(1).BorderColor(Colors.Blue.Darken2).Background(Colors.Grey.Lighten5).Padding(8).Column(res =>
                    {
                        res.Item().Row(r => { r.RelativeItem().Text("Subtotal Hospitalización:"); r.ConstantItem(80).AlignRight().Text($"${proforma.TotalHospitalizacion:N2}"); });
                        res.Item().Row(r => { r.RelativeItem().Text("Subtotal Quirófano / Anestesia:"); r.ConstantItem(80).AlignRight().Text($"${proforma.TotalQuirofano:N2}"); });
                        res.Item().Row(r => { r.RelativeItem().Text("Subtotal Laboratorio Clínico:"); r.ConstantItem(80).AlignRight().Text($"${proforma.TotalLaboratorio:N2}"); });
                        res.Item().Row(r => { r.RelativeItem().Text("Subtotal Imagenología:"); r.ConstantItem(80).AlignRight().Text($"${proforma.TotalImagen:N2}"); });
                        res.Item().PaddingVertical(3).LineHorizontal(0.5f);
                        res.Item().Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL PROFORMA USD:").Bold().FontSize(10).FontColor(Colors.Blue.Darken3);
                            r.ConstantItem(80).AlignRight().Text($"${proforma.TotalGeneral:N2}").Bold().FontSize(10).FontColor(Colors.Blue.Darken3);
                        });
                    });
                });

                page.Footer().AlignCenter().Text($"e-ForVets — Documento de pre-facturación ({proforma.NumeroFacturaBc})");
            });
        }).GeneratePdf();
    }

    public byte[] GenerarProformaConsolidadaExcel(OrdenProformaResumenDto proforma)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Proforma_{proforma.IdOrden}");

        ws.Cell(1, 1).Value = $"e-ForVets - PROFORMA CONSOLIDADA ({proforma.NumeroFacturaBc})";
        ws.Range(1, 1, 1, 6).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#1E3A8A")).Font.SetFontColor(XLColor.White);

        ws.Cell(2, 1).Value = $"Orden: #{proforma.IdOrden} | NHCL: #{proforma.Nhcl} | Paciente: {proforma.MascotaNombre} | Cliente: {proforma.ClienteNombre} ({proforma.ClienteCedula})";
        ws.Range(2, 1, 2, 6).Merge().Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#DBEAFE"));

        string[] headers = ["Módulo Origen", "Código", "Descripción Servicio / Insumo", "Cantidad", "Precio Unitario", "Subtotal USD"];
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(4, i + 1).Value = headers[i];
        ws.Range(4, 1, 4, 6).Style.Font.SetBold(true).Fill.SetBackgroundColor(XLColor.FromHtml("#E2E8F0"));

        int row = 5;
        foreach (var item in proforma.Items.Where(i => i.Seleccionado))
        {
            ws.Cell(row, 1).Value = item.OrigenModulo;
            ws.Cell(row, 2).Value = item.CodigoItemBc;
            ws.Cell(row, 3).Value = item.DescripcionItem;
            ws.Cell(row, 4).Value = item.Cantidad;
            ws.Cell(row, 5).Value = item.PrecioUnitario;
            ws.Cell(row, 6).Value = item.Subtotal;
            row++;
        }

        ws.Cell(row + 1, 5).Value = "TOTAL USD:";
        ws.Cell(row + 1, 5).Style.Font.SetBold(true);
        ws.Cell(row + 1, 6).Value = proforma.TotalGeneral;
        ws.Cell(row + 1, 6).Style.Font.SetBold(true);

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ========================================================================
    // 18. AGENDA DE CITAS MÉDICAS (PDF)
    // ========================================================================
    public byte[] GenerarAgendaCitasPdf(List<CitaMedicaDto> citas, DateTime desde, DateTime hasta)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                    col.Item().Text($"REPORTE DE AGENDA DE CITAS MÉDICAS ({desde:yyyy-MM-dd} al {hasta:yyyy-MM-dd})").Bold().FontSize(10);
                    col.Item().PaddingVertical(3).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                });

                page.Content().PaddingTop(6).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.1f);
                        c.RelativeColumn(0.8f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(2.2f);
                        c.RelativeColumn(1.3f);
                        c.RelativeColumn(2.0f);
                        c.RelativeColumn(2.5f);
                        c.RelativeColumn(1.0f);
                    });

                    t.Header(h =>
                    {
                        string[] cols = ["Fecha", "Hora", "Mascota (NHCL)", "Propietario", "Teléfono", "Médico Asignado", "Motivo / Observación", "Estado"];
                        foreach (var colName in cols)
                            h.Cell().Border(0.5f).Background(Colors.Blue.Lighten4).Padding(3).Text(colName).Bold();
                    });

                    foreach (var c in citas)
                    {
                        t.Cell().Border(0.5f).Padding(3).Text($"{c.CiFecha:yyyy-MM-dd}");
                        t.Cell().Border(0.5f).Padding(3).AlignCenter().Text(c.CiHora).Bold();
                        t.Cell().Border(0.5f).Padding(3).Text($"{c.CiNombreMas} (#{c.CiNhcl})");
                        t.Cell().Border(0.5f).Padding(3).Text(c.CiNombre);
                        t.Cell().Border(0.5f).Padding(3).Text(c.CiTelefono);
                        t.Cell().Border(0.5f).Padding(3).Text(c.NombreMedico);
                        t.Cell().Border(0.5f).Padding(3).Text(c.CiObservacion);
                        t.Cell().Border(0.5f).Padding(3).AlignCenter().Text(c.CiEstadoCita switch
                        {
                            "C" => "Confirmada",
                            "A" => "Atendida",
                            "X" => "Cancelada",
                            _ => "Agendada"
                        });
                    }
                });

                page.Footer().AlignCenter().Text("e-ForVets — Agenda médica institucional");
            });
        }).GeneratePdf();
    }

    public byte[] GenerarPartesOperatoriosPdf(List<ParteOperatorioResumenDto> partes, DateTime desde, DateTime hasta)
    {
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(x => x.FontSize(8));
            page.Header().Column(column =>
            {
                column.Item().Text("e-ForVets — Gestión veterinaria").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                column.Item().Text($"PARTE OPERATORIO CONSOLIDADO · {desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}").Bold().FontSize(14);
                column.Item().Text($"Registros: {partes.Count}").FontColor(Colors.Grey.Darken1);
            });
            page.Content().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(65);
                    columns.ConstantColumn(52);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.4f);
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(2.2f);
                });
                table.Header(header =>
                {
                    foreach (var title in new[] { "Fecha", "Orden / NHCL", "Paciente", "Propietario", "Procedimiento", "Diagnóstico / hallazgos" })
                        header.Cell().Background(Colors.Blue.Lighten4).Border(.5f).Padding(4).Text(title).Bold();
                });
                foreach (var parte in partes)
                {
                    foreach (var value in new[]
                    {
                        parte.Fecha.ToString("dd/MM/yyyy HH:mm"), $"{parte.IdOrden} / {parte.Nhcl}", parte.Paciente,
                        parte.Propietario, parte.Procedimiento, parte.Diagnostico
                    })
                        table.Cell().Border(.5f).Padding(4).Text(value ?? string.Empty);
                }
            });
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("e-ForVets · Generado ");
                text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            });
        })).GeneratePdf();
    }

    public byte[] GenerarIStatPdf(PacienteHeaderDto paciente, IStatDto istat, string? nombreMedico = null)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-ISTAT|NHCL:{istat.Nhcl}|ID:{istat.Id}|FECHA:{istat.Fecha:yyyyMMddHHmm}");
        var medicoTratante = !string.IsNullOrWhiteSpace(nombreMedico) 
            ? nombreMedico 
            : (!string.IsNullOrWhiteSpace(istat.QuienIngresa) ? istat.QuienIngresa : "Médico Veterinario Tratante");
        var especialidad = !string.IsNullOrWhiteSpace(istat.Especialidad) ? istat.Especialidad : "Medicina Veterinaria / Laboratorio";

        var panelGases = new List<(string Param, string Abbr, string Val, string Unit, string RefCan, string RefFel)>
        {
            ("Potencial de Hidrógeno", "pH", istat.Ph, "pH", "7.35 - 7.45", "7.35 - 7.45"),
            ("Presión Parcial CO2", "pCO2", istat.Pressure, "mmHg", "35.0 - 45.0", "35.0 - 45.0"),
            ("Presión Parcial O2", "pO2", istat.Oxygen, "mmHg", "80.0 - 100.0", "80.0 - 100.0"),
            ("Bicarbonato Actual", "HCO3", istat.Bicarbonato, "mmol/L", "18.0 - 24.0", "18.0 - 24.0"),
            ("Dióxido de Carbono Total", "TCO2", istat.Carbon, "mmol/L", "19.0 - 25.0", "19.0 - 25.0"),
            ("Exceso de Base en LEC", "BE ecf", istat.Base, "mmol/L", "-4.0 a +4.0", "-4.0 a +4.0"),
            ("Saturación de Oxígeno", "sO2", istat.Saturation, "%", "95.0 - 100.0", "95.0 - 100.0"),
            ("Lactato", "Lac", istat.Lactato, "mmol/L", "< 2.50", "< 2.50")
        };

        var panelElectrolitos = new List<(string Param, string Abbr, string Val, string Unit, string RefCan, string RefFel)>
        {
            ("Sodio Sérico", "Na+", istat.Sodio, "mmol/L", "140.0 - 154.0", "146.0 - 158.0"),
            ("Potasio Sérico", "K+", istat.Potasio, "mmol/L", "3.8 - 5.3", "3.7 - 5.3"),
            ("Cloruro", "Cl-", istat.Chloride, "mmol/L", "105.0 - 120.0", "112.0 - 126.0"),
            ("Calcio Ionizado", "iCa", istat.Ionized, "mmol/L", "1.12 - 1.40", "1.12 - 1.40"),
            ("Anión Gap", "AnGap", istat.Gap, "mmol/L", "12.0 - 24.0", "13.0 - 27.0")
        };

        var panelBioquimica = new List<(string Param, string Abbr, string Val, string Unit, string RefCan, string RefFel)>
        {
            ("Hematocrito Estimado", "Hto", istat.Hematocrito, "%", "37.0 - 55.0", "30.0 - 45.0"),
            ("Hemoglobina Calculada", "Hb", istat.Hemoglobina, "g/dL", "12.0 - 18.0", "8.0 - 15.0"),
            ("Glucosa Sanguínea", "Glu", istat.Glucosa, "mg/dL", "70.0 - 140.0", "75.0 - 150.0"),
            ("Nitrógeno Ureico / Urea", "BUN", istat.Urea, "mg/dL", "7.0 - 27.0", "15.0 - 34.0"),
            ("Creatinina Sérica", "Crea", istat.Creatinina, "mg/dL", "0.5 - 1.5", "0.6 - 1.8")
        };

        bool esFelino = (paciente.Especie ?? "").IndexOf("Fel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (paciente.Especie ?? "").IndexOf("Gato", StringComparison.OrdinalIgnoreCase) >= 0;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("HOSPITAL DE ESPECIALIDADES VETERINARIAS - e-ForVets").Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                            c.Item().Text("REPORTE OFICIAL DE GASOMETRÍA Y ELECTROLITOS CLÍNICOS (i-STAT)").Bold().FontSize(10.5f).FontColor(Colors.Grey.Darken3);
                            c.Item().Text($"Fecha / Hora del Análisis: {istat.Fecha:dd/MM/yyyy HH:mm}   |   Registro #{istat.Id}").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(50).Image(qrBytes);
                    });

                    col.Item().PaddingTop(4).Background(Colors.Blue.Lighten5).Border(0.6f).BorderColor(Colors.Blue.Lighten3).Padding(6).Row(r =>
                    {
                        r.RelativeItem(2.2f).Column(c =>
                        {
                            c.Item().Text(t => { t.Span("Paciente: ").Bold(); t.Span(paciente.PacienteNombre); });
                            c.Item().Text(t => { t.Span("Especie / Raza: ").Bold(); t.Span($"{paciente.Especie} / {paciente.Raza}"); });
                            c.Item().Text(t => { t.Span("NHCL: ").Bold(); t.Span(paciente.Nhcl.ToString()); });
                        });
                        r.RelativeItem(2.2f).Column(c =>
                        {
                            c.Item().Text(t => { t.Span("Propietario: ").Bold(); t.Span(paciente.PropietarioNombre); });
                            c.Item().Text(t => { t.Span("Sexo / Edad: ").Bold(); t.Span($"{paciente.Sexo} / {paciente.EdadTexto}"); });
                            c.Item().Text(t => { t.Span("Orden de Atención: ").Bold(); t.Span(paciente.OrdenAtencionId > 0 ? $"#{paciente.OrdenAtencionId}" : "Ambulatorio / Consulta"); });
                        });
                        r.RelativeItem(2f).Column(c =>
                        {
                            c.Item().Text(t => { t.Span("Responsable: ").Bold(); t.Span(medicoTratante); });
                            c.Item().Text(t => { t.Span("Especialidad: ").Bold(); t.Span(especialidad); });
                            c.Item().Text(t => { t.Span("Tipo Muestra: ").Bold(); t.Span("Sangre Total Heparinizada"); });
                        });
                    });
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    void RenderPanel(string titulo, List<(string Param, string Abbr, string Val, string Unit, string RefCan, string RefFel)> items, string colorHex)
                    {
                        col.Item().PaddingTop(6).Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(6).Text(titulo).Bold().FontSize(9f).FontColor(Colors.Blue.Darken4);
                        
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2.6f);
                                c.ConstantColumn(46);
                                c.ConstantColumn(70);
                                c.ConstantColumn(55);
                                c.RelativeColumn(1.8f);
                                c.RelativeColumn(1.8f);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten4).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(3).Text("Parámetro / Analito").Bold().FontSize(8f);
                                h.Cell().Background(Colors.Grey.Lighten4).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(3).AlignCenter().Text("Abrev.").Bold().FontSize(8f);
                                h.Cell().Background(Colors.Grey.Lighten4).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(3).AlignCenter().Text("Resultado").Bold().FontSize(8f);
                                h.Cell().Background(Colors.Grey.Lighten4).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(3).AlignCenter().Text("Unidades").Bold().FontSize(8f);
                                h.Cell().Background(esFelino ? Colors.Grey.Lighten4 : Colors.Blue.Lighten5).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(3).AlignCenter().Text("Ref. Canino").Bold().FontSize(8f);
                                h.Cell().Background(esFelino ? Colors.Blue.Lighten5 : Colors.Grey.Lighten4).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(3).AlignCenter().Text("Ref. Felino").Bold().FontSize(8f);
                            });

                            foreach (var it in items)
                            {
                                bool tieneValor = !string.IsNullOrWhiteSpace(it.Val);
                                var bgRow = tieneValor ? Colors.White : Colors.Grey.Lighten5;

                                table.Cell().Background(bgRow).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(it.Param).FontSize(8f);
                                table.Cell().Background(bgRow).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(it.Abbr).Bold().FontSize(8f);
                                table.Cell().Background(bgRow).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(tieneValor ? it.Val : "---").Bold().FontSize(9f).FontColor(tieneValor ? Colors.Blue.Darken3 : Colors.Grey.Medium);
                                table.Cell().Background(bgRow).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(it.Unit).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                                table.Cell().Background(esFelino ? bgRow : Colors.Blue.Lighten5).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(it.RefCan).FontSize(7.5f);
                                table.Cell().Background(esFelino ? Colors.Blue.Lighten5 : bgRow).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(it.RefFel).FontSize(7.5f);
                            }
                        });
                    }

                    RenderPanel("1. GASES SANGUÍNEOS Y ESTADO ÁCIDO - BASE", panelGases, "#0284c7");
                    RenderPanel("2. ELECTROLITOS Y MINERALES", panelElectrolitos, "#059669");
                    RenderPanel("3. PARÁMETROS HEMATOLÓGICOS Y BIOQUÍMICOS", panelBioquimica, "#d97706");

                    col.Item().PaddingTop(12).Row(r =>
                    {
                        r.RelativeItem(1.4f).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c =>
                        {
                            c.Item().Text("NOTAS METODOLÓGICAS:").Bold().FontSize(7.5f);
                            c.Item().Text("• Cartucho Point-of-Care i-STAT (Abbott).").FontSize(7f).FontColor(Colors.Grey.Darken2);
                            c.Item().Text("• Los rangos fisiológicos de referencia aplican a pacientes caninos y felinos en reposo normotérmico a nivel de mar/moderada altitud.").FontSize(7f).FontColor(Colors.Grey.Darken2);
                            c.Item().Text("• Todo valor anómalo debe interpretarse en conjunción con el cuadro clínico y signos vitales del paciente.").FontSize(7f).FontColor(Colors.Grey.Darken2);
                        });

                        r.ConstantItem(25);

                        r.RelativeItem(1f).Column(c =>
                        {
                            c.Item().Height(30);
                            c.Item().BorderTop(0.8f).BorderColor(Colors.Grey.Darken2).AlignCenter().Text(medicoTratante).Bold().FontSize(8.5f);
                            c.Item().AlignCenter().Text(especialidad).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                            c.Item().AlignCenter().Text("Firma y Sello Responsable").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                page.Footer().Row(r =>
                {
                    r.RelativeItem().Text("Sistema de Gestión e-ForVets • Reporte Oficial i-STAT").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    r.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }
    // ========================================================================
    // IMPRESIÓN MASIVA DE FICHAS MÉDICAS POR LOTES / SEGUROS (vt_imprimirfichas.aspx)
    // ========================================================================
    public byte[] GenerarFichasMedicasLotePdf(PacienteHeaderDto paciente, List<FichaLoteItemDto> lote)
    {
        var qrBytes = GenerarCodigoQrPng($"EFORVETS-LOTE-FICHAS|NHCL:{paciente.Nhcl}|TOTAL:{lote.Count}|FECHA:{DateTime.Now:yyyyMMdd}");

        return Document.Create(container =>
        {
            // 1. PÁGINA DE RESUMEN EJECUTIVO (PORTADA PARA AUDITORÍA Y SEGUROS)
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Lato"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("HOSPITAL DE ESPECIALIDADES VETERINARIAS - e-ForVets")
                                .Bold().FontSize(13f).FontColor(Colors.Blue.Darken4);
                            c.Item().Text("EXPEDIENTE CLÍNICO CONSOLIDADO — AUDITORÍA Y RECLAMO DE SEGUROS")
                                .Bold().FontSize(10f).FontColor(Colors.Blue.Darken2);
                            c.Item().Text($"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm}   |   Expediente Paciente NHCL #{paciente.Nhcl}")
                                .FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(50).AlignRight().Column(colQr =>
                        {
                            colQr.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Image(qrBytes);
                            colQr.Item().AlignCenter().Text($"EXP #{paciente.Nhcl}").Bold().FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                        });
                    });

                    // Ficha del Paciente y Tutor
                    col.Item().PaddingTop(4).Background(Colors.Blue.Lighten5).Border(0.6f).BorderColor(Colors.Blue.Lighten3).Padding(5).Row(r =>
                    {
                        r.RelativeItem(2.2f).Column(c =>
                        {
                            c.Item().Text(t => { t.Span("PACIENTE: ").Bold().FontSize(7.5f).FontColor(Colors.Blue.Darken4); t.Span(paciente.PacienteNombre).Bold().FontSize(8.5f); });
                            c.Item().Text(t => { t.Span("NHCL: ").Bold().FontSize(7.5f); t.Span($"#{paciente.Nhcl}").Bold().FontColor(Colors.Blue.Darken3); });
                            c.Item().Text(t => { t.Span("Especie / Raza: ").Bold().FontSize(7.5f); t.Span($"{paciente.Especie} / {paciente.Raza}").FontSize(7.5f); });
                            c.Item().Text(t => { t.Span("Sexo / Edad: ").Bold().FontSize(7.5f); t.Span($"{paciente.Sexo} / {paciente.EdadTexto}").FontSize(7.5f); });
                        });

                        r.RelativeItem(2.2f).Column(c =>
                        {
                            c.Item().Text("TUTOR / ASEGURADO:").Bold().FontSize(7.5f).FontColor(Colors.Blue.Darken4);
                            c.Item().Text(string.IsNullOrWhiteSpace(paciente.ClienteNombre) ? paciente.PropietarioNombre : paciente.ClienteNombre).Bold().FontSize(8f);
                            c.Item().Text(t => { t.Span("Cédula / RUC: ").Bold().FontSize(7.5f); t.Span(string.IsNullOrWhiteSpace(paciente.ClienteCedula) ? "Sin registrar" : paciente.ClienteCedula).FontSize(7.5f); });
                            c.Item().Text(t => { t.Span("Origen: ").Bold().FontSize(7.5f); t.Span(paciente.Origen).FontSize(7.5f); });
                        });

                        r.RelativeItem(2f).Column(c =>
                        {
                            c.Item().Text("DATOS DEL EXPEDIENTE:").Bold().FontSize(7.5f).FontColor(Colors.Blue.Darken4);
                            c.Item().Text(t => { t.Span("Atenciones Seleccionadas: ").Bold().FontSize(7.5f); t.Span($"{lote.Count} consulta(s)").Bold().FontColor(Colors.Blue.Darken3); });
                            c.Item().Text(t => { t.Span("Uso del Documento: ").Bold().FontSize(7.5f); t.Span("Auditoría / Seguros").FontSize(7.5f); });
                            c.Item().Text(t => { t.Span("Estado: ").Bold().FontSize(7.5f); t.Span("Certificado y Auditado").Bold().FontSize(7.5f).FontColor(Colors.Green.Darken3); });
                        });
                    });
                });

                page.Content().PaddingTop(6).Column(col =>
                {
                    // Métricas resumidas KPI
                    col.Item().PaddingBottom(4).Row(r =>
                    {
                        var minFec = lote.Any() ? lote.Min(x => x.Ficha.MedFecha) : DateTime.Now;
                        var maxFec = lote.Any() ? lote.Max(x => x.Ficha.MedFecha) : DateTime.Now;
                        var totalDiags = lote.Sum(x => x.Diagnosticos.Count);
                        var totalMedicamentos = lote.Sum(x => x.Receta.Count);

                        void AddKpi(string tit, string val)
                        {
                            r.RelativeItem().Background(Colors.Grey.Lighten5).Border(0.5f).BorderColor(Colors.Blue.Lighten3).Padding(3.5f).Column(c =>
                            {
                                c.Item().AlignCenter().Text(tit).Bold().FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                                c.Item().AlignCenter().Text(val).Bold().FontSize(9f).FontColor(Colors.Blue.Darken4);
                            });
                        }

                        AddKpi("TOTAL CONSULTAS", $"{lote.Count}");
                        AddKpi("DESDE", minFec.ToString("dd/MM/yyyy"));
                        AddKpi("HASTA", maxFec.ToString("dd/MM/yyyy"));
                        AddKpi("DIAGNÓSTICOS", $"{totalDiags}");
                        AddKpi("MEDICAMENTOS", $"{totalMedicamentos}");
                    });

                    col.Item().PaddingTop(4).PaddingBottom(2).Text("RESUMEN CRONOLÓGICO DE ATENCIONES SELECCIONADAS").Bold().FontSize(8.5f).FontColor(Colors.Blue.Darken4);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(20);    // #
                            c.ConstantColumn(75);    // Fecha
                            c.ConstantColumn(45);    // Orden
                            c.RelativeColumn(1.8f);  // Médico
                            c.RelativeColumn(2.5f);  // Motivo
                            c.RelativeColumn(3f);    // Diagnósticos CIE-10
                            c.ConstantColumn(65);    // Constantes (Peso/Temp)
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).AlignCenter().Text("#").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                            h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).Text("FECHA").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                            h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).AlignCenter().Text("ORDEN").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                            h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).Text("MÉDICO").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                            h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).Text("MOTIVO DE CONSULTA").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                            h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).Text("DIAGNÓSTICOS CIE-10").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                            h.Cell().Background(Colors.Blue.Lighten4).Border(0.5f).BorderColor(Colors.Blue.Lighten2).Padding(2.5f).AlignCenter().Text("CONSTANTES").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                        });

                        int index = 1;
                        foreach (var it in lote.OrderBy(x => x.Ficha.MedFecha))
                        {
                            var bg = index % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;
                            var diags = it.Diagnosticos.Any()
                                ? string.Join("; ", it.Diagnosticos.Select(d => $"[{d.CodigoDiag}] {d.NombreCie10}"))
                                : "Sin diagnósticos registrados";
                            var vit = $"{(it.Ficha.Peso > 0 ? it.Ficha.Peso.ToString("N1") + "kg" : "-")} | {(it.Ficha.Temperatura > 0 ? it.Ficha.Temperatura.ToString("N1") + "°C" : "-")}";

                            table.Cell().Background(bg).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).AlignCenter().Text(index.ToString()).FontSize(7f);
                            table.Cell().Background(bg).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(it.Ficha.MedFecha.ToString("dd/MM/yyyy HH:mm")).FontSize(7f);
                            table.Cell().Background(bg).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).AlignCenter().Text($"#{it.Ficha.MedOrden}").Bold().FontSize(7f).FontColor(Colors.Blue.Darken3);
                            table.Cell().Background(bg).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(string.IsNullOrWhiteSpace(it.Ficha.UserNombre) ? "Médico Tratante" : it.Ficha.UserNombre).FontSize(7f);
                            table.Cell().Background(bg).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(it.Ficha.MedMotivo ?? "—").FontSize(7f);
                            table.Cell().Background(bg).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(diags).FontSize(6.8f);
                            table.Cell().Background(bg).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).AlignCenter().Text(vit).FontSize(6.8f);
                            index++;
                        }
                    });

                    // Bloque de certificación institucional
                    col.Item().PaddingTop(10).Row(r =>
                    {
                        r.RelativeItem(1.4f).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c =>
                        {
                            c.Item().Text("CERTIFICACIÓN CLÍNICA INSTITUCIONAL PARA AUDITORÍAS Y SEGUROS:").Bold().FontSize(7f).FontColor(Colors.Blue.Darken4);
                            c.Item().Text("Se certifica que la información constante en el presente expediente consolidado corresponde fielmente a los registros médicos electrónicos archivados en el sistema e-ForVets del Hospital Veterinario. El presente reporte es válido para trámites de cobertura ante compañías aseguradoras, peritajes y auditorías clínicas.").FontSize(6.8f).FontColor(Colors.Grey.Darken2);
                        });

                        r.ConstantItem(25);

                        r.RelativeItem(0.9f).Column(c =>
                        {
                            c.Item().Height(30);
                            c.Item().BorderTop(0.8f).BorderColor(Colors.Grey.Darken2).AlignCenter().Text("Dirección Médica / Auditoría").Bold().FontSize(7.5f);
                            c.Item().AlignCenter().Text("Hospital de Especialidades Veterinarias").FontSize(7f).FontColor(Colors.Grey.Darken1);
                            c.Item().AlignCenter().Text("Firma y Sello Institucional").Italic().FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });

                page.Footer().Row(r =>
                {
                    r.RelativeItem().Text($"e-ForVets — Expediente Clínico Consolidado | NHCL: {paciente.Nhcl}").FontSize(7f).FontColor(Colors.Grey.Darken1);
                    r.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7f).FontColor(Colors.Grey.Darken1));
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" de ");
                        t.TotalPages();
                    });
                });
            });

            // 2. PÁGINAS DE DETALLE COMPLETO SOAP PARA CADA ATENCIÓN
            foreach (var item in lote.OrderBy(x => x.Ficha.MedFecha))
            {
                var f = item.Ficha;
                var qrItem = GenerarCodigoQrPng($"EFORVETS-FICHAMEDICA|ORDEN:{f.MedOrden}|NHCL:{f.MedNhcl}|FECHA:{f.MedFecha:yyyyMMdd}");

                container.Page(page =>
                {
                    ComponerPaginaFichaMedica(page, paciente, f, item.Diagnosticos, item.Receta, qrItem, $"EXPEDIENTE CLÍNICO — ATENCIÓN #{f.MedOrden}");
                });
            }
        }).GeneratePdf();
    }

}
