using Kursserver.Extracts;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Kursserver.Admin
{
    [Authorize]
    public static class DeleteUser
    {
        public static void DeletUserEndpoint(this WebApplication app, string connectionString)
        {
            app.MapDelete("api/delete-user", async (HttpContext context) =>
            {
                var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
                if (connect == null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "Database connection failed." });
                    return;
                }

                var request = await context.Request.ReadFromJsonAsync<ExtractNewUserInfo>();
                if (request == null || string.IsNullOrEmpty(request.Email))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request data." });
                    return;
                }

                string sqlquery = "DELETE FROM Users WHERE Email=@Email";
                try
                {
                    using (var command = connect.CreateCommand())
                    {
                        command.CommandText = sqlquery;
                        command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar).Value = request.Email;
                        int rows = await command.ExecuteNonQueryAsync();
                        if (rows > 0)
                        {
                            await context.Response.WriteAsJsonAsync(new { message = "User deleted successfully." });
                        }
                        else
                        {
                            context.Response.StatusCode = 500;
                            await context.Response.WriteAsJsonAsync(new { error = "Failed to delete user." });
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
