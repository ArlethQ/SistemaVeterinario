using Microsoft.AspNetCore.Components;

namespace HosvetKardex.Web.Components.Events;

[EventHandler("onconsultaridentificacion", typeof(ConsultaIdentificacionEventArgs))]
public static class EventHandlers
{
}

public sealed class ConsultaIdentificacionEventArgs : EventArgs
{
    public string Valor { get; set; } = string.Empty;
}
