using Npgsql;
using System;

var connStr = "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;SSL Mode=Require;Trust Server Certificate=true;Timeout=15;Command Timeout=15;";

using var conn = new NpgsqlConnection(connStr);
conn.Open();

// อัปเดต Weeranut: ภาคกลาง, ภาคตะวันออก, ภาคอีสาน
using var cmd1 = new NpgsqlCommand(@"UPDATE ""Users"" SET ""AllowedRegion"" = @r WHERE ""Username"" = 'Weeranut'", conn);
cmd1.Parameters.AddWithValue("r", "ภาคกลาง,ภาคตะวันออก,ภาคอีสาน");
int r1 = cmd1.ExecuteNonQuery();
Console.WriteLine($"Weeranut updated: {r1} row(s)");

// ตรวจสอบทั้ง 3 คน
using var cmd2 = new NpgsqlCommand(@"SELECT ""Username"", ""FullName"", ""SalesRepCode"", ""AllowedRegion"", ""AllowedProvinces"" FROM ""Users"" WHERE ""Username"" IN ('Sunya','Weeranut','NamPetCh32') ORDER BY ""Id""", conn);
using var rdr = cmd2.ExecuteReader();
Console.WriteLine("=== Current Supervisor Settings ===");
while (rdr.Read()) {
    Console.WriteLine($"User: {rdr["Username"]} | Name: {rdr["FullName"]}");
    Console.WriteLine($"  SalesRepCode:    {rdr["SalesRepCode"]}");
    Console.WriteLine($"  AllowedRegion:   {rdr["AllowedRegion"]}");
    Console.WriteLine($"  AllowedProvinces:{rdr["AllowedProvinces"]}");
    Console.WriteLine("---");
}
