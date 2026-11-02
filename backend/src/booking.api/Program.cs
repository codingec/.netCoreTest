using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using booking.core.Constants;
using booking.infrastructure.Persistence;
using booking.infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // Angular app URL
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // only if needed
        });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Booking API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {token}"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwt = builder.Configuration.GetSection("Jwt");
var rawKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key missing.");
if (Encoding.UTF8.GetByteCount(rawKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes (256 bits) for HS256.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = key
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("Booking.Read", p => p.RequireClaim(Permissions.ClaimType, Permissions.Bookings.Read));
    opt.AddPolicy("Booking.Create", p => p.RequireClaim(Permissions.ClaimType, Permissions.Bookings.Create));
    opt.AddPolicy("Booking.Update", p => p.RequireClaim(Permissions.ClaimType, Permissions.Bookings.Update));
    opt.AddPolicy("Booking.Delete", p => p.RequireClaim(Permissions.ClaimType, Permissions.Bookings.Delete));

    opt.AddPolicy("Service.Read", p => p.RequireClaim(Permissions.ClaimType, Permissions.Services.Read));
    opt.AddPolicy("Service.Update", p => p.RequireClaim(Permissions.ClaimType, Permissions.Services.Update));
    opt.AddPolicy("Service.Create", p => p.RequireClaim(Permissions.ClaimType, Permissions.Services.Create));
    opt.AddPolicy("Service.Delete", p => p.RequireClaim(Permissions.ClaimType, Permissions.Services.Delete));

    opt.AddPolicy("Office.Create", p => p.RequireClaim(Permissions.ClaimType, Permissions.Offices.Create));
    opt.AddPolicy("Office.Read", p => p.RequireClaim(Permissions.ClaimType, Permissions.Offices.Read));
    opt.AddPolicy("Office.Update", p => p.RequireClaim(Permissions.ClaimType, Permissions.Offices.Update));
    opt.AddPolicy("Office.Delete", p => p.RequireClaim(Permissions.ClaimType, Permissions.Offices.Delete));
});

builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IServicesService, ServicesService>();
builder.Services.AddScoped<IOfficeService, OfficeService>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddHostedService<DbInitializerHostedService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAngularApp");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();