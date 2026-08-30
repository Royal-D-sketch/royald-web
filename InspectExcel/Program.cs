using System;
using Npgsql;

class Program {
    static async System.Threading.Tasks.Task Main() {
        var connStr = "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;SSL Mode=Require;Trust Server Certificate=true;";
        using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        var sql = "ALTER TABLE \"Users\" ADD COLUMN IF NOT EXISTS \"CanChangeDebtStatus\" boolean NOT NULL DEFAULT FALSE;";
        using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("[OK] Column CanChangeDebtStatus added to Users table.");
    }
}
