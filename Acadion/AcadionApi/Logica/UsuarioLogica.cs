using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios; 
using Microsoft.AspNetCore.Identity;

namespace AcadionApi.Logica
{
    public class UsuarioLogica : IUsuarioLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsuarioLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // =========================
        // REGISTRAR USUARIO
        // =========================
        public async Task<UsuarioDto> RegistrarUsuarioAsync(UsuarioCrearDto dto)
        {
            // Validar persona existente
            var personaExistente =
                await _unitOfWork.Personas.GetByIdAsync(dto.PersonaId);

            if (personaExistente == null)
                throw new ArgumentException(
                    "La persona especificada no existe.");

            // Validar username único
            var usuarioExistente =
                await _unitOfWork.Usuarios
                    .GetByNombreUsuarioAsync(dto.NombreUsuario);

            if (usuarioExistente != null)
                throw new InvalidOperationException(
                    "El nombre de usuario ya existe.");

            var nuevoUsuario = new Usuario
            {
                // Relación con Persona
                PersonaId = personaExistente.Id,
                Persona = personaExistente,

                // Datos propios del usuario
                NombreUsuario = dto.NombreUsuario,

                Rol = (Rol)dto.Rol,

                Estado = (EstadoUsuario)dto.Estado,

                TelefonoContacto = dto.TelefonoContacto,

                // Estudiante
                //Matricula = dto.Matricula,

                //Legajo = dto.Legajo,

                // Docente
                //Especialidad = dto.Especialidad,

                //TituloAcademico = dto.TituloAcademico
            };

            // HASH PASSWORD
            var passwordHasher = new PasswordHasher<Usuario>();

            nuevoUsuario.PasswordHash =
                passwordHasher.HashPassword(
                    nuevoUsuario,
                    dto.Password);

            await _unitOfWork.Usuarios.AddAsync(nuevoUsuario);

            await _unitOfWork.SaveChangesAsync();

            return new UsuarioDto
            {
                Id = nuevoUsuario.Id,

                PersonaId = nuevoUsuario.PersonaId,

                NombreUsuario = nuevoUsuario.NombreUsuario,

                // Datos de Persona
                Nombre = nuevoUsuario.Persona.Nombre,

                Apellido = nuevoUsuario.Persona.Apellido,

                Dni = nuevoUsuario.Persona.Dni,

                FechaNacimiento =
                    nuevoUsuario.Persona.FechaNacimiento,

                // Datos Usuario
                Email = nuevoUsuario.EmailInstitucional,

                Rol = nuevoUsuario.Rol.ToString(),

                Estado = nuevoUsuario.Estado.ToString(),

                FechaCreacion = nuevoUsuario.FechaCreacion,

                FechaUltimoAcceso =
                    nuevoUsuario.FechaUltimoAcceso,

                TelefonoContacto =
                    nuevoUsuario.TelefonoContacto,

                // Estudiante
                Matricula = nuevoUsuario.Matricula,

                Legajo = nuevoUsuario.Legajo,

                PromedioGeneral =
                    nuevoUsuario.PromedioGeneral,

                // Docente
                Especialidad =
                    nuevoUsuario.Especialidad,

                TituloAcademico =
                    nuevoUsuario.TituloAcademico
            };
        }

        // =========================
        // LOGIN
        // =========================
        public async Task<bool> LoginAsync(LoginDto dto)
        {
            var usuario = await _unitOfWork.Usuarios
                .GetByNombreUsuarioAsync(dto.NombreUsuario);

            if (usuario == null)
                return false;

            var passwordHasher = new PasswordHasher<Usuario>();

            var resultado =
                passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.PasswordHash,
                    dto.Password);

            return resultado ==
                   PasswordVerificationResult.Success;
        }

        // =========================
        // OBTENER TODOS
        // =========================
        public async Task<IEnumerable<UsuarioListaDto>> ObtenerUsuariosAsync()
        {
        var usuarios = await _unitOfWork.Usuarios.GetAllAsync();

        return usuarios.Select(u => new UsuarioListaDto
        {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        // Evitamos el NullReferenceException usando el operador ?.
        Nombre = u.Persona?.Nombre ?? "Sin Nombre",
        Apellido = u.Persona?.Apellido ?? "Sin Apellido",
        Rol = u.Rol.ToString(),
        Estado = u.Estado.ToString()
        });
        }


        // =========================
        // OBTENER POR ID
        // =========================
        public async Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(int id)
        {
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);

        if (usuario == null)
            return null;
        return new UsuarioDto
        {
        Id = usuario.Id,
        PersonaId = usuario.PersonaId,
        NombreUsuario = usuario.NombreUsuario,

        // Mapeo seguro de Persona
        Nombre = usuario.Persona?.Nombre ?? string.Empty,
        Apellido = usuario.Persona?.Apellido ?? string.Empty,
        Dni = usuario.Persona?.Dni ?? 0,
        FechaNacimiento = usuario.Persona?.FechaNacimiento ?? DateTime.MinValue,

        // Datos Usuario
        Email = usuario.EmailInstitucional,
        Rol = usuario.Rol.ToString(),
        Estado = usuario.Estado.ToString(),
        FechaCreacion = usuario.FechaCreacion,
        FechaUltimoAcceso = usuario.FechaUltimoAcceso,
        TelefonoContacto = usuario.TelefonoContacto,

        // Estudiante
        Matricula = usuario.Matricula,
        Legajo = usuario.Legajo,
        PromedioGeneral = usuario.PromedioGeneral,

        // Docente
        Especialidad = usuario.Especialidad,
        TituloAcademico = usuario.TituloAcademico
        };
    }

        // =========================
        // ACTUALIZAR
        // =========================
        public async Task<bool>
            ActualizarUsuarioAsync(
                int id,
                UsuarioActualizarDto dto)
        {
            if (dto == null)
                throw new ArgumentException(
                    "Los datos de actualización no pueden ser nulos.");

            var usuarioExistente =
                await _unitOfWork.Usuarios.GetByIdAsync(id);

            if (usuarioExistente == null)
                return false;

            usuarioExistente.NombreUsuario =
                dto.NombreUsuario;

            usuarioExistente.EmailInstitucional =
                dto.Email;

            usuarioExistente.Rol =
                (Rol)dto.Rol;

            usuarioExistente.Estado =
                (EstadoUsuario)dto.Estado;

            usuarioExistente.TelefonoContacto =
                dto.TelefonoContacto;

            // Estudiante
            usuarioExistente.Matricula =
                dto.Matricula;

            usuarioExistente.Legajo =
                dto.Legajo;

            usuarioExistente.PromedioGeneral =
                dto.PromedioGeneral;

            // Docente
            usuarioExistente.Especialidad =
                dto.Especialidad;

            usuarioExistente.TituloAcademico =
                dto.TituloAcademico;

            await _unitOfWork.Usuarios
                .UpdateAsync(usuarioExistente);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // ELIMINAR
        // =========================
        public async Task<bool>
            EliminarUsuarioAsync(int id)
        {
            var usuario =
                await _unitOfWork.Usuarios.GetByIdAsync(id);

            if (usuario == null)
                return false;

            await _unitOfWork.Usuarios
                .DeleteAsync(usuario);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        // =========================
        // BUSCAR USERNAME
        // =========================
        public async Task<Usuario?>
            GetByNombreUsuarioAsync(
                string nombreUsuario)
        {
            return await _unitOfWork.Usuarios
                .GetByNombreUsuarioAsync(nombreUsuario);
        }
    }
}