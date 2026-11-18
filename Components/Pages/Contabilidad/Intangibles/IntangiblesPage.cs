using Microsoft.AspNetCore.Components;
using HosvetKardex.Web.Components.Pages.Contabilidad.ActivosFijos;
using Simetric.Services.EContax;

namespace HosvetKardex.Web.Components.Pages.Contabilidad.Intangibles;

public abstract class IntangiblesPage : ActivosFijosPage
{
    [Inject] protected EContaxAmortizacionService Amortizacion { get; set; } = null!;
    protected override EContaxActivosFijosService Servicio => Amortizacion;
}