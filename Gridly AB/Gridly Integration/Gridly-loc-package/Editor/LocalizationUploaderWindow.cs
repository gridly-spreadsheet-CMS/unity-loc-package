using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model;
using System.Linq;
using System.IO;
using UnityEditor.Localization;
using Assets.Gridly_loc_package.Editor.Scripts;
using System;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Text;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Enum;
using GridlyAB.GridlyIntegration.Editor;
using GridlyAB.GridlyIntegration.Gridly_loc_package.Editor.gridly.Api;
using GridlyAB.GridlyIntegration.Gridly_loc_package.Editor;
using GridlyAB.GridlyIntegration.Gridly_loc_package.Editor.gridly.Utils;


namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor
{

    public class LocalizationUploaderWindow : EditorWindow
    {
        private const string SelectedTablesKey = "Gridly_SelectedTables";
        private const string ExportPathKey = "Gridly_ExportPath";
        private const string ExportViewIdKey = "Gridly_ExportViewId";
        private const string ImportViewIdKey = "Gridly_ImportViewId";
        private const string ExportApiKeyKey = "Gridly_ExportApiKey";
        private const string ImportApiKeyKey = "Gridly_ImportApiKey";
        private const string DeleteExtraRecordsKey = "Gridly_DeleteExtraRecords";
        private const string UseDifferentImportViewKey = "Gridly_UseDifferentImportView";
        private const string ImportOptionKey = "Gridly_ImportOption";
        private const string ExportOptionKey = "Gridly_ExportOption";



        private static ApiClient apiClient;
        private static ColumnResolver columnResolver;

        private static string exportPath;
        private static string exportViewId;
        private static string importViewId;
        private static string exportApiKey;
        private static string importApiKey;
        private static bool deleteExtraRecordsKey;
        private static bool useDifferentImportView; // New flag for using different view for import
        private static SmartOption selectedImportOption;
        private static SmartOption selectedExportOption;

        private static ReorderableList tableList;
        private static List<TableSelection> selectedTables = new List<TableSelection>();
        private static List<Locale> availableLocales;

        private static List<string> availableFiles = new List<string>();
        private static List<string> availableLanguages = new List<string>();
        private static int selectedFileMask = 0; // Bitmask for selected files
        private static int selectedLanguageMask = 0; // Bitmask for selected languages

        private static List<CsvRecord> deletedRecords = new List<CsvRecord>();


        private static bool dataFetched = false; // Flag to track if data is fetched
        private Vector2 scrollPosition = Vector2.zero;


        // Static list to manage coroutines
        private static List<IEnumerator> coroutinesInProgress = new List<IEnumerator>();


        [MenuItem("Tools/Gridly Integration")]
        public static void ShowWindow()
        {
            GetWindow<LocalizationUploaderWindow>("Gridly Integration");
            OnShow();
        }


        private static void OnShow()
        {
            var windowInstance = GetWindow<LocalizationUploaderWindow>("Gridly Integration");
            // Check if UnityEngine.Localization is installed
            if (!IsLocalizationPackageInstalled())
            {
                ErrorDialog.ShowDialog("Package Missing", "The UnityEngine.Localization package is required to use this plugin. Please install it from the Package Manager.\nYou can find more information at: https://docs.unity3d.com/Packages/com.unity.localization@0.10/manual/Installation.html");
                return;
            }
            // Create instances of ApiClient and ColumnResolver
            apiClient = new();
            columnResolver = new(apiClient);

            // Fetch available locales
            availableLocales = LocalizationEditorSettings.GetLocales().ToList();

            // Load saved settings
            exportPath = EditorPrefs.GetString(ExportPathKey, "Assets/LocalizationExports/");
            exportViewId = EditorPrefs.GetString(ExportViewIdKey, "VIEWID"); // Default value
            importViewId = EditorPrefs.GetString(ImportViewIdKey, "importViewId"); // Default value
            exportApiKey = EditorPrefs.GetString(ExportApiKeyKey, "EXPORTAPIKEY");
            importApiKey = EditorPrefs.GetString(ImportApiKeyKey, "IMPORTAPIKEY");// Default value
            deleteExtraRecordsKey = EditorPrefs.GetBool(DeleteExtraRecordsKey, true);
            useDifferentImportView = EditorPrefs.GetBool(UseDifferentImportViewKey, false);
            selectedImportOption = (SmartOption)EditorPrefs.GetInt(ImportOptionKey, (int)SmartOption.AllStrings);
            selectedExportOption = (SmartOption)EditorPrefs.GetInt(ExportOptionKey, (int)SmartOption.AllStrings);


            // Load saved table selections
            LoadSelectedTables();

            // Initialize the reorderable list
            tableList = new ReorderableList(selectedTables, typeof(TableSelection), true, true, true, true);

            tableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Localization Tables");
            };

            tableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = selectedTables[index];

                float halfWidth = rect.width / 2;
                element.Table = (StringTableCollection)EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y, halfWidth, EditorGUIUtility.singleLineHeight),
                    element.Table,
                    typeof(StringTableCollection),
                    false
                );

                if (element.Table != null)
                {
                    // Get the locale names
                    string[] localeNames = element.GetLocaleNames(availableLocales);

                    // Ensure that localeNames is not null or empty
                    if (localeNames != null && localeNames.Length > 0)
                    {
                        // Ensure LocaleMask is within a valid range
                        int maxMaskValue = (1 << localeNames.Length) - 1;  // Maximum value with all bits set

                        if (element.LocaleMask > maxMaskValue || element.LocaleMask < 0)
                        {
                            element.LocaleMask = 0; // Reset to 0 if out of bounds
                        }

                        // Display the MaskField and update LocaleMask
                        element.LocaleMask = EditorGUI.MaskField(
                            new Rect(rect.x + halfWidth + 5, rect.y, halfWidth - 5, EditorGUIUtility.singleLineHeight),
                            "Select Locales",
                            element.LocaleMask,
                            localeNames
                        );

                        // Ensure that the LocaleMask does not exceed the maximum allowed mask value
                        element.LocaleMask &= maxMaskValue;
                    }
                    else
                    {
                        Debug.LogError("Locale names array is null or empty. MaskField cannot be displayed.");
                    }
                }
            };

            tableList.onAddCallback = (ReorderableList list) =>
            {
                selectedTables.Add(new TableSelection());
            };

            tableList.onRemoveCallback = (ReorderableList list) =>
            {
                selectedTables.RemoveAt(list.index);
            };

            if (useDifferentImportView)
            {
                if (!string.IsNullOrEmpty(importApiKey) && !string.IsNullOrEmpty(importViewId))
                {
                    windowInstance.GetViewData();
                }
                else
                {
                    Debug.LogWarning("Import API Key or View ID is missing. Please configure them in the settings.");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(exportApiKey) && !string.IsNullOrEmpty(exportViewId))
                {
                    windowInstance.GetViewData();
                }
                else
                {
                    Debug.LogWarning("Export API Key or View ID is missing. Please configure them in the settings.");
                }
            }

        }

        private void OnGUI()
        {
            GUILayout.Label("Connection settings", EditorStyles.boldLabel);

            // Folder browser for Export Path
            EditorGUIUtility.labelWidth = 220;
            GUILayout.BeginHorizontal();
            exportPath = EditorGUILayout.TextField("Export Path", exportPath);
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(75)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Export Folder", exportPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    exportPath = selectedPath;
                }
            }
            GUILayout.EndHorizontal();

            // Export View ID and API Key
            exportViewId = EditorGUILayout.TextField("Gridly Export View ID", exportViewId);
            exportApiKey = EditorGUILayout.PasswordField("Gridly Export API Key", exportApiKey);

            // Toggle for using a different import view and API key
            useDifferentImportView = EditorGUILayout.Toggle("Use Different View for Import", useDifferentImportView);

            // Show import-specific fields only if useDifferentImportView is true
            if (useDifferentImportView)
            {
                importViewId = EditorGUILayout.TextField("Gridly Import View ID", importViewId);
                importApiKey = EditorGUILayout.PasswordField("Gridly Import API Key", importApiKey);
            }
            GUILayout.Label("Import Options", EditorStyles.boldLabel);

            // Radio buttons for import options
            selectedImportOption = (SmartOption)EditorGUILayout.EnumPopup("Select Import Type", selectedImportOption);
            selectedExportOption = (SmartOption)EditorGUILayout.EnumPopup("Select Export Type", selectedExportOption);


            deleteExtraRecordsKey = EditorGUILayout.Toggle("Delete removed records from Gridly", deleteExtraRecordsKey);
            EditorGUIUtility.labelWidth = 0;

            GUILayout.Space(10);
            GUILayout.Label("Export into Gridly", EditorStyles.boldLabel);

            // Draw the reorderable list in a scroll view
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            tableList.DoLayoutList();
            EditorGUILayout.EndScrollView();


            if (GUILayout.Button("Export CSV and upload to Gridly"))
            {
                // Check if deleteExtraRecordsKey is enabled
                if (deleteExtraRecordsKey)
                {
                    // Show confirmation dialog
                    bool proceedWithExport = EditorUtility.DisplayDialog(
                        "Confirm Record Deletion",
                        "Records found in Gridly but not in Unity will be deleted when a sync is performed. If you create Unity records in Gridly these will be deleted unless imported into Unity first.\nDo you want to continue?",
                        "Yes",
                        "Cancel"
                    );

                    // If the user clicked "Yes," proceed with the export and upload
                    if (proceedWithExport)
                    {
                        ExportAndUploadCsv();
                    }
                }
                else
                {
                    // If deleteExtraRecordsKey is not enabled, proceed with export directly
                    ExportAndUploadCsv();
                }
            }

            GUILayout.Space(20);
            GUILayout.Label("Import from Gridly", EditorStyles.boldLabel);

            // Button to fetch and populate both files and languages
            if (GUILayout.Button("Get view data from Gridly"))
            {
                GetViewData();
            }



            // Render dropdowns only if data is fetched
            if (dataFetched)
            {
                if (availableFiles.Count > 0)
                {
                    selectedFileMask = ValidateAndResetMask(selectedFileMask, availableFiles);
                    selectedFileMask = EditorGUILayout.MaskField("Select Files", selectedFileMask, availableFiles.ToArray());

                    GUILayout.Space(10);
                }
                else
                {
                    GUILayout.Label("No files available.", EditorStyles.helpBox);
                }

                if (availableLanguages.Count > 0)
                {
                    selectedLanguageMask = ValidateAndResetMask(selectedLanguageMask, availableLanguages);
                    selectedLanguageMask = EditorGUILayout.MaskField("Select Languages", selectedLanguageMask, availableLanguages.ToArray());

                    GUILayout.Space(10);
                }
                else
                {
                    GUILayout.Label("No languages available.", EditorStyles.helpBox);
                }

                if (availableFiles.Count > 0 && availableLanguages.Count > 0)
                {
                    if (GUILayout.Button("Import Selected Data"))
                    {
                        OnImportButtonClicked();
                    }
                }
            }
            else
            {
                GUILayout.Label("Data is not loaded. Please click 'Get View Data' to load.", EditorStyles.helpBox);
            }
        }



        private async void GetViewData()
        {
            string viewIdToUse = useDifferentImportView ? importViewId : exportViewId;
            string apiKeyToUse = useDifferentImportView ? importApiKey : exportApiKey;

            // Fetch the files from the view using GetPathsTree
            try
            {
                var paths = await apiClient.GetPathsTreeAsync(viewIdToUse, apiKeyToUse);
                OnPathsFetched(paths);
            }
            catch (Exception ex)
            {
                OnErrorFetchingPaths(ex.Message);
            }
        }

        private void OnPathsFetched(List<string> paths)
        {
            availableFiles = paths;
            selectedFileMask = 0; // Reset the file mask

            // Fetch languages from the view
            FetchLanguagesFromView();
        }

        private async void FetchLanguagesFromView()
        {
            string viewIdToUse = useDifferentImportView ? importViewId : exportViewId;
            string apiKeyToUse = useDifferentImportView ? importApiKey : exportApiKey;

            try
            {
                var view = await apiClient.GetViewAsync(viewIdToUse, apiKeyToUse);
                OnViewFetched(view);
            }
            catch (Exception ex)
            {
                OnErrorFetchingView(ex.Message);
            }
        }

        private void OnViewFetched(View view)
        {
            availableLanguages.Clear();
            foreach (var column in view.columns)
            {
                if (column.type == "language")
                {
                    availableLanguages.Add(column.name); // Add language column name or ID as needed
                }
            }
            selectedLanguageMask = 0; // Reset the language mask
            dataFetched = true; // Mark data as fetched
            Repaint(); // Refresh the GUI to show the updated dropdowns

            // Log fetched records to the console
            /*
            Debug.Log("Fetched Records from View:");
            foreach (var column in view.columns)
            {
                Debug.Log($"Column Name: {column.name}, Type: {column.type}");
            }
            */
        }


        private void OnErrorFetchingPaths(string errorMessage)
        {
            ErrorDialog.ShowDialog("Error", $"Failed to fetch View information from Gridly: {errorMessage}");
            Debug.LogError($"Failed to fetch paths: {errorMessage}");
        }

        private void OnErrorFetchingView(string errorMessage)
        {
            Debug.LogError($"Failed to fetch view: {errorMessage}");
        }

        private int ValidateAndResetMask(int mask, List<string> options)
        {
            // Ensure the mask does not reference indices outside the bounds of the options array
            if (options.Count == 0)
            {
                return 0; // Reset mask if no options are available
            }

            int validMask = 0;
            for (int i = 0; i < options.Count; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    validMask |= (1 << i);
                }
            }
            return validMask;
        }

        private void OnImportButtonClicked()
        {
            List<string> selectedFileNames = GetSelectedOptions(availableFiles, selectedFileMask);
            List<string> selectedLanguageNames = GetSelectedOptions(availableLanguages, selectedLanguageMask);

            if (selectedFileNames.Count == 0 || selectedLanguageNames.Count == 0)
            {
                ErrorDialog.ShowDialog("Error", "You must select at least one file and one language");
                return;
            }

            if (useDifferentImportView)
            {
                // Create an instance of LocalizationImporter and start importing data
                LocalizationImporter importer = new LocalizationImporter(apiClient, importViewId, importApiKey, selectedImportOption);
                importer.ImportData(selectedFileNames, selectedLanguageNames);
            }
            else
            {
                // Create an instance of LocalizationImporter and start importing data
                LocalizationImporter importer = new LocalizationImporter(apiClient, exportViewId, exportApiKey, selectedImportOption);
                importer.ImportData(selectedFileNames, selectedLanguageNames);
            }

            SaveSelectedTables();  // Save the selected tables and locales when data is imported
        }

        private List<string> GetSelectedOptions(List<string> options, int mask)
        {
            List<string> selectedOptions = new List<string>();

            for (int i = 0; i < options.Count; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    selectedOptions.Add(options[i]);
                }
            }

            return selectedOptions;
        }

        private async void ExportAndUploadCsv()
        {
            await ExportAndUploadCsvAsync();
        }

        private async Task ExportAndUploadCsvAsync()
        {

            await FileUtils.DeleteAllFilesAsync(exportPath);
            List<string> localeIdentifiers = selectedTables
                .Where(tableSelection => tableSelection.Table != null)
                .SelectMany(tableSelection => tableSelection.Table.StringTables
                    .Where(table => tableSelection.IsLocaleSelected(table.LocaleIdentifier, availableLocales))
                    .Select(table => table.LocaleIdentifier.Code.Replace("-", "")))
                .Distinct()
                .ToList();

            List<string> csvFiles = new List<string>();

            // Call the ColumnResolver to process the uploaded data
            if (columnResolver != null)
            {
                await columnResolver.StartColumnResolverAsync(exportViewId, exportApiKey, localeIdentifiers);
            }
            else
            {
                Debug.LogError("ColumnResolver not found or not properly initialized.");
                return;
            }

            // Export the localization data for each selected table and selected locales
            foreach (var tableSelection in selectedTables)
            {
                if (tableSelection.Table != null)
                {
                    foreach (var table in tableSelection.Table.StringTables)
                    {
                        if (tableSelection.IsLocaleSelected(table.LocaleIdentifier, availableLocales))
                        {
                            string path = exportPath + ($"/_{tableSelection.Table.TableCollectionName}_{table.LocaleIdentifier}.csv");
                            var entries = table.Values.Where(entry => entry.Key != null);

                            // Await the export operation asynchronously
                            int numberOfRecords = await LocalizationExporter.ExportLocalizationCSVAsync(path, tableSelection.Table, table.LocaleIdentifier, selectedExportOption);

                            if (numberOfRecords > 0)
                            {
                                csvFiles.Add(path);
                            }
                        }
                    }
                }
            }
            // merge only if there is asingle CSV
            if (csvFiles.Count > 0)
            {
                // Once all exports are done, merge the CSV files asynchronously
                string outputPath = exportPath + "/_allRecords.csv";
                await CsvMerger.MergeCsvFilesAsync(csvFiles, outputPath);

                // Upload the merged CSV
                if (apiClient != null)
                {
                    try
                    {
                        string uploadResult = await apiClient.UploadCsvFileAsync(exportViewId, exportApiKey, outputPath);
                        OnUploadSuccess(uploadResult);
                    }
                    catch (Exception ex)
                    {
                        OnUploadError(ex.Message);
                    }
                }
                else
                {
                    Debug.LogError("ApiClient not found or not properly initialized.");
                    return;
                }

                await ExportViewAsCsv();
            }

            Dictionary<string, int> exportSummary = new Dictionary<string, int>();
            foreach (var file in csvFiles)
            {
                int recordCount = LoadCsvRecords(file, "_recordId", "_pathTag").Count;  // Count records in each file
                exportSummary[Path.GetFileName(file)] = recordCount;
            }

            // Call the ExportSummaryWindow to show the summary
            ExportSummaryWindow.ShowWindow(exportSummary, deletedRecords.Count);

            SaveSelectedTables();  // Save the selected tables and locales after exporting data
        }

        private async Task ExportViewAsCsv()
        {
            await ExportViewAsCsvAsync();
        }

        private async Task ExportViewAsCsvAsync()
        {
            string url = $"https://api.gridly.com/v1/views/{exportViewId}/export";
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.SetRequestHeader("Authorization", "ApiKey " + exportApiKey);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 300;

            try
            {
                // Await the custom awaiter for the UnityWebRequest
                await SendWebRequestAsync(request);

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Failed to export view as CSV: {request.error}");
                    Debug.LogError($"Response Code: {request.responseCode}");
                    return;
                }

                // Process and save the CSV content
                string csvContent = request.downloadHandler.text;
                Debug.Log($"CSV Content Length: {csvContent.Length}");
                SaveCsvToFile(csvContent);
                if (deleteExtraRecordsKey)
                {
                    // After saving the CSV, sync the records with Gridly
                    await SyncRecordsWithGridlyAsync();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception occurred: {ex.Message}");
            }
        }

        // Helper method to await a UnityWebRequest
        private async Task SendWebRequestAsync(UnityWebRequest request)
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();  // Wait until the request is done
            }
        }

        private void SaveCsvToFile(string csvContent)
        {
            string path = $"{exportPath}/GridlyViewExport.csv";

            try
            {
                File.WriteAllText(path, csvContent);
                Debug.Log($"CSV exported and saved at: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save CSV file: {e.Message}");
            }
        }

        private async Task SyncRecordsWithGridlyAsync()
        {
            // Initialize deletedRecords as a list of CsvRecord objects
            deletedRecords = new List<CsvRecord>();

            // Paths for Unity and Gridly CSVs
            string unityCsvPath = Path.Combine(exportPath, "_allRecords.csv");
            string gridlyCsvPath = Path.Combine(exportPath, "GridlyViewExport.csv");

            // Load and compare records from both CSVs
            var unityRecords = LoadCsvRecords(unityCsvPath, "_recordId", "_pathTag");  // Unity column names
            var gridlyRecords = LoadCsvRecords(gridlyCsvPath, "Record ID", "Path");  // Gridly column names

            // Normalize records by combining Path and RecordId, and trim and lowercase for comparison
            var unityRecordKeys = new HashSet<string>(
                unityRecords.Select(ur => $"{ur.Path.Trim().ToLower()}_{ur.RecordId.Trim().ToLower()}")
            );

            // Find records in Gridly that are not present in Unity by both Path and RecordId
            var recordsToDelete = gridlyRecords
                .Where(gr => !unityRecordKeys.Contains($"{gr.Path.Trim().ToLower()}_{gr.RecordId.Trim().ToLower()}"))
                .ToList();

            // Populate deletedRecords with the records to delete (including both ID and Path)
            foreach (var record in recordsToDelete)
            {
                deletedRecords.Add(new CsvRecord { RecordId = record.RecordId, Path = record.Path });
            }

            // Initiate deletion in Gridly if there are extra records
            if (deletedRecords.Any())
            {
                Debug.Log($"Found {deletedRecords.Count} extra record(s) in Gridly to delete.");
                await DeleteRecordsFromGridlyAsync(deletedRecords.Select(r => r.RecordId).ToList());
            }
            else
            {
                Debug.Log("No extra records found in Gridly to delete.");
            }
        }





        private async Task DeleteRecordsFromGridlyAsync(List<string> recordIds, int maxRetries = 5, int retryDelayMilliseconds = 5000)
        {
            string url = $"https://api.gridly.com/v1/views/{exportViewId}/records";

            // Construct JSON payload for UnityWebRequest
            string jsonData = "{\"ids\":[" + string.Join(",", recordIds.Select(id => $"\"{id}\"")) + "]}";
            Debug.Log($"Generated JSON for deletion: {jsonData}");

            int attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;

                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbDELETE))
                {
                    request.SetRequestHeader("Authorization", "ApiKey " + exportApiKey);
                    request.SetRequestHeader("Content-Type", "application/json");

                    // Attach the JSON payload to the request
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();

                    Debug.Log($"Request payload length: {bodyRaw.Length} bytes");

                    // Await the request completion
                    await SendWebRequestAsync(request);

                    // Check for errors
                    if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        Debug.LogError($"Failed to delete records: {request.error} (Code: {request.responseCode})");
                        Debug.LogError($"Response Text: {request.downloadHandler.text}");

                        // Check if the response indicates a backup state (409 Conflict with specific message)
                        if (request.responseCode == 409 && request.downloadHandler.text.Contains("\"gridBackuping\""))
                        {
                            Debug.LogWarning($"Gridly is in a backup state. Retrying in {retryDelayMilliseconds} ms... (Attempt {attempt}/{maxRetries})");
                            await Task.Delay(retryDelayMilliseconds);
                            continue; // Retry
                        }

                        // For other errors, break and fail
                        Debug.LogError("Aborting deletion due to an error unrelated to backup state.");
                        return;
                    }
                    else
                    {
                        Debug.Log("Records successfully deleted from Gridly.");
                        return; // Exit on success
                    }
                }
            }

            Debug.LogError($"Failed to delete records after {maxRetries} attempts. Gridly might still be in a backup state.");
        }


        private List<CsvRecord> LoadCsvRecords(string path, string recordIdHeader, string pathHeader)
        {
            var records = new List<CsvRecord>();

            if (!File.Exists(path))
            {
                Debug.LogError($"CSV file not found at path: {path}");
                return records;
            }

            // Read the entire file content as a single string
            string fileContent = File.ReadAllText(path);

            // Parse CSV content
            var rows = ParseCsv(fileContent);
            if (rows.Count == 0) return records;

            // Parse header to determine column indexes
            var headers = rows[0];
            int recordIdIndex = Array.IndexOf(headers, recordIdHeader);
            int pathIndex = Array.IndexOf(headers, pathHeader);

            if (recordIdIndex == -1 || pathIndex == -1)
            {
                Debug.LogError("Could not find specified headers in CSV file.");
                return records;
            }

            // Parse each row using identified column indexes
            foreach (var row in rows.Skip(1))
            {
                if (row.Length <= Math.Max(recordIdIndex, pathIndex)) continue;

                records.Add(new CsvRecord
                {
                    RecordId = row[recordIdIndex].Trim().Trim('"'),
                    Path = row[pathIndex].Trim().Trim('"')
                });
            }

            return records;
        }

        // Helper method to parse CSV content with support for quoted fields and inline newlines
        private List<string[]> ParseCsv(string content)
        {
            var rows = new List<string[]>();
            var currentRow = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in content)
            {
                if (c == '\"')
                {
                    if (inQuotes && currentField.Length > 0 && currentField[currentField.Length - 1] == '\"')
                    {
                        // Handle escaped double quotes
                        currentField.Append('\"');
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // End of a field
                    currentRow.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if (c == '\n' && !inQuotes)
                {
                    // End of a row
                    currentRow.Add(currentField.ToString());
                    rows.Add(currentRow.ToArray());
                    currentRow.Clear();
                    currentField.Clear();
                }
                else
                {
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


        private class CsvRecord
        {
            public string RecordId { get; set; }
            public string Path { get; set; }
        }

        private void SaveSelectedTables()
        {
            string serializedTables = string.Join("|", selectedTables.Select(t =>
                $"{t.Table?.TableCollectionName},{t.LocaleMask}"));
            EditorPrefs.SetString(SelectedTablesKey, serializedTables);
        }

        private static void LoadSelectedTables()
        {
            selectedTables.Clear();
            string serializedTables = EditorPrefs.GetString(SelectedTablesKey, "");

            if (!string.IsNullOrEmpty(serializedTables))
            {
                foreach (string tableData in serializedTables.Split('|'))
                {
                    var parts = tableData.Split(',');
                    if (parts.Length == 2)
                    {
                        var tableCollectionName = parts[0];
                        int localeMask = int.Parse(parts[1]);

                        // Get the StringTableCollection using LocalizationSettings.AssetDatabase
                        var tableCollection = LocalizationEditorSettings.GetStringTableCollection(tableCollectionName);
                        if (tableCollection != null)
                        {
                            selectedTables.Add(new TableSelection
                            {
                                Table = tableCollection,
                                LocaleMask = localeMask
                            });
                        }
                        else
                        {
                            Debug.LogWarning($"Table collection {tableCollectionName} not found.");
                        }
                    }
                }
            }
        }

        // Handle upload success
        private void OnUploadSuccess(string responseContent)
        {
            Debug.Log("File uploaded successfully: " + responseContent);
        }

        // Handle upload error
        private void OnUploadError(string errorMessage)
        {
            Debug.LogError("Failed to upload file: " + errorMessage);
        }

        private void OnDisable()
        {
            // Save preferences when the window is closed or disabled
            EditorPrefs.SetString(ExportPathKey, exportPath);
            EditorPrefs.SetString(ExportViewIdKey, exportViewId);
            EditorPrefs.SetString(ExportApiKeyKey, exportApiKey);
            EditorPrefs.SetString(ImportViewIdKey, importViewId);
            EditorPrefs.SetString(ImportApiKeyKey, importApiKey);
            EditorPrefs.SetBool(DeleteExtraRecordsKey, deleteExtraRecordsKey);
            EditorPrefs.SetBool(UseDifferentImportViewKey, useDifferentImportView);
            EditorPrefs.SetInt(ImportOptionKey, (int)selectedImportOption);
            EditorPrefs.SetInt(ExportOptionKey, (int)selectedExportOption);
        }

        private class TableSelection
        {
            public StringTableCollection Table;
            public int LocaleMask;

            public string[] GetLocaleNames(List<Locale> availableLocales)
            {
                var localeNames = new string[availableLocales.Count];
                for (int i = 0; i < availableLocales.Count; i++)
                {
                    localeNames[i] = availableLocales[i].Identifier.Code;
                }
                return localeNames;
            }

            public bool IsLocaleSelected(LocaleIdentifier localeIdentifier, List<Locale> availableLocales)
            {
                for (int i = 0; i < availableLocales.Count; i++)
                {
                    if (availableLocales[i].Identifier == localeIdentifier && (LocaleMask & (1 << i)) != 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static bool IsLocalizationPackageInstalled()
        {
            // Try to get the LocalizationSettings type from UnityEngine.Localization
            var type = Type.GetType("UnityEngine.Localization.Settings.LocalizationSettings, Unity.Localization");

            // If the type is null, the package is not installed
            return type != null;
        }
    }
}
