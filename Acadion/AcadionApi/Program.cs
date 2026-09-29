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

var builder = WebApplication.CreateBuilder(args);

// Evita depender del registro de eventos de Windows (requiere privilegios elevados).
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Servicios
builder.Services.AddOpenApi();

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
    });
builder.Services.AddAuthorization();
var permitirArchivosLocales = builder.Environment.IsDevelopment();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .SetIsOriginAllowed(origin =>
        (permitirArchivosLocales && origin == "null") ||
        (Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback))
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

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
app.UseAuthentication();
app.UseAuthorization();


// Endpoints
// Usuarios
app.MapUsuarioEndpoints();
// Personas
app.MapPersonaEndpoints();
// Login
app.MapLoginEndpoints();
// Materias
app.MapMateriaEndpoints();
// Inscripciones
app.MapEstudianteMateriaEndpoints();
// Anios
app.MapAnioEndpoints();
// Carreras
app.MapCarreraEndpoints();
// Asistencias
app.MapAsistenciaEndpoints();
// Exámenes
app.MapExamenEndpoints();
// Notas
app.MapNotaExamenEndpoints();
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

try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await NotificacionAutomaticaService.ReprogramarPendientesAsync(context);
}
catch (Exception error)
{
    app.Logger.LogCritical(error,
        "No fue posible aplicar las migraciones o reconciliar las notificaciones automáticas al iniciar.");
    throw;
}

app.Run();
