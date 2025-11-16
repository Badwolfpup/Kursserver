using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Kursserver.Admin
{
    [Authorize]
    public static class AddUser
    {
        public static void AddUserEndpoint(this WebApplication app, string connectionString)
        {
            app.MapPost("api/add-user", async (HttpContext context) =>
            {
                var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
                if (connect == null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "Database connection failed." });
                    return;
                }

                var request = await context.Request.ReadFromJsonAsync<ExtractNewUserInfo>();
                if (request == null || string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName) || string.IsNullOrEmpty(request.Email))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request data." });
                    return;
                }

                string sqlquery = "INSERT into USERS (FirstName, LastName, Email) VALUES (@FirstName, @LastName, @Email)";
                try
                {
                    using (var command = connect.CreateCommand())
                    {
                        command.CommandText = sqlquery;
                        command.Parameters.Add("@FirstName", System.Data.SqlDbType.NVarChar).Value = request.FirstName;
                        command.Parameters.Add("@LastName", System.Data.SqlDbType.NVarChar).Value = request.LastName;
                        command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar).Value = request.Email;
                        int rows = await command.ExecuteNonQueryAsync();
                        if (rows > 0)
                        {
                            await context.Response.WriteAsJsonAsync(new { message = "User added successfully." });
                        }
                        else
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsJsonAsync(new { error = "Failed to add user." });
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
