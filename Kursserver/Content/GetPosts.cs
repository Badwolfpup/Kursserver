using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace Kursserver.Content
{
    [Authorize]
    public static class GetPosts
    {
        public static void GetPostsEndpoints(this WebApplication app, string connectionstring)
        {
            app.MapGet("/api/get-posts", async (HttpContext context) =>
            {
                var connect = await DatabaseHelper.ConnectToDatabase(connectionstring);
                if (connect == null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Database connection failed.");
                    return;
                }
                try
                {
                    string sqlquery = "SELECT Html, PublishedAt, AuthorId, Pinned from Posts ORDER BY Pinned DESC, PublishedAt DESC";
                    using (var command = connect.CreateCommand())
                    {
                        command.CommandText = sqlquery;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var posts = new List<Dictionary<string, object>>();
                            while (await reader.ReadAsync())
                            {
                                var authorName = await GetAuthorName(connectionstring, (int)reader.GetValue(2));
                                var post = new Dictionary<string, object>
                                {
                                    { "Html", reader.GetValue(0) },
                                    { "PublishedAt", reader.GetValue(1) },
                                    { "Author", authorName },
                                    { "Pinned", reader.GetValue(3) }
                                };
                                posts.Add(post);
                            }
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(posts);

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
                finally
                {
                    await connect.CloseAsync();
                }
            });
        }

        private static async Task<string> GetAuthorName(string connectionString, int authorId)
        {
            var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
            if (connect == null)
            {
                return "Unknown Author";
            }
            string sqlquery = "SELECT FirstName, LastName FROM Users WHERE Id = @AuthorId";
            try
            {
                using (var command = connect.CreateCommand())
                {
                    command.CommandText = sqlquery;
                    command.Parameters.Add("@AuthorId", System.Data.SqlDbType.Int).Value = authorId;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string firstName = reader.GetString(0);
                            string lastName = reader.GetString(1);
                            return $"{firstName} {lastName}";
                        }
                        else
                        {
                            return "Unknown Author";
                        }
                    }
                }
            }
            catch (Exception)
            {
                return "Unknown Author";
            }
            finally
            {
                await connect.CloseAsync();
            }
        }
    }
}
