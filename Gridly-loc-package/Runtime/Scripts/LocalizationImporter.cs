using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading;
using Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Model;
using System.Collections;
using Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Dialog;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Enum;

namespace Assets.Gridly_loc_package.Runtime.Scripts
{
    public class LocalizationImporter
    {
        private ApiClient apiClient;
        private string viewId;
        private string apiKey;
        private CancellationTokenSource cancellationTokenSource;

        // Store results per file and language
        private Dictionary<string, Dictionary<string, ImportResponse>> fileLanguageResponses = new Dictionary<string, Dictionary<string, ImportResponse>>();
        private int totalLanguagesProcessed = 0;
        private int totalLanguagesToProcess = 0;

        public LocalizationImporter(ApiClient apiClient, string viewId, string apiKey, SmartOption selectedImportOpiton)
        {
            this.apiClient = apiClient;
            this.viewId = viewId;
            this.apiKey = apiKey;
        }

        // Entry point for importing data with cancellation support
        public void ImportData(List<string> fileNames, List<string> languageCodes)
        {
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            totalLanguagesToProcess = fileNames.Count * languageCodes.Count;
            totalLanguagesProcessed = 0;

            // Initialize progress bar
            ProgressBarUtility.InitializeProgressBar("Importing Localization Data", totalLanguagesToProcess);

            EditorApplication.update += () => UpdateImportProgress(token);

            // Start the import process as a coroutine
            apiClient.StartCoroutine(ImportLocalizationCoroutine(fileNames, languageCodes, token));
        }

        // The coroutine that processes the localization import asynchronously
        private IEnumerator ImportLocalizationCoroutine(List<string> fileNames, List<string> languageCodes, CancellationToken token)
        {
            foreach (string fileName in fileNames)
            {
                if (!fileLanguageResponses.ContainsKey(fileName))
                {
                    fileLanguageResponses[fileName] = new Dictionary<string, ImportResponse>();
                }

                foreach (string languageCode in languageCodes)
                {
                    // Check if the process has been canceled
                    if (ProgressBarUtility.IsCancelled() || token.IsCancellationRequested)
                    {
                        Debug.Log("Import canceled by user.");
                        cancellationTokenSource.Cancel(); // Stop the import process
                        ProgressBarUtility.ClearProgressBar(); // Clear the progress bar
                        yield break; // Exit the coroutine immediately
                    }

                    // Flag to check if the callback has been called
                    bool isRecordsFetched = false;

                    // Fetch the records for the file and language, asynchronously
                    apiClient.GetRecords(viewId, apiKey, fileName, languageCode, records =>
                    {
                        OnRecordsFetched(records, fileName, languageCode);
                        totalLanguagesProcessed++;

                        // Update the progress bar
                        ProgressBarUtility.IncrementProgress($"Processing {fileName} ({languageCode})");

                        // Clear progress bar when all languages are processed
                        if (totalLanguagesProcessed >= totalLanguagesToProcess)
                        {
                            ProgressBarUtility.ClearProgressBar();
                            ImportSummaryWindow.ShowWindow(fileLanguageResponses);  // Open the custom window
                        }

                        // Mark that the records have been fetched
                        isRecordsFetched = true;

                    }, errorMessage =>
                    {
                        Debug.LogError($"Error fetching records for {fileName} ({languageCode}): {errorMessage}");
                        isRecordsFetched = true; // Mark as finished even if it fails
                    });

                    // Wait until the records have been fetched or error occurred
                    yield return new WaitUntil(() => isRecordsFetched);

                    // Simulate a small delay (or handle rate-limited API calls here)
                    //yield return new WaitForSeconds(0.2f);
                }
            }

            // Clear the progress bar after the entire process is done
            ProgressBarUtility.ClearProgressBar();
        }



        // Progress bar update and cancellation logic
        private void UpdateImportProgress(CancellationToken token)
        {
            // Cancel the token if the progress bar is canceled
            if (ProgressBarUtility.IsCancelled() && !token.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                Debug.Log("Cancellation requested.");
                ProgressBarUtility.ClearProgressBar();
            }
        }

        private void OnRecordsFetched(List<Record> records, string fileName, string languageCode)
        {
            ImportResponse importResponse = UpdateLocalization(records);

            // Add or update response per file and language
            if (!fileLanguageResponses[fileName].ContainsKey(languageCode))
            {
                fileLanguageResponses[fileName][languageCode] = importResponse;
            }
            else
            {
                fileLanguageResponses[fileName][languageCode].Added += importResponse.Added;
                fileLanguageResponses[fileName][languageCode].Updated += importResponse.Updated;
            }
        }

        private void OnErrorFetchingRecords(string errorMessage)
        {
            Debug.LogError($"Failed to fetch records: {errorMessage}");
        }

        public static ImportResponse UpdateLocalization(List<Record> records)
        {
            ImportResponse importResponse = new ImportResponse();

            foreach (Record record in records)
            {
                string localeIdentifier = ConvertLanguageCode(record.cells[0].columnId);
                string tableCollectionName = record.path;
                string entryKey = record.id;
                string translation = record.cells[0].value;

                var locale = LocalizationSettings.AvailableLocales.GetLocale(localeIdentifier);
                if (locale == null)
                {
                    importResponse.Error = $"Locale not found: {localeIdentifier}";
                    return importResponse;
                }

                var table = LocalizationSettings.StringDatabase.GetTable(tableCollectionName, locale) as StringTable;
                if (table == null)
                {
                    importResponse.Error = $"Table not found for collection: {tableCollectionName} and locale: {localeIdentifier}";
                    return importResponse;
                }

                var entry = table.GetEntry(entryKey);
                if (entry != null)
                {
                    if (entry.Value != translation)
                    {
                        entry.Value = translation;
                        Debug.Log($"Updated entry {entryKey} with translation: {translation}");
                        importResponse.Updated++;
                    }
                }
                else
                {
                    table.AddEntry(entryKey, translation);
                    Debug.Log($"Added new entry {entryKey} with translation: {translation}");
                    importResponse.Added++;
                }

                UnityEditor.EditorUtility.SetDirty(table);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }

            return importResponse;
        }

        private static string ConvertLanguageCode(string langCode)
        {
            if (langCode.Length == 4)
            {
                return langCode.Substring(0, 2).ToLower() + "-" + langCode.Substring(2, 2).ToUpper();
            }
            return langCode;
        }

    }
}
