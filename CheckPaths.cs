using System;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        using (var connection = new SqliteConnection("Data Source=RoyalD.Web/royald.db"))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT FilePath FROM FileAttachment LIMIT 10";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine(reader.GetString(0));
                }
            }
        }
    }
}
