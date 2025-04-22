
using JwtApp.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JwtApp.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JwtApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase // Inject IAuthService
    {
        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(UserDTO request) 
        {
            var user = await authService.RegisterAsync(request); 
            if (user is null)
            {
                return BadRequest("Username already exists or registration failed.");
            }
   
            return Ok($"User '{user.UserName}' registered successfully as Student.");
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDTO>> Login(UserDTO request) 
        {
            var tokenResponse = await authService.LoginAsync(request);
            if (tokenResponse is null)
                return BadRequest("Invalid Username or password");

            return Ok(tokenResponse);
        }

        [HttpPost("refresh-token")] 
        public async Task<ActionResult<TokenResponseDTO>> RefreshToken(RefreshTokenRequestDTO request)
        {
            var tokenResponse = await authService.RefreshTokenAsync(request);
            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken) || string.IsNullOrEmpty(tokenResponse.RefreshToken))
                return BadRequest("Invalid client request or refresh token expired."); // More specific error

            return Ok(tokenResponse);
        }

  
        [HttpGet]
        [Authorize]
        public ActionResult<string> Get()
        {
           
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var userName = User.FindFirstValue(ClaimTypes.Name); 
            return $"Hello {userName} (ID: {userId}), you are authenticated!";
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public ActionResult<string> GetAdmin()
        {
            var userName = User.FindFirstValue(ClaimTypes.Name);
            return $"Hello Admin '{userName}'!";
        }
    }
}