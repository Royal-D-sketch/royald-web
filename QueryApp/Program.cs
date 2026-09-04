using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        using var conn = new SqliteConnection(@"Data Source=..\royald.db");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE OutstandingDebts 
            SET Status = 6, 
                BadDebtAmount = 11340, 
                RemainingAmount = 0, 
                BadDebtDate = '2026-04-17 00:00:00' 
            WHERE BillNo IN ('R100150', 'R108995');
        ";
        cmd.ExecuteNonQuery();
        Console.WriteLine("Updated R100150 and R108995 to Bad Debt (Status 5)");
    }
}
