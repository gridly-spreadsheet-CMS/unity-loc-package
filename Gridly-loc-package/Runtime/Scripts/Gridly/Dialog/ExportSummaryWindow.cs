using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Dialog
{
    public class ExportSummaryWindow : EditorWindow
    {
        private Dictionary<string, int> exportedRecords;
        private int deletedRecordCount;
        private Vector2 scrollPosition;

        public static void ShowWindow(Dictionary<string, int> exportSummary, int deletedRecords)
        {
            ExportSummaryWindow window = GetWindow<ExportSummaryWindow>("Export Summary");
            window.exportedRecords = exportSummary;
            window.deletedRecordCount = deletedRecords;
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Localization Export Summary", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Show deleted record count
            GUILayout.Label($"Records Deleted from Gridly: {deletedRecordCount}", EditorStyles.label);
            GUILayout.Space(10);

            // Add scrollable area for export details
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height - 100));

            // Headers for the table
            GUILayout.BeginHorizontal();
            GUILayout.Label("File", EditorStyles.label, GUILayout.Width(500));
            GUILayout.Label("Exported Records", EditorStyles.label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Display each file's export record count
            foreach (var entry in exportedRecords)
            {
                string fileName = entry.Key;
                int recordCount = entry.Value;

                GUILayout.BeginHorizontal();
                GUILayout.Label(fileName, GUILayout.Width(500));
                GUILayout.Label(recordCount.ToString(), GUILayout.Width(100));
                GUILayout.EndHorizontal();

                // Add a separator line between each file row
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            }

            GUILayout.EndScrollView();  // End the scrollable area

            GUILayout.Space(20);
            if (GUILayout.Button("Close"))
            {
                this.Close();
            }
        }
    }
}
