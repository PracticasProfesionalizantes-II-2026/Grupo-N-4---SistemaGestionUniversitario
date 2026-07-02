using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using AcadionApi.Datos;
using AcadionApi.Endpoints;
using AcadionApi.Logica;
using AcadionApi.Repositorios;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddOpenApi();

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

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild",
    "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};


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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}