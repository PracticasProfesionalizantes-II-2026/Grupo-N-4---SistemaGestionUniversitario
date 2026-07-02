using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AcadionApi.DTOs;
using AcadionApi.Repositorios;

namespace AcadionApi.Logica
{
    public class PersonaLogica : IPersonaLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public PersonaLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PersonaDto> RegistrarPersonaAsync(PersonaCrearDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Nombre) ||
                dto.Dni == 0)
            {
                throw new ArgumentException("Datos inválidos.");
            }

            if (dto.FechaNacimiento > DateTime.Today)
            {
                throw new ArgumentException("La fecha de nacimiento no puede ser futura.");
            }

            var personaExistente = await _unitOfWork.Personas.GetByDniAsync(dto.Dni);

            if (personaExistente != null)
            {
                throw new InvalidOperationException("El DNI ya está registrado.");
            }

            var emailExistente = await _unitOfWork.Personas.GetByEmailAsync(dto.Email);

            if (emailExistente != null)
            {
                throw new InvalidOperationException("El email ya está registrado.");
            }

            var nuevaPersona = new Persona
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Dni = dto.Dni,
                FechaNacimiento = dto.FechaNacimiento,
                Direccion = dto.Direccion,
                Localidad = dto.Localidad,
                CodigoPostal = dto.CodigoPostal,
                Email = dto.Email
            };

            await _unitOfWork.Personas.AddAsync(nuevaPersona);
            await _unitOfWork.SaveChangesAsync();

            return new PersonaDto
            {
                Id = nuevaPersona.Id,
                Nombre = nuevaPersona.Nombre,
                Apellido = nuevaPersona.Apellido,
                Dni = nuevaPersona.Dni,
                FechaNacimiento = nuevaPersona.FechaNacimiento,
                Edad = nuevaPersona.Edad,
                Direccion = nuevaPersona.Direccion,
                Localidad = nuevaPersona.Localidad,
                CodigoPostal = nuevaPersona.CodigoPostal,
                Email = nuevaPersona.Email
            };
        }

                public async Task<IEnumerable<PersonaDto>> ObtenerPersonasAsync()
        {
            var personas = await _unitOfWork.Personas.GetAllAsync();

            return personas.Select(p => new PersonaDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                Dni = p.Dni,
                FechaNacimiento = p.FechaNacimiento,
                Edad = p.Edad,
                Direccion = p.Direccion,
                Localidad = p.Localidad,
                CodigoPostal = p.CodigoPostal,
                Email = p.Email
            });
        }

                public async Task<PersonaDto?> ObtenerPersonaPorIdAsync(int id)
        {
            var p = await _unitOfWork.Personas.GetByIdAsync(id);

            if (p == null)
                return null;

            return new PersonaDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Apellido = p.Apellido,
                Dni = p.Dni,
                FechaNacimiento = p.FechaNacimiento,
                Edad = p.Edad,
                Direccion = p.Direccion,
                Localidad = p.Localidad,
                CodigoPostal = p.CodigoPostal,
                Email = p.Email
            };
        }

                public async Task<bool> ActualizarPersonaAsync(int id, PersonaActualizarDto dto)
        {
            if (dto == null)
                throw new ArgumentException("Datos inválidos.");

            var p = await _unitOfWork.Personas.GetByIdAsync(id);

            if (p == null)
                return false;

            p.Nombre = dto.Nombre;
            p.Apellido = dto.Apellido;
            p.Dni = dto.Dni;
            p.FechaNacimiento = dto.FechaNacimiento;
            p.Direccion = dto.Direccion;
            p.Localidad = dto.Localidad;
            p.CodigoPostal = dto.CodigoPostal;
            p.Email = dto.Email;

            await _unitOfWork.Personas.UpdateAsync(p);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> EliminarPersonaAsync(int id)
        {
            var p = await _unitOfWork.Personas.GetByIdAsync(id);
            if (p == null) return false;

            await _unitOfWork.Personas.DeleteAsync(p);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}