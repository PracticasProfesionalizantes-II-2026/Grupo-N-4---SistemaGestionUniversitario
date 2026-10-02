using AcadionApi.Datos;
using AcadionApi.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace AcadionApi.Endpoints;

public static class DocumentosEndpoints
{
    public static void MapDocumentosEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/me/documentos").RequireAuthorization();

        group.MapGet("/constancia-alumno-regular", async (HttpContext http, AppDbContext context) =>
        {
            var estudiante = await ObtenerEstudianteAsync(http.User.ObtenerUsuarioId(), context);
            if (estudiante is null) return Results.Forbid();
            var lineas = Encabezado(estudiante).Concat(new[]
            {
                "",
                $"Se deja constancia de que {estudiante.Persona.Apellido}, {estudiante.Persona.Nombre}",
                $"es alumno/a regular de {estudiante.Carrera?.Nombre ?? "la institución"}.",
                $"Estado de la cuenta: {estudiante.Estado}.",
                "",
                $"Emitida el {DateTime.Now:dd/MM/yyyy}.",
                "Documento generado por Acadion."
            });
            return Archivo("Constancia de alumno regular", lineas, $"constancia-{estudiante.Legajo}.pdf");
        });

        group.MapGet("/historial-academico", async (HttpContext http, AppDbContext context) =>
        {
            var estudiante = await ObtenerEstudianteAsync(http.User.ObtenerUsuarioId(), context);
            if (estudiante is null) return Results.Forbid();
            var historial = await context.EstudianteMaterias.AsNoTracking()
                .Where(i => i.IdEstudiante == estudiante.Id && i.Estado != "Cancelada")
                .OrderBy(i => i.CicloLectivo).ThenBy(i => i.Materia!.Nombre)
                .Select(i => $"{i.CicloLectivo} - {i.Materia!.Nombre} - {i.Estado}")
                .ToListAsync();
            var lineas = Encabezado(estudiante).Concat(new[] { "", "Historia académica:" })
                .Concat(historial.Count > 0 ? historial : ["Sin registros académicos."]);
            return Archivo("Historial académico", lineas, $"historial-{estudiante.Legajo}.pdf");
        });

        group.MapGet("/materias-aprobadas", async (HttpContext http, AppDbContext context) =>
        {
            var estudiante = await ObtenerEstudianteAsync(http.User.ObtenerUsuarioId(), context);
            if (estudiante is null) return Results.Forbid();
            var aprobadas = await context.EstudianteMaterias.AsNoTracking()
                .Where(i => i.IdEstudiante == estudiante.Id &&
                    new[] { "Aprobada", "Aprobado", "Promocionada", "Promocionado" }.Contains(i.Estado))
                .OrderBy(i => i.Materia!.Nombre)
                .Select(i => $"{i.Materia!.Nombre} - {i.Estado} - Ciclo {i.CicloLectivo}")
                .ToListAsync();
            var lineas = Encabezado(estudiante).Concat(new[] { "", "Materias aprobadas:" })
                .Concat(aprobadas.Count > 0 ? aprobadas : ["Sin materias aprobadas registradas."]);
            return Archivo("Certificado de materias aprobadas", lineas, $"materias-aprobadas-{estudiante.Legajo}.pdf");
        });

        group.MapGet("/comprobante-final/{examenId:int}", async (int examenId,
            HttpContext http, AppDbContext context) =>
        {
            var estudiante = await ObtenerEstudianteAsync(http.User.ObtenerUsuarioId(), context);
            if (estudiante is null) return Results.Forbid();
            var inscripcion = await context.InscripcionesExamenes.AsNoTracking()
                .Include(i => i.Examen).ThenInclude(e => e.Materia)
                .SingleOrDefaultAsync(i => i.ExamenId == examenId && i.EstudianteId == estudiante.Id);
            if (inscripcion is null) return Results.NotFound(new { mensaje = "No existe una inscripción a ese final." });
            var lineas = Encabezado(estudiante).Concat(new[]
            {
                "", $"Materia: {inscripcion.Examen.Materia!.Nombre}",
                $"Fecha del final: {inscripcion.Examen.Fecha:dd/MM/yyyy HH:mm}",
                $"Estado: {inscripcion.Estado}",
                $"Inscripción registrada: {inscripcion.FechaInscripcionUtc:dd/MM/yyyy HH:mm} UTC"
            });
            return Archivo("Comprobante de inscripción a final", lineas,
                $"inscripcion-final-{examenId}-{estudiante.Legajo}.pdf");
        });
    }

    private static Task<Usuario?> ObtenerEstudianteAsync(int id, AppDbContext context) =>
        context.Usuarios.AsNoTracking().Include(u => u.Persona).Include(u => u.Carrera)
            .Include(u => u.PlanEstudio)
            .SingleOrDefaultAsync(u => u.Id == id && u.RolId == RolesSistema.EstudianteId);

    private static IEnumerable<string> Encabezado(Usuario estudiante) =>
    [
        "Acadion - Sistema de Gestión Universitaria",
        $"Estudiante: {estudiante.Persona.Apellido}, {estudiante.Persona.Nombre}",
        $"DNI: {estudiante.Persona.Dni}",
        $"Legajo: {estudiante.Legajo}",
        $"Carrera: {estudiante.Carrera?.Nombre ?? "Sin carrera"}",
        $"Plan: {estudiante.PlanEstudio?.Codigo ?? estudiante.Carrera?.PlanEstudios ?? "Sin plan"}"
    ];

    private static IResult Archivo(string titulo, IEnumerable<string> lineas, string nombre) =>
        Results.File(PdfSimple.Crear(titulo, lineas), "application/pdf", nombre);
}
