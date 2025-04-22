using JwtApp.Data;
using JwtApp.DTO;
using JwtApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace JwtApp.Services
{
    public class AuthService(JWTDbContext context,IConfiguration configuration) : IAuthService
    {
        public async Task<TokenResponseDTO?> LoginAsync(UserDTO request)
        {
            var user = await context.Users.FirstOrDefaultAsync(x => x.UserName == request.UserName); // get user from db

            if (user is null)
            {
                return null;
            }
            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return await CreateTokenResponse(user);
        }

        private async Task<TokenResponseDTO> CreateTokenResponse(User? user)
        {
            var response = new TokenResponseDTO
            {
                AccessToken = CreateToken(user), // create access token
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user) // create refresh token
            };
            return response;
        }

        public async Task<User?> RegisterAsync(UserDTO request)
        {
            if(await context.Users.AnyAsync(x => x.UserName == request.UserName))
            {
                return null; // user already exist
            }
            var user = new User(); // user instance

            var hashedPassword = new PasswordHasher<User>()
                         .HashPassword(user, request.Password); // hash password that user sends

            user.UserName = request.UserName; // give username
            user.PasswordHash = hashedPassword; // give password hash

            context.Users.Add(user); // adding to db
            context.SaveChangesAsync();
            return user;
        }

        public async Task<TokenResponseDTO?> RefreshTokenAsync(RefreshTokenRequestDTO request)
        {
            var user = await ValidateRefreshTokenAsync(request.UserID,request.RefreshToken);
            if (user is null)
                return null;

            return await CreateTokenResponse(user); // create new token 

        }
        private async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var user = await context.Users.FindAsync(userId);
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return null; // invalid token
            }
            return user; // valid token
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
            user.RefreshToken = refreshToken; // save refresh token to db
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // set expiry time
            await context.SaveChangesAsync();
            return refreshToken;
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim> // claims sent with the token
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: configuration.GetValue<string>("AppSettings:Issuer"),
                audience: configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }


    }
}
