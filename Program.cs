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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
