using Microsoft.Data.SqlClient;

namespace Form1204;

public static class DbHelper
{
    // Заполняются при входе через LoginWindow
    public static string Server   { get; set; } = "";
    public static string Database { get; set; } = "";
    public static string User     { get; set; } = "";
    public static string Password { get; set; } = "";

    private static string BuildConnectionString(string database) =>
        $"Server={Server};Database={database};User Id={User};Password={Password};" +
        "Connect Timeout=10;TrustServerCertificate=True;";

    public static SqlConnection Connect() =>
        new(BuildConnectionString(Database));

    public static SqlConnection ConnectMaster() =>
        new(BuildConnectionString("master"));

    /// <summary>Возвращает список баз, имя которых начинается с «erbd».</summary>
    public static List<string> GetErbdDatabases()
    {
        using var conn = ConnectMaster();
        conn.Open();

        using var cmd = new SqlCommand(
            "SELECT name FROM sys.databases WHERE name LIKE 'erbd%' ORDER BY name",
            conn);

        var result = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(reader.GetString(0));

        return result;
    }

    /// <summary>Ищет человека по штрихкоду в dbo.sht_Sheets_R.</summary>
    public static PersonInfo? GetPersonByBarcode(SqlConnection conn, string barcode)
    {
        using var cmd = new SqlCommand("""
            SELECT TOP 1 Barcode, Surname, Name, SecondName, AuditoriumCode
            FROM dbo.sht_Sheets_R
            WHERE Barcode = @barcode
            """, conn);

        cmd.Parameters.AddWithValue("@barcode", barcode);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        string surname    = reader["Surname"]    is DBNull ? "" : reader["Surname"].ToString()!;
        string name       = reader["Name"]       is DBNull ? "" : reader["Name"].ToString()!;
        string secondName = reader["SecondName"] is DBNull ? "" : reader["SecondName"].ToString()!;
        string aud        = reader["AuditoriumCode"] is DBNull ? "" : reader["AuditoriumCode"].ToString()!;

        return new PersonInfo
        {
            Fio = $"{surname} {name} {secondName}".Trim(),
            Aud = aud
        };
    }

    /// <summary>Вычисляет разницу между двумя временами в минутах (in - out).</summary>
    public static int CalcMinutes(string outTime, string inTime)
    {
        if (TimeOnly.TryParseExact(outTime, "H:mm", out var t1) &&
            TimeOnly.TryParseExact(inTime,  "H:mm", out var t2))
        {
            return (int)(t2 - t1).TotalMinutes;
        }
        return 0;
    }
}
