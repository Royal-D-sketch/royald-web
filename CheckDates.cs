using System;
using System.Linq;
using Npgsql;

class Program {
    static void Main() {
        string connString = ""Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.aezdxtmksmvpxcuxrkyf;Password=royal-d-admin-password123;Pooling=true"";
        using var conn = new NpgsqlConnection(connString);
        conn.Open();
        
        using var cmd = new NpgsqlCommand(""SELECT COUNT(*), EXTRACT(MONTH FROM \""ReceiptDate\"") as m, EXTRACT(YEAR FROM \""ReceiptDate\"") as y FROM \""OutstandingDebts\"" WHERE \""RemainingAmount\"" = 0 AND \""ReceiptDate\"" IS NOT NULL GROUP BY y, m ORDER BY y, m"", conn);
        using var reader = cmd.ExecuteReader();
        while(reader.Read()) {
            Console.WriteLine($""Year: {reader[2]}, Month: {reader[1]} -> Count: {reader[0]}"");
        }
    }
}
