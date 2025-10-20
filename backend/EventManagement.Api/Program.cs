using EventManagement.Api.Middleware;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Services;
using EventManagement.Domain.Interfaces.Security;
using EventManagement.Infrastructure;
using EventManagement.Infrastructure.Data;
using EventManagement.Infrastructure.Interfaces;
using EventManagement.Infrastructure.Repository;
using EventManagement.Infrastructure.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<IUserService>();

builder.Services.AddDbContext<EventManagementDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Event Management API",
        Version = "v1",
        Description = "Event Management System API"
    });

    // Jwt Authentication support in swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \\\"Authorization: Bearer {token}\\\"\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
             new OpenApiSecurityScheme
             {
                 Reference = new OpenApiReference
                 {
                     Type = ReferenceType.SecurityScheme,
                     Id = "Bearer"
                 }
             },
             Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Dependency Injection for auth
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Seeding
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();