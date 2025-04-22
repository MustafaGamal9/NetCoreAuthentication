﻿using Microsoft.AspNetCore.Identity;

namespace JwtApp.Models
{
    public class User : IdentityUser<Guid>
    {
        public string? RefreshToken { get; set; } = string.Empty;

        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
