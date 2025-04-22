using JwtApp.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtApp.Data
{
    public class JWTDbContext(DbContextOptions<JWTDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
    }

}
