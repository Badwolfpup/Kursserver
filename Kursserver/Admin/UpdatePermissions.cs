using Kursserver.Login;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Data;
using System.Runtime.CompilerServices;

namespace Kursserver.Admin
{
    [Authorize]
    public static class UpdatePermissions
    {
        public static void UpdatePermissionsEndpoints(this WebApplication app, string connectionString)
        {
            app.MapPatch("api/update-user-permissions", async (HttpContext context) =>
            {
                var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
                if (connect == null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "Database connection failed." });
                    return;
                }
                var request = await context.Request.ReadFromJsonAsync<ExtractUpdatedUserPermissions>();
                if (request == null || string.IsNullOrEmpty(request.Email) || request.Permissions == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request data." });
                    return;
                }
                try
                {
                    string sqlquery = "SELECT id FROM Users WHERE Email = @Email";
                    int? userId = null;
                    using (var idCommand = connect.CreateCommand())
                    {
                        idCommand.CommandText = sqlquery;
                        idCommand.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar).Value = request.Email;
                        var result = await idCommand.ExecuteScalarAsync();
                        if (result != null) userId = (int)result;
                    }
                    if (userId == null)
                    {
                        context.Response.StatusCode = 404;

                        await context.Response.WriteAsJsonAsync(new { error = "User not found." });
                        return;
                    }
                    var setclause = new List<string>();
                    var parameters = new List<SqlParameter>();

                    foreach (var item in request.Permissions)
                    {
                        setclause.Add($"{item.Key} = @{item.Key}");
                        parameters.Add(new SqlParameter($"@{item.Key}", SqlDbType.Bit) { Value = item.Value });
                    }
                    string updateQuery = $"UPDATE UserPermissions SET {string.Join(", ", setclause)} WHERE user_id = @UserId";
                    parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) { Value = userId });
                    using (var updateCommand = connect.CreateCommand())
                    {
                        updateCommand.CommandText = updateQuery;
                        updateCommand.Parameters.AddRange(parameters.ToArray());
                        int rows = await updateCommand.ExecuteNonQueryAsync();
                        if (rows > 0)
                        {
                            await context.Response.WriteAsJsonAsync(new { message = "Permissions updated successfully." });
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsJsonAsync(new { error = "No permissions updated." });
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
