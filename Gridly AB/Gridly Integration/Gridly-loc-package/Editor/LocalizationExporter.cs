using System.IO;
using System.Threading.Tasks;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Csv;
using System.Linq;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Enum;
using GridlyAB.GridlyIntegration.Gridly_loc_package.Editor.gridly.Dialog;
using System.Collections.Generic;

namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor
{
    /// <summary>
    /// Provides functionality to export Unity localization data to CSV format for Gridly integration.
    /// </summary>
    public static class LocalizationExporter
    {
        // CSV column constants
        private const string RecordIdColumn = "_recordId";
        private const string PathTagColumn = "_pathTag";
        private const string SmartStringColumn = "SmartString";
        private const string CsvColumnSeparator = ",";
        private const string ProgressBarTitle = "Exporting Localization Data";

        /// <summary>
        /// Exports localization data from a StringTableCollection to a CSV file for the specified locale.
        /// </summary>
        /// <param name="filePath">The path where the CSV file will be saved.</param>
        /// <param name="stringTableCollection">The StringTableCollection containing the localization data.</param>
        /// <param name="localeIdentifier">The locale identifier for the data to export.</param>
        /// <param name="selectedExportOption">The smart string filtering option.</param>
        /// <returns>The number of entries successfully exported to the CSV file.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when stringTableCollection is null.</exception>
        public static async Task<int> ExportLocalizationCSVAsync(
            string filePath, 
            StringTableCollection stringTableCollection, 
            LocaleIdentifier localeIdentifier, 
            SmartOption selectedExportOption)
        {
            ValidateInputParameters(stringTableCollection, filePath);
            
            var stringTable = GetStringTable(stringTableCollection, localeIdentifier);
            if (stringTable == null)
            {
                return 0;
            }

            var validEntries = GetValidEntries(stringTable);
            if (!validEntries.Any())
            {
                Debug.LogWarning($"StringTable {stringTable.name} contains no valid entries");
                return 0;
            }

            EnsureDirectoryExists(filePath);
            
            return await ExportEntriesToCsvAsync(filePath, stringTable, localeIdentifier, selectedExportOption, validEntries);
        }

        /// <summary>
        /// Validates the input parameters for the export operation.
        /// </summary>
        /// <param name="stringTableCollection">The StringTableCollection to validate.</param>
        /// <param name="filePath">The file path to validate.</param>
        private static void ValidateInputParameters(StringTableCollection stringTableCollection, string filePath)
        {
            if (stringTableCollection == null)
            {
                throw new System.ArgumentNullException(nameof(stringTableCollection), "String Table Collection cannot be null");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new System.ArgumentException("File path cannot be null or empty", nameof(filePath));
            }
        }

        /// <summary>
        /// Retrieves the StringTable for the specified locale from the collection.
        /// </summary>
        /// <param name="stringTableCollection">The StringTableCollection to search in.</param>
        /// <param name="localeIdentifier">The locale identifier.</param>
        /// <returns>The StringTable for the specified locale, or null if not found.</returns>
        private static StringTable GetStringTable(StringTableCollection stringTableCollection, LocaleIdentifier localeIdentifier)
        {
            var table = stringTableCollection.GetTable(localeIdentifier) as StringTable;
            if (table == null)
            {
                Debug.LogError($"No StringTable found for locale {localeIdentifier}");
                return null;
            }

            if (table.Values.Count == 0)
            {
                Debug.LogWarning($"StringTable {table.name} is empty");
                return null;
            }

            return table;
        }

        /// <summary>
        /// Filters the table entries to only include those with valid keys.
        /// </summary>
        /// <param name="stringTable">The StringTable to filter.</param>
        /// <returns>An enumerable of valid table entries.</returns>
        private static IEnumerable<object> GetValidEntries(StringTable stringTable)
        {
            return stringTable.Values.Where(entry => !string.IsNullOrEmpty(entry.Key));
        }

        /// <summary>
        /// Ensures the directory for the specified file path exists.
        /// </summary>
        /// <param name="filePath">The file path whose directory should be created.</param>
        private static void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Exports the filtered entries to a CSV file asynchronously.
        /// </summary>
        /// <param name="filePath">The path where the CSV file will be saved.</param>
        /// <param name="stringTable">The StringTable containing the data.</param>
        /// <param name="localeIdentifier">The locale identifier.</param>
        /// <param name="selectedExportOption">The smart string filtering option.</param>
        /// <param name="validEntries">The filtered entries to export.</param>
        /// <returns>The number of entries successfully exported.</returns>
        private static async Task<int> ExportEntriesToCsvAsync(
            string filePath, 
            StringTable stringTable, 
            LocaleIdentifier localeIdentifier, 
            SmartOption selectedExportOption, 
            IEnumerable<object> validEntries)
        {
            int totalEntries = validEntries.Count();
            ProgressBarUtility.InitializeProgressBar(ProgressBarTitle, totalEntries);

            var csv = CreateCsvExport();
            int exportedEntryCount = 0;

            try
            {
                exportedEntryCount = await ProcessEntriesAsync(
                    validEntries, 
                    stringTable, 
                    localeIdentifier, 
                    selectedExportOption, 
                    csv, 
                    totalEntries);

                if (exportedEntryCount > 0)
                {
                    await File.WriteAllTextAsync(filePath, csv.Export());
                    Debug.Log($"Localization data exported to {filePath}");
                }
            }
            finally
            {
                ProgressBarUtility.ClearProgressBar();
            }

            return exportedEntryCount;
        }

        /// <summary>
        /// Creates a new CsvExport instance with the configured settings.
        /// </summary>
        /// <returns>A configured CsvExport instance.</returns>
        private static CsvExport CreateCsvExport()
        {
            return new CsvExport(
                columnSeparator: CsvColumnSeparator,
                includeColumnSeparatorDefinitionPreamble: false, // Excel compatibility
                includeHeaderRow: true
            );
        }

        /// <summary>
        /// Processes each entry and adds it to the CSV if it meets the filtering criteria.
        /// </summary>
        /// <param name="validEntries">The entries to process.</param>
        /// <param name="stringTable">The StringTable containing the data.</param>
        /// <param name="localeIdentifier">The locale identifier.</param>
        /// <param name="selectedExportOption">The smart string filtering option.</param>
        /// <param name="csv">The CsvExport instance to add rows to.</param>
        /// <param name="totalEntries">The total number of entries for progress tracking.</param>
        /// <returns>The number of entries successfully processed.</returns>
        private static async Task<int> ProcessEntriesAsync(
            IEnumerable<object> validEntries,
            StringTable stringTable,
            LocaleIdentifier localeIdentifier,
            SmartOption selectedExportOption,
            CsvExport csv,
            int totalEntries)
        {
            int processedEntryCount = 0;

            foreach (var entry in validEntries)
            {
                if (ShouldSkipEntry(entry, selectedExportOption))
                {
                    continue;
                }

                if (ProgressBarUtility.IsCancelled())
                {
                    Debug.LogWarning("Export canceled by user.");
                    return 0;
                }

                AddEntryToCsv(csv, entry, stringTable, localeIdentifier);
                processedEntryCount++;
                
                // Use dynamic to access the Key property for progress reporting
                dynamic dynamicEntry = entry;
                ProgressBarUtility.IncrementProgress($"Exporting entry {processedEntryCount}/{totalEntries} ({dynamicEntry.Key})");
            }

            return processedEntryCount;
        }

        /// <summary>
        /// Determines if an entry should be skipped based on the selected export option.
        /// </summary>
        /// <param name="entry">The entry to evaluate.</param>
        /// <param name="selectedExportOption">The smart string filtering option.</param>
        /// <returns>True if the entry should be skipped, false otherwise.</returns>
        private static bool ShouldSkipEntry(object entry, SmartOption selectedExportOption)
        {
            // Use dynamic to access properties at runtime
            dynamic dynamicEntry = entry;
            return (selectedExportOption == SmartOption.OnlyNonSmart && dynamicEntry.IsSmart) ||
                   (selectedExportOption == SmartOption.OnlySmart && !dynamicEntry.IsSmart);
        }

        /// <summary>
        /// Adds a single entry to the CSV export.
        /// </summary>
        /// <param name="csv">The CsvExport instance to add the row to.</param>
        /// <param name="entry">The entry to add.</param>
        /// <param name="stringTable">The StringTable containing the entry.</param>
        /// <param name="localeIdentifier">The locale identifier.</param>
        private static void AddEntryToCsv(
            CsvExport csv, 
            object entry, 
            StringTable stringTable, 
            LocaleIdentifier localeIdentifier)
        {
            // Use dynamic to access properties at runtime
            dynamic dynamicEntry = entry;
            
            csv.AddRow();
            csv[RecordIdColumn] = dynamicEntry.Key;
            csv[PathTagColumn] = stringTable.TableCollectionName;
            csv[localeIdentifier.Code.Replace("-", "")] = dynamicEntry.LocalizedValue;
            csv[SmartStringColumn] = dynamicEntry.IsSmart.ToString().ToLower();
        }
    }
}
