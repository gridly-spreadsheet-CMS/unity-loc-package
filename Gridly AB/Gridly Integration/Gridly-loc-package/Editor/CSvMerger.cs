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
    /// <summary>
    /// Utility class for merging multiple CSV files into a single consolidated file.
    /// Handles localization data from Gridly exports and combines them by record ID and path tag.
    /// </summary>
    public class CsvMerger
    {
        #region Constants

        private const int RECORD_ID_COLUMN_INDEX = 0;
        private const int PATH_TAG_COLUMN_INDEX = 1;
        private const int LANGUAGE_COLUMN_INDEX = 2;
        private const int SMART_STRING_COLUMN_INDEX = 3;
        private const string SMART_STRING_COLUMN_NAME = "SmartString";
        private const string RECORD_ID_HEADER = "_recordId";
        private const string PATH_TAG_HEADER = "_pathTag";
        private const string CSV_SEPARATOR = ",";
        private const string QUOTE_CHARACTER = "\"";
        private const string ESCAPED_QUOTE = "\"\"";
        private const char NEWLINE_CHARACTER = '\n';
        private const string EMPTY_STRING = "";

        #endregion

        #region Public Methods

        /// <summary>
        /// Asynchronously merges multiple CSV files into a single output file.
        /// Combines localization data by record ID and path tag, organizing translations by language.
        /// </summary>
        /// <param name="csvFilePaths">List of CSV file paths to merge.</param>
        /// <param name="outputFilePath">The path where the merged CSV file will be written.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when csvFilePaths or outputFilePath is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when csvFilePaths is empty.</exception>
        public static async Task MergeCsvFilesAsync(List<string> csvFilePaths, string outputFilePath)
        {
            ValidateInputParameters(csvFilePaths, outputFilePath);

            var mergedData = new Dictionary<string, Dictionary<string, string>>();
            var languages = new List<string>();

            await ProcessCsvFiles(csvFilePaths, mergedData, languages);
            await WriteMergedCsvFile(outputFilePath, mergedData, languages);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the input parameters for the merge operation.
        /// </summary>
        /// <param name="csvFilePaths">The list of CSV file paths to validate.</param>
        /// <param name="outputFilePath">The output file path to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when parameters are null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when csvFilePaths is empty.</exception>
        private static void ValidateInputParameters(List<string> csvFilePaths, string outputFilePath)
        {
            if (csvFilePaths == null || csvFilePaths.Count == 0)
            {
                throw new ArgumentException("No CSV files provided for merging.", nameof(csvFilePaths));
            }

            if (string.IsNullOrEmpty(outputFilePath))
            {
                throw new ArgumentNullException(nameof(outputFilePath), "Output file path cannot be null or empty.");
            }
        }

        /// <summary>
        /// Processes all CSV files and populates the merged data structure.
        /// </summary>
        /// <param name="csvFilePaths">The list of CSV file paths to process.</param>
        /// <param name="mergedData">The dictionary to store merged data.</param>
        /// <param name="languages">The list to store unique languages.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task ProcessCsvFiles(List<string> csvFilePaths, Dictionary<string, Dictionary<string, string>> mergedData, List<string> languages)
        {
            foreach (string filePath in csvFilePaths)
            {
                await ProcessSingleCsvFile(filePath, mergedData, languages);
            }
        }

        /// <summary>
        /// Processes a single CSV file and adds its data to the merged structure.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to process.</param>
        /// <param name="mergedData">The dictionary to store merged data.</param>
        /// <param name="languages">The list to store unique languages.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task ProcessSingleCsvFile(string filePath, Dictionary<string, Dictionary<string, string>> mergedData, List<string> languages)
        {
            try
            {
                string fileContent = await File.ReadAllTextAsync(filePath);
                var rows = ParseCsvContent(fileContent);
                
                if (rows.Count == 0)
                {
                    Debug.LogWarning($"CSV file {filePath} is empty or could not be parsed.");
                    return;
                }

                ProcessCsvRows(rows, mergedData, languages);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing CSV file {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes the rows of a CSV file and extracts localization data.
        /// </summary>
        /// <param name="rows">The parsed CSV rows.</param>
        /// <param name="mergedData">The dictionary to store merged data.</param>
        /// <param name="languages">The list to store unique languages.</param>
        private static void ProcessCsvRows(List<string[]> rows, Dictionary<string, Dictionary<string, string>> mergedData, List<string> languages)
        {
            if (rows.Count < 2) return; // Need at least header and one data row

            var headers = rows[0];
            string language = ExtractLanguageFromHeaders(headers);
            
            if (!languages.Contains(language))
            {
                languages.Add(language);
            }

            ProcessDataRows(rows, mergedData, language);
        }

        /// <summary>
        /// Extracts the language from the CSV headers.
        /// </summary>
        /// <param name="headers">The header row of the CSV.</param>
        /// <returns>The language code extracted from the headers.</returns>
        private static string ExtractLanguageFromHeaders(string[] headers)
        {
            if (headers.Length > LANGUAGE_COLUMN_INDEX)
            {
                return headers[LANGUAGE_COLUMN_INDEX].Trim();
            }
            return EMPTY_STRING;
        }

        /// <summary>
        /// Processes the data rows of a CSV file and adds them to the merged data structure.
        /// </summary>
        /// <param name="rows">The parsed CSV rows.</param>
        /// <param name="mergedData">The dictionary to store merged data.</param>
        /// <param name="language">The language code for this file.</param>
        private static void ProcessDataRows(List<string[]> rows, Dictionary<string, Dictionary<string, string>> mergedData, string language)
        {
            for (int i = 1; i < rows.Count; i++)
            {
                var values = rows[i];
                if (values.Length < 4) continue; // Skip incomplete rows

                var recordData = ExtractRecordData(values);
                var recordKey = CreateRecordKey(recordData.RecordId, recordData.PathTag);

                if (!mergedData.ContainsKey(recordKey))
                {
                    mergedData[recordKey] = new Dictionary<string, string> { { SMART_STRING_COLUMN_NAME, recordData.SmartString } };
                }

                mergedData[recordKey][language] = recordData.Translation;
            }
        }

        /// <summary>
        /// Extracts record data from a CSV row.
        /// </summary>
        /// <param name="values">The values from a CSV row.</param>
        /// <returns>A record data structure containing the extracted values.</returns>
        private static (string RecordId, string PathTag, string Translation, string SmartString) ExtractRecordData(string[] values)
        {
            return (
                values[RECORD_ID_COLUMN_INDEX].Trim(),
                values[PATH_TAG_COLUMN_INDEX].Trim(),
                values[LANGUAGE_COLUMN_INDEX].Trim(),
                values[SMART_STRING_COLUMN_INDEX].Trim()
            );
        }

        /// <summary>
        /// Creates a unique key for a record based on record ID and path tag.
        /// </summary>
        /// <param name="recordId">The record ID.</param>
        /// <param name="pathTag">The path tag.</param>
        /// <returns>A unique key string.</returns>
        private static string CreateRecordKey(string recordId, string pathTag)
        {
            return $"{recordId},{pathTag}";
        }

        /// <summary>
        /// Writes the merged CSV data to the output file.
        /// </summary>
        /// <param name="outputFilePath">The path where the merged CSV file will be written.</param>
        /// <param name="mergedData">The merged data to write.</param>
        /// <param name="languages">The list of languages.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task WriteMergedCsvFile(string outputFilePath, Dictionary<string, Dictionary<string, string>> mergedData, List<string> languages)
        {
            CreateOutputDirectory(outputFilePath);
            var csvContent = BuildCsvContent(mergedData, languages);
            await File.WriteAllTextAsync(outputFilePath, csvContent);
            Debug.Log($"Merged CSV file written to {outputFilePath}");
        }

        /// <summary>
        /// Creates the output directory if it doesn't exist.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        private static void CreateOutputDirectory(string outputFilePath)
        {
            string directoryPath = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Builds the CSV content from the merged data.
        /// </summary>
        /// <param name="mergedData">The merged data to convert to CSV.</param>
        /// <param name="languages">The list of languages.</param>
        /// <returns>The CSV content as a string.</returns>
        private static string BuildCsvContent(Dictionary<string, Dictionary<string, string>> mergedData, List<string> languages)
        {
            var csvBuilder = new StringBuilder();
            
            // Add header
            csvBuilder.AppendLine(BuildCsvHeader(languages));
            
            // Add data rows
            foreach (var entry in mergedData)
            {
                csvBuilder.AppendLine(BuildCsvRow(entry, languages));
            }
            
            return csvBuilder.ToString();
        }

        /// <summary>
        /// Builds the CSV header row.
        /// </summary>
        /// <param name="languages">The list of languages.</param>
        /// <returns>The CSV header as a string.</returns>
        private static string BuildCsvHeader(List<string> languages)
        {
            var headerParts = new List<string>
            {
                RECORD_ID_HEADER,
                PATH_TAG_HEADER,
                SMART_STRING_COLUMN_NAME
            };
            
            headerParts.AddRange(languages.Select(EscapeCsvValue));
            return string.Join(CSV_SEPARATOR, headerParts);
        }

        /// <summary>
        /// Builds a CSV data row.
        /// </summary>
        /// <param name="entry">The key-value pair containing record data.</param>
        /// <param name="languages">The list of languages.</param>
        /// <returns>The CSV row as a string.</returns>
        private static string BuildCsvRow(KeyValuePair<string, Dictionary<string, string>> entry, List<string> languages)
        {
            var keyParts = entry.Key.Split(',');
            var translations = entry.Value;

            var rowParts = new List<string>
            {
                EscapeCsvValue(keyParts[0]),
                EscapeCsvValue(keyParts[1]),
                EscapeCsvValue(translations[SMART_STRING_COLUMN_NAME])
            };

            foreach (string language in languages)
            {
                string translation = translations.ContainsKey(language) ? translations[language] : EMPTY_STRING;
                rowParts.Add(EscapeCsvValue(translation));
            }

            return string.Join(CSV_SEPARATOR, rowParts);
        }

        /// <summary>
        /// Escapes a CSV value by handling quotes, commas, and newlines.
        /// </summary>
        /// <param name="value">The value to escape.</param>
        /// <returns>The escaped CSV value.</returns>
        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return EMPTY_STRING;
            }

            if (value.Contains(QUOTE_CHARACTER) || value.Contains(CSV_SEPARATOR) || value.Contains(NEWLINE_CHARACTER))
            {
                return $"{QUOTE_CHARACTER}{value.Replace(QUOTE_CHARACTER, ESCAPED_QUOTE)}{QUOTE_CHARACTER}";
            }
            
            return value;
        }

        /// <summary>
        /// Parses CSV content into a list of string arrays representing rows and columns.
        /// </summary>
        /// <param name="content">The CSV content to parse.</param>
        /// <returns>A list of string arrays, where each array represents a row.</returns>
        private static List<string[]> ParseCsvContent(string content)
        {
            var rows = new List<string[]>();
            var currentField = new StringBuilder();
            var currentRow = new List<string>();
            bool inQuotes = false;

            for (int i = 0; i < content.Length; i++)
            {
                char currentCharacter = content[i];

                if (currentCharacter == '"')
                {
                    HandleQuoteCharacter(content, ref i, currentField, ref inQuotes);
                }
                else if (currentCharacter == ',' && !inQuotes)
                {
                    EndField(currentRow, currentField);
                }
                else if (currentCharacter == NEWLINE_CHARACTER && !inQuotes)
                {
                    EndRow(rows, currentRow, currentField);
                }
                else
                {
                    currentField.Append(currentCharacter);
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

        /// <summary>
        /// Handles quote characters in CSV parsing, including escaped quotes.
        /// </summary>
        /// <param name="content">The CSV content being parsed.</param>
        /// <param name="index">The current index in the content.</param>
        /// <param name="currentField">The current field being built.</param>
        /// <param name="inQuotes">Whether we're currently inside quotes.</param>
        private static void HandleQuoteCharacter(string content, ref int index, StringBuilder currentField, ref bool inQuotes)
        {
            if (inQuotes && index + 1 < content.Length && content[index + 1] == '"')
            {
                // Handle escaped double quotes ("" -> ")
                currentField.Append('"');
                index++; // Skip the next quote
            }
            else
            {
                // Toggle the inQuotes state
                inQuotes = !inQuotes;
            }
        }

        /// <summary>
        /// Ends the current field and adds it to the current row.
        /// </summary>
        /// <param name="currentRow">The current row being built.</param>
        /// <param name="currentField">The current field being built.</param>
        private static void EndField(List<string> currentRow, StringBuilder currentField)
        {
            currentRow.Add(currentField.ToString());
            currentField.Clear();
        }

        /// <summary>
        /// Ends the current row and adds it to the rows collection.
        /// </summary>
        /// <param name="rows">The collection of rows.</param>
        /// <param name="currentRow">The current row being built.</param>
        /// <param name="currentField">The current field being built.</param>
        private static void EndRow(List<string[]> rows, List<string> currentRow, StringBuilder currentField)
        {
            currentRow.Add(currentField.ToString());
            rows.Add(currentRow.ToArray());
            currentRow.Clear();
            currentField.Clear();
        }

        #endregion
    }
}
