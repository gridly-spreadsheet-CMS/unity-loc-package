using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Model;

namespace Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Dialog
{
    public class ImportSummaryWindow : EditorWindow
    {
        private Dictionary<string, Dictionary<string, ImportResponse>> fileLanguageResponses;
        private List<string> allLanguages;  // To store all the unique languages
        private Vector2 scrollPosition;  // To track the scroll position

        public static void ShowWindow(Dictionary<string, Dictionary<string, ImportResponse>> responses)
        {
            ImportSummaryWindow window = GetWindow<ImportSummaryWindow>("Import Summary");
            window.fileLanguageResponses = responses;
            window.minSize = new Vector2(600, 600);
            window.CollectAllLanguages();
            window.Show();
        }

        private void CollectAllLanguages()
        {
            allLanguages = new List<string>();

            foreach (var fileEntry in fileLanguageResponses)
            {
                foreach (var languageEntry in fileEntry.Value)
                {
                    if (!allLanguages.Contains(languageEntry.Key))
                    {
                        allLanguages.Add(languageEntry.Key);
                    }
                }
            }

            // Sort languages to ensure consistent column order
            allLanguages.Sort();
        }

        void OnGUI()
        {
            GUILayout.Label("Localization Import Summary", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Add scrollable area
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height - 50));

            // Headers for the table
            GUILayout.BeginHorizontal();
            GUILayout.Label("File", EditorStyles.label, GUILayout.Width(200));

            // Add a label for each language
            foreach (var language in allLanguages)
            {
                GUILayout.Label(language + " (Updated)", EditorStyles.label, GUILayout.Width(100));
                GUILayout.Label(language + " (Added)", EditorStyles.label, GUILayout.Width(100));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Display each file's results in one line
            foreach (var fileEntry in fileLanguageResponses)
            {
                string fileName = fileEntry.Key;

                GUILayout.BeginHorizontal();
                GUILayout.Label(fileName, GUILayout.Width(200));

                foreach (var language in allLanguages)
                {
                    if (fileEntry.Value.ContainsKey(language))
                    {
                        var response = fileEntry.Value[language];
                        GUILayout.Label(response.Updated.ToString(), GUILayout.Width(100));
                        GUILayout.Label(response.Added.ToString(), GUILayout.Width(100));
                    }
                    else
                    {
                        // If no data for this language, show empty or zero
                        GUILayout.Label("0", GUILayout.Width(100));
                        GUILayout.Label("0", GUILayout.Width(100));
                    }
                }

                GUILayout.EndHorizontal();

                // Add a separator line between each file row
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1)); // This creates a thin horizontal line
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
