using Npgsql;
using System;
using System.Text.RegularExpressions;

var connStr = "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;SSL Mode=Require;Trust Server Certificate=true;Timeout=15;Command Timeout=15;";

using var conn = new NpgsqlConnection(connStr);
conn.Open();

// Clean up numeric codes from SalesRepCode in Users
var updates = new[]
{
    ("Sunya", "สัญญา, สัญญาลักษณ์, สัณห์, สัญญา LN, สัญญา N, สิริญญา, ชาติวุธ N"),
    ("Weeranut", "วีรนุช"),
    ("Chanthima", "จันทิมา"),
    ("Thanyarak", "ธัญลักษณ์, ธัญญลักษณ์"),
    ("Werasak", "วีระศักดิ์, วีรนุช T"),
    ("Apinya", "อภิญญา"),
    ("Thanasak", "ธนะศักดิ์, ธนาศักดิ์"),
    ("Thinnaphat", "ทินภัทร, ปานชลี"),
    ("Chuleewan", "ชุลีวรรณ W, ชุลีวรรณ, สัญญา T"),
    ("Boonserm", "บุญเสริม")
};

foreach (var (user, cleanRep) in updates)
{
    using var cmd = new NpgsqlCommand(@"UPDATE ""Users"" SET ""SalesRepCode"" = @rep WHERE ""Username"" = @u", conn);
    cmd.Parameters.AddWithValue("rep", cleanRep);
    cmd.Parameters.AddWithValue("u", user);
    int affected = cmd.ExecuteNonQuery();
    Console.WriteLine($"Updated {user}: {affected} row -> {cleanRep}");
}

// Verify updated
using var cmdAll = new NpgsqlCommand(@"SELECT ""Username"", ""FullName"", ""SalesRepCode"" FROM ""Users"" WHERE ""Position"" LIKE '%ผู้แทน%' ORDER BY ""Id""", conn);
using var rdrAll = cmdAll.ExecuteReader();
Console.WriteLine("\n=== Verified Sales Rep Users in DB ===");
while (rdrAll.Read()) {
    Console.WriteLine($"User: [{rdrAll["Username"]}] | Name: [{rdrAll["FullName"]}] | RepCode: [{rdrAll["SalesRepCode"]}]");
}
