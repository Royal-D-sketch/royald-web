using System;
using Npgsql;

var connStr = "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;SSL Mode=Require;Trust Server Certificate=true;Timeout=15;";
using var conn = new NpgsqlConnection(connStr);
conn.Open();

// Remove สัญญาลักษณ์ from Users table
using var updateCmd = new NpgsqlCommand(@"UPDATE ""Users"" SET ""SalesRepCode"" = REPLACE(REPLACE(""SalesRepCode"", 'สัญญาลักษณ์, ', ''), ', สัญญาลักษณ์', '') WHERE ""SalesRepCode"" LIKE '%สัญญาลักษณ์%'", conn);
int affected = updateCmd.ExecuteNonQuery();
Console.WriteLine($"Updated Users to remove สัญญาลักษณ์: {affected} row(s)");

// Verify
using var verifyCmd = new NpgsqlCommand(@"SELECT ""Username"", ""SalesRepCode"" FROM ""Users"" WHERE ""Username"" = 'Sunya'", conn);
using var rdr = verifyCmd.ExecuteReader();
while (rdr.Read())
{
    Console.WriteLine($"Verified Sunya: {rdr["Username"]} -> {rdr["SalesRepCode"]}");
}
