using Microsoft.Data.Sqlite;

var dbPath = @"C:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\royal-d-debtor-web\royald.db";
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

void AddColumn(string table, string column, string def)
{
    var checkCmd = conn.CreateCommand();
    checkCmd.CommandText = $"PRAGMA table_info({table})";
    using var reader = checkCmd.ExecuteReader();
    bool exists = false;
    while (reader.Read())
    {
        if (reader.GetString(1) == column) exists = true;
    }
    if (!exists)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {def}";
        cmd.ExecuteNonQuery();
        Console.WriteLine($"Added {column} to {table}");
    }
}

// Users (AppUser)
AddColumn("Users", "AllowedRegion", "TEXT NOT NULL DEFAULT ''");
AddColumn("Users", "AllowedProvinces", "TEXT NOT NULL DEFAULT ''");
AddColumn("Users", "AllowedDistricts", "TEXT NOT NULL DEFAULT ''");
AddColumn("Users", "CanDownloadOrScreenCapture", "INTEGER NOT NULL DEFAULT 0");
AddColumn("Users", "CurrentSessionToken", "TEXT NOT NULL DEFAULT ''");

// AuditLogs
AddColumn("AuditLogs", "District", "TEXT NOT NULL DEFAULT ''");
AddColumn("AuditLogs", "Province", "TEXT NOT NULL DEFAULT ''");
AddColumn("AuditLogs", "DurationMinutes", "INTEGER NULL");

// Create FileAttachments table
var createCmd = conn.CreateCommand();
createCmd.CommandText = @"
CREATE TABLE IF NOT EXISTS FileAttachments (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    OutstandingDebtId INTEGER NULL,
    PaymentRecordId INTEGER NULL,
    FileName TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    ContentType TEXT NOT NULL,
    UploadedAt TEXT NOT NULL,
    UploadedBy TEXT NOT NULL,
    FOREIGN KEY (OutstandingDebtId) REFERENCES OutstandingDebts (Id) ON DELETE CASCADE,
    FOREIGN KEY (PaymentRecordId) REFERENCES PaymentRecords (Id) ON DELETE CASCADE
)";
createCmd.ExecuteNonQuery();
Console.WriteLine("FileAttachments table ensured.");

Console.WriteLine("Migration complete!");
