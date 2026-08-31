using System;
using Npgsql;

class Program {
    static async System.Threading.Tasks.Task Main() {
        var connStr = "Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pssccxujypweaahkbvdw;Password=029030445Rd*;Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;";
        using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM \"SalesBills\" WHERE \"Credit\" = 0;";
        var zeroCount = await cmd.ExecuteScalarAsync();
        Console.WriteLine($"Remaining bills with Credit = 0: {zeroCount}");
    }
}
