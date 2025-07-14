using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog;
using UnityEngine;

namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor.gridly.Api
{
    /// <summary>
    /// Handles the resolution and creation of columns in Gridly views for localization data.
    /// </summary>
    public class ColumnResolver
    {
        // Column Constants
        private const string LanguageColumnType = "language";
        private const string BooleanColumnType = "boolean";
        private const string SmartStringColumnName = "SmartString";
        private const string SmartStringColumnId = "SmartString";
        private const string TargetLanguageLocalizationType = "targetLanguage";
        
        // Error Messages
        private const string ApiClientNotAvailableError = "ApiClient is not available.";
        private const string ErrorFetchingViewMessage = "Error while fetching the view: {0}";
        private const string ErrorFetchingViewDialogTitle = "Error";
        private const string ErrorFetchingViewDialogMessage = "Error while fetching the view:\n{0}";
        private const string MissingColumnCreatingMessage = "Missing column, creating: {0}";
        private const string SmartStringColumnCreatedMessage = "SmartString column created successfully.";
        private const string FailedToCreateSmartStringColumnMessage = "Failed to create SmartString column: {0}";
        private const string ColumnCreatedSuccessfullyMessage = "Column for locale '{0}' created successfully.";
        private const string FailedToCreateColumnMessage = "Failed to create column for locale '{0}': {1}";

        // Fields
        private readonly ApiClient _apiClient;
        private List<string> _localeIdentifiers;
        private string _apiKey;

        /// <summary>
        /// Initializes a new instance of the ColumnResolver class.
        /// </summary>
        /// <param name="client">The API client for Gridly communication.</param>
        /// <exception cref="ArgumentNullException">Thrown when client is null.</exception>
        public ColumnResolver(ApiClient client)
        {
            _apiClient = client ?? throw new ArgumentNullException(nameof(client), "API client cannot be null.");
        }

        /// <summary>
        /// Starts the column resolution process for the specified view and locale identifiers.
        /// </summary>
        /// <param name="viewId">The ID of the view to process.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="localeIdentifiers">The list of locale identifiers to process.</param>
        /// <exception cref="ArgumentNullException">Thrown when viewId, apiKey, or localeIdentifiers is null or empty.</exception>
        public async Task StartColumnResolverAsync(string viewId, string apiKey, List<string> localeIdentifiers)
        {
            ValidateColumnResolverParameters(viewId, apiKey, localeIdentifiers);
            ValidateApiClientAvailability();

            try
            {
                await ProcessColumnResolutionAsync(viewId, apiKey, localeIdentifiers);
            }
            catch (Exception ex)
            {
                HandleColumnResolutionError(ex);
            }
        }

        /// <summary>
        /// Validates the parameters for column resolution.
        /// </summary>
        /// <param name="viewId">The view ID to validate.</param>
        /// <param name="apiKey">The API key to validate.</param>
        /// <param name="localeIdentifiers">The locale identifiers to validate.</param>
        private static void ValidateColumnResolverParameters(string viewId, string apiKey, List<string> localeIdentifiers)
        {
            if (string.IsNullOrEmpty(viewId))
            {
                throw new ArgumentNullException(nameof(viewId), "View ID cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty.");
            }

            if (localeIdentifiers == null || localeIdentifiers.Count == 0)
            {
                throw new ArgumentNullException(nameof(localeIdentifiers), "Locale identifiers cannot be null or empty.");
            }
        }

        /// <summary>
        /// Validates that the API client is available.
        /// </summary>
        private void ValidateApiClientAvailability()
        {
            if (_apiClient == null)
            {
                Debug.LogError(ApiClientNotAvailableError);
                throw new InvalidOperationException(ApiClientNotAvailableError);
            }
        }

        /// <summary>
        /// Processes the column resolution workflow.
        /// </summary>
        /// <param name="viewId">The view ID to process.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="localeIdentifiers">The locale identifiers to process.</param>
        private async Task ProcessColumnResolutionAsync(string viewId, string apiKey, List<string> localeIdentifiers)
        {
            // Store the locale identifiers and API key
            _localeIdentifiers = new List<string>(localeIdentifiers);
            _apiKey = apiKey;

            // Fetch the view asynchronously
            var view = await _apiClient.GetViewAsync(viewId, apiKey);
            await ProcessViewAsync(view);
        }

        /// <summary>
        /// Handles errors that occur during column resolution.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        private void HandleColumnResolutionError(Exception ex)
        {
            Debug.LogError(string.Format(ErrorFetchingViewMessage, ex.Message));
            ErrorDialog.ShowDialog(ErrorFetchingViewDialogTitle, string.Format(ErrorFetchingViewDialogMessage, ex.Message));
        }

        /// <summary>
        /// Processes the view to determine which columns need to be created.
        /// </summary>
        /// <param name="view">The view to process.</param>
        private async Task ProcessViewAsync(View view)
        {
            if (view?.columns == null)
            {
                Debug.LogWarning("View or view columns are null. Cannot process columns.");
                return;
            }

            var smartStringColumnExists = CheckExistingColumns(view);
            
            await CreateMissingColumnsAsync(view.id, smartStringColumnExists);
        }

        /// <summary>
        /// Checks existing columns in the view and removes matching locale identifiers.
        /// </summary>
        /// <param name="view">The view to check.</param>
        /// <returns>True if SmartString column exists, false otherwise.</returns>
        private bool CheckExistingColumns(View view)
        {
            var smartStringColumnExists = false;

            foreach (var column in view.columns)
            {
                // Check for existing language columns
                CheckForExistingLanguageColumn(column);
                
                // Check for SmartString column
                if (IsSmartStringColumn(column))
                {
                    smartStringColumnExists = true;
                }
            }

            return smartStringColumnExists;
        }

        /// <summary>
        /// Checks if a column is an existing language column and removes the locale identifier.
        /// </summary>
        /// <param name="column">The column to check.</param>
        private void CheckForExistingLanguageColumn(Column column)
        {
            if (IsLanguageColumn(column))
            {
                RemoveMatchingLocaleIdentifier(column.languageCode);
            }
        }

        /// <summary>
        /// Removes a locale identifier if it matches the given language code.
        /// </summary>
        /// <param name="languageCode">The language code to match.</param>
        private void RemoveMatchingLocaleIdentifier(string languageCode)
        {
            if (_localeIdentifiers.Contains(languageCode))
            {
                _localeIdentifiers.Remove(languageCode);
            }
        }

        /// <summary>
        /// Determines if a column is a language column.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <returns>True if the column is a language column, false otherwise.</returns>
        private static bool IsLanguageColumn(Column column)
        {
            return column.type == LanguageColumnType && !string.IsNullOrEmpty(column.languageCode);
        }

        /// <summary>
        /// Determines if a column is the SmartString column.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <returns>True if the column is the SmartString column, false otherwise.</returns>
        private static bool IsSmartStringColumn(Column column)
        {
            return column.name == SmartStringColumnName;
        }

        /// <summary>
        /// Creates missing columns for the view.
        /// </summary>
        /// <param name="viewId">The view ID.</param>
        /// <param name="smartStringColumnExists">Whether the SmartString column already exists.</param>
        private async Task CreateMissingColumnsAsync(string viewId, bool smartStringColumnExists)
        {
            // Create SmartString column if it does not exist
            if (!smartStringColumnExists)
            {
                await CreateSmartStringColumnAsync(viewId);
            }

            // Create columns for the remaining locales
            await CreateLocaleColumnsAsync(viewId);
        }

        /// <summary>
        /// Creates columns for the remaining locale identifiers.
        /// </summary>
        /// <param name="viewId">The view ID.</param>
        private async Task CreateLocaleColumnsAsync(string viewId)
        {
            foreach (var locale in _localeIdentifiers)
            {
                Debug.Log(string.Format(MissingColumnCreatingMessage, locale));
                await CreateMissingColumnAsync(viewId, locale);
            }
        }

        /// <summary>
        /// Creates a SmartString column in the specified view.
        /// </summary>
        /// <param name="viewId">The view ID where the column will be created.</param>
        private async Task CreateSmartStringColumnAsync(string viewId)
        {
            var newColumn = CreateSmartStringColumnDefinition();

            try
            {
                await _apiClient.CreateColumnAsync(viewId, _apiKey, newColumn);
                Debug.Log(SmartStringColumnCreatedMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(FailedToCreateSmartStringColumnMessage, ex.Message));
            }
        }

        /// <summary>
        /// Creates a column definition for the SmartString column.
        /// </summary>
        /// <returns>A ColumnCreation object for the SmartString column.</returns>
        private static ColumnCreation CreateSmartStringColumnDefinition()
        {
            return new ColumnCreation
            {
                id = SmartStringColumnId,
                name = SmartStringColumnName,
                type = BooleanColumnType
            };
        }

        /// <summary>
        /// Creates a missing column for a specific locale.
        /// </summary>
        /// <param name="viewId">The view ID where the column will be created.</param>
        /// <param name="locale">The locale identifier for the column.</param>
        private async Task CreateMissingColumnAsync(string viewId, string locale)
        {
            var newColumn = CreateLocaleColumnDefinition(locale);

            try
            {
                await _apiClient.CreateColumnAsync(viewId, _apiKey, newColumn);
                Debug.Log(string.Format(ColumnCreatedSuccessfullyMessage, locale));
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format(FailedToCreateColumnMessage, locale, ex.Message));
            }
        }

        /// <summary>
        /// Creates a column definition for a locale column.
        /// </summary>
        /// <param name="locale">The locale identifier for the column.</param>
        /// <returns>A ColumnCreation object for the locale column.</returns>
        private static ColumnCreation CreateLocaleColumnDefinition(string locale)
        {
            return new ColumnCreation
            {
                id = locale,
                isTarget = true,
                name = locale,
                type = LanguageColumnType,
                languageCode = locale,
                localizationType = TargetLanguageLocalizationType
            };
        }
    }
}