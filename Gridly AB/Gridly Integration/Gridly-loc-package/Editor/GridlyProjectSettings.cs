using System;
using UnityEngine;
using UnityEditor;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Enum;

namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor
{
    /// <summary>
    /// Project-specific settings for Gridly Integration plugin.
    /// Stored in ProjectSettings folder so it can be version-controlled and shared with the team.
    /// </summary>
    [CreateAssetMenu(fileName = "GridlySettings", menuName = "Gridly/Project Settings", order = 1)]
    public class GridlyProjectSettings : ScriptableObject
    {
        [Header("Export Settings")]
        public string exportPath = "Assets/LocalizationExports/";
        public string exportViewId = "VIEWID";
        public string exportApiKey = "EXPORTAPIKEY";
        public SmartOption selectedExportOption = SmartOption.AllStrings;

        [Header("Import Settings")]
        public string importViewId = "importViewId";
        public string importApiKey = "IMPORTAPIKEY";
        public SmartOption selectedImportOption = SmartOption.AllStrings;
        public bool useDifferentImportView = false;

        [Header("General Settings")]
        public bool deleteExtraRecords = true;
        public string selectedTables = ""; // Serialized table selections

        private const string SettingsPath = "ProjectSettings/GridlySettings.asset";
        private static GridlyProjectSettings _instance;

        /// <summary>
        /// Gets or creates the Gridly project settings instance.
        /// </summary>
        public static GridlyProjectSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<GridlyProjectSettings>(SettingsPath);
                    if (_instance == null)
                    {
                        _instance = CreateInstance<GridlyProjectSettings>();
                        // Ensure ProjectSettings directory exists
                        string directory = System.IO.Path.GetDirectoryName(SettingsPath);
                        if (!System.IO.Directory.Exists(directory))
                        {
                            System.IO.Directory.CreateDirectory(directory);
                        }
                        AssetDatabase.CreateAsset(_instance, SettingsPath);
                        AssetDatabase.SaveAssets();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Saves the settings to disk.
        /// </summary>
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

    }
}

