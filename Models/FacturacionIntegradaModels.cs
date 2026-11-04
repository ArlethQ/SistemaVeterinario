namespace HosvetKardex.Web.Models;

public sealed class EstadoFacturacionIntegradaDto
{
    public bool EsquemaDisponible { get; set; }
    public bool EmisorConfigurado { get; set; }
    public bool CajaConfigurada { get; set; }
    public int UsuariosVeterinarios { get; set; }
    public int UsuariosFacturacion { get; set; }
    public int ClientesSincronizados { get; set; }
    public string Mensaje { get; set; } = string.Empty;

    public bool ListaParaConfigurarFacturas =>
        EsquemaDisponible && EmisorConfigurado && CajaConfigurada;
}

public sealed class ResultadoSincronizacionClienteDto
{
    public bool Exito { get; set; }
    public int? CodClienteFacturacion { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
