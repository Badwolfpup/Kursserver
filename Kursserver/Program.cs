using Kursserver.Endpoints;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("jwt", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

//builder.WebHost.UseKestrel(options =>
//{
//    options.ListenAnyIP(5000); // HTTP port
//    options.ListenAnyIP(5001, listenOptions =>
//    {
//        listenOptions.UseHttps(); // HTTPS port
//    });
//    options.ListenAnyIP(51000, listenOptions =>
//    {
//        listenOptions.UseHttps(); // SignalR over HTTPS port
//    });
//});

//builder.Services.AddSignalR().AddJsonProtocol(options =>
//{
//    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
//});

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

builder.Services.AddScoped<EmailService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddHttpClient();

builder.Services.AddScoped<AnthropicService>();
builder.Services.AddScoped<DeepSeekService>();
builder.Services.AddScoped<GrokService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Enable authentication and authorization middleware
app.MapAttendanceEndpoints();
app.MapPermissionEndpoints();
app.MapPostEndpoints();
app.MapUserEndpoints();
app.MapUtilityEndpoints();
app.MapValidationEndpoints(jwtSettings);
app.MapProjectEndpoints();
app.MapExerciseEndpoints();
app.MapPostGetPostEndpoint();
app.MapNoClassEndpoints();
app.MapAnthropicEndpoints();
app.MapDeepSeekEndpoints();
app.MapGrokEndpoints();
app.MapTicketEndpoints();

app.MapControllers();
//app.MapFallbackToFile("index.html");
if (!app.Environment.IsDevelopment())
{
    app.MapFallback(async context =>
    {
        var path = context.Request.Path.Value ?? "";

        // If request has file extension, it should have been handled by UseStaticFiles
        // If we get here, the file doesn't exist - return 404
        if (Path.HasExtension(path))
        {
            context.Response.StatusCode = 404;
            return;
        }

        // For SPA routes (no extension), serve index.html
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(
            Path.Combine(app.Environment.WebRootPath, "index.html")
        );
    });
}

app.Run();
