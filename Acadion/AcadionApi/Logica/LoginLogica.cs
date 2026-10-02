using Microsoft.AspNetCore.Identity;
using AcadionApi.Repositorios;
using AcadionApi.Logica.DTOs;
using AcadionApi.Seguridad;

namespace AcadionApi.Logica
{
    public class LoginLogica : ILoginLogica
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public LoginLogica(IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<LoginRespuestaDto?> LoginAsync(LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NombreUsuario) || string.IsNullOrEmpty(dto.Password))
                return null;

            var usuario = await _unitOfWork.Usuarios
                .GetByNombreUsuarioAsync(dto.NombreUsuario);

            if (usuario == null)
                return null;

            var passwordHasher = new PasswordHasher<Usuario>();

            var resultado = passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.PasswordHash,
                dto.Password);

            if (resultado == PasswordVerificationResult.Failed)
                return null;

            if (usuario.Estado == EstadoUsuario.Inactivo)
                throw new InvalidOperationException(
                    "La cuenta está inactiva. Contactá a Secretaría para solicitar su reactivación.");

            if (usuario.Estado == EstadoUsuario.Suspendido)
                throw new InvalidOperationException(
                    "La cuenta está suspendida. Contactá a Secretaría para conocer el motivo.");

            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
                usuario.PasswordHash = passwordHasher.HashPassword(usuario, dto.Password);

            usuario.FechaUltimoAcceso = DateTime.UtcNow;
            await _unitOfWork.Usuarios.UpdateAsync(usuario);

            var token = _tokenService.Generar(usuario);
            return new LoginRespuestaDto
            {
                PersonaId = usuario.PersonaId,
                UsuarioId = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                Rol = usuario.Rol.Nombre,
                Permisos = usuario.Rol.RolPermisos.Select(rp => rp.Permiso.Codigo).ToArray(),
                Token = token.Token,
                ExpiraEnUtc = token.ExpiraEnUtc,
                DebeCambiarPassword = usuario.DebeCambiarPassword
            };
        }

        public async Task<bool> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto)
        {
            SeguridadContrasena.Validar(dto.PasswordNueva);

            if (string.IsNullOrEmpty(dto.PasswordActual))
                return false;

            var usuario = await _unitOfWork.Usuarios.GetByIdAsync(usuarioId);
            if (usuario is null)
                return false;

            var passwordHasher = new PasswordHasher<Usuario>();
            var verificacion = passwordHasher.VerifyHashedPassword(
                usuario, usuario.PasswordHash, dto.PasswordActual);

            if (verificacion == PasswordVerificationResult.Failed)
                return false;

            usuario.PasswordHash = passwordHasher.HashPassword(usuario, dto.PasswordNueva);
            usuario.DebeCambiarPassword = false;
            await _unitOfWork.Usuarios.UpdateAsync(usuario);
            return true;
        }
    }
}
