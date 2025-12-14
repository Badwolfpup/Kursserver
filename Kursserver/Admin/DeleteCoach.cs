using Kursserver.Extracts;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using System.Transactions;

namespace Kursserver.Admin
{
    [Authorize]
    public static class DeleteCoach
    {
        public static void DeleteCoachEndpoint(this WebApplication app, string connectionString)
        {
            app.MapDelete("api/delete-coach", async (HttpContext context) =>
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


                try
                {
                    using (var command = connect.CreateCommand())
                    {
                        using (var transaction =  connect.BeginTransaction())
                        {
                            command.Transaction = transaction;
                            try
                            {
                                command.CommandText = "DELETE FROM StudentCoach WHERE coach_id = (SELECT id FROM Users WHERE Email = @Email);";
                                command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar).Value = request.Email;
                                await command.ExecuteNonQueryAsync();

                                command.CommandText = "DELETE FROM Users WHERE Email=@Email";
                                int rows = await command.ExecuteNonQueryAsync();
                                transaction.Commit();
                                if (rows > 0)
                                {
                                    await context.Response.WriteAsJsonAsync(new { message = "Coach deleted successfully." });

                                }
                                else
                                {
                                    context.Response.StatusCode = 500;
                                    await context.Response.WriteAsJsonAsync(new { error = "Failed to delete user." });
                                }
                            }
                            catch
                            {
                                transaction.Rollback();
                                // Handle error
                            }
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
