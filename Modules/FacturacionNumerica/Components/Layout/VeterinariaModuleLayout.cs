using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Simetric.Components.Layout;

/// <summary>
/// Layout neutro para los componentes heredados que aún declaran MainLayout.
/// La navegación y la seguridad visibles pertenecen al sistema veterinario.
/// </summary>
public sealed class MainLayout : LayoutComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, Body);
    }
}
