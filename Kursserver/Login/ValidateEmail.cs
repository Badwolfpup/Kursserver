using Kursserver.Extracts;
using Kursserver.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Kursserver.Login
{
    public static class ValidateEmail
    {
        public static readonly ConcurrentDictionary<string, int> passcodeStore = new();

        public static void EmailValidationEndpoint(this WebApplication app, IConfiguration jwtConfig, string connectionString)
        {
            app.MapPost("api/email-validation", async (context) =>
            {
                var request = await context.Request.ReadFromJsonAsync<ExtractEmail>();
                if (request?.Email == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid email." });
                    return;
                }
                bool emailexists = await CheckEmailExists(request.Email, connectionString);
                if (emailexists)
                {
                    int passcode = Random.Shared.Next(100000, 999999);

                    passcodeStore[request.Email] = passcode;
                    Debug.WriteLine($"Generated passcode: {passcode.ToString()} for email: {request.Email}, exists: {emailexists}");
                    await context.Response.WriteAsJsonAsync(new { passcode });
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Email does not exist." });
                }
            });
        }

        public static async Task<bool> CheckEmailExists(string mail, string connectionString)
        {
            var connect = await DatabaseHelper.ConnectToDatabase(connectionString);
            if (connect != null) 
            { 
                Debug.WriteLine("Database connection test passed in ValidateUser constructor.");
                string sqlQuery = $"SELECT COUNT(*) from Users WHERE Email=@Email";
                try
                {
                    using (var command = connect.CreateCommand())
                    {
                        command.CommandText = sqlQuery;
                        command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar).Value = mail;
                        var result = (int)await command.ExecuteScalarAsync();
                        if (result > 0)
                        {
                            Debug.WriteLine("Email found in database.");
                            return true;
                        }
                        else
                        {
                            Debug.WriteLine("Email not found in database.");
                            return false;
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Debug.WriteLine("SQL Error: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    await connect.CloseAsync();
                }
                return false; // Default return if an exception occurs
            }
            else
            {
                Debug.WriteLine("Database connection test failed in ValidateUser constructor.");
                return false;
            }
        }




    }
}
