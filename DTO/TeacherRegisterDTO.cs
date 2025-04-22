using System.ComponentModel.DataAnnotations;

namespace JwtApp.DTO
{
    public class TeacherRegisterDTO
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;
    }
}

