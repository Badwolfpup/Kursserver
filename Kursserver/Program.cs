using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Kursserver.Login;
using Kursserver.Admin;
using Kursserver.Content;
using Kursserver.Utils;


var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt");
var connectionString = builder.Configuration.GetSection("Database")["ConnectionString"];
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options => {
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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(5000); // HTTP port
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS port
    });
    options.ListenAnyIP(51000, listenOptions =>
    {
        listenOptions.UseHttps(); // SignalR over HTTPS port
    });
});

builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true) // Allow any origin
              .AllowCredentials();
    });
});

var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// Enable authentication and authorization middleware
app.EmailValidationEndpoint(jwtSettings, connectionString);
app.PasscodeValidationEndpoint(jwtSettings, connectionString);
app.GetUsersEndpoint(connectionString);
app.InactivateUserEndpoint(connectionString);
app.ActivateUserEndpoint(connectionString);
app.AddUserEndpoint(connectionString);
app.DeleteUserEndpoint(connectionString);
app.GetPermissionEndpoint(connectionString);
app.UpdatePermissionsEndpoints(connectionString);
app.UploadImageEndpoints(connectionString);
app.AddPostEndpoints(connectionString);
app.GetPostsEndpoints(connectionString);
app.AddCoachEndpoint(connectionString);
app.DeleteCoachEndpoint(connectionString);
app.GetCoachesEndpoint(connectionString);
app.InactivateCoachesEndpoint(connectionString);
app.ActivateCoachesEndpoint(connectionString);
app.GetWeekEndpints();
app.GetWeeklyAttendanceEndpoints(connectionString);
app.UpdateAttendanceEndpoints(connectionString);

app.MapGet("/", () => "Hello World!");

app.Run();
