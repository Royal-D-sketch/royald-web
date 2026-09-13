using System;
using System.Linq;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        using (var connection = new SqliteConnection("Data Source=royald.db"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT CustomerCode, CustomerName FROM OutstandingDebts WHERE CustomerCode = '420040' LIMIT 1;";
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read()) Console.WriteLine("OutstandingDebts: " + reader.GetString(0) + " - " + reader.GetString(1));
            }
            
            command.CommandText = "SELECT Code, Name FROM Customers WHERE Code = '420040';";
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read()) Console.WriteLine("Customers: " + reader.GetString(0) + " - " + (reader.IsDBNull(1) ? "NULL" : reader.GetString(1)));
                else Console.WriteLine("Customers: Not Found");
            }
            
            command.CommandText = "SELECT COUNT(*) FROM Customers WHERE Name IS NULL OR Name = '' OR Name = '-';";
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read()) Console.WriteLine("Missing Names in Customers: " + reader.GetInt32(0));
            }
        }
    }
}
