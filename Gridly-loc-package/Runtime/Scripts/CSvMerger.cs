using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class CsvMerger
{
    public static async Task MergeCsvFilesAsync(List<string> csvFilePaths, string outputFilePath)
    {
        Dictionary<string, Dictionary<string, string>> mergedData = new Dictionary<string, Dictionary<string, string>>();
        List<string> languages = new List<string>();

        foreach (string filePath in csvFilePaths)
        {
            List<string> lines = new List<string>();

            // Read the file asynchronously using StreamReader
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        lines.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading file {filePath}: {ex.Message}");
                continue;
            }

            if (lines.Count == 0)
            {
                Debug.LogWarning($"File {filePath} is empty or could not be read.");
                continue;
            }

            // Get the language and SmartString from the header
            string[] headers = ParseCsvLine(lines[0]);
            if (headers.Length < 4)
            {
                Debug.LogError($"File {filePath} does not have enough columns in the header.");
                continue;
            }

            string language = headers[2].Trim();
            if (!languages.Contains(language)) languages.Add(language);

            for (int i = 1; i < lines.Count; i++)
            {
                string[] values = ParseCsvLine(lines[i]);

                if (values.Length < 4)
                {
                    Debug.LogWarning($"Skipping malformed line in {filePath}: {lines[i]}");
                    continue;
                }

                string recordId = values[0].Trim();
                string pathTag = values[1].Trim();
                string translation = values[2].Trim();
                string smartString = values[3].Trim(); // New SmartString column

                string key = $"{recordId},{pathTag}";

                if (!mergedData.ContainsKey(key))
                {
                    mergedData[key] = new Dictionary<string, string> { { "SmartString", smartString } };
                }

                mergedData[key][language] = translation;
            }
        }

        string directoryPath = Path.GetDirectoryName(outputFilePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        using (StreamWriter writer = new StreamWriter(outputFilePath))
        {
            // Write the header with SmartString and languages
            await writer.WriteLineAsync($"\"_recordId\",\"_pathTag\",\"SmartString\",{string.Join(",", languages.Select(lang => $"\"{lang}\""))}");

            foreach (var entry in mergedData)
            {
                string key = entry.Key;
                var translations = entry.Value;

                List<string> row = new List<string>(key.Split(',').Select(EscapeCsvValue))
                {
                    EscapeCsvValue(translations.ContainsKey("SmartString") ? translations["SmartString"] : string.Empty)
                };

                foreach (string language in languages)
                {
                    row.Add(EscapeCsvValue(translations.ContainsKey(language) ? translations[language] : string.Empty));
                }

                await writer.WriteLineAsync(string.Join(",", row));
            }
        }
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains("\"") || value.Contains(","))
        {
            return $"\"{value.Replace("\"", "\"\"")}\""; // Escape double quotes by doubling them
        }
        return $"\"{value}\"";
    }

    private static string[] ParseCsvLine(string line)
    {
        List<string> values = new List<string>();
        bool inQuotes = false;
        string currentValue = string.Empty;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    // Handle escaped double quotes
                    currentValue += "\"";
                    i++; // Skip the next quote
                }
                else
                {
                    inQuotes = !inQuotes; // Toggle inQuotes
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue);
                currentValue = string.Empty;
            }
            else
            {
                currentValue += c;
            }
        }

        if (!string.IsNullOrEmpty(currentValue))
        {
            values.Add(currentValue);
        }

        return values.ToArray();
    }
}
