using System.IO;
using System.Threading.Tasks;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using static Csv.CsvExport; // Import the CsvExport library
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Enum;
using Csv;
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using GridlyAB.GridlyIntegration.Gridly_loc_package.Editor.gridly.Dialog;

namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor
{
    public static class LocalizationExporter
    {
        public static async Task<int> ExportLocalizationCSVAsync(string filePath, StringTableCollection stringTableCollection, LocaleIdentifier localeIdentifier, SmartOption selectedExportOption)
        {
            if (stringTableCollection == null)
            {
                Debug.LogError("String Table Collection not found!");
                return 0;
            }

            // Get the StringTable for the specified locale
            var table = stringTableCollection.GetTable(localeIdentifier) as StringTable;
            if (table == null)
            {
                Debug.LogError($"No StringTable found for locale {localeIdentifier}");
                return 0;
            }
            if (table.Values.Count == 0)
            {
                Debug.LogWarning($"StringTable {table.name} is empty");
                return 0;
            }

            // Ensure the directory exists
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var entries = table.Values.Where(entry => entry.Key != null);
            //var entriesList = entries.ToList();

            // Initialize progress bar with the number of entries to process
            int totalEntries = table.Values.Where(entry => entry.Key != null).Count();


            ProgressBarUtility.InitializeProgressBar("Exporting Localization Data", totalEntries);

            // Create CSV data using CsvExport
            var csv = new CsvExport(
                columnSeparator: ",",
                includeColumnSeparatorDefinitionPreamble: false, //Excel wants this in CSV files
                includeHeaderRow: true
            );



            // Process each entry and add rows to the CSV
            int csvEntry = 0;
            foreach (var entry in entries)
            {
                if (selectedExportOption == SmartOption.OnlyNonSmart && entry.IsSmart || selectedExportOption == SmartOption.OnlySmart && !entry.IsSmart)
                {
                    continue;
                }

                // Check if the process has been canceled
                if (ProgressBarUtility.IsCancelled())
                {
                    Debug.LogWarning("Export canceled by user.");
                    ProgressBarUtility.ClearProgressBar();  // Clear the progress bar
                    return 0;  // Exit the method early
                }

                if (!string.IsNullOrEmpty(entry.Key))
                {
                    csv.AddRow();
                    csv["_recordId"] = entry.Key;
                    csv["_pathTag"] = table.TableCollectionName;
                    csv[localeIdentifier.Code.Replace("-", "")] = entry.LocalizedValue;
                    csv["SmartString"] = entry.IsSmart.ToString().ToLower();
                }

                // Increment the progress after processing each entry
                csvEntry++;
                ProgressBarUtility.IncrementProgress($"Exporting entry {csvEntry}/{totalEntries} ({entry.Key})");
            }

            if (csvEntry > 0)
            {
                // Write CSV data to file
                await File.WriteAllTextAsync(filePath, csv.Export());
            }

            // Clear progress bar after export completes
            ProgressBarUtility.ClearProgressBar();
            Debug.Log($"Localization data exported to {filePath}");
            return csvEntry;
        }
    }
}
