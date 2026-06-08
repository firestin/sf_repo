using System.Text;
using System.Text.RegularExpressions;

namespace Form1204;

public static class CsvLoader
{
    private const string TasksDir = @"C:\IXORAUSE55\TR55erbd02\Tasks";

    /// <summary>
    /// Ищет самый свежий CSV с датой в имени (YYYYMMDD.csv),
    /// исключая файлы из подпапки Reports.
    /// </summary>
    public static string? FindLatestCsv()
    {
        if (!Directory.Exists(TasksDir))
            return null;

        var files = Directory
            .EnumerateFiles(TasksDir, "*.csv", SearchOption.AllDirectories)
            .Where(f =>
                !f.Contains(@"\Reports\", StringComparison.OrdinalIgnoreCase) &&
                Regex.IsMatch(Path.GetFileName(f), @"\d{8}\.csv$"))
            .ToList();

        return files.Count == 0
            ? null
            : files.MaxBy(File.GetLastWriteTime);
    }

    /// <summary>
    /// Читает CSV (разделитель «;»), пробуя кодировки cp1251 → utf-8-sig → utf-8.
    /// Возвращает список словарей «заголовок → значение».
    /// </summary>
    public static List<Dictionary<string, string>> Load(string path)
    {
        // cp1251 не входит в стандартные .NET-кодировки без регистрации провайдера
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        int[] codepages = [1251, 65001]; // 65001 = UTF-8

        foreach (var cp in codepages)
        {
            try
            {
                var enc = cp == 65001
                    ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)  // utf-8-sig
                    : Encoding.GetEncoding(cp);

                return ReadCsv(path, enc);
            }
            catch
            {
                // пробуем следующую кодировку
            }
        }

        throw new Exception("Не удалось прочитать CSV ни в одной из кодировок.");
    }

    private static List<Dictionary<string, string>> ReadCsv(string path, Encoding enc)
    {
        var result = new List<Dictionary<string, string>>();
        string[]? headers = null;

        foreach (var line in File.ReadLines(path, enc))
        {
            var parts = line.Split(';');

            if (headers is null)
            {
                headers = parts;
                continue;
            }

            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < headers.Length; i++)
                row[headers[i]] = i < parts.Length ? parts[i] : "";

            result.Add(row);
        }

        return result;
    }
}
