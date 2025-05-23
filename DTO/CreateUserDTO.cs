﻿using System.ComponentModel.DataAnnotations;

namespace JwtApp.DTO
{
    public class CreateUserDTO
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Admin|Student|Teacher)$", ErrorMessage = "Role must be either 'Admin' or 'Student or Teacher'.")]
        public string Role { get; set; } = string.Empty; 


    }
}
