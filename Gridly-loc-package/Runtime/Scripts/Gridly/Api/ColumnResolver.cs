using UnityEngine;
using Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Dialog;
using System.Runtime.CompilerServices;

public class ColumnResolver : MonoBehaviour
{
    private ApiClient apiClient;
    private List<string> LocaleIdentifiers;
    private string ApiKey;

    public void Initialize(ApiClient client)
    {
        this.apiClient = client;
    }

    public void StartColumnResolver(string viewId, string apiKey, List<string> localeIdentifiers)
    {
        if (apiClient != null)
        {
            // Store the locale identifiers and fetch the view
            LocaleIdentifiers = localeIdentifiers;
            apiClient.GetView(viewId, apiKey, ProcessView, HandleError);
            ApiKey = apiKey;
        }
        else
        {
            Debug.LogError("ApiClient is not available.");
        }
    }

    // Private method to process the View object
    private void ProcessView(View view)
    {
        bool smartStringColumnExists = false;
        // Remove locales that already have corresponding columns in the view
        foreach (Column column in view.columns)
        {
            foreach (string locale in LocaleIdentifiers.ToList())
            {
                if (column.type == "language" && column.languageCode == locale)
                {
                    LocaleIdentifiers.Remove(locale);
                }
            }
            if(column.name == "SmartString")
            {
                smartStringColumnExists = true;
            }
        }
        if(!smartStringColumnExists)
        {
            CreateSmartStringColumn(view.id);
        }
        // Create columns for the remaining locales
        foreach (string locale in LocaleIdentifiers)
        {
            Debug.Log("Missing column, creating: " + locale);
            CreateMissingColumn(view.id, locale);
        }
    }

    private void CreateSmartStringColumn(string viewId)
    {
        ColumnCreation newColumn = new ColumnCreation
        {
            id = "SmartString",
            name = "SmartString",
            type = "boolean"
        };

        apiClient.CreateColumn(viewId, ApiKey, newColumn, OnCreateColumnSuccess, OnCreateColumnError);
    }

    // Method to create a missing column
    private void CreateMissingColumn(string viewId, string locale)
    {
        ColumnCreation newColumn = new ColumnCreation
        {
            id = locale,
            isTarget = true,  // Adjust as needed
            name = locale,
            type = "language",
            languageCode = locale,
            localizationType = "targetLanguage"  // Adjust as needed
        };

        apiClient.CreateColumn(viewId, ApiKey, newColumn, OnCreateColumnSuccess, OnCreateColumnError);
    }

    // Callback method for successful column creation
    private void OnCreateColumnSuccess(string responseContent)
    {
        //Debug.Log("Column created successfully: " + responseContent);
    }

    // Callback method for failed column creation
    private void OnCreateColumnError(string errorMessage)
    {
        Debug.LogError("Failed to create column: " + errorMessage);
    }

    // Private method to handle errors during view fetching
    private void HandleError(string errorMessage)
    {
        Debug.LogError("Error while fetching the view: " + errorMessage);

        // Show the custom error dialog
        ErrorDialog.ShowDialog("Error", "Error while fetching the view:\n" + errorMessage);
    }
}
