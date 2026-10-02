using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;
using AcadionApi.Endpoints;
using AcadionApi.Logica;
using AcadionApi.Repositorios;
using AcadionApi.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Evita depender del registro de eventos de Windows (requiere privilegios elevados).
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Servicios
builder.Services.AddOpenApi();
if (builder.Environment.IsEnvironment("Testing"))
{
    var clavesPrueba = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "AcadionTests", "data-protection"));
    clavesPrueba.Create();
    builder.Services.AddDataProtection().PersistKeysToFileSystem(clavesPrueba);
}

var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la configuración JWT.");
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Seccion));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue("acadion_access", out var cookieToken))
                    context.Token = cookieToken;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 8,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("recuperacion", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.Seccion));
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
var rutaPerfiles = builder.Configuration["Uploads:RutaPerfiles"];
if (string.IsNullOrWhiteSpace(rutaPerfiles))
{
    var raizPersistente = Environment.GetEnvironmentVariable("HOME");
    if (string.IsNullOrWhiteSpace(raizPersistente))
        raizPersistente = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    rutaPerfiles = Path.Combine(raizPersistente, "Acadion", "perfiles");
}
Directory.CreateDirectory(rutaPerfiles);
builder.Services.AddSingleton(new PerfilStorage(rutaPerfiles));
var rutaDocumentos = builder.Configuration["Uploads:RutaDocumentos"];
if (string.IsNullOrWhiteSpace(rutaDocumentos))
{
    var raizDocumentos = builder.Environment.IsEnvironment("Testing")
        ? Path.Combine(Path.GetTempPath(), "AcadionTests", Guid.NewGuid().ToString("N"))
        : Environment.GetEnvironmentVariable("HOME");
    if (string.IsNullOrWhiteSpace(raizDocumentos))
        raizDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    rutaDocumentos = Path.Combine(raizDocumentos, "Acadion", "documentos");
}
builder.Services.AddSingleton(new DocumentoStorage(rutaDocumentos));
var permitirArchivosLocales = builder.Environment.IsDevelopment();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .SetIsOriginAllowed(origin =>
        (permitirArchivosLocales && origin == "null") ||
        (Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback))
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 8,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// =======================================================
// SERVICIOS (INJECCIÓN DE DEPENDENCIAS)
// =======================================================

// --- ESTRUCTURA DE DATOS CENTRAL ---
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// --- USUARIOS ---
builder.Services.AddScoped<IUsuarioLogica, UsuarioLogica>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
// --- PERSONAS ---
builder.Services.AddScoped<IPersonaLogica, PersonaLogica>();
builder.Services.AddScoped<IPersonaRepositorio, PersonaRepositorio>();
// --- LOGIN ---
builder.Services.AddScoped<ILoginLogica, LoginLogica>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGestionUsuariosService, GestionUsuariosService>();
// --- MATERIAS ---
builder.Services.AddScoped<IMateriaLogica, MateriaLogica>();
builder.Services.AddScoped<IMateriaRepositorio, MateriaRepositorio>();
// --- INSCRIPCIONES (ESTUDIANTE MATERIA) ---
builder.Services.AddScoped<IEstudianteMateriaLogica, EstudianteMateriaLogica>();
builder.Services.AddScoped<IEstudianteMateriaRepositorio, EstudianteMateriaRepositorio>();
// --- ANIOS ACADÉMICOS ---
builder.Services.AddScoped<IAnioLogica, AnioLogica>();
builder.Services.AddScoped<IAnioRepositorio, AnioRepositorio>();
// --- CARRERAS ---
builder.Services.AddScoped<ICarreraLogica, CarreraLogica>();
builder.Services.AddScoped<ICarreraRepositorio, CarreraRepositorio>();
// --- ASISTENCIAS ---
builder.Services.AddScoped<IAsistenciaLogica, AsistenciaLogica>();
builder.Services.AddScoped<IAsistenciaRepositorio, AsistenciaRepositorio>();
// --- EXÁMENES ---
builder.Services.AddScoped<IExamenLogica, ExamenLogica>();
builder.Services.AddScoped<IExamenRepositorio, ExamenRepositorio>();
// --- NOTAS EXÁMENES ---
builder.Services.AddScoped<INotaExamenLogica, NotaExamenLogica>();
builder.Services.AddScoped<INotaExamenRepositorio, NotaExamenRepositorio>();
// --- HORARIOS MATERIAS ---
builder.Services.AddScoped<IHorarioMateriaLogica, HorarioMateriaLogica>();
builder.Services.AddScoped<IHorarioMateriaRepositorio, HorarioMateriaRepositorio>();

var app = builder.Build();

var redireccionesPantallasAntiguas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["/PanelDirectivo.html"] = "/",
    ["/Profesores.html"] = "/GestionUsuarios.html",
    ["/Secretarios.html"] = "/GestionUsuarios.html",
    ["/Estudiantes.html"] = "/GestionUsuarios.html",
    ["/Materia.html"] = "/GestionMaterias.html",
    ["/Clases.html"] = "/GestionMaterias.html",
    ["/CrearClase.html"] = "/GestionMaterias.html",
    ["/Calificaciones.html"] = "/GestionMaterias.html",
    ["/Asistencia.html"] = "/GestionMaterias.html",
    ["/Reuniones.html"] = "/PanelSecretaria.html",
    ["/CrearReunion.html"] = "/PanelSecretaria.html"
};

app.Use(async (http, siguiente) =>
{
    if (HttpMethods.IsGet(http.Request.Method) &&
        redireccionesPantallasAntiguas.TryGetValue(http.Request.Path, out var destino))
    {
        http.Response.Redirect(destino, permanent: false);
        return;
    }
    await siguiente();
});


// Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapScalarApiReference();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseDefaultFiles();
app.UseCors();
app.UseStaticFiles();
if (app.Environment.IsDevelopment())
{
    var rutaFrontDesarrollo = Path.GetFullPath(Path.Combine(
        app.Environment.ContentRootPath, "..", "AcadionFront"));
    if (!Directory.Exists(rutaFrontDesarrollo))
        throw new DirectoryNotFoundException(
            $"No se encontró el frontend de Acadion en '{rutaFrontDesarrollo}'.");

    var proveedorFrontDesarrollo = new PhysicalFileProvider(rutaFrontDesarrollo);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = proveedorFrontDesarrollo
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = proveedorFrontDesarrollo,
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            context.Context.Response.Headers.Pragma = "no-cache";
            context.Context.Response.Headers.Expires = "0";
        }
    });
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(rutaPerfiles),
    RequestPath = "/uploads/perfiles"
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditoriaMiddleware>();
app.UseMiddleware<ValidacionEntradaMiddleware>();


// Endpoints
// Login
app.MapLoginEndpoints();
// Materias
app.MapMateriaEndpoints();
// Inscripciones
app.MapEstudianteMateriaEndpoints();
// Carreras
app.MapCarreraEndpoints();
app.MapPlanEstudioEndpoints();
app.MapComisionEndpoints();
app.MapCalendarioEndpoints();
app.MapTurnosExamenFinalEndpoints();
app.MapEquivalenciasEndpoints();
app.MapDocumentosEndpoints();
// Horarios
app.MapHorarioMateriaEndpoints();
// Gestión segura de cuentas, estructura académica y portales por rol
app.MapGestionUsuariosEndpoints();
app.MapGestionAcademicaEndpoints();
app.MapAsistenciaPersonalEndpoints();
app.MapPortalEndpoints();
app.MapRolesEndpoints();
app.MapNotificacionesEndpoints();
app.MapPagosEndpoints();
app.MapDirectivoEndpoints();
app.MapAuditoriaEndpoints();

try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        await context.Database.MigrateAsync();
    else if (!await context.Database.CanConnectAsync())
        throw new InvalidOperationException("No fue posible conectar con la base de datos.");
    await NotificacionAutomaticaService.ReprogramarPendientesAsync(context);
}
catch (Exception error)
{
    app.Logger.LogCritical(error,
        "No fue posible validar la base o reconciliar las notificaciones automáticas al iniciar.");
    throw;
}

app.Run();

public partial class Program;
