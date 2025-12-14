using Azure;
using Kursserver.Extracts;
using Kursserver.Utils;
using Microsoft.AspNetCore.Authorization;
using System.Net.WebSockets;

namespace Kursserver.Admin
{
    [Authorize]
    public static class GetWeeklyAttendance
    {
        public static void GetWeeklyAttendanceEndpoints(this WebApplication app, string connectionString)
        {
            app.MapPost("/api/get-weekly-attendance", async (HttpContext context) =>
            {
                var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
                if (connect == null)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "Database connection failed." });
                    return;
                }
                var request = await context.Request.ReadFromJsonAsync<ExtractDate>();
                if (request == null || string.IsNullOrEmpty(request.ChosenDate))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request data." });
                    return;
                }
                DateTime monday = GetMonday(request.ChosenDate);
                try
                {
                    string sqlquery = "SELECT a.Status, a.Date, u.Id, u.FirstName, u.LastName FROM Attendance a INNER JOIN Users u ON a.Id = u.Id WHERE u.IsActive = 1 AND a.Date >= @monday AND a.Date <= DATEADD(DAY, 3, @monday)";
                    using (var command = connect.CreateCommand())
                    {
                        command.CommandText = sqlquery;
                        command.Parameters.Add("@monday", System.Data.SqlDbType.Date).Value = monday;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var attendanceList = new List<AttendanceResult>();
                            while (await reader.ReadAsync())
                            {
                                attendanceList.Add(new AttendanceResult
                                {
                                    Status = reader.GetInt32(0),
                                    Date = reader.GetDateTime(1),
                                    UserId = reader.GetInt32(2),
                                    FirstName = reader.GetString(3),
                                    LastName = reader.GetString(4)
                                });
                            }
                            var usergroups = attendanceList.GroupBy(x => x.UserId);
                            var response = new List<object>();
                            foreach (var group in usergroups)
                            {
                                    var user = group.First();
                                    var attendanceDict = new Dictionary<DateTime, bool>();

                                    foreach (var item in group)
                                    {
                                        attendanceDict[item.Date] = item.Status == 1;
                                    }
                                    for (int i=0; i < 4; i++)
                                    {
                                        var day = monday.AddDays(i);
                                        if (!attendanceDict.ContainsKey(day)) attendanceDict[day] = false;
                                    }
                                    var ordered = attendanceDict.OrderBy(x => x.Key).ToDictionary(y => y.Key, y => y.Value);
                                    response.Add(new
                                    {
                                        id = user.UserId,
                                        name = $"{user.FirstName} {user.LastName}",
                                        attendance = ordered
                                    });

                            }
                            
                            await context.Response.WriteAsJsonAsync(response);
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

        private static DateTime GetMonday(string inputdate)
        {
            if (DateTime.TryParse(inputdate, out DateTime date))
            {
                int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
                return date.AddDays(diff);
            }
            return DateTime.Today;
        }
    }

  
}
