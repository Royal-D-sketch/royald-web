using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        using (var connection = new SqliteConnection("Data Source=royald.db"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT FilePath FROM FileAttachment WHERE FilePath LIKE '%supabase%' LIMIT 1";
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    Console.WriteLine(reader.GetString(0));
                }
            }
        }
    }
}
