using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog;
using UnityEngine;


public class ColumnResolver
{
    private ApiClient apiClient;
    private List<string> LocaleIdentifiers;
    private string ApiKey;

    public ColumnResolver(ApiClient client)
    {
        this.apiClient = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task StartColumnResolverAsync(string viewId, string apiKey, List<string> localeIdentifiers)
    {
        if (apiClient == null)
        {
            Debug.LogError("ApiClient is not available.");
            return;
        }

        try
        {
            // Store the locale identifiers
            LocaleIdentifiers = new List<string>(localeIdentifiers);
            ApiKey = apiKey;

            // Fetch the view asynchronously
            View view = await apiClient.GetViewAsync(viewId, apiKey);
            await ProcessViewAsync(view);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while fetching the view: {ex.Message}");
            ErrorDialog.ShowDialog("Error", "Error while fetching the view:\n" + ex.Message);
        }
    }

    // Private method to process the View object
    private async Task ProcessViewAsync(View view)
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
            if (column.name == "SmartString")
            {
                smartStringColumnExists = true;
            }
        }

        // Create SmartString column if it does not exist
        if (!smartStringColumnExists)
        {
            await CreateSmartStringColumnAsync(view.id);
        }

        // Create columns for the remaining locales
        foreach (string locale in LocaleIdentifiers)
        {
            Debug.Log("Missing column, creating: " + locale);
            await CreateMissingColumnAsync(view.id, locale);
        }
    }

    private async Task CreateSmartStringColumnAsync(string viewId)
    {
        ColumnCreation newColumn = new ColumnCreation
        {
            id = "SmartString",
            name = "SmartString",
            type = "boolean"
        };

        try
        {
            await apiClient.CreateColumnAsync(viewId, ApiKey, newColumn);
            Debug.Log("SmartString column created successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create SmartString column: {ex.Message}");
        }
    }

    // Method to create a missing column
    private async Task CreateMissingColumnAsync(string viewId, string locale)
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

        try
        {
            await apiClient.CreateColumnAsync(viewId, ApiKey, newColumn);
            Debug.Log($"Column for locale '{locale}' created successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create column for locale '{locale}': {ex.Message}");
        }
    }
}
