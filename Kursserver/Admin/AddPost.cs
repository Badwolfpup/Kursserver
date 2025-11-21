using Kursserver.Extracts;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Security.Claims;

namespace Kursserver.Admin
{
    [Authorize]
    public static class AddPost
    {
        public static void AddPostEndpoints(this WebApplication app, string connectionString)
        {
            app.MapPost("api/add-post", async (HttpContext context) =>
            {
                var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
                if (connect == null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "Database connection failed." });
                    return;
                }

                var request = await context.Request.ReadFromJsonAsync<ExtractPost>();
                if (request == null)
                {
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request payload." });
                    return;
                }
                try
                {
                    string sqlquery = "INSERT INTO posts (html, delta, authorId) VALUES (@html, @delta, @authorid);";
                    try
                    {
                        using (var command = connect.CreateCommand())
                        {
                            command.CommandText = sqlquery;
                            command.Parameters.Add("html", System.Data.SqlDbType.NVarChar).Value = request.Html;
                            command.Parameters.Add("delta", System.Data.SqlDbType.NVarChar).Value = System.Text.Json.JsonSerializer.Serialize(request.Delta); ;
                            var id = await GetAuthorID(connectionString, request.Email);
                            if (id < 0)
                            {
                                context.Response.StatusCode = 401; // Unauthorized
                                await context.Response.WriteAsJsonAsync(new { error = "Author not found." });
                                return;
                            }
                            command.Parameters.Add("authorid", System.Data.SqlDbType.Int).Value = id;
                            Debug.WriteLine(command.CommandText);
                            try
                            {
                                int rows = await command.ExecuteNonQueryAsync();
                                if (rows > 0)
                                {
                                    await context.Response.WriteAsJsonAsync(new { message = "Post added successfully." });
                                }
                                else
                                {
                                    context.Response.StatusCode = 500;
                                    await context.Response.WriteAsJsonAsync(new { error = "Failed to add post." });
                                }
                            }
                            catch (SqlException ex)
                            {
                                Console.WriteLine($"1.Database error: {ex.Message}");

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Unexpected error: {ex.Message}");
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine($"Database error: {ex.Message}");

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected error: {ex.Message}");
                    }

                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500; // Internal Server Error
                    await context.Response.WriteAsJsonAsync(new { error = "An error occurred while adding the post." });
                }
            });
        }


        private static async Task<int> GetAuthorID(string connectionString, string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return -1;
            var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
            string sqlquery = "SELECT id FROM Users WHERE Email = @Email";
            try
            {
                using (var command = connect.CreateCommand())
                {
                    command.CommandText = sqlquery;
                    command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar).Value = email;
                    var result = await command.ExecuteScalarAsync(); // Await this for async
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Database error in GetAuthorID: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in GetAuthorID: {ex.Message}");
                throw;
            }
        }
    }
}
