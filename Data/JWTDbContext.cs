using JwtApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JwtApp.Data
{
    public class JWTDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public JWTDbContext(DbContextOptions<JWTDbContext> options) : base(options) { }

 

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
  
        }
    }
}