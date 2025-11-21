using Azure.Core;
using Kursserver.Extracts;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Kursserver.Admin
{
    [Authorize]
    public static class GetPermissions
    {
        public static void GetPermissionEndpoint(this WebApplication app, string connectionString)
        {
            app.MapPost("api/fetch-user-permissions", async (HttpContext context) =>
            {
                var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
                if (connect == null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "Database connection failed." });
                    return;
                }
                var request = await context.Request.ReadFromJsonAsync<ExtractEmail>();
                if (request == null || string.IsNullOrEmpty(request.Email))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request data." });
                    return;
                }
                string sqlQuery = "SELECT * FROM UserPermissions WHERE user_id = (SELECT id FROM Users WHERE Email = @Email)";
                try
                {
                    using (var command = connect.CreateCommand())
                    {
                        command.CommandText = sqlQuery;
                        command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar).Value = request.Email;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var permissions = new List<Dictionary<string, object>>();
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row.Add(reader.GetName(i), reader.GetValue(i));
                                }
                                permissions.Add(row);
                            }
                            await context.Response.WriteAsJsonAsync(permissions);
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = $"Internal server error: {ex.Message}" });
                }
                finally
                {
                    await connect.CloseAsync();
                }
            });
        }
    }
}
