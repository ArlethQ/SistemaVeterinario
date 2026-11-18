using Microsoft.EntityFrameworkCore;
using Simetric.Data;

namespace Simetric.Services.EContax;

public sealed class EContaxAmortizacionService(
    IDbContextFactory<AppDbContext> factory,
    EContaxSeguridadService seguridad,
    EContaxContabilidadService contabilidad)
    : EContaxActivosFijosService(factory, seguridad, contabilidad, true);