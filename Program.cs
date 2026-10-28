using HosvetKardex.Web.Components;
using HosvetKardex.Web.Data;
using HosvetKardex.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuración local no versionada para credenciales y secretos de desarrollo.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Configuración global de exportación QuestPDF
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.UseSystemFonts = true;
QuestPDF.Settings.ThrowOnMissingFontFamilies = false;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Servicios de datos, negocio, inventario y reportería PDF/Excel
builder.Services.AddScoped<ISqlDataService, SqlDataService>();
builder.Services.AddScoped<SessionStateService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IKardexService, KardexService>();
builder.Services.AddScoped<IHospitalizacionService, HospitalizacionService>();
builder.Services.AddScoped<IQuirofanoService, QuirofanoService>();
builder.Services.AddScoped<IConsultaExternaService, ConsultaExternaService>();
builder.Services.AddScoped<ILaboratorioImagenService, LaboratorioImagenService>();
builder.Services.AddScoped<IAdmisionFacturacionService, AdmisionFacturacionService>();
builder.Services.AddScoped<IAdministracionClinicaService, AdministracionClinicaService>();
builder.Services.AddScoped<IGestionMigradaService, GestionMigradaService>();
builder.Services.AddSingleton<IReportExportService, ReportExportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

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
