using System.ComponentModel.DataAnnotations;

namespace JwtApp.DTO
{
    public class CreateUserDTO
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Admin|Student)$", ErrorMessage = "Role must be either 'Admin' or 'Student'.")]
        public string Role { get; set; } = string.Empty; 
    }
}
