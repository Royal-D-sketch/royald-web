using System;
using Npgsql;

var connStr = "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;SSL Mode=Require;Trust Server Certificate=true;Timeout=15;";
using var conn = new NpgsqlConnection(connStr);
conn.Open();

// Ensure Admins have CreditNote
var admins = new[] { "admin", "ART", "Kullaya" };
foreach (var admin in admins)
{
    using var selectCmd = new NpgsqlCommand(@"SELECT ""AllowedPages"" FROM ""Users"" WHERE ""Username"" = @u", conn);
    selectCmd.Parameters.AddWithValue("u", admin);
    var pages = (string?)selectCmd.ExecuteScalar();
    if (pages != null && !pages.Contains("CreditNote"))
    {
        var newPages = pages + ",CreditNote";
        using var updateCmd = new NpgsqlCommand(@"UPDATE ""Users"" SET ""AllowedPages"" = @p WHERE ""Username"" = @u", conn);
        updateCmd.Parameters.AddWithValue("p", newPages);
        updateCmd.Parameters.AddWithValue("u", admin);
        updateCmd.ExecuteNonQuery();
        Console.WriteLine($"Added CreditNote to {admin}");
    }
}

// Ensure sales reps do NOT have CreditNote by default
using var repCmd = new NpgsqlCommand(@"SELECT ""Username"", ""AllowedPages"" FROM ""Users"" WHERE ""Position"" LIKE '%ผู้แทน%' OR ""Position"" LIKE '%พนักงานขาย%'", conn);
using var repRdr = repCmd.ExecuteReader();
var repsToUpdate = new System.Collections.Generic.List<(string user, string newPages)>();
while (repRdr.Read())
{
    var u = repRdr.GetString(0);
    var p = repRdr.IsDBNull(1) ? "" : repRdr.GetString(1);
    if (p.Contains("CreditNote"))
    {
        var filtered = string.Join(",", p.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(x => !x.Trim().Equals("CreditNote", StringComparison.OrdinalIgnoreCase)));
        repsToUpdate.Add((u, filtered));
    }
}
repRdr.Close();

foreach (var (user, newPages) in repsToUpdate)
{
    using var updateCmd = new NpgsqlCommand(@"UPDATE ""Users"" SET ""AllowedPages"" = @p WHERE ""Username"" = @u", conn);
    updateCmd.Parameters.AddWithValue("p", newPages);
    updateCmd.Parameters.AddWithValue("u", user);
    updateCmd.ExecuteNonQuery();
    Console.WriteLine($"Removed CreditNote from {user}");
}

Console.WriteLine("Database permissions verified successfully.");
