using Npgsql;
using System;

var connStr = "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;SSL Mode=Require;Trust Server Certificate=true;Timeout=15;Command Timeout=15;";

using var conn = new NpgsqlConnection(connStr);
conn.Open();

Console.WriteLine("=== SUPERVISOR USERS ===");
var sql = @"SELECT ""Id"", ""Username"", ""FullName"", ""Role"", ""Position"", ""SalesRepCode"",
           ""AllowedRegion"", ""AllowedProvinces"", ""IsActive""
    FROM ""Users""
    WHERE ""Username"" IN ('Sunya','Weeranut','Namphet')
       OR ""FullName"" ILIKE '%สัญญา%'
       OR ""FullName"" ILIKE '%วีรนุช%'
       OR ""FullName"" ILIKE '%น้ำเพชร%'
    ORDER BY ""Id""";

using var cmd = new NpgsqlCommand(sql, conn);
using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"ID:{reader["Id"]} | User:'{reader["Username"]}' | Name:'{reader["FullName"]}' | SalesRepCode:'{reader["SalesRepCode"]}'");
    Console.WriteLine($"  AllowedRegion:    '{reader["AllowedRegion"]}'");
    Console.WriteLine($"  AllowedProvinces: '{reader["AllowedProvinces"]}'");
    Console.WriteLine($"  IsActive: {reader["IsActive"]}");
    Console.WriteLine("---");
}
