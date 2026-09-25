using AcadionApi.Logica.DTOs;

namespace AcadionApi.Logica
{
    public interface ILoginLogica
    {
        Task<LoginRespuestaDto?> LoginAsync(LoginDto dto);
        Task<bool> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto);
    }
}
