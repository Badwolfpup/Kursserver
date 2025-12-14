using Kursserver.Extracts;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Kursserver.Admin
{
    [Authorize]
    public static class UpdateAttendance
    {
        public static void UpdateAttendanceEndpoints(this WebApplication app, string connectionString)
        {
            app.MapPatch("api/update-attendance", async (HttpContext context) =>
            {
                var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
                if (connect == null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "Database connection failed." });
                    return;
                }
                var request = await context.Request.ReadFromJsonAsync<ExtractUpdateAttendance>();
                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request data." });
                    return;
                }
                try
                {
                    if (DateTime.TryParse(request.Date, out DateTime date)) {
                        string sqlquery = $"UPDATE Attendance SET Status = {(request.Attended ? 1 : 0)} WHERE StudentId = @UserId AND Date = @Date";
                        using (var command = connect.CreateCommand())
                        {
                            command.CommandText = sqlquery;
                            command.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = request.UserId;
                            command.Parameters.Add("@Date", System.Data.SqlDbType.Date).Value = date;

                            int rows = await command.ExecuteNonQueryAsync();
                            if (rows > 0)
                            {
                                await context.Response.WriteAsJsonAsync(new { message = "Attendance updated successfully." });
                            }
                            else
                            {
                                context.Response.StatusCode = 404;
                                await context.Response.WriteAsJsonAsync(new { error = "No attendance updated." });
                            }
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsJsonAsync(new { error = "Wrong date format" });
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
