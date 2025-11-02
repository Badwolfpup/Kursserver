using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace Kursserver.Utils
{
    public static class DatabaseHelper
    {
        //private static readonly string ConnectionString = $"Server=localhost;Database=Kurshemsida;User Id=kursDB;Password=Hudiksvall2025!;TrustServerCertificate=True;Encrypt=True";  // Move this to config if needed

        public static async Task<SqlConnection> ConnectToDatabase(string ConnectionString)
        {
            var connect = new SqlConnection(ConnectionString);
            try
            {
                await connect.OpenAsync();
                Debug.WriteLine("Database connection established successfully.");
                return connect;
            }
            catch (SqlException ex)
            {
                // Handle the exception
                Debug.WriteLine("Error: " + ex.Message);
                connect?.Dispose();
                connect = null;
                return connect;
            }
            catch (Exception ex)
            {
                // Catch other general errors (e.g., network issues)
                Debug.WriteLine($"Error: {ex.Message}");
                connect?.Dispose();
                connect = null;
                return connect;
            }

        }

    }
}
