using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Form1204
{
    public static class DbHelper
    {
        public static string Server   { get; set; } = "";
        public static string Database { get; set; } = "";
        public static string User     { get; set; } = "";
        public static string Password { get; set; } = "";

        private static string BuildConnectionString(string database)
        {
            return string.Format(
                "Server={0};Database={1};User Id={2};Password={3};Connect Timeout=10;TrustServerCertificate=True;",
                Server, database, User, Password);
        }

        public static SqlConnection Connect()
        {
            return new SqlConnection(BuildConnectionString(Database));
        }

        public static SqlConnection ConnectMaster()
        {
            return new SqlConnection(BuildConnectionString("master"));
        }

        public static List<string> GetErbdDatabases()
        {
            using (var conn = ConnectMaster())
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    "SELECT name FROM sys.databases WHERE name LIKE 'erbd%' ORDER BY name", conn))
                {
                    var result = new List<string>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            result.Add(reader.GetString(0));
                    }
                    return result;
                }
            }
        }

        public static PersonInfo GetPersonByBarcode(SqlConnection conn, string barcode)
        {
            using (var cmd = new SqlCommand(
                "SELECT TOP 1 Barcode, Surname, Name, SecondName, AuditoriumCode " +
                "FROM dbo.sht_Sheets_R WHERE Barcode = @barcode", conn))
            {
                cmd.Parameters.AddWithValue("@barcode", barcode);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    string surname    = reader["Surname"]        is DBNull ? "" : reader["Surname"].ToString();
                    string name       = reader["Name"]           is DBNull ? "" : reader["Name"].ToString();
                    string secondName = reader["SecondName"]     is DBNull ? "" : reader["SecondName"].ToString();
                    string aud        = reader["AuditoriumCode"] is DBNull ? "" : reader["AuditoriumCode"].ToString();

                    return new PersonInfo
                    {
                        Fio = string.Format("{0} {1} {2}", surname, name, secondName).Trim(),
                        Aud = aud
                    };
                }
            }
        }

        public static int CalcMinutes(string outTime, string inTime)
        {
            DateTime t1, t2;
            if (DateTime.TryParseExact(outTime, new[] { "H:mm", "HH:mm" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out t1) &&
                DateTime.TryParseExact(inTime, new[] { "H:mm", "HH:mm" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out t2))
            {
                return (int)(t2 - t1).TotalMinutes;
            }
            return 0;
        }
    }
}
