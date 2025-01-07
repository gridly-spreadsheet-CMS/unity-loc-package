using System.IO;
using System.Threading.Tasks;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Dialog;
using Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Enum;

public static class LocalizationExporter
{
    public static async Task ExportLocalizationCSVAsync(string filePath, StringTableCollection stringTableCollection, LocaleIdentifier localeIdentifier, SmartOption selectedExportOption)
    {
        if (stringTableCollection == null)
        {
            Debug.LogError("String Table Collection not found!");
            return;
        }

        // Get the StringTable for the specified locale
        var table = stringTableCollection.GetTable(localeIdentifier) as StringTable;
        if (table == null)
        {
            Debug.LogError($"No StringTable found for locale {localeIdentifier}");
            return;
        }

        // Ensure the directory exists
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Initialize progress bar with the number of entries to process
        int totalEntries = table.Values.Count;
        ProgressBarUtility.InitializeProgressBar("Exporting Localization Data", totalEntries);

        // Create or overwrite the file
        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            // Write CSV header
            await writer.WriteLineAsync("\"_recordId\",\"_pathTag\",\"" + table.LocaleIdentifier.Code.Replace("-", "") +"\",\"SmartString\"");

            // Process each entry and update the progress bar
            int currentEntry = 0;
            foreach (var entry in table.Values)
            {
                if(selectedExportOption == SmartOption.OnlyNonSmart && entry.IsSmart || selectedExportOption == SmartOption.OnlySmart && !entry.IsSmart)
                {
                    continue;
                }
                // Check if the process has been canceled
                if (ProgressBarUtility.IsCancelled())
                {
                    Debug.LogWarning("Export canceled by user.");
                    ProgressBarUtility.ClearProgressBar();  // Clear the progress bar
                    return;  // Exit the method early
                }
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    await writer.WriteLineAsync($"\"{entry.Key}\",\"{table.TableCollectionName}\",\"{entry.LocalizedValue}\",\"{entry.IsSmart.ToString().ToLower()}\"");
                }

                // Increment the progress after processing each entry
                currentEntry++;
                ProgressBarUtility.IncrementProgress($"Exporting entry {currentEntry}/{totalEntries} ({entry.Key})");

                // Replace Thread.Sleep with Task.Delay to avoid blocking the main thread
                //await Task.Delay(25);  // Introduce a small delay to simulate time-consuming export
            }
        }

        // Clear progress bar after export completes
        ProgressBarUtility.ClearProgressBar();
        Debug.Log($"Localization data exported to {filePath}");
    }
}
