using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Infrastructure;
using EMSApp.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;
using EMSApp.Domain.Entities;
using EMSApp.Application.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Framework & core
builder.Services.AddControllers().AddJsonOptions(opts =>
{
    var c = opts.JsonSerializerOptions.Converters;
    c.Add(new DateOnlyJsonConverter());   // yyyy-MM-dd
    c.Add(new TimeOnlyJsonConverter());   // HH:mm:ss
    c.Add(new TimeSpanJsonConverter());   // c (hh:mm:ss)
    c.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
});

//  Infrastructure layer
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection(nameof(DatabaseSettings)));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    return new MongoClient(opts.ConnectionString);
});
builder.Services.AddInfrastructure();

// Application layer
builder.Services.AddApplication();
builder.Services.AddHostedService<LeaveCompletionOverdueTaskService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
      policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyMethod()
        .AllowAnyHeader()
    );
});

// Authentication & Authorization

var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "text/plain";
                var msg = ctx.ErrorDescription ?? "Token invalid or expired";
                return ctx.Response.WriteAsync(msg);
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("ManagerOnly", p => p.RequireRole("Manager"));
    options.AddPolicy("EmployeeOnly", p => p.RequireRole("Employee"));
});

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EMSApp API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter: {mytoken}"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // Mappings
    // DateOnly → "string" format="date"
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    // TimeOnly → "string" format="time"
    c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });
    // TimeSpan → "string" format="duration"
    c.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string", Format = "duration" });
});
builder.Services.AddAutoMapper(typeof(UserMappingProfile).Assembly);
builder.Services.AddAutoMapper(typeof(AssignmentMappingProfile).Assembly);
builder.Services.AddAutoMapper(typeof(AssignmentFeedbackMappingProfile).Assembly);

var app = builder.Build();

// 4) HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngularDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Extension methods to wire up DI in Program.cs
/// </summary>
public static class StartupExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // register all I*Service / implementations
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPunchRecordService, PunchRecordService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<IBreakSessionService, BreakSessionService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IAssignmentFeedbackService, AssignmentFeedbackService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // MongoDbContext
        services.AddScoped<IMongoDbContext, MongoDbContext>();

        // register all I*Repository / implementations
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPunchRecordRepository, PunchRecordRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<IBreakSessionRepository, BreakSessionRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IAssignmentFeedbackRepository, AssignmentFeedbackRepository>();

        return services;
    }
}
