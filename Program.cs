using JwtApp.Data;
using JwtApp.Models; 
using JwtApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity; 
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<JWTDbContext>(options =>
  options.UseSqlServer(builder.Configuration.GetConnectionString("db")));

// *** Add Identity Core services ***
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options => 
{
    // Identity options 
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.SignIn.RequireConfirmedAccount = false; 
    options.User.RequireUniqueEmail = false; 
})
.AddEntityFrameworkStores<JWTDbContext>() // Tells Identity to use your DbContext
.AddDefaultTokenProviders(); 

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IAuthService, AuthService>();


builder.Services.AddAuthentication(options => 
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        // ValidIssuer = builder.Configuration["AppSettings:Issuer"],
        ValidIssuer = "MyAwesomeApp",
        ValidateAudience = true,
        //ValidAudience = builder.Configuration["AppSettings:Audience"],
        ValidAudience = "MyAwesomeAudience",
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]!)),
        ValidateIssuerSigningKey = true,
        
    };
});


builder.Services.AddAuthorization(); 


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "JwtApp", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme { /* ... */ });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement { /* ... */ });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => { /* ... */ });

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed Roles 
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    string[] roleNames = { "Admin", "Student" }; 
    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
    }
}
// *** Seed Admin User ***
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    string adminUserName = "Mustafa";
    var adminUser = await userManager.FindByNameAsync(adminUserName);
    if (adminUser == null)
    {
        adminUser = new User { Id = Guid.NewGuid(), UserName = adminUserName, Email = "Mustafa@example.com", EmailConfirmed = true }; // Use Guid Id
                                                                                                                                    // Choose a strong password for the admin user
        var result = await userManager.CreateAsync(adminUser, "12345Mm@");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine("Admin user created successfully.");
        }
        else
        {
            Console.WriteLine($"Error creating admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}


app.Run();