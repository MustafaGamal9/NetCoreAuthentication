using JwtApp.DTO;
using JwtApp.Models;

namespace JwtApp.Services
{
    public interface IAuthService
    {
        Task<TokenResponseDTO?> LoginAsync(LoginDTO request);

        Task<TokenResponseDTO?> RefreshTokenAsync(RefreshTokenRequestDTO request);
    }
}
