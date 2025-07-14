using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Added for IEnumerable

namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor.gridly.Utils
{
    /// <summary>
    /// Utility class for file operations in the Gridly integration system.
    /// Provides methods for managing files, particularly CSV files used in localization.
    /// </summary>
    public class FileUtils
    {
        #region Constants

        private const string CSV_EXTENSION = ".csv";
        private const string CSV_META_EXTENSION = ".csv.meta";
        private const string FILE_PREFIX = "_";
        private const string FOLDER_PATH_NULL_MESSAGE = "The folder path cannot be null or empty.";
        private const string FOLDER_NOT_EXISTS_MESSAGE = "The folder path '{0}' does not exist.";
        private const string FILE_DELETED_MESSAGE = "Deleted: {0}";
        private const string SUCCESS_MESSAGE = "Filtered files deleted successfully.";
        private const string ERROR_MESSAGE = "An error occurred while deleting files: {0}";

        #endregion

        #region Public Methods

        /// <summary>
        /// Asynchronously deletes all files in the specified folder that match the filtering criteria.
        /// Files must start with "_" and end with ".csv" or ".csv.meta".
        /// </summary>
        /// <param name="folderPath">The path to the folder containing files to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when folderPath is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified folder does not exist.</exception>
        public static async Task DeleteAllFilesAsync(string folderPath)
        {
            ValidateFolderPath(folderPath);
            ValidateFolderExists(folderPath);

            try
            {
                var filesToDelete = GetFilteredFiles(folderPath);
                await DeleteFilesAsync(filesToDelete);
                LogSuccess();
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates that the folder path is not null or empty.
        /// </summary>
        /// <param name="folderPath">The folder path to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when folderPath is null or empty.</exception>
        private static void ValidateFolderPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentNullException(nameof(folderPath), FOLDER_PATH_NULL_MESSAGE);
            }
        }

        /// <summary>
        /// Validates that the specified folder exists.
        /// </summary>
        /// <param name="folderPath">The folder path to validate.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown when the folder does not exist.</exception>
        private static void ValidateFolderExists(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException(string.Format(FOLDER_NOT_EXISTS_MESSAGE, folderPath));
            }
        }

        /// <summary>
        /// Gets all files in the specified folder that match the filtering criteria.
        /// </summary>
        /// <param name="folderPath">The path to the folder to search.</param>
        /// <returns>An enumerable of file paths that match the criteria.</returns>
        private static IEnumerable<string> GetFilteredFiles(string folderPath)
        {
            var allFiles = Directory.GetFiles(folderPath);
            return allFiles.Where(IsFileMatchingCriteria);
        }

        /// <summary>
        /// Determines if a file matches the deletion criteria.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>True if the file matches the criteria, false otherwise.</returns>
        private static bool IsFileMatchingCriteria(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return fileName.StartsWith(FILE_PREFIX) && 
                   (filePath.EndsWith(CSV_EXTENSION) || filePath.EndsWith(CSV_META_EXTENSION));
        }

        /// <summary>
        /// Asynchronously deletes the specified files.
        /// </summary>
        /// <param name="filesToDelete">The collection of file paths to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task DeleteFilesAsync(IEnumerable<string> filesToDelete)
        {
            foreach (string filePath in filesToDelete)
            {
                await Task.Run(() => File.Delete(filePath));
                LogFileDeleted(filePath);
            }
        }

        /// <summary>
        /// Logs a message when a file is successfully deleted.
        /// </summary>
        /// <param name="filePath">The path of the deleted file.</param>
        private static void LogFileDeleted(string filePath)
        {
            Console.WriteLine(string.Format(FILE_DELETED_MESSAGE, filePath));
        }

        /// <summary>
        /// Logs a success message when all files are deleted.
        /// </summary>
        private static void LogSuccess()
        {
            Console.WriteLine(SUCCESS_MESSAGE);
        }

        /// <summary>
        /// Logs an error message when an exception occurs.
        /// </summary>
        /// <param name="errorMessage">The error message to log.</param>
        private static void LogError(string errorMessage)
        {
            Console.WriteLine(string.Format(ERROR_MESSAGE, errorMessage));
        }

        #endregion
    }
}
