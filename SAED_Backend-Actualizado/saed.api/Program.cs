using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using saed.api.Configuration;
using saed.api.Data;
using saed.api.Data.Repositories;
using saed.api.Security;
using saed.api.Seed;
using saed.api.Services;
using saed.api.Services.Universidad;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//  Evitar que .NET remapee los claims del JWT a nombres largos de Microsoft 
// Sin esto, [Authorize(Roles = "Admin")] no puede leer el claim "role" del token
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

//  Controladores con enums como string 
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();

//  Base de datos 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

//  Servicios 
builder.Services.Configure<UniversidadApiOptions>(
    builder.Configuration.GetSection("UniversidadApi"));
builder.Services.Configure<SeedAdminOptions>(
    builder.Configuration.GetSection("SeedAdmin"));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AdminAuthService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProcesoService>();
builder.Services.AddScoped<EvaluacionService>();
builder.Services.AddScoped<InstrumentoService>();
builder.Services.AddScoped<CalificacionService>();
builder.Services.AddScoped<CatalogoUniversidadService>();
builder.Services.AddScoped<ReportesService>();

// Repositorios 
builder.Services.AddScoped<ProcesoRepository>();
builder.Services.AddScoped<InstrumentoRepository>();

//  HTTP 
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IUniversidadApiService, UniversidadApiService>();
builder.Services.AddHttpContextAccessor();

// Autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(JwtConfiguration.GetSigningKey(builder.Configuration, builder.Environment))),

            RoleClaimType = AuthPolicies.RoleClaimType
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.AdminOnly, policy =>
        policy.RequireAuthenticatedUser()
            .RequireRole(AuthPolicies.AdminRole));

    options.AddPolicy(AuthPolicies.StudentOnly, policy =>
        policy.RequireAuthenticatedUser()
            .RequireRole(AuthPolicies.StudentRole));

    options.AddPolicy(AuthPolicies.StudentOrAdmin, policy =>
        policy.RequireAuthenticatedUser()
            .RequireRole(AuthPolicies.StudentRole, AuthPolicies.AdminRole));
});

//  Swagger 
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SAED", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingrese el token JWT: Bearer {token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

//  CORS 
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out _))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList()
    ?? new List<string>();

if (builder.Environment.IsDevelopment())
{
    corsOrigins.AddRange(new[]
    {
        "http://localhost:4200",
        "https://localhost:4200",
        "http://127.0.0.1:4200",
        "https://127.0.0.1:4200",
        "http://localhost:53040",
        "https://localhost:53040"
    });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        if (corsOrigins.Count == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy.WithOrigins(corsOrigins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//  Pipeline 
var app = builder.Build();

StartupConfigurationValidator.Validate(app.Configuration, app.Environment);

if (JwtConfiguration.UsesLocalDevelopmentSigningKey(app.Configuration, app.Environment))
{
    app.Logger.LogWarning(
        "Jwt:Key no está configurado. Se usará una clave local temporal solo para desarrollo. Configure Jwt__Key en producción.");
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (app.Environment.IsDevelopment() &&
        app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        app.Logger.LogInformation("Aplicando migraciones pendientes de EF Core para Development.");
        await context.Database.MigrateAsync();
    }

    var seedOptions = scope.ServiceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<SeedAdminOptions>>()
        .Value;

    await DbInitializer.SeedAdminAsync(context, seedOptions);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program
{
}
