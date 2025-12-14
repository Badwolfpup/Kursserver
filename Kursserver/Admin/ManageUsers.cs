using Kursserver.Extracts;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using System.Runtime.CompilerServices;

namespace Kursserver.Admin
{
    [Authorize]
    public static class ManageUsers
    {
        public static async Task FetchUsers(string connectionString, HttpContext context)
        {
            var connect = DatabaseHelper.ConnectToDatabase(connectionString);
            string sqlQuery = "SELECT u.id, u.FirstName, u.LastName, u.Email, u.Course, u.IsActive, c.Email AS CoachEmail FROM Users u LEFT JOIN StudentCoach sc ON u.id = sc.student_id LEFT JOIN Users c ON sc.coach_id = c.id WHERE u.AuthLevel = 2";
            try
            {
                using (var command = (await connect).CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var users = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            users.Add(new
                            {
                                Id = reader.GetInt32(0),
                                FirstName = reader.GetString(1),
                                LastName = reader.GetString(2),
                                Email = reader.GetString(3),
                                Course = reader.GetInt32(4),
                                IsActive = reader.GetBoolean(5),
                                Coach = reader.IsDBNull(6) ? null : reader.GetString(6)
                            });
                        }
                        await context.Response.WriteAsJsonAsync(users);

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
                await(await connect).CloseAsync();
            }
        }

        public static void GetUsersEndpoint(this WebApplication app, string connectionString)
        {

            app.MapGet("api/fetch-users", async (context) =>
            {
                await FetchUsers(connectionString, context);
            });
        }

        public static void InactivateUserEndpoint(this WebApplication app, string connectionString)
        {
            app.MapPost("api/inactivate-user", async (HttpContext context) =>
            {
                var connect = await DatabaseHelper.ConnectToDatabase(connectionString);  
                var request = await context.Request.ReadFromJsonAsync<ExtractEmail>();
                string sqlquery = "UPDATE Users SET IsActive=0 WHERE Email=@Email";
                try
                {
                    using (var command = connect.CreateCommand())
                    {
                        command.CommandText = sqlquery;
                        command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar).Value = request.Email;
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected> 0)
                        {
                            await FetchUsers(connectionString, context);
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsJsonAsync(new { error = "User not found." });
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

        public static void ActivateUserEndpoint(this WebApplication app, string connectionString)
        {
            app.MapPost("api/activate-user", async (HttpContext context) =>
            {
                var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
                var request = await context.Request.ReadFromJsonAsync<ExtractEmail>();
                string sqlquery = "UPDATE Users SET IsActive=1 WHERE Email=@Email";
                try
                {
                    using (var command = connect.CreateCommand())
                    {
                        command.CommandText = sqlquery;
                        command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar).Value = request.Email;
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            await FetchUsers(connectionString, context);
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsJsonAsync(new { error = "User not found." });
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
