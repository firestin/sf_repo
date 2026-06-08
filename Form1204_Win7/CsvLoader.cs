using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Form1204
{
    public static class CsvLoader
    {
        private const string TasksDir = @"C:\IXORAUSE55\TR55erbd02\Tasks";

        public static string FindLatestCsv()
        {
            if (!Directory.Exists(TasksDir))
                return null;

            var files = Directory
                .EnumerateFiles(TasksDir, "*.csv", SearchOption.AllDirectories)
                .Where(f =>
                    f.IndexOf(@"\Reports\", StringComparison.OrdinalIgnoreCase) < 0 &&
                    Regex.IsMatch(Path.GetFileName(f), @"\d{8}\.csv$"))
                .ToList();

            if (files.Count == 0)
                return null;

            return files.OrderByDescending(f => File.GetLastWriteTime(f)).First();
        }

        public static List<Dictionary<string, string>> Load(string path)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            int[] codepages = new[] { 1251, 65001 };

            foreach (var cp in codepages)
            {
                try
                {
                    Encoding enc = cp == 65001
                        ? new UTF8Encoding(true)
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
            string[] headers = null;

            foreach (var line in File.ReadLines(path, enc))
            {
                var parts = line.Split(';');

                if (headers == null)
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
}
