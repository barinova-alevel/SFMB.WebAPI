using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SFMB.BL.Services;
using SFMB.BL.Services.Interfaces;
using SFMB.DAL;
using SFMB.DAL.Entities;
using SFMB.DAL.Repositories;
using SFMB.DAL.Repositories.Interfaces;
using WebApiForAz.Middleware;

//IConfigurationRoot configurationBuilder = new ConfigurationBuilder()
//    .AddEnvironmentVariables("DefaultConnection")
//                //.AddJsonFile("appsettings.json")
//                .Build();


Console.Title = $"WebApi - {DateTime.Now:HH:mm}";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "[HH:mm:ss] ";
    options.UseUtcTimestamp = false;
});

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = builder.Configuration.GetValue<string>("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<SfmbDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Identity
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<SfmbDbContext>()
.AddDefaultTokenProviders()
.AddSignInManager();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("JWT configuration is missing. Please set Jwt:Key, Jwt:Issuer, and Jwt:Audience in configuration.");
}

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
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddScoped<IOperationTypeRepository, OperationTypeRepository>();
builder.Services.AddScoped<IOperationRepository, OperationRepository>();
builder.Services.AddScoped<IDailyReportRepository, DailyReportRepository>();
builder.Services.AddScoped<IPeriodReportRepository, PeriodReportRepository>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<IOperationTypeService, OperationTypeService>();
builder.Services.AddScoped<IDailyReportService, DailyReportService>();
builder.Services.AddScoped<IPeriodReportService, PeriodReportService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SFM API",
        Version = "v1"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Apply database migrations and seed data
//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    try
//    {
//        Apply pending migrations
//       var context = services.GetRequiredService<SfmbDbContext>();
//        Console.WriteLine("Applying database migrations...");
//        await context.Database.MigrateAsync();
//        Console.WriteLine("Database migrations applied successfully.");

//        // Seed database with roles and default users
//        await DbSeeder.SeedRolesAndUsersAsync(services);
//    }
//    catch (Exception ex)
//    {
//        // Log error if needed
//        Console.WriteLine($"Error applying migrations or seeding database: {ex.Message}");
//    }
//}

app.UseMiddleware<ExceptionHandlingMiddleware>();

//if (app.Environment.IsDevelopment())
//{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
