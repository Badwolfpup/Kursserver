using Kursserver.Extracts;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using System.Data;

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

                string sqlQuery = "INSERT INTO USERS (FirstName, LastName, Email) VALUES (@FirstName, @LastName, @Email); SELECT SCOPE_IDENTITY();";

                try
                {
                    using (var command = connect.CreateCommand())
                    {
                        command.CommandText = sqlQuery;
                        command.Parameters.Add("@FirstName", SqlDbType.NVarChar).Value = request.FirstName;
                        command.Parameters.Add("@LastName", SqlDbType.NVarChar).Value = request.LastName;
                        command.Parameters.Add("@Email", SqlDbType.NVarChar).Value = request.Email;

                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            int userid = Convert.ToInt32(result);

                            if (!string.IsNullOrEmpty(request.Coach))  // Better check for empty/null
                            {
                                string sqlQuery2 = @"INSERT INTO StudentCoach (student_id, coach_id)
                                     SELECT @StudentId, u.id FROM Users u WHERE u.Email = @CoachEmail";

                                using (var command2 = connect.CreateCommand())
                                {
                                    command2.CommandText = sqlQuery2;
                                    command2.Parameters.Add("@StudentId", SqlDbType.Int).Value = userid;
                                    command2.Parameters.Add("@CoachEmail", SqlDbType.NVarChar).Value = request.Coach;

                                    try
                                    {
                                        int rows2 = await command2.ExecuteNonQueryAsync();
                                        if (rows2 <= 0)
                                        {
                                            context.Response.StatusCode = 500;
                                            await context.Response.WriteAsJsonAsync(new { error = "Couldn't find coach's email in database." });
                                            return;  
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        context.Response.StatusCode = 500;
                                        await context.Response.WriteAsJsonAsync(new { error = "Failed to assign coach: " + ex.Message });
                                        return;
                                    }
                                }
                            }
                            await context.Response.WriteAsJsonAsync(new { message = "User added successfully.", userId = userid });
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
                    await context.Response.WriteAsJsonAsync(new { error = "Database error: " + ex.Message });
                }
                finally
                {
                    await connect.CloseAsync();
                }

            });
        }
    }
}
