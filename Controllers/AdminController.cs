using JwtApp.DTO; 
using JwtApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var roleName = request.Role;
            if (!await _roleManager.RoleExistsAsync(roleName))
                return BadRequest($"Invalid role specified: {roleName}. Role must be 'Admin', 'Student', or 'Teacher'.");

            var existingUserByName = await _userManager.FindByNameAsync(request.UserName);
            if (existingUserByName != null)
                return BadRequest($"Username '{request.UserName}' is already taken.");

            var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingUserByEmail != null)
                return BadRequest($"Email '{request.Email}' is already registered.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                EmailConfirmed = true,
                Level = request.Role == "Student" ? "N/A (Admin Created)" : null,
                Subject = request.Role == "Teacher" ? "N/A (Admin Created)" : null
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(new { Message = "User creation failed.", Errors = result.Errors.Select(e => e.Description) });

            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
                return BadRequest(new { Message = $"User '{request.UserName}' created, but failed to assign role '{roleName}'.", Errors = roleResult.Errors.Select(e => e.Description) });

            return Ok(new
            {
                Message = $"CONFIRMED: User '{user.UserName}' created successfully with role '{roleName}'.",
                UserId = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Role = roleName
            });
        }

        // --- GET ALL USERS  ---
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userResults = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userResults.Add(new
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles,
                    Level = user.Level,
                    Subject = user.Subject
                });
            }
            return Ok(userResults);
        }


        [HttpGet("users/{id:guid}")]
        public async Task<ActionResult<object>> GetUserById(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound($"User with ID '{id}' not found.");

            var roles = await _userManager.GetRolesAsync(user);
            var userResult = new
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles,
                Level = user.Level,
                Subject = user.Subject
            };
            return Ok(userResult);
        }

        // PUT /api/Admin/users/{id}
        [HttpPut("users/{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] CreateUserDTO request) // should remove password later for security purposes
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound($"User with ID '{id}' not found.");

            if (!await _roleManager.RoleExistsAsync(request.Role))
                return BadRequest($"Invalid role specified: {request.Role}. Role must be 'Admin', 'Student', or 'Teacher'.");

            if (user.UserName != request.UserName)
            {
                var existingUserByName = await _userManager.FindByNameAsync(request.UserName);
                if (existingUserByName != null && existingUserByName.Id != user.Id)
                    return BadRequest($"Username '{request.UserName}' is already taken by another user.");
                await _userManager.SetUserNameAsync(user, request.UserName);
            }
            if (user.Email != request.Email)
            {
                var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
                if (existingUserByEmail != null && existingUserByEmail.Id != user.Id)
                    return BadRequest($"Email '{request.Email}' is already registered by another user.");
                await _userManager.SetEmailAsync(user, request.Email);
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                // Generate a password reset token 
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Use the token to reset the password using Identity's secure mechanism
                var passwordResult = await _userManager.ResetPasswordAsync(user, resetToken, request.Password);

                if (!passwordResult.Succeeded)
                {
                    return BadRequest(new { Message = $"Password update failed for user '{user.UserName}'.", Errors = passwordResult.Errors.Select(e => e.Description) });
                }
                // Password successfully updated and hashed by Identity
            }


 
            user.Level = request.Role == "Student" ? user.Level : null; 
            user.Subject = request.Role == "Teacher" ? user.Subject : null; 
           // If we want update Level/Subject, we should add these fields to CreateUserDTO or use diffrent DTO


            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(request.Role) || currentRoles.Count > 1) 
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                    return BadRequest(new { Message = $"Failed to remove existing roles for user '{user.UserName}'.", Errors = removeResult.Errors.Select(e => e.Description) });

                var addResult = await _userManager.AddToRoleAsync(user, request.Role);
                if (!addResult.Succeeded)
                    return BadRequest(new { Message = $"Failed to assign new role '{request.Role}' for user '{user.UserName}'.", Errors = addResult.Errors.Select(e => e.Description) });
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
  
                return BadRequest(new { Message = $"Failed to save final updates for user '{user.UserName}'.", Errors = updateResult.Errors.Select(e => e.Description) });

            return Ok(new { Message = $"CONFIRMED: User '{user.UserName}' (ID: {id}) updated successfully." + (!string.IsNullOrWhiteSpace(request.Password) ? " Password was changed." : "") });
        }

        // --- DELETE USER ---
        [HttpDelete("users/{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound($"User with ID '{id}' not found.");

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == id.ToString())
                return BadRequest("Administrators cannot delete their own account.");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { Message = $"Failed to delete user '{user.UserName}'.", Errors = result.Errors.Select(e => e.Description) });

            return Ok(new { Message = $"CONFIRMED: User '{user.UserName}' (ID: {id}) deleted successfully." });
        }
    }
}