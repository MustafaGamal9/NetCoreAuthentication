using JwtApp.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using JwtApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace JwtApp.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {


        [HttpPost("register")]
        public async Task<ActionResult<TokenResponseDTO>> Register(UserDTO request)
        {
            var result = await authService.RegisterAsync(request);
            if (result is null)
            {
                return BadRequest("Username already exists");
            }
            return Ok(result);

        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDTO request)
        {
            var token = await authService.LoginAsync(request);
            if (token is null)
                return BadRequest("Invalid Username or password");

            return Ok(token);

        }

        [HttpPost("Refresh-Token")]
        public async Task<ActionResult<TokenResponseDTO>> RefreshToken(RefreshTokenRequestDTO request)
        {
            var token = await authService.RefreshTokenAsync(request);
            if (token is null || token.AccessToken is null || token.RefreshToken is null)
                return BadRequest("Invalid Token");
            return Ok(token);
        }

        [HttpGet]
        [Authorize]
        public ActionResult<string> Get() // for testing
        {
            return "Hello im authenticated";
        }
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public ActionResult<string> GetAdmin() // for testing
        {
            return "Hello im The Admin";
        }

    }
}
