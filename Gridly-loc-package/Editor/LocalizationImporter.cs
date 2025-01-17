using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Enum;

namespace Assets.Gridly_loc_package.Editor.Scripts
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

        public LocalizationImporter(ApiClient apiClient, string viewId, string apiKey, SmartOption selectedImportOption)
        {
            this.apiClient = apiClient;
            this.viewId = viewId;
            this.apiKey = apiKey;
        }

        public async void ImportData(List<string> fileNames, List<string> languageCodes)
        {
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            totalLanguagesToProcess = fileNames.Count * languageCodes.Count;
            totalLanguagesProcessed = 0;

            // Initialize progress bar
            ProgressBarUtility.InitializeProgressBar("Importing Localization Data", totalLanguagesToProcess);

            try
            {
                await ImportLocalizationDataAsync(fileNames, languageCodes, token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Import process canceled by user.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during import: {ex.Message}");
            }
            finally
            {
                ProgressBarUtility.ClearProgressBar();
            }
        }

        private async Task ImportLocalizationDataAsync(List<string> fileNames, List<string> languageCodes, CancellationToken token)
        {
            foreach (string fileName in fileNames)
            {
                if (!fileLanguageResponses.ContainsKey(fileName))
                {
                    fileLanguageResponses[fileName] = new Dictionary<string, ImportResponse>();
                }

                foreach (string languageCode in languageCodes)
                {
                    if (token.IsCancellationRequested || ProgressBarUtility.IsCancelled())
                    {
                        throw new OperationCanceledException();
                    }

                    try
                    {
                        // Fetch records using `ApiClient.GetRecordsAsync()`
                        List<Record> records = await apiClient.GetRecordsAsync(viewId, apiKey, fileName, languageCode);
                        OnRecordsFetched(records, fileName, languageCode);

                        totalLanguagesProcessed++;
                        ProgressBarUtility.IncrementProgress($"Processing {fileName} ({languageCode})");

                        if (totalLanguagesProcessed >= totalLanguagesToProcess)
                        {
                            ImportSummaryWindow.ShowWindow(fileLanguageResponses);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error fetching records for {fileName} ({languageCode}): {ex.Message}");
                    }
                }
            }
        }

        private void UpdateImportProgress(CancellationToken token)
        {
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

        public static ImportResponse UpdateLocalization(List<Record> records)
        {
            ImportResponse importResponse = new ImportResponse();

            List<StringTable> tablesToSave = new List<StringTable>();

            foreach (Record record in records)
            {
                string localeIdentifier = ConvertLanguageCode(record.cells[0].columnId);
                string tableCollectionName = record.path;
                string entryKey = record.id;
                string translation = record.cells[0].GetValueAsString();

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
                tablesToSave.Add(table);

            }
            foreach (StringTable table in tablesToSave)
            {
                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
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