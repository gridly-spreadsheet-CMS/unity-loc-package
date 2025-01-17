using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Assets.Gridly_loc_package.Editor.gridly.Utils
{
    public class FileUtils
    {
        public static async Task DeleteAllFilesAsync(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Console.WriteLine("The folder path cannot be null or empty.");
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"The folder path '{folderPath}' does not exist.");
                return;
            }

            try
            {
                // Get all files in the folder
                string[] files = Directory.GetFiles(folderPath);

                // Filter files that start with "_" and end with ".csv" or ".csv.meta"
                var filteredFiles = files.Where(file =>
                    Path.GetFileName(file).StartsWith("_") &&
                    (file.EndsWith(".csv") || file.EndsWith(".csv.meta")));

                // Delete each file asynchronously
                foreach (string file in filteredFiles)
                {
                    await Task.Run(() => File.Delete(file));
                    Console.WriteLine($"Deleted: {file}");
                }

                Console.WriteLine("Filtered files deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while deleting files: {ex.Message}");
            }
        }
    }
}
