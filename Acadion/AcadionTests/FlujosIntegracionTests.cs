using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcadionApi.Datos;
using AcadionApi.Seguridad;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AcadionTests;

public sealed class FlujosIntegracionTests
{
    [Fact]
    public async Task Bootstrap_Login_Y_Sesion_Completan_El_Flujo_Real()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var alta = await client.PostAsJsonAsync("/api/auth/bootstrap-secretario", new
        {
            nombre = "Ana",
            apellido = "Prueba",
            dni = 30111222,
            fechaNacimiento = new DateTime(1990, 4, 12),
            direccion = "Calle Central 123",
            localidad = "Rafaela",
            codigoPostal = 2300,
            emailPersonal = "ana.prueba@example.com",
            emailInstitucional = "ana.prueba@acadion.test",
            telefonoContacto = "3492123456"
        });

        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var cuenta = await alta.Content.ReadFromJsonAsync<CuentaCreada>();
        Assert.NotNull(cuenta);
        Assert.Equal("ana.prueba", cuenta.NombreUsuario);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            nombreUsuario = cuenta.NombreUsuario,
            password = "30111222"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers, h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));

        var sesion = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.OK, sesion.StatusCode);
    }

    [Theory]
    [InlineData("/usuarios/")]
    [InlineData("/personas/")]
    [InlineData("/anios/")]
    [InlineData("/asistencias/")]
    [InlineData("/examenes/")]
    [InlineData("/notas/")]
    public async Task Endpoints_Crud_Antiguos_No_Exponen_Datos_Sin_Sesion(string ruta)
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient();

        var respuesta = await client.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("/carreras/")]
    [InlineData("/materias/")]
    [InlineData("/horarios/")]
    [InlineData("/inscripciones/")]
    public async Task Endpoints_Compatibles_Que_Sigue_Usando_El_Front_Requieren_Sesion(
        string ruta)
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient();

        var respuesta = await client.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Endpoint_Antiguo_De_Eliminacion_De_Personas_Ya_No_Existe()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
        await CrearEIniciarSecretarioAsync(client, 30222333, "Berta", "Segura");

        var respuesta = await client.DeleteAsync("/personas/999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("/Profesores.html", "/GestionUsuarios.html")]
    [InlineData("/Secretarios.html", "/GestionUsuarios.html")]
    [InlineData("/Materia.html", "/GestionMaterias.html")]
    [InlineData("/Clases.html", "/GestionMaterias.html")]
    [InlineData("/Reuniones.html", "/PanelSecretaria.html")]
    public async Task Pantallas_De_Demostracion_Redirigen_A_Una_Pantalla_Real(
        string antigua, string destino)
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var respuesta = await client.GetAsync(antigua);

        Assert.Equal(HttpStatusCode.Found, respuesta.StatusCode);
        Assert.Equal(destino, respuesta.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Recuperacion_No_Se_Ofrece_Si_No_Hay_Correo_Configurado()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient();

        var estado = await client.GetFromJsonAsync<RecuperacionDisponible>(
            "/api/auth/recuperacion/disponible");
        var solicitud = await client.PostAsJsonAsync("/api/auth/recuperacion/solicitar", new
        {
            email = "persona@example.com"
        });

        Assert.NotNull(estado);
        Assert.False(estado.Disponible);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, solicitud.StatusCode);
    }

    [Fact]
    public async Task Inscripcion_Respeta_Correlativas_Y_Persiste_Al_Habilitarse()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(ClienteConCookies());
        var escenario = await SembrarEscenarioAcademicoAsync(factory, incluirCorrelativa: true);
        await IniciarSesionAsync(client, escenario.UsuarioEstudiante, escenario.PasswordEstudiante);

        var rechazada = await client.PostAsJsonAsync("/api/me/inscripciones", new
        {
            materiaId = escenario.MateriaId
        });
        Assert.Equal(HttpStatusCode.Conflict, rechazada.StatusCode);
        Assert.Contains("regularizar", await rechazada.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase);

        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            context.EstudianteMaterias.Add(new EstudianteMateria
            {
                IdEstudiante = escenario.EstudianteId,
                IdMateria = escenario.CorrelativaId!.Value,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year - 1,
                Estado = "Regular"
            });
            await context.SaveChangesAsync();
        }

        var aceptada = await client.PostAsJsonAsync("/api/me/inscripciones", new
        {
            materiaId = escenario.MateriaId
        });
        Assert.Equal(HttpStatusCode.Created, aceptada.StatusCode);

        using var verificacion = factory.Services.CreateScope();
        var db = verificacion.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.EstudianteMaterias.AnyAsync(i =>
            i.IdEstudiante == escenario.EstudianteId &&
            i.IdMateria == escenario.MateriaId && i.Estado == "Cursando"));
    }

    [Fact]
    public async Task Carga_De_Nota_Solo_Acepta_Estudiantes_Inscriptos()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(ClienteConCookies());
        var escenario = await SembrarEscenarioAcademicoAsync(factory);
        int examenId;
        int estudianteNoInscriptoId;
        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            context.EstudianteMaterias.Add(new EstudianteMateria
            {
                IdEstudiante = escenario.EstudianteId,
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Estado = "Cursando"
            });
            var noInscripto = CrearUsuario("alumno.sininscripcion", "clave-segura-456",
                40999888, RolesSistema.EstudianteId, escenario.CarreraId, escenario.PlanId);
            context.Usuarios.Add(noInscripto);
            var examen = new Examen
            {
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Fecha = DateTime.UtcNow.AddDays(2),
                TipoExamen = "Parcial",
                NotaMinimaRegularizacion = 6,
                NotaMinimaPromocion = 8
            };
            context.Add(examen);
            await context.SaveChangesAsync();
            examenId = examen.IdExamen;
            estudianteNoInscriptoId = noInscripto.Id;
        }
        await IniciarSesionAsync(client, escenario.UsuarioDocente, escenario.PasswordDocente);

        var ajena = await client.PostAsJsonAsync("/api/docente/notas", new
        {
            idExamen = examenId,
            idEstudiante = estudianteNoInscriptoId,
            nota = 8
        });
        Assert.Equal(HttpStatusCode.BadRequest, ajena.StatusCode);

        var propia = await client.PostAsJsonAsync("/api/docente/notas", new
        {
            idExamen = examenId,
            idEstudiante = escenario.EstudianteId,
            nota = 8,
            observaciones = "Evaluación aprobada"
        });
        Assert.Equal(HttpStatusCode.OK, propia.StatusCode);

        using var verificacion = factory.Services.CreateScope();
        var db = verificacion.ServiceProvider.GetRequiredService<AppDbContext>();
        var nota = await db.Set<NotaExamen>().SingleAsync(n =>
            n.IdExamen == examenId && n.IdEstudiante == escenario.EstudianteId);
        Assert.Equal(8, nota.Nota);
        Assert.Equal("Promocionó", nota.Condicion);
        var notificacion = await db.NotificacionesGenerales.SingleAsync(n =>
            n.UsuarioDestinoId == escenario.EstudianteId &&
            n.ClaveAutomatica == $"AUTO:NOTA:{examenId}:ESTUDIANTE:{escenario.EstudianteId}");
        Assert.Equal("Se cargó una nueva nota", notificacion.Titulo);
        Assert.Contains("Programación", notificacion.Mensaje);
    }

    [Fact]
    public async Task Final_Aprobado_Actualiza_La_Materia_Y_Conserva_Al_Estudiante_En_El_Acta()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(ClienteConCookies());
        var escenario = await SembrarEscenarioAcademicoAsync(factory);
        int examenId;
        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            context.EstudianteMaterias.Add(new EstudianteMateria
            {
                IdEstudiante = escenario.EstudianteId,
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Estado = "Regular"
            });
            var final = new Examen
            {
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Fecha = DateTime.UtcNow.AddDays(-1),
                TipoExamen = "Final",
                NotaMinimaRegularizacion = 6
            };
            context.Add(final);
            await context.SaveChangesAsync();
            context.InscripcionesExamenes.Add(new InscripcionExamen
            {
                ExamenId = final.IdExamen,
                EstudianteId = escenario.EstudianteId,
                Estado = "Inscripto"
            });
            await context.SaveChangesAsync();
            examenId = final.IdExamen;
        }
        await IniciarSesionAsync(client, escenario.UsuarioDocente, escenario.PasswordDocente);

        var respuesta = await client.PostAsJsonAsync("/api/docente/notas", new
        {
            idExamen = examenId,
            idEstudiante = escenario.EstudianteId,
            nota = 7
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var planilla = await client.GetFromJsonAsync<JsonElement>(
            $"/api/docente/examenes/{examenId}/planilla");
        Assert.Single(planilla.GetProperty("estudiantes").EnumerateArray());
        using var verificacion = factory.Services.CreateScope();
        var db = verificacion.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Aprobado", (await db.InscripcionesExamenes.SingleAsync()).Estado);
        Assert.Equal("Aprobada", (await db.EstudianteMaterias.SingleAsync()).Estado);
    }

    [Fact]
    public async Task Recuperatorio_Reemplaza_La_Nota_Original_Y_Define_Regular_O_Libre()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(ClienteConCookies());
        var escenario = await SembrarEscenarioAcademicoAsync(factory);
        int parcialId;
        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            context.EstudianteMaterias.Add(new EstudianteMateria
            {
                IdEstudiante = escenario.EstudianteId,
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Estado = "Cursando"
            });
            var parcial = new Examen
            {
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Fecha = DateTime.UtcNow.AddDays(1),
                TipoExamen = "Parcial",
                NotaMinimaRegularizacion = 6,
                NotaMinimaPromocion = 8
            };
            context.Add(parcial);
            await context.SaveChangesAsync();
            parcialId = parcial.IdExamen;
        }
        await IniciarSesionAsync(client, escenario.UsuarioDocente, escenario.PasswordDocente);

        var sinOriginal = await client.PostAsJsonAsync("/api/docente/examenes", new
        {
            idMateria = escenario.MateriaId,
            cicloLectivo = DateTime.UtcNow.Year,
            fecha = DateTime.UtcNow.AddDays(2),
            tipoExamen = "Recuperatorio"
        });
        Assert.Equal(HttpStatusCode.BadRequest, sinOriginal.StatusCode);

        var creado = await client.PostAsJsonAsync("/api/docente/examenes", new
        {
            idMateria = escenario.MateriaId,
            cicloLectivo = DateTime.UtcNow.Year,
            fecha = DateTime.UtcNow.AddDays(2),
            tipoExamen = "Recuperatorio",
            examenRecuperadoId = parcialId
        });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var recuperatorioId = (await creado.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("idExamen").GetInt32();

        var parcialDesaprobado = await client.PostAsJsonAsync("/api/docente/notas", new
        {
            idExamen = parcialId,
            idEstudiante = escenario.EstudianteId,
            nota = 3
        });
        Assert.Equal(HttpStatusCode.OK, parcialDesaprobado.StatusCode);

        var recuperatorioAprobado = await client.PostAsJsonAsync("/api/docente/notas", new
        {
            idExamen = recuperatorioId,
            idEstudiante = escenario.EstudianteId,
            nota = 7
        });
        Assert.Equal(HttpStatusCode.OK, recuperatorioAprobado.StatusCode);
        using (var verificacion = factory.Services.CreateScope())
        {
            var db = verificacion.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal("Regular", (await db.EstudianteMaterias.SingleAsync()).Estado);
            Assert.Equal(2, await db.Set<NotaExamen>().CountAsync());
        }

        var recuperatorioDesaprobado = await client.PostAsJsonAsync("/api/docente/notas", new
        {
            idExamen = recuperatorioId,
            idEstudiante = escenario.EstudianteId,
            nota = 3
        });
        Assert.Equal(HttpStatusCode.OK, recuperatorioDesaprobado.StatusCode);
        using var verificacionFinal = factory.Services.CreateScope();
        var dbFinal = verificacionFinal.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Libre", (await dbFinal.EstudianteMaterias.SingleAsync()).Estado);
    }

    [Fact]
    public async Task Reporte_De_Finales_Muestra_Todos_Los_Intentos_Y_Conserva_El_Aprobado()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(ClienteConCookies());
        var escenario = await SembrarEscenarioAcademicoAsync(factory);
        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            context.EstudianteMaterias.Add(new EstudianteMateria
            {
                IdEstudiante = escenario.EstudianteId,
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Estado = "Cursando"
            });
            var desaprobado = new Examen
            {
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Fecha = DateTime.UtcNow.AddDays(-20),
                TipoExamen = "Final",
                NotaMinimaRegularizacion = 6
            };
            var aprobado = new Examen
            {
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Fecha = DateTime.UtcNow.AddDays(-10),
                TipoExamen = "Final",
                NotaMinimaRegularizacion = 6
            };
            context.AddRange(desaprobado, aprobado);
            await context.SaveChangesAsync();
            context.InscripcionesExamenes.AddRange(
                new InscripcionExamen
                {
                    ExamenId = desaprobado.IdExamen,
                    EstudianteId = escenario.EstudianteId,
                    Estado = "Desaprobado"
                },
                new InscripcionExamen
                {
                    ExamenId = aprobado.IdExamen,
                    EstudianteId = escenario.EstudianteId,
                    Estado = "Aprobado"
                });
            context.AddRange(
                new NotaExamen
                {
                    IdExamen = desaprobado.IdExamen,
                    IdEstudiante = escenario.EstudianteId,
                    Nota = 2,
                    Condicion = "Desaprobó"
                },
                new NotaExamen
                {
                    IdExamen = aprobado.IdExamen,
                    IdEstudiante = escenario.EstudianteId,
                    Nota = 7,
                    Condicion = "Aprobó"
                });
            await context.SaveChangesAsync();
        }
        await IniciarSesionAsync(client, escenario.UsuarioEstudiante,
            escenario.PasswordEstudiante);

        var notas = await client.GetFromJsonAsync<JsonElement>("/api/me/notas");
        var finales = notas.EnumerateArray()
            .Where(item => item.GetProperty("esFinal").GetBoolean())
            .ToArray();
        Assert.Equal(2, finales.Length);
        Assert.Contains(finales, item => item.GetProperty("nota").GetDecimal() == 2 &&
            item.GetProperty("resultado").GetString() == "Desaprobó");
        Assert.All(finales, item => Assert.Equal(1, item.GetProperty("numeroAnio").GetInt32()));
        Assert.All(finales, item => Assert.False(string.IsNullOrWhiteSpace(
            item.GetProperty("corregidoPor").GetString())));

        var resumen = await client.GetFromJsonAsync<JsonElement>("/api/me/resumen-academico");
        var materia = resumen.EnumerateArray().Single();
        Assert.Equal("Aprobada", materia.GetProperty("estado").GetString());
        Assert.Equal(7, materia.GetProperty("calificacionFinal").GetDecimal());
    }

    [Fact]
    public async Task Pago_Actualizado_Por_Secretaria_Queda_Registrado()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(ClienteConCookies());
        await CrearEIniciarSecretarioAsync(client, 30333444, "Carla", "Pagos");
        int estudianteId;
        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            var estudiante = CrearUsuario("estudiante.pagos", "clave-segura-789",
                40111222, RolesSistema.EstudianteId);
            context.Usuarios.Add(estudiante);
            await context.SaveChangesAsync();
            estudianteId = estudiante.Id;
        }
        var ahora = DateTime.UtcNow;

        var respuesta = await client.PutAsJsonAsync(
            $"/api/gestion/pagos/{estudianteId}/cuotas/{ahora.Year}/{ahora.Month}", new
            {
                estado = "AL_DIA",
                fechaPago = ahora,
                importe = 25000m,
                metodoPago = "Transferencia",
                observaciones = "Validado en prueba integral"
            });
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);

        using var verificacion = factory.Services.CreateScope();
        var db = verificacion.ServiceProvider.GetRequiredService<AppDbContext>();
        var cuota = await db.CuotasMensuales.SingleAsync(c =>
            c.EstudianteId == estudianteId && c.Anio == ahora.Year && c.Mes == ahora.Month);
        Assert.Equal(EstadoPago.AlDia, cuota.Estado);
        Assert.Equal(25000m, cuota.Importe);
        Assert.NotNull(cuota.ValidadoPorUsuarioId);
    }

    [Fact]
    public async Task Calendario_Personal_Muestra_Evaluaciones_E_Institucionales_Del_Usuario()
    {
        await using var factory = new AcadionWebFactory();
        using var studentClient = factory.CreateClient(ClienteConCookies());
        var escenario = await SembrarEscenarioAcademicoAsync(factory);
        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            context.EstudianteMaterias.Add(new EstudianteMateria
            {
                IdEstudiante = escenario.EstudianteId,
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Estado = "Cursando"
            });
            var fecha = new DateTime(DateTime.UtcNow.Year, 6, 10, 18, 0, 0, DateTimeKind.Utc);
            context.AddRange(
                new Examen { IdMateria = escenario.MateriaId, IdDocente = escenario.DocenteId,
                    CicloLectivo = fecha.Year, Fecha = fecha, TipoExamen = "Parcial" },
                new Examen { IdMateria = escenario.MateriaId, IdDocente = escenario.DocenteId,
                    CicloLectivo = fecha.Year, Fecha = fecha.AddDays(2), TipoExamen = "Presentacion" },
                new EventoCalendario { Titulo = "Feriado institucional", Tipo = "Feriado",
                    Descripcion = "No se dictan clases.", FechaInicioUtc = fecha.AddDays(3),
                    FechaFinUtc = fecha.AddDays(4), CreadoPorUsuarioId = escenario.DocenteId });
            await context.SaveChangesAsync();
        }
        await IniciarSesionAsync(studentClient, escenario.UsuarioEstudiante,
            escenario.PasswordEstudiante);

        var studentResponse = await studentClient.GetAsync(
            $"/api/calendario/personal?cicloLectivo={DateTime.UtcNow.Year}");
        Assert.Equal(HttpStatusCode.OK, studentResponse.StatusCode);
        var studentJson = await studentResponse.Content.ReadAsStringAsync();
        Assert.Contains("Parcial de Programación", studentJson);
        Assert.Contains("Presentación de Programación", studentJson);
        Assert.Contains("Feriado institucional", studentJson);

        using var teacherClient = factory.CreateClient(ClienteConCookies());
        await IniciarSesionAsync(teacherClient, escenario.UsuarioDocente,
            escenario.PasswordDocente);
        var teacherItems = await teacherClient.GetFromJsonAsync<JsonElement>(
            $"/api/calendario/personal?cicloLectivo={DateTime.UtcNow.Year}");
        Assert.Equal(4, teacherItems.GetArrayLength());
        Assert.Contains("Inscripción a materias", teacherItems.GetRawText());
        Assert.Contains("Parcial de Programación", teacherItems.GetRawText());
    }

    [Fact]
    public async Task Notificacion_Se_Marca_Como_Leida_De_Forma_Individual()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(ClienteConCookies());
        var escenario = await SembrarEscenarioAcademicoAsync(factory);
        int notificacionId;
        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificacion = new NotificacionGeneral
            {
                Titulo = "Aviso de prueba",
                Mensaje = "Contenido dirigido al estudiante",
                RolDestinoId = RolesSistema.EstudianteId,
                Activa = true
            };
            context.NotificacionesGenerales.Add(notificacion);
            await context.SaveChangesAsync();
            notificacionId = notificacion.Id;
        }
        await IniciarSesionAsync(client, escenario.UsuarioEstudiante, escenario.PasswordEstudiante);

        var iniciales = await client.GetFromJsonAsync<JsonElement>("/api/me/notificaciones/no-leidas");
        Assert.Equal(1, iniciales.GetProperty("cantidad").GetInt32());
        var lectura = await client.PatchAsync($"/api/me/notificaciones/{notificacionId}/leida", null);
        Assert.Equal(HttpStatusCode.NoContent, lectura.StatusCode);
        var finales = await client.GetFromJsonAsync<JsonElement>("/api/me/notificaciones/no-leidas");
        Assert.Equal(0, finales.GetProperty("cantidad").GetInt32());
    }

    [Fact]
    public async Task Estudiante_Sube_Comprobante_Y_Queda_En_Revision()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(ClienteConCookies());
        var escenario = await SembrarEscenarioAcademicoAsync(factory);
        await IniciarSesionAsync(client, escenario.UsuarioEstudiante, escenario.PasswordEstudiante);
        var ahora = DateTime.UtcNow;
        using var contenido = new MultipartFormDataContent();
        var archivo = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("%PDF-1.4\n%%EOF"));
        archivo.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        contenido.Add(archivo, "archivo", "comprobante.pdf");
        contenido.Add(new StringContent("Transferencia"), "metodoPago");
        contenido.Add(new StringContent("18500"), "importe");

        var respuesta = await client.PostAsync(
            $"/api/me/pagos/cuotas/{ahora.Year}/{ahora.Month}/comprobante", contenido);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        using var verificacion = factory.Services.CreateScope();
        var db = verificacion.ServiceProvider.GetRequiredService<AppDbContext>();
        var cuota = await db.CuotasMensuales.SingleAsync(c => c.EstudianteId == escenario.EstudianteId);
        Assert.Equal(EstadoPago.EnRevision, cuota.Estado);
        Assert.Equal(18500m, cuota.Importe);
        Assert.False(string.IsNullOrWhiteSpace(cuota.ComprobanteUrl));
    }

    [Fact]
    public async Task Secretaria_Adjunta_Justificativo_Y_Queda_Identificada()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(ClienteConCookies());
        var escenario = await SembrarEscenarioAcademicoAsync(factory);
        int asistenciaId;
        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            var secretaria = CrearUsuario("secretaria.justifica", "Clave-segura-321",
                42777111, RolesSistema.SecretarioId);
            var inscripcion = new EstudianteMateria
            {
                IdEstudiante = escenario.EstudianteId, IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId, CicloLectivo = DateTime.UtcNow.Year,
                Estado = "Cursando"
            };
            context.AddRange(secretaria, inscripcion);
            await context.SaveChangesAsync();
            var asistencia = new Asistencia
            {
                IdEstudianteMateria = inscripcion.IdEstudianteMateria,
                IdDocente = escenario.DocenteId, Fecha = DateTime.UtcNow.Date,
                Tipo = "Ausente", CantidadInasistencias = 1
            };
            context.Add(asistencia);
            await context.SaveChangesAsync();
            asistenciaId = asistencia.IdAsistencia;
        }
        await IniciarSesionAsync(client, "secretaria.justifica", "Clave-segura-321");
        using var contenido = new MultipartFormDataContent();
        var archivo = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("%PDF-1.4\n%%EOF"));
        archivo.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        contenido.Add(archivo, "archivo", "certificado.pdf");

        var respuesta = await client.PostAsync(
            $"/api/gestion/usuarios/{escenario.EstudianteId}/inasistencias/{asistenciaId}/justificacion-archivo", contenido);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var verificacion = factory.Services.CreateScope();
        var db = verificacion.ServiceProvider.GetRequiredService<AppDbContext>();
        var asistenciaGuardada = await db.Set<Asistencia>().SingleAsync(a => a.IdAsistencia == asistenciaId);
        Assert.True(asistenciaGuardada.Justificada);
        Assert.NotNull(asistenciaGuardada.JustificadaPorUsuarioId);
        Assert.False(string.IsNullOrWhiteSpace(asistenciaGuardada.JustificativoArchivo));
    }

    [Fact]
    public async Task Directivo_Abre_Detalle_De_Carrera_E_Inasistencias()
    {
        await using var factory = new AcadionWebFactory();
        using var client = factory.CreateClient(ClienteConCookies());
        var escenario = await SembrarEscenarioAcademicoAsync(factory);
        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            var directivo = CrearUsuario("directivo.detalle", "Clave-segura-654",
                42888222, RolesSistema.DirectivoId);
            var inscripcion = new EstudianteMateria
            {
                IdEstudiante = escenario.EstudianteId, IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId, CicloLectivo = DateTime.UtcNow.Year,
                Estado = "Cursando"
            };
            context.AddRange(directivo, inscripcion);
            await context.SaveChangesAsync();
            context.Add(new Asistencia
            {
                IdEstudianteMateria = inscripcion.IdEstudianteMateria,
                IdDocente = escenario.DocenteId, Fecha = DateTime.UtcNow.Date,
                Tipo = "Ausente", CantidadInasistencias = 1
            });
            await context.SaveChangesAsync();
        }
        await IniciarSesionAsync(client, "directivo.detalle", "Clave-segura-654");

        var carrera = await client.GetAsync($"/api/directivo/detalle/carreras/{escenario.CarreraId}?cicloLectivo={DateTime.UtcNow.Year}");
        var materia = await client.GetAsync($"/api/directivo/detalle/materias/{escenario.MateriaId}/inasistencias?cicloLectivo={DateTime.UtcNow.Year}");
        Assert.Equal(HttpStatusCode.OK, carrera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, materia.StatusCode);
        Assert.Contains("Persona", await carrera.Content.ReadAsStringAsync());
        Assert.Contains("inasistencias", await materia.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Estudiante_Inactivo_No_Aparece_Al_Docente_Pero_Si_A_Secretaria()
    {
        await using var factory = new AcadionWebFactory();
        var escenario = await SembrarEscenarioAcademicoAsync(factory);
        const string usuarioSecretaria = "secretaria.inactivos";
        const string claveSecretaria = "Clave-segura-987";
        using (var alcance = factory.Services.CreateScope())
        {
            var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
            var estudiante = await context.Usuarios.SingleAsync(u => u.Id == escenario.EstudianteId);
            estudiante.Estado = EstadoUsuario.Inactivo;
            context.EstudianteMaterias.Add(new EstudianteMateria
            {
                IdEstudiante = escenario.EstudianteId,
                IdMateria = escenario.MateriaId,
                IdDocente = escenario.DocenteId,
                CicloLectivo = DateTime.UtcNow.Year,
                Estado = "Cursando"
            });
            context.Usuarios.Add(CrearUsuario(usuarioSecretaria, claveSecretaria,
                42999333, RolesSistema.SecretarioId));
            await context.SaveChangesAsync();
        }

        using var docente = factory.CreateClient(ClienteConCookies());
        await IniciarSesionAsync(docente, escenario.UsuarioDocente, escenario.PasswordDocente);
        var listaDocente = await docente.GetFromJsonAsync<JsonElement>(
            $"/api/docente/alumnos?materiaId={escenario.MateriaId}&cicloLectivo={DateTime.UtcNow.Year}");
        Assert.Equal(0, listaDocente.GetArrayLength());

        using var secretaria = factory.CreateClient(ClienteConCookies());
        await IniciarSesionAsync(secretaria, usuarioSecretaria, claveSecretaria);
        var listaSecretaria = await secretaria.GetFromJsonAsync<JsonElement>(
            $"/api/gestion/usuarios/?rolId={RolesSistema.EstudianteId}");
        Assert.Contains(escenario.UsuarioEstudiante, listaSecretaria.GetRawText());
        Assert.Contains("Inactivo", listaSecretaria.GetRawText());
    }

    private static async Task CrearEIniciarSecretarioAsync(
        HttpClient client, long dni, string nombre, string apellido)
    {
        var alta = await client.PostAsJsonAsync("/api/auth/bootstrap-secretario", new
        {
            nombre,
            apellido,
            dni,
            fechaNacimiento = new DateTime(1991, 5, 10),
            direccion = "Calle de prueba 100",
            localidad = "Rafaela",
            codigoPostal = 2300,
            emailPersonal = $"{nombre.ToLowerInvariant()}@example.com",
            emailInstitucional = $"{nombre.ToLowerInvariant()}@acadion.test",
            telefonoContacto = "3492111111"
        });
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var cuenta = await alta.Content.ReadFromJsonAsync<CuentaCreada>();
        Assert.NotNull(cuenta);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            nombreUsuario = cuenta.NombreUsuario,
            password = dni.ToString()
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private static WebApplicationFactoryClientOptions ClienteConCookies() => new()
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
        AllowAutoRedirect = false
    };

    private static async Task IniciarSesionAsync(HttpClient client, string usuario, string password)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            nombreUsuario = usuario,
            password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private static Usuario CrearUsuario(string nombreUsuario, string password, long dni,
        int rolId, int? carreraId = null, int? planId = null)
    {
        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            RolId = rolId,
            Estado = EstadoUsuario.Activo,
            DebeCambiarPassword = false,
            CarreraId = carreraId,
            PlanEstudioId = planId,
            Persona = new Persona
            {
                Nombre = "Persona",
                Apellido = nombreUsuario,
                Dni = dni,
                FechaNacimiento = new DateTime(2000, 1, 1),
                Email = $"{nombreUsuario}@example.com"
            }
        };
        usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, password);
        return usuario;
    }

    private static async Task<EscenarioAcademico> SembrarEscenarioAcademicoAsync(
        AcadionWebFactory factory, bool incluirCorrelativa = false)
    {
        using var alcance = factory.Services.CreateScope();
        var context = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
        var carrera = new Carrera
        {
            Nombre = "Tecnicatura de prueba",
            PlanEstudios = "Plan de prueba",
            Tipo = "Tecnicatura",
            DuracionAnios = 3,
            CapacidadMaximaEstudiantes = 40
        };
        context.Add(carrera);
        await context.SaveChangesAsync();
        var plan = new PlanEstudio
        {
            CarreraId = carrera.IdCarrera,
            Codigo = "PLAN-TEST",
            VigenteDesde = DateTime.UtcNow.Year,
            Activo = true
        };
        var anio = new Anio
        {
            IdCarrera = carrera.IdCarrera,
            NumeroAnio = 1,
            NombreAnio = "Primer año"
        };
        context.AddRange(plan, anio);
        await context.SaveChangesAsync();
        var correlativa = new Materia
        {
            Nombre = "Introducción",
            Modalidad = "Presencial",
            Estado = "Activa",
            IdAnio = anio.IdAnio,
            PlanEstudioId = plan.Id
        };
        var materia = new Materia
        {
            Nombre = "Programación",
            Modalidad = "Presencial",
            Estado = "Activa",
            IdAnio = anio.IdAnio,
            PlanEstudioId = plan.Id
        };
        if (incluirCorrelativa) materia.Correlativas.Add(correlativa);
        context.AddRange(correlativa, materia);
        var docente = CrearUsuario("docente.prueba", "clave-docente-123", 35111222,
            RolesSistema.DocenteId);
        var estudiante = CrearUsuario("estudiante.prueba", "clave-alumno-123", 45111222,
            RolesSistema.EstudianteId, carrera.IdCarrera, plan.Id);
        context.Usuarios.AddRange(docente, estudiante);
        await context.SaveChangesAsync();
        context.DocentesMaterias.Add(new DocenteMateria
        {
            IdDocente = docente.Id,
            IdMateria = materia.IdMateria,
            CicloLectivo = DateTime.UtcNow.Year,
            Cuatrimestre = "Anual"
        });
        context.PeriodosInscripcionMaterias.Add(new PeriodoInscripcionMateria
        {
            CarreraId = carrera.IdCarrera,
            CicloLectivo = DateTime.UtcNow.Year,
            FechaInicioUtc = DateTime.UtcNow.AddDays(-1),
            FechaFinUtc = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();

        return new EscenarioAcademico(carrera.IdCarrera, plan.Id, materia.IdMateria,
            incluirCorrelativa ? correlativa.IdMateria : null, docente.Id, estudiante.Id,
            docente.NombreUsuario, "clave-docente-123", estudiante.NombreUsuario,
            "clave-alumno-123");
    }

    private sealed class CuentaCreada
    {
        public string NombreUsuario { get; set; } = string.Empty;
    }

    private sealed class RecuperacionDisponible
    {
        public bool Disponible { get; set; }
    }

    private sealed record EscenarioAcademico(int CarreraId, int PlanId, int MateriaId,
        int? CorrelativaId, int DocenteId, int EstudianteId, string UsuarioDocente,
        string PasswordDocente, string UsuarioEstudiante, string PasswordEstudiante);
}

public sealed class AcadionWebFactory : WebApplicationFactory<Program>
{
    private readonly string _baseDePrueba = $"acadion-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "clave-exclusiva-para-pruebas-integracion-acadion-2026");
        builder.UseSetting("Jwt:Issuer", "AcadionTests");
        builder.UseSetting("Jwt:Audience", "AcadionTests");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.ConfigureAppConfiguration((_, configuracion) =>
        {
            configuracion.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "clave-exclusiva-para-pruebas-integracion-acadion-2026",
                ["Jwt:Issuer"] = "AcadionTests",
                ["Jwt:Audience"] = "AcadionTests",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Uploads:RutaPerfiles"] = Path.Combine(Path.GetTempPath(), "acadion-tests-perfiles")
            });
        });
        builder.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            servicios.RemoveAll<DbContextOptions<AppDbContext>>();
            servicios.RemoveAll<AppDbContext>();
            servicios.AddDbContext<AppDbContext>(opciones =>
            {
                opciones.UseInMemoryDatabase(_baseDePrueba);
                opciones.ConfigureWarnings(advertencias =>
                    advertencias.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
            servicios.RemoveAll<IEmailService>();
            servicios.AddSingleton<IEmailService, EmailDePrueba>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var alcance = host.Services.CreateScope();
        alcance.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    private sealed class EmailDePrueba : IEmailService
    {
        public bool EstaConfigurado => false;

        public Task EnviarAsync(string destinatario, string asunto, string cuerpoTexto) =>
            throw new InvalidOperationException("El correo no debe enviarse en estas pruebas.");
    }
}
