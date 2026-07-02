namespace AcadionApi.Logica
{
    public interface ILoginLogica
    {
        Task<bool> LoginAsync(LoginDto dto);
    }
}