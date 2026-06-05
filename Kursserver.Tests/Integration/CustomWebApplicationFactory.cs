using Kursserver.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kursserver.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's SQL Server DbContext registration. AddDbContext registers
            // several services — the options, the context, and (EF Core 9+) an
            // IDbContextOptionsConfiguration<ApplicationDbContext> that carries the
            // UseSqlServer call. All must go, or SQL Server and SQLite providers end up
            // registered together ("Only a single database provider can be registered").
            var efDescriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ApplicationDbContext) ||
                (d.ServiceType.FullName?.Contains("DbContextOptionsConfiguration", StringComparison.Ordinal) ?? false))
                .ToList();
            foreach (var d in efDescriptors)
                services.Remove(d);

            // Use SQLite in-memory for tests
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection));

            // Replace auth with the test handler. Program.cs sets DefaultAuthenticate/
            // Challenge to JwtBearer explicitly, so overriding only DefaultScheme is not
            // enough — [Authorize] would keep using Bearer and every request returns 401.
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            // Replace the email service with a capturing fake (singleton so tests can
            // read what was sent after a request runs in its own scope)
            var emailDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null)
                services.Remove(emailDescriptor);
            services.AddSingleton<IEmailService, FakeEmailService>();

            // Ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
