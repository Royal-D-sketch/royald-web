using Microsoft.Data.Sqlite;

var dbPath = @"C:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\royal-d-debtor-web\royald.db";
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

// ตรวจสอบ Users table columns
var pragmaCmd = conn.CreateCommand();
pragmaCmd.CommandText = "PRAGMA table_info(Users)";
var reader = pragmaCmd.ExecuteReader();
var cols = new List<string>();
while (reader.Read()) cols.Add(reader.GetString(1));
reader.Close();

Console.WriteLine("Current Users columns: " + string.Join(", ", cols));

// เพิ่ม column ที่ขาด
if (!cols.Contains("SalesRepCode")) {
    var cmd = conn.CreateCommand();
    cmd.CommandText = "ALTER TABLE Users ADD COLUMN SalesRepCode TEXT NOT NULL DEFAULT ''";
    cmd.ExecuteNonQuery();
    Console.WriteLine("Added SalesRepCode column");
}

if (!cols.Contains("SessionTimeoutMinutes")) {
    var cmd = conn.CreateCommand();
    cmd.CommandText = "ALTER TABLE Users ADD COLUMN SessionTimeoutMinutes INTEGER NULL DEFAULT 10";
    cmd.ExecuteNonQuery();
    Console.WriteLine("Added SessionTimeoutMinutes column");
}

Console.WriteLine("Migration complete!");
