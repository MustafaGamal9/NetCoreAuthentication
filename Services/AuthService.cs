using JwtApp.Data;
using JwtApp.DTO;
using JwtApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System; 
using System.Collections.Generic; 
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks; 

namespace JwtApp.Services
{
    // Inject UserManager and RoleManager
    public class AuthService(
        JWTDbContext context, 
        IConfiguration configuration,
        UserManager<User> userManager, 
        RoleManager<IdentityRole<Guid>> roleManager)
        : IAuthService
    {
        public async Task<TokenResponseDTO?> LoginAsync(LoginDTO request)
        {
            // Find user by username
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user is null)
            {
                return null; // User not found
            }

            // Check password using Identity's password hasher
            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return null; // Invalid password
            }

            // User found and password is correct, generate tokens
            return await CreateTokenResponse(user);
        }

        private async Task<TokenResponseDTO> CreateTokenResponse(User user)
        {
            var accessToken = await CreateToken(user); // Pass user to CreateToken
            var refreshToken = await GenerateAndSaveRefreshTokenAsync(user); // Generate and save refresh token

            var response = new TokenResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return response;
        }

 

        public async Task<TokenResponseDTO?> RefreshTokenAsync(RefreshTokenRequestDTO request)
        {
            // Validate the refresh token against the stored one
            var user = await ValidateRefreshTokenAsync(request.UserID, request.RefreshToken);
            if (user is null)
                return null; // Invalid user or token

            // Generate new tokens
            return await CreateTokenResponse(user);
        }

        private async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            // Use UserManager to find the user by ID
            var user = await userManager.FindByIdAsync(userId.ToString());

            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null; // Invalid user, token mismatch, or token expired
            }
            return user; // Valid token
        }


        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

  
        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); 

            context.Users.Update(user); 
            await context.SaveChangesAsync(); 


            return refreshToken;
        }


        private async Task<string> CreateToken(User user)
        {
            var roles = await userManager.GetRolesAsync(user);

            var claims = new List<Claim>
                   {
                       new Claim(ClaimTypes.Name, user.UserName),
                       new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                       
                   };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = configuration.GetValue<string>("AppSettings:Issuer"),
                Audience = configuration.GetValue<string>("AppSettings:Audience"),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}