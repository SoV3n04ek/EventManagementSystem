using EventManagement.Api.Middleware;
using EventManagement.Application.Interfaces;
using EventManagement.Application.Services;
using EventManagement.Infrastructure;
using EventManagement.Infrastructure.Data;
using EventManagement.Infrastructure.Interfaces;
using EventManagement.Infrastructure.Repository;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddDbContext<EventManagementDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

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
app.UseAuthorization();
app.MapControllers();

app.Run();