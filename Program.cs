using HosvetKardex.Web.Components;
using HosvetKardex.Web.Data;
using HosvetKardex.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Simetric.Data;
using Simetric.Services;
using Simetric.Services.EContax;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Configuración local no versionada para credenciales y secretos de desarrollo.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// La validacion de certificados pertenece al facturador migrado. Conservamos
// el mismo endpoint de E-FACT y, durante el desarrollo con ambos repositorios,
// reutilizamos su clave sin copiarla ni exponerla en este codigo fuente.
builder.Configuration["FirmaInfoApi:Url"] ??= "http://68.178.204.190:8090/api/firma/info";
builder.Configuration["FirmaInfoApi:RutaFirmasBase"] ??= "App_Data";
if (string.IsNullOrWhiteSpace(builder.Configuration["FirmaInfoApi:ApiKey"]))
{
    var eFactDirectory = Path.GetFullPath(Path.Combine(
        builder.Environment.ContentRootPath,
        "..",
        "E-FACT",
        "Simetric"));
    var eFactSettingsPath = Path.Combine(eFactDirectory, "appsettings.json");
    if (File.Exists(eFactSettingsPath))
    {
        var eFactConfiguration = new ConfigurationBuilder()
            .SetBasePath(eFactDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        builder.Configuration["FirmaInfoApi:ApiKey"] =
            eFactConfiguration["FirmaInfoApi:ApiKey"];
    }
}

// Configuración global de exportación QuestPDF
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.UseSystemFonts = true;
QuestPDF.Settings.ThrowOnMissingFontFamilies = false;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(InternalApiAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, InternalApiAuthenticationHandler>(
        InternalApiAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Simetric.Controllers.FacturasController).Assembly);

// Servicios de datos, negocio, inventario y reportería PDF/Excel
builder.Services.AddScoped<ISqlDataService, SqlDataService>();
builder.Services.AddScoped<SessionStateService>();
builder.Services.AddScoped<VeterinariaAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<VeterinariaAuthenticationStateProvider>());
builder.Services.AddScoped<IAuthService, HosvetKardex.Web.Services.AuthService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IKardexService, KardexService>();
builder.Services.AddScoped<IHospitalizacionService, HospitalizacionService>();
builder.Services.AddScoped<IQuirofanoService, QuirofanoService>();
builder.Services.AddScoped<IConsultaExternaService, ConsultaExternaService>();
builder.Services.AddScoped<ILaboratorioImagenService, LaboratorioImagenService>();
builder.Services.AddScoped<IAdmisionFacturacionService, AdmisionFacturacionService>();
builder.Services.AddScoped<IFacturacionIntegradaService, FacturacionIntegradaService>();
builder.Services.AddScoped<FacturacionPageAccessService>();
builder.Services.AddScoped<IAdministracionClinicaService, AdministracionClinicaService>();
builder.Services.AddScoped<IGestionMigradaService, GestionMigradaService>();
builder.Services.AddScoped<IEFactCatalogService, EFactCatalogService>();
builder.Services.AddSingleton<IReportExportService, ReportExportService>();

// Módulo original de Numerica: utiliza la misma base integrada y la sesión veterinaria.
var numericaConnection = builder.Configuration.GetConnectionString("VeterinariaConnection") ?? string.Empty;
if (numericaConnection.StartsWith("ConnectionString=", StringComparison.OrdinalIgnoreCase))
    numericaConnection = numericaConnection["ConnectionString=".Length..];
builder.Configuration["ConnectionStrings:DefaultConnection"] = numericaConnection;

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(numericaConnection, sql => sql.EnableRetryOnFailure(2)));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<InternalApiToken>();
builder.Services.AddScoped(sp => new HttpClient(new InternalApiHeaderHandler(
    sp.GetRequiredService<InternalApiToken>()))
{
    BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri),
    Timeout = TimeSpan.FromSeconds(60)
});

builder.Services.AddSingleton<SqlAuditService>();
builder.Services.AddSingleton<AuditService>();
builder.Services.AddSingleton<CurrentUserContext>();
builder.Services.AddSingleton<AuditActorResolver>();
builder.Services.AddScoped<ICajaSerieResolver, CajaSerieResolver>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFacturaPdfService, FacturaPdfService>();
builder.Services.AddScoped<IGuiaRemisionPdfService, GuiaRemisionPdfService>();
builder.Services.AddScoped<ILiquidacionCompraPdfService, LiquidacionCompraPdfService>();
builder.Services.AddScoped<INotaCreditoPdfService, NotaCreditoPdfService>();
builder.Services.AddScoped<INotaDebitoPdfService, NotaDebitoPdfService>();
builder.Services.AddScoped<IRetencionPdfService, RetencionPdfService>();
builder.Services.AddScoped<IdentificacionService>();
builder.Services.AddScoped<FacturacionService>();
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<TaxService>();
builder.Services.AddScoped<ProductCategoryService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TipoClienteService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<PagoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<AbonoService>();
builder.Services.AddScoped<CuentasPagarService>();
builder.Services.AddScoped<FacturaCompraManualService>();
builder.Services.AddScoped<GuiaRemisionService>();
builder.Services.AddScoped<NotaCreditoService>();
builder.Services.AddScoped<NotaDebitoService>();
builder.Services.AddScoped<RetencionGeneradaService>();
builder.Services.AddScoped<RetencionesService>();
builder.Services.AddScoped<RetencionCorreoService>();
builder.Services.AddScoped<ComprobanteRetencionGenerator>();
builder.Services.AddScoped<ComprasXmlService>();
builder.Services.AddScoped<LiquidacionCompraService>();
builder.Services.AddScoped<LiquidacionCompraXmlGenerator>();
builder.Services.AddScoped<ISimpleExcelExportService, SimpleExcelExportService>();
builder.Services.AddScoped<IFacturasExcelService, FacturasExcelService>();
builder.Services.AddScoped<ReporteComprobantesService>();
builder.Services.AddScoped<IReporteComprobantesPdfService, ReporteComprobantesPdfService>();
builder.Services.AddScoped<IReporteComprobantesExcelService, ReporteComprobantesExcelService>();
builder.Services.AddScoped<IEstadoCuentaExcelService, EstadoCuentaExcelService>();
builder.Services.AddScoped<IEstadoCuentaPdfService, EstadoCuentaPdfService>();
builder.Services.AddScoped<SweetAlertService>();
builder.Services.AddScoped<InitialSequencePromptService>();
builder.Services.AddScoped<ComprobanteCorreoEstadoService>();
builder.Services.AddScoped<EmisionControlService>();
builder.Services.AddScoped<EmisorSistemaService>();
builder.Services.AddScoped<EmisorOnboardingService>();
builder.Services.AddScoped<ConfiguracionService>();
builder.Services.AddScoped<AdminCajaSecuenciaService>();
builder.Services.AddScoped<EmisorCertificadoProtector>();
builder.Services.AddScoped<EmisorCertificadoValidator>();
builder.Services.AddScoped<FirmaPathResolver>();
builder.Services.AddScoped<FacturaStoredProcedureBootstrapService>();
builder.Services.AddSingleton<FacturaPersistenceSchemaService>();
builder.Services.AddSingleton<CotizacionSchemaService>();
builder.Services.AddScoped<DemoCatalogService>();
builder.Services.AddScoped<UbicacionEcuadorCatalogService>();
builder.Services.AddHttpClient<CedulaLookupService>(client =>
{
    client.BaseAddress = new Uri("http://nessoftfact-001-site6.atempurl.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
    // ComisionesService es utilizado por la emision de facturas. No se habilitan
    // los portales comerciales de aliados ni el back-office de Numerica.
builder.Services.AddScoped<ComisionesService>();
builder.Services.AddScoped<EContaxTenantService>();
builder.Services.AddScoped<EContaxSeguridadService>();
builder.Services.AddScoped<EContaxInventarioService>();
builder.Services.AddScoped<EContaxSharedDataService>();
builder.Services.AddScoped<EContaxAdministracionService>();
builder.Services.AddScoped<EContaxFacturacionService>();
builder.Services.AddScoped<EContaxOrganizacionService>();
builder.Services.AddScoped<EContaxBodegaService>();
builder.Services.AddScoped<EContaxCatalogService>();
builder.Services.AddScoped<IEContaxInventarioReportePdfService, EContaxInventarioReportePdfService>();
builder.Services.AddScoped<EContaxOrdenCompraService>();
builder.Services.AddScoped<EContaxRecepcionCompraService>();
builder.Services.AddScoped<EContaxOrganizacionBootstrapService>();
builder.Services.AddSingleton<TutorialRegistryService>();
builder.Services.AddScoped<TutorialStateService>();
builder.Services.AddScoped<MenuStateService>();
builder.Services.AddScoped<AppAccessService>();
builder.Services.AddScoped<SelectedAppServiceStateService>();
builder.Services.AddHttpClient<FirmaInfoApiService>(client => client.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddHttpClient<SriXmlProcessorService>(client => client.Timeout = TimeSpan.FromSeconds(90));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Sirve archivos generados en tiempo de ejecución, como fotografías de pacientes.
app.UseStaticFiles();
app.MapStaticAssets();

// Fotografías cargadas después de publicar/compilar, resueltas por NHCL.
app.MapGet("/pacientes-fotos/{nhcl:int}", (int nhcl, IWebHostEnvironment env) =>
{
    string directorio = Path.Combine(env.WebRootPath, "PacientesFotos");
    if (!Directory.Exists(directorio)) return Results.NotFound();
    string? archivo = Directory.EnumerateFiles(directorio, $"{nhcl}.*").FirstOrDefault();
    if (archivo is null) return Results.NotFound();
    string contentType = Path.GetExtension(archivo).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };
    return Results.File(archivo, contentType);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
