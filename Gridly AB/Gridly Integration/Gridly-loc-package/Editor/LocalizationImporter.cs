using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Enum;
using GridlyAB.GridlyIntegration.Gridly_loc_package.Editor.gridly.Api;
using GridlyAB.GridlyIntegration.Gridly_loc_package.Editor.gridly.Dialog;
using System.Linq;
using UnityEditor.Localization;
using Assets.Gridly_AB.Gridly_Integration.Gridly_loc_package.Editor.gridly.Model;

namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor
{
    /// <summary>
    /// Handles importing localization data from Gridly into Unity's Localization system.
    /// </summary>
    public class LocalizationImporter
    {
        // Constants
        private const string ProgressBarTitle = "Importing Localization Data";
        private const string LocaleNotFoundError = "Locale not found: {0}";
        private const string TableNotFoundError = "Table not found for collection: {0} and locale: {1}";
        private const string NoRecordsError = "No records provided for import.";
        private const string NoFileNamesError = "No file names provided for import.";
        private const string NoLanguageCodesError = "No language codes provided for import.";
        private const string LocaleIdentifierEmptyError = "Locale identifier is null or empty.";
        private const string LocaleNotFoundForIdentifierError = "Locale not found for identifier: {0}";

        // Fields
        private readonly ApiClient _apiClient;
        private readonly string _viewId;
        private readonly string _apiKey;
        private readonly SmartOption _selectedImportOption;
        private CancellationTokenSource _cancellationTokenSource;
        private List<Locale> _availableLocales;

        // Import tracking
        private readonly Dictionary<string, Dictionary<string, ImportResponse>> _fileLanguageResponses =
            new Dictionary<string, Dictionary<string, ImportResponse>>();
        private int _totalLanguagesProcessed = 0;
        private int _totalLanguagesToProcess = 0;

        /// <summary>
        /// Initializes a new instance of the LocalizationImporter class.
        /// </summary>
        /// <param name="apiClient">The API client for Gridly communication.</param>
        /// <param name="viewId">The Gridly view ID.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="selectedImportOption">The smart string filtering option.</param>
        /// <exception cref="ArgumentNullException">Thrown when apiClient, viewId, or apiKey is null.</exception>
        public LocalizationImporter(ApiClient apiClient, string viewId, string apiKey, SmartOption selectedImportOption)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _viewId = viewId ?? throw new ArgumentNullException(nameof(viewId));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _selectedImportOption = selectedImportOption;

            InitializeAvailableLocales();
        }

        /// <summary>
        /// Starts the import process for the specified files and language codes.
        /// </summary>
        /// <param name="fileNames">List of file names to import.</param>
        /// <param name="languageCodes">List of language codes to import.</param>
        /// <exception cref="ArgumentException">Thrown when fileNames or languageCodes is null or empty.</exception>
        public async void ImportData(List<string> fileNames, List<string> languageCodes)
        {
            ValidateImportParameters(fileNames, languageCodes);

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            _totalLanguagesToProcess = fileNames.Count * languageCodes.Count;
            _totalLanguagesProcessed = 0;

            ProgressBarUtility.InitializeProgressBar(ProgressBarTitle, _totalLanguagesToProcess);

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

        /// <summary>
        /// Updates the Unity localization system with the imported records.
        /// </summary>
        /// <param name="records">The records to import into Unity's localization system.</param>
        /// <returns>An ImportResponse containing the results of the import operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when records is null.</exception>
        public static ImportResponse UpdateLocalization(List<Record> records)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            var importResponse = new ImportResponse();

            if (records.Count == 0)
            {
                importResponse.Error = NoRecordsError;
                return importResponse;
            }

            var tablesToSave = new List<StringTable>();

            foreach (var record in records)
            {
                var recordImportResult = ProcessRecord(record);
                if (recordImportResult.HasError)
                {
                    importResponse.Error = recordImportResult.Error;
                    return importResponse;
                }

                importResponse.Added += recordImportResult.Added;
                importResponse.Updated += recordImportResult.Updated;

                if (recordImportResult.TableToSave != null)
                {
                    tablesToSave.Add(recordImportResult.TableToSave);
                }
            }

            SaveModifiedTables(tablesToSave);
            return importResponse;
        }

        /// <summary>
        /// Initializes the available locales from Unity's Localization system.
        /// </summary>
        private void InitializeAvailableLocales()
        {
            try
            {
                _availableLocales = LocalizationEditorSettings.GetLocales().ToList();

                if (_availableLocales == null || _availableLocales.Count == 0)
                {
                    Debug.LogWarning("No locales found in LocalizationEditorSettings. Trying LocalizationSettings.AvailableLocales.");
                    _availableLocales = LocalizationSettings.AvailableLocales.Locales.ToList();
                }

                Debug.Log($"Initialized {_availableLocales?.Count ?? 0} available locales for import.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing available locales: {ex.Message}");
                _availableLocales = new List<Locale>();
            }
        }

        /// <summary>
        /// Validates the import parameters.
        /// </summary>
        /// <param name="fileNames">List of file names to validate.</param>
        /// <param name="languageCodes">List of language codes to validate.</param>
        private static void ValidateImportParameters(List<string> fileNames, List<string> languageCodes)
        {
            if (fileNames == null || fileNames.Count == 0)
            {
                throw new ArgumentException(NoFileNamesError, nameof(fileNames));
            }

            if (languageCodes == null || languageCodes.Count == 0)
            {
                throw new ArgumentException(NoLanguageCodesError, nameof(languageCodes));
            }
        }

        /// <summary>
        /// Asynchronously imports localization data for the specified files and language codes.
        /// </summary>
        /// <param name="fileNames">List of file names to import.</param>
        /// <param name="languageCodes">List of language codes to import.</param>
        /// <param name="token">Cancellation token for the operation.</param>
        private async Task ImportLocalizationDataAsync(List<string> fileNames, List<string> languageCodes, CancellationToken token)
        {
            foreach (var fileName in fileNames)
            {
                InitializeFileResponse(fileName);

                foreach (var languageCode in languageCodes)
                {
                    if (token.IsCancellationRequested || ProgressBarUtility.IsCancelled())
                    {
                        throw new OperationCanceledException();
                    }

                    await ProcessFileLanguageAsync(fileName, languageCode);
                }
            }
        }

        /// <summary>
        /// Initializes the response dictionary for a file.
        /// </summary>
        /// <param name="fileName">The name of the file to initialize.</param>
        private void InitializeFileResponse(string fileName)
        {
            if (!_fileLanguageResponses.ContainsKey(fileName))
            {
                _fileLanguageResponses[fileName] = new Dictionary<string, ImportResponse>();
            }
        }

        /// <summary>
        /// Processes a single file-language combination.
        /// </summary>
        /// <param name="fileName">The name of the file to process.</param>
        /// <param name="languageCode">The language code to process.</param>
        private async Task ProcessFileLanguageAsync(string fileName, string languageCode)
        {
            try
            {
                var records = await _apiClient.GetRecordsAsync(_viewId, _apiKey, fileName, languageCode);
                ProcessRecordsFetched(records, fileName, languageCode);

                _totalLanguagesProcessed++;
                ProgressBarUtility.IncrementProgress($"Processing {fileName} ({languageCode})");

                if (_totalLanguagesProcessed >= _totalLanguagesToProcess)
                {
                    ImportSummaryWindow.ShowWindow(_fileLanguageResponses);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error fetching records for {fileName} ({languageCode}): {ex.Message}");
            }
        }

        /// <summary>
        /// Processes the records fetched from Gridly.
        /// </summary>
        /// <param name="records">The records fetched from Gridly.</param>
        /// <param name="fileName">The name of the file being processed.</param>
        /// <param name="languageCode">The language code being processed.</param>
        private void ProcessRecordsFetched(List<Record> records, string fileName, string languageCode)
        {
            var importResponse = UpdateLocalization(records);

            if (!_fileLanguageResponses[fileName].ContainsKey(languageCode))
            {
                _fileLanguageResponses[fileName][languageCode] = importResponse;
            }
            else
            {
                _fileLanguageResponses[fileName][languageCode].Added += importResponse.Added;
                _fileLanguageResponses[fileName][languageCode].Updated += importResponse.Updated;
            }
        }

        /// <summary>
        /// Processes a single record for import.
        /// </summary>
        /// <param name="record">The record to process.</param>
        /// <returns>A RecordImportResult containing the processing results.</returns>
        private static RecordImportResult ProcessRecord(Record record)
        {
            var result = new RecordImportResult();

            var localeIdentifier = ConvertLanguageCode(record.cells[0].columnId);
            var tableCollectionName = record.path;
            var entryKey = record.id;
            var translation = record.cells[0].GetValueAsString();

            var locale = GetLocale(localeIdentifier);
            if (locale == null)
            {
                result.Error = string.Format(LocaleNotFoundError, localeIdentifier);
                return result;
            }

            var table = GetStringTable(tableCollectionName, locale);
            if (table == null)
            {
                result.Error = string.Format(TableNotFoundError, tableCollectionName, localeIdentifier);
                return result;
            }

            ProcessTableEntry(table, entryKey, translation, result);
            result.TableToSave = table;

            return result;
        }

        /// <summary>
        /// Gets a StringTable for the specified collection and locale.
        /// </summary>
        /// <param name="tableCollectionName">The name of the table collection.</param>
        /// <param name="locale">The locale for the table.</param>
        /// <returns>The StringTable, or null if not found.</returns>
        private static StringTable GetStringTable(string tableCollectionName, Locale locale)
        {
            return LocalizationSettings.StringDatabase.GetTable(tableCollectionName, locale) as StringTable;
        }

        /// <summary>
        /// Processes a table entry, either updating existing or adding new.
        /// </summary>
        /// <param name="table">The StringTable to process.</param>
        /// <param name="entryKey">The key of the entry.</param>
        /// <param name="translation">The translation value.</param>
        /// <param name="result">The result object to update.</param>
        private static void ProcessTableEntry(StringTable table, string entryKey, string translation, RecordImportResult result)
        {
            var entry = table.GetEntry(entryKey);
            if (entry != null)
            {
                if (entry.Value != translation)
                {
                    entry.Value = translation;
                    Debug.Log($"Updated entry {entryKey} with translation: {translation}");
                    result.Updated++;
                }
            }
            else
            {
                table.AddEntry(entryKey, translation);
                Debug.Log($"Added new entry {entryKey} with translation: {translation}");
                result.Added++;
            }
        }

        /// <summary>
        /// Saves all modified tables to disk.
        /// </summary>
        /// <param name="tablesToSave">The list of tables to save.</param>
        private static void SaveModifiedTables(List<StringTable> tablesToSave)
        {
            foreach (var table in tablesToSave)
            {
                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Gets a locale by identifier using multiple fallback methods to ensure availability.
        /// </summary>
        /// <param name="localeIdentifier">The locale identifier to find.</param>
        /// <returns>The found locale, or null if not found.</returns>
        private static Locale GetLocale(string localeIdentifier)
        {
            if (string.IsNullOrEmpty(localeIdentifier))
            {
                Debug.LogError(LocaleIdentifierEmptyError);
                return null;
            }

            // Method 1: Try LocalizationSettings.AvailableLocales
            var locale = TryGetLocaleFromSettings(localeIdentifier);
            if (locale != null) return locale;

            // Method 2: Try LocalizationEditorSettings.GetLocales()
            locale = TryGetLocaleFromEditorSettings(localeIdentifier);
            if (locale != null) return locale;

            // Method 3: Try with normalized format
            locale = TryGetLocaleWithNormalizedIdentifier(localeIdentifier);
            if (locale != null) return locale;

            Debug.LogError(string.Format(LocaleNotFoundForIdentifierError, localeIdentifier));
            return null;
        }

        /// <summary>
        /// Tries to get a locale from LocalizationSettings.AvailableLocales.
        /// </summary>
        /// <param name="localeIdentifier">The locale identifier to find.</param>
        /// <returns>The found locale, or null if not found.</returns>
        private static Locale TryGetLocaleFromSettings(string localeIdentifier)
        {
            try
            {
                return LocalizationSettings.AvailableLocales.GetLocale(localeIdentifier);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error getting locale from LocalizationSettings.AvailableLocales: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tries to get a locale from LocalizationEditorSettings.GetLocales().
        /// </summary>
        /// <param name="localeIdentifier">The locale identifier to find.</param>
        /// <returns>The found locale, or null if not found.</returns>
        private static Locale TryGetLocaleFromEditorSettings(string localeIdentifier)
        {
            try
            {
                var editorLocales = LocalizationEditorSettings.GetLocales();
                return editorLocales.FirstOrDefault(l =>
                    l.Identifier.Code.Equals(localeIdentifier, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error getting locale from LocalizationEditorSettings.GetLocales(): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tries to get a locale using a normalized identifier format.
        /// </summary>
        /// <param name="localeIdentifier">The locale identifier to find.</param>
        /// <returns>The found locale, or null if not found.</returns>
        private static Locale TryGetLocaleWithNormalizedIdentifier(string localeIdentifier)
        {
            try
            {
                var normalizedIdentifier = NormalizeLocaleIdentifier(localeIdentifier);
                return LocalizationSettings.AvailableLocales.GetLocale(normalizedIdentifier);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error getting locale with normalized identifier: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Normalizes a locale identifier to handle different formats.
        /// </summary>
        /// <param name="localeIdentifier">The locale identifier to normalize.</param>
        /// <returns>The normalized locale identifier.</returns>
        private static string NormalizeLocaleIdentifier(string localeIdentifier)
        {
            if (string.IsNullOrEmpty(localeIdentifier))
                return localeIdentifier;

            // If it's in format "enUS", convert to "en-US"
            if (localeIdentifier.Length == 4 &&
                char.IsLetter(localeIdentifier[0]) && char.IsLetter(localeIdentifier[1]) &&
                char.IsLetter(localeIdentifier[2]) && char.IsLetter(localeIdentifier[3]))
            {
                return localeIdentifier.Substring(0, 2).ToLower() + "-" + localeIdentifier.Substring(2, 2).ToUpper();
            }

            return localeIdentifier;
        }

        /// <summary>
        /// Converts a language code from Gridly format to Unity format.
        /// </summary>
        /// <param name="langCode">The language code to convert.</param>
        /// <returns>The converted language code.</returns>
        private static string ConvertLanguageCode(string langCode)
        {
            if (string.IsNullOrEmpty(langCode))
                return langCode;

            if (langCode.Length == 4)
            {
                return langCode.Substring(0, 2).ToLower() + "-" + langCode.Substring(2, 2).ToUpper();
            }
            return langCode;
        }

        
    }
}