using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        try
        {
            using var conn = new SqliteConnection(@"Data Source=..\royald.db");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE Users ADD COLUMN CanRestoreCancelledBill INTEGER NOT NULL DEFAULT 0;";
            cmd.ExecuteNonQuery();
            Console.WriteLine("Added CanRestoreCancelledBill to SQLite Users table.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("SQLite: " + ex.Message);
        }
    }
}
