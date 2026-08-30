using System;
using System.Web;

namespace RoyalD.Web.Services
{
    public static class PostgreSqlConnectionStringParser
    {
        public static string Parse(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return "";
            string input = rawInput.Trim().Trim('\'', '"', '`');

            if (input.StartsWith("psql ", StringComparison.OrdinalIgnoreCase))
            {
                input = input.Substring(5).Trim().Trim('\'', '"', '`');
            }

            if (input.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) || 
                input.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    int schemeEnd = input.IndexOf("://");
                    string withoutScheme = input.Substring(schemeEnd + 3);

                    int lastAt = withoutScheme.LastIndexOf('@');
                    int firstSlash = withoutScheme.IndexOf('/', lastAt >= 0 ? lastAt : 0);
                    
                    string userPart = lastAt > 0 ? withoutScheme.Substring(0, lastAt) : "";
                    string hostPart = lastAt > 0 
                        ? (firstSlash > lastAt ? withoutScheme.Substring(lastAt + 1, firstSlash - lastAt - 1) : withoutScheme.Substring(lastAt + 1))
                        : "";
                    string dbPart = firstSlash >= 0 ? withoutScheme.Substring(firstSlash + 1).Split('?')[0] : "postgres";

                    int firstColon = userPart.IndexOf(':');
                    string username = firstColon >= 0 ? userPart.Substring(0, firstColon) : userPart;
                    string password = firstColon >= 0 ? userPart.Substring(firstColon + 1) : "";

                    string host = hostPart;
                    int port = 5432;
                    int hostColon = hostPart.LastIndexOf(':');
                    if (hostColon >= 0 && int.TryParse(hostPart.Substring(hostColon + 1), out int p))
                    {
                        port = p;
                        host = hostPart.Substring(0, hostColon);
                    }

                    username = HttpUtility.UrlDecode(username);
                    password = HttpUtility.UrlDecode(password);
                    if (string.IsNullOrEmpty(dbPart)) dbPart = "postgres";

                    return $"Host={host};Port={port};Database={dbPart};Username={username};Password={password};Pooling=true;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;";
                }
                catch
                {
                    return input;
                }
            }

            if (!input.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase))
            {
                input += ";SSL Mode=Require;Trust Server Certificate=true;";
            }
            return input;
        }
    }
}
