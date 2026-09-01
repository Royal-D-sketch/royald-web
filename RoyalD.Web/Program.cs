using System;
using Microsoft.Data.Sqlite;
class Program {
    static void Main() {
        try {
            using (var c = new SqliteConnection("Data Source=../royald.db")) {
                c.Open();
                var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*), SUM(TotalAmount) FROM SalesBills";
                using (var r = cmd.ExecuteReader()) {
                    if(r.Read()) Console.WriteLine($"Bills: {r[0]}, Total Amount: {r[1]}");
                }
            }
        } catch (Exception ex) { Console.WriteLine(ex); }
    }
}