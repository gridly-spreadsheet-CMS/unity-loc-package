using System;
using System.Reflection;
using UnityEngine;

namespace GridlyAB.GridlyIntegration.Editor.gridly.Util
{
    /// <summary>
    /// Utility class for JSON operations in the Gridly integration system.
    /// Provides methods for converting objects to JSON while filtering out empty or null values.
    /// </summary>
    public static class JsonHelper
    {
        #region Constants

        private const BindingFlags FIELD_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts an object to JSON format while filtering out null and empty string values.
        /// Creates a copy of the object to avoid modifying the original.
        /// </summary>
        /// <param name="obj">The object to convert to JSON.</param>
        /// <returns>A JSON string representation of the object with empty fields removed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
        /// <exception cref="ArgumentException">Thrown when obj is not a valid object type.</exception>
        public static string ToFilteredJson(object obj)
        {
            ValidateInputObject(obj);

            var objectCopy = CreateObjectCopy(obj);
            RemoveEmptyFieldsFromObject(objectCopy);
            
            return JsonUtility.ToJson(objectCopy);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates that the input object is not null and is a valid type for JSON conversion.
        /// </summary>
        /// <param name="obj">The object to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when obj is null.</exception>
        /// <exception cref="ArgumentException">Thrown when obj is not a valid object type.</exception>
        private static void ValidateInputObject(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Object cannot be null for JSON conversion.");
            }

            if (obj.GetType().IsPrimitive || obj is string)
            {
                throw new ArgumentException("Object must be a complex type for field filtering.", nameof(obj));
            }
        }

        /// <summary>
        /// Creates a deep copy of the input object to avoid modifying the original.
        /// </summary>
        /// <param name="obj">The object to clone.</param>
        /// <returns>A new instance of the object with copied field values.</returns>
        /// <exception cref="InvalidOperationException">Thrown when object creation fails.</exception>
        private static object CreateObjectCopy(object obj)
        {
            try
            {
                var objectType = obj.GetType();
                var objectCopy = Activator.CreateInstance(objectType);
                
                if (objectCopy == null)
                {
                    throw new InvalidOperationException($"Failed to create instance of type {objectType.Name}.");
                }

                CopyFieldValues(obj, objectCopy);
                return objectCopy;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Failed to create object copy: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Copies all field values from the source object to the target object.
        /// </summary>
        /// <param name="sourceObject">The object to copy values from.</param>
        /// <param name="targetObject">The object to copy values to.</param>
        private static void CopyFieldValues(object sourceObject, object targetObject)
        {
            var fields = sourceObject.GetType().GetFields(FIELD_BINDING_FLAGS);
            
            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(sourceObject);
                field.SetValue(targetObject, fieldValue);
            }
        }

        /// <summary>
        /// Removes empty fields from the object by setting them to null.
        /// This ensures they are omitted from the JSON output.
        /// </summary>
        /// <param name="obj">The object to process.</param>
        private static void RemoveEmptyFieldsFromObject(object obj)
        {
            if (obj == null) return;

            var fields = obj.GetType().GetFields(FIELD_BINDING_FLAGS);
            
            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(obj);
                
                if (IsFieldValueEmpty(fieldValue))
                {
                    field.SetValue(obj, null);
                }
            }
        }

        /// <summary>
        /// Determines if a field value should be considered empty and removed from JSON output.
        /// </summary>
        /// <param name="fieldValue">The field value to check.</param>
        /// <returns>True if the field value is null or an empty/whitespace string, false otherwise.</returns>
        private static bool IsFieldValueEmpty(object fieldValue)
        {
            if (fieldValue == null)
            {
                return true;
            }

            if (fieldValue is string stringValue)
            {
                return string.IsNullOrWhiteSpace(stringValue);
            }

            return false;
        }

        #endregion
    }
}