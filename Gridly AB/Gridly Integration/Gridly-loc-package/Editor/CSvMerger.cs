using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Csv; // Import the CsvExport library

namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor
{
    public class CsvMerger
    {
        public static async Task MergeCsvFilesAsync(List<string> csvFilePaths, string outputFilePath)
        {
            if (csvFilePaths == null || csvFilePaths.Count == 0)
            {
                Debug.LogError("No CSV files provided for merging.");
                return;
            }

            // Dictionary to store merged data
            Dictionary<string, Dictionary<string, string>> mergedData = new Dictionary<string, Dictionary<string, string>>();
            List<string> languages = new List<string>();

            foreach (string filePath in csvFilePaths)
            {
                // Read the entire file as a single string
                string fileContent = await File.ReadAllTextAsync(filePath);

                // Parse the CSV content into rows
                List<string[]> rows = ParseCsv(fileContent);

                // Parse header
                string[] headers = rows[0];
                string language = headers[2].Trim(); // Assuming language is always in the third column
                if (!languages.Contains(language)) languages.Add(language);

                // Process data rows
                for (int i = 1; i < rows.Count; i++)
                {
                    string[] values = rows[i];

                    string recordId = values[0].Trim();
                    string pathTag = values[1].Trim();
                    string translation = values[2].Trim();
                    string smartString = values[3].Trim(); // SmartString column

                    string key = $"{recordId},{pathTag}";

                    if (!mergedData.ContainsKey(key))
                    {
                        mergedData[key] = new Dictionary<string, string> { { "SmartString", smartString } };
                    }

                    mergedData[key][language] = translation;
                }
            }

            // Create output directory if it doesn't exist
            string directoryPath = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Write the CSV manually
            StringBuilder csvBuilder = new StringBuilder();

            // Add the header
            csvBuilder.AppendLine($"_recordId,_pathTag,SmartString,{string.Join(",", languages.Select(EscapeCsvValue))}");

            // Add data rows to the CSV
            foreach (var entry in mergedData)
            {
                string key = entry.Key;
                var translations = entry.Value;

                string[] keyParts = key.Split(',');

                List<string> row = new List<string>
        {
            EscapeCsvValue(keyParts[0]),
            EscapeCsvValue(keyParts[1]),
            EscapeCsvValue(translations["SmartString"])
        };

                foreach (string language in languages)
                {
                    row.Add(EscapeCsvValue(translations.ContainsKey(language) ? translations[language] : string.Empty));
                }

                csvBuilder.AppendLine(string.Join(",", row));
            }

            // Write the CSV to the output file
            await File.WriteAllTextAsync(outputFilePath, csvBuilder.ToString());
            Debug.Log($"Merged CSV file written to {outputFilePath}");
        }

        private static string EscapeCsvValue(string value)
        {
            // Escape quotes and wrap the field in quotes if it contains special characters
            if (value.Contains("\"") || value.Contains(",") || value.Contains("\n"))
            {
                value = $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }



        private static List<string[]> ParseCsv(string content)
        {
            List<string[]> rows = new List<string[]>();
            StringBuilder currentField = new StringBuilder();
            List<string> currentRow = new List<string>();
            bool inQuotes = false;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];

                if (c == '\"')
                {
                    if (inQuotes && i + 1 < content.Length && content[i + 1] == '\"')
                    {
                        // Handle escaped double quotes ("" -> ")
                        currentField.Append('\"');
                        i++; // Skip the next quote
                    }
                    else
                    {
                        // Toggle the inQuotes state
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // End of field when not inside quotes
                    currentRow.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if (c == '\n' && !inQuotes)
                {
                    // End of row when not inside quotes
                    currentRow.Add(currentField.ToString());
                    rows.Add(currentRow.ToArray());
                    currentRow.Clear();
                    currentField.Clear();
                }
                else
                {
                    // Append the character to the current field
                    currentField.Append(c);
                }
            }

            // Handle the last field and row
            if (currentField.Length > 0)
            {
                currentRow.Add(currentField.ToString());
            }
            if (currentRow.Count > 0)
            {
                rows.Add(currentRow.ToArray());
            }

            return rows;
        }



    }
}
