using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog
{
    /// <summary>
    /// A window for displaying import summary information in the Unity Editor.
    /// Shows details about imported records organized by file and language.
    /// </summary>
    public class ImportSummaryWindow : EditorWindow
    {
        #region Constants

        private const float MIN_WINDOW_WIDTH = 600f;
        private const float MIN_WINDOW_HEIGHT = 600f;
        private const float FILE_COLUMN_WIDTH = 200f;
        private const float LANGUAGE_COLUMN_WIDTH = 100f;
        private const float SPACING_SMALL = 10f;
        private const float SPACING_LARGE = 20f;
        private const float SCROLL_VIEW_HEIGHT_OFFSET = 50f;
        private const float SEPARATOR_HEIGHT = 1f;
        private const string WINDOW_TITLE = "Import Summary";
        private const string SUMMARY_LABEL = "Localization Import Summary";
        private const string FILE_HEADER = "File";
        private const string UPDATED_SUFFIX = " (Updated)";
        private const string ADDED_SUFFIX = " (Added)";
        private const string CLOSE_BUTTON_TEXT = "Close";
        private const string DEFAULT_COUNT = "0";

        #endregion

        #region Private Fields

        private Dictionary<string, Dictionary<string, ImportResponse>> _fileLanguageResponses;
        private List<string> _allLanguages;
        private Vector2 _scrollPosition;

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the import summary window with the specified response data.
        /// </summary>
        /// <param name="responses">Dictionary containing file names mapped to language responses.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when responses is null.</exception>
        public static void ShowWindow(Dictionary<string, Dictionary<string, ImportResponse>> responses)
        {
            ValidateResponses(responses);
            
            var window = CreateWindow();
            ConfigureWindow(window, responses);
            window.CollectAllLanguages();
            window.Show();
        }

        #endregion

        #region Unity Editor Methods

        /// <summary>
        /// Handles the GUI rendering for the import summary window.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();
            DrawImportTable();
            DrawCloseButton();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the responses data.
        /// </summary>
        /// <param name="responses">The responses to validate.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when responses is null.</exception>
        private static void ValidateResponses(Dictionary<string, Dictionary<string, ImportResponse>> responses)
        {
            if (responses == null)
            {
                throw new System.ArgumentNullException(nameof(responses), "Responses cannot be null.");
            }
        }

        /// <summary>
        /// Creates a new import summary window instance.
        /// </summary>
        /// <returns>The created window instance.</returns>
        private static ImportSummaryWindow CreateWindow()
        {
            return GetWindow<ImportSummaryWindow>(WINDOW_TITLE);
        }

        /// <summary>
        /// Configures the window with the provided data.
        /// </summary>
        /// <param name="window">The window to configure.</param>
        /// <param name="responses">The import response data.</param>
        private static void ConfigureWindow(ImportSummaryWindow window, Dictionary<string, Dictionary<string, ImportResponse>> responses)
        {
            window._fileLanguageResponses = responses;
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
        }

        /// <summary>
        /// Collects all unique languages from the response data.
        /// </summary>
        private void CollectAllLanguages()
        {
            _allLanguages = new List<string>();

            foreach (var fileEntry in _fileLanguageResponses)
            {
                foreach (var languageEntry in fileEntry.Value)
                {
                    if (!_allLanguages.Contains(languageEntry.Key))
                    {
                        _allLanguages.Add(languageEntry.Key);
                    }
                }
            }

            _allLanguages.Sort();
        }

        /// <summary>
        /// Draws the main header of the window.
        /// </summary>
        private void DrawHeader()
        {
            GUILayout.Label(SUMMARY_LABEL, EditorStyles.boldLabel);
            GUILayout.Space(SPACING_SMALL);
        }

        /// <summary>
        /// Draws the import table with file names and language-specific results.
        /// </summary>
        private void DrawImportTable()
        {
            var scrollViewHeight = position.height - SCROLL_VIEW_HEIGHT_OFFSET;
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(position.width), GUILayout.Height(scrollViewHeight));

            DrawTableHeaders();
            GUILayout.Space(SPACING_SMALL);
            DrawTableRows();

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws the table headers including file and language columns.
        /// </summary>
        private void DrawTableHeaders()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(FILE_HEADER, EditorStyles.label, GUILayout.Width(FILE_COLUMN_WIDTH));

            foreach (var language in _allLanguages)
            {
                GUILayout.Label(language + UPDATED_SUFFIX, EditorStyles.label, GUILayout.Width(LANGUAGE_COLUMN_WIDTH));
                GUILayout.Label(language + ADDED_SUFFIX, EditorStyles.label, GUILayout.Width(LANGUAGE_COLUMN_WIDTH));
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the table rows with import data for each file.
        /// </summary>
        private void DrawTableRows()
        {
            foreach (var fileEntry in _fileLanguageResponses)
            {
                DrawTableRow(fileEntry.Key, fileEntry.Value);
                DrawSeparator();
            }
        }

        /// <summary>
        /// Draws a single table row for a file.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="languageResponses">The language-specific responses for this file.</param>
        private void DrawTableRow(string fileName, Dictionary<string, ImportResponse> languageResponses)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fileName, GUILayout.Width(FILE_COLUMN_WIDTH));

            foreach (var language in _allLanguages)
            {
                DrawLanguageColumns(languageResponses, language);
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the updated and added columns for a specific language.
        /// </summary>
        /// <param name="languageResponses">The language responses for the current file.</param>
        /// <param name="language">The language to display data for.</param>
        private void DrawLanguageColumns(Dictionary<string, ImportResponse> languageResponses, string language)
        {
            if (languageResponses.ContainsKey(language))
            {
                var response = languageResponses[language];
                GUILayout.Label(response.Updated.ToString(), GUILayout.Width(LANGUAGE_COLUMN_WIDTH));
                GUILayout.Label(response.Added.ToString(), GUILayout.Width(LANGUAGE_COLUMN_WIDTH));
            }
            else
            {
                GUILayout.Label(DEFAULT_COUNT, GUILayout.Width(LANGUAGE_COLUMN_WIDTH));
                GUILayout.Label(DEFAULT_COUNT, GUILayout.Width(LANGUAGE_COLUMN_WIDTH));
            }
        }

        /// <summary>
        /// Draws a separator line between table rows.
        /// </summary>
        private void DrawSeparator()
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(SEPARATOR_HEIGHT));
        }

        /// <summary>
        /// Draws the close button.
        /// </summary>
        private void DrawCloseButton()
        {
            GUILayout.Space(SPACING_LARGE);
            if (GUILayout.Button(CLOSE_BUTTON_TEXT))
            {
                Close();
            }
        }

        #endregion
    }
}
