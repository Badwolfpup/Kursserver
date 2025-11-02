using Kursserver.Utils;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Kursserver.Login
{
    public static class UserRole
    {
        public static async Task<string> GetUserRole(string mail, string dbinfo)
        {
            var connect = await DatabaseHelper.ConnectToDatabase(dbinfo);
            if (connect != null)
            {
                Debug.WriteLine("Database connection test passed in ValidateUser constructor.");
                string sqlQuery = $"SELECT AuthLevel from Users WHERE Email=@Email";
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
                            return result == 1 ? "Admin" : "Regular";
                        }
                        else
                        {
                            Debug.WriteLine("Email not found in database.");
                            return "";
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
                return ""; // Default return if an exception occurs
            }
            else
            {
                Debug.WriteLine("Database connection test failed in ValidateUser constructor.");
                return "";
            }
        }

    }
}
