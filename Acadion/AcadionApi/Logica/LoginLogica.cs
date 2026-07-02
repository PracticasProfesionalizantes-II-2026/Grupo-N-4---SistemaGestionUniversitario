using Microsoft.AspNetCore.Identity;
using AcadionApi.Repositorios;

namespace AcadionApi.Logica
{
    public class LoginLogica : ILoginLogica
    {
        private readonly IUnitOfWork _unitOfWork;

        public LoginLogica(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> LoginAsync(LoginDto dto)
        {
            var usuario = await _unitOfWork.Usuarios
                .GetByNombreUsuarioAsync(dto.NombreUsuario);

            if (usuario == null)
                return false;

            var passwordHasher = new PasswordHasher<Usuario>();

            var resultado = passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.PasswordHash,
                dto.Password);

            return resultado == PasswordVerificationResult.Success;
        }
    }
}