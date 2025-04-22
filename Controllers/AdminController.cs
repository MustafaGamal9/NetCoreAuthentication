using JwtApp.DTO; 
using JwtApp.Models; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JwtApp.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // POST /api/Admin/create-user
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var roleName = request.Role; // e.g., "Admin" or "Student"
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return BadRequest($"Invalid role specified: {roleName}. Role must be 'Admin' or 'Student'.");
            }

            // 2. Check if username is already taken
            var existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null)
            {
                return BadRequest($"Username '{request.UserName}' is already taken.");
            }

            // 3. Create the new user object
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                SecurityStamp = Guid.NewGuid().ToString(),
                EmailConfirmed = true,
                RefreshToken = null,
                RefreshTokenExpiryTime = null
            };

            // 4. Attempt to create the user with the provided password
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "User creation failed.", Errors = result.Errors.Select(e => e.Description) });
            }

            // 5. Assign the specified role to the new user
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                return BadRequest(new { Message = $"User '{request.UserName}' created, but failed to assign role '{roleName}'.", Errors = roleResult.Errors.Select(e => e.Description) });
            }

            // 6. Success - Return details about the created user (excluding sensitive info)
            return Ok(new
            {
                Message = $"User '{user.UserName}' created successfully with role '{roleName}'.",
                UserId = user.Id,
                Username = user.UserName
            });
        }


    }
}