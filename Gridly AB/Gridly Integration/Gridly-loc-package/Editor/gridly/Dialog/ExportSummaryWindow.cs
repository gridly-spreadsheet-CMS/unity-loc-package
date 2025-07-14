using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog
{
    /// <summary>
    /// A window for displaying export summary information in the Unity Editor.
    /// Shows details about exported records and deleted records from Gridly.
    /// </summary>
    public class ExportSummaryWindow : EditorWindow
    {
        #region Constants

        private const float MIN_WINDOW_WIDTH = 400f;
        private const float MIN_WINDOW_HEIGHT = 300f;
        private const float FILE_COLUMN_WIDTH = 500f;
        private const float RECORD_COUNT_COLUMN_WIDTH = 100f;
        private const float SPACING_SMALL = 10f;
        private const float SPACING_LARGE = 20f;
        private const float SCROLL_VIEW_HEIGHT_OFFSET = 100f;
        private const float SEPARATOR_HEIGHT = 1f;
        private const string WINDOW_TITLE = "Export Summary";
        private const string SUMMARY_LABEL = "Localization Export Summary";
        private const string DELETED_RECORDS_LABEL = "Records Deleted from Gridly: ";
        private const string FILE_HEADER = "File";
        private const string EXPORTED_RECORDS_HEADER = "Exported Records";
        private const string CLOSE_BUTTON_TEXT = "Close";

        #endregion

        #region Private Fields

        private Dictionary<string, int> _exportedRecords;
        private int _deletedRecordCount;
        private Vector2 _scrollPosition;

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the export summary window with the specified data.
        /// </summary>
        /// <param name="exportSummary">Dictionary containing file names and their exported record counts.</param>
        /// <param name="deletedRecords">The number of records deleted from Gridly.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when exportSummary is null.</exception>
        public static void ShowWindow(Dictionary<string, int> exportSummary, int deletedRecords)
        {
            ValidateExportSummary(exportSummary);
            
            var window = CreateWindow();
            ConfigureWindow(window, exportSummary, deletedRecords);
            window.Show();
        }

        #endregion

        #region Unity Editor Methods

        /// <summary>
        /// Handles the GUI rendering for the export summary window.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();
            DrawDeletedRecordsInfo();
            DrawExportTable();
            DrawCloseButton();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the export summary data.
        /// </summary>
        /// <param name="exportSummary">The export summary to validate.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when exportSummary is null.</exception>
        private static void ValidateExportSummary(Dictionary<string, int> exportSummary)
        {
            if (exportSummary == null)
            {
                throw new System.ArgumentNullException(nameof(exportSummary), "Export summary cannot be null.");
            }
        }

        /// <summary>
        /// Creates a new export summary window instance.
        /// </summary>
        /// <returns>The created window instance.</returns>
        private static ExportSummaryWindow CreateWindow()
        {
            return GetWindow<ExportSummaryWindow>(WINDOW_TITLE);
        }

        /// <summary>
        /// Configures the window with the provided data.
        /// </summary>
        /// <param name="window">The window to configure.</param>
        /// <param name="exportSummary">The export summary data.</param>
        /// <param name="deletedRecords">The number of deleted records.</param>
        private static void ConfigureWindow(ExportSummaryWindow window, Dictionary<string, int> exportSummary, int deletedRecords)
        {
            window._exportedRecords = exportSummary;
            window._deletedRecordCount = deletedRecords;
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
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
        /// Draws the deleted records information.
        /// </summary>
        private void DrawDeletedRecordsInfo()
        {
            GUILayout.Label($"{DELETED_RECORDS_LABEL}{_deletedRecordCount}", EditorStyles.label);
            GUILayout.Space(SPACING_SMALL);
        }

        /// <summary>
        /// Draws the export table with file names and record counts.
        /// </summary>
        private void DrawExportTable()
        {
            var scrollViewHeight = position.height - SCROLL_VIEW_HEIGHT_OFFSET;
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(position.width), GUILayout.Height(scrollViewHeight));

            DrawTableHeaders();
            GUILayout.Space(SPACING_SMALL);
            DrawTableRows();

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws the table headers.
        /// </summary>
        private void DrawTableHeaders()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(FILE_HEADER, EditorStyles.label, GUILayout.Width(FILE_COLUMN_WIDTH));
            GUILayout.Label(EXPORTED_RECORDS_HEADER, EditorStyles.label, GUILayout.Width(RECORD_COUNT_COLUMN_WIDTH));
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the table rows with export data.
        /// </summary>
        private void DrawTableRows()
        {
            foreach (var entry in _exportedRecords)
            {
                DrawTableRow(entry.Key, entry.Value);
                DrawSeparator();
            }
        }

        /// <summary>
        /// Draws a single table row.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="recordCount">The number of exported records.</param>
        private void DrawTableRow(string fileName, int recordCount)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fileName, GUILayout.Width(FILE_COLUMN_WIDTH));
            GUILayout.Label(recordCount.ToString(), GUILayout.Width(RECORD_COUNT_COLUMN_WIDTH));
            GUILayout.EndHorizontal();
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
