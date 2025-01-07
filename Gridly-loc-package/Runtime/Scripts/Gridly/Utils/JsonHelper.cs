using System;
using System.Reflection;
using UnityEngine;

public static class JsonHelper
{
    public static string ToFilteredJson(object obj)
    {
        // Get a copy of the object to avoid modifying the original
        var objCopy = CloneObject(obj);

        // Remove fields with null or empty values
        RemoveEmptyFields(objCopy);

        // Convert the filtered object to JSON
        return JsonUtility.ToJson(objCopy);
    }

    private static void RemoveEmptyFields(object obj)
    {
        if (obj == null) return;

        var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            var value = field.GetValue(obj);

            // Check if the value is null or an empty string
            if (value == null || (value is string strValue && string.IsNullOrWhiteSpace(strValue)))
            {
                field.SetValue(obj, null); // Set the field to null to omit from JSON
            }
        }
    }

    private static object CloneObject(object obj)
    {
        // Create a new instance and copy field values
        var clone = Activator.CreateInstance(obj.GetType());
        foreach (var field in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            field.SetValue(clone, field.GetValue(obj));
        }
        return clone;
    }
}


