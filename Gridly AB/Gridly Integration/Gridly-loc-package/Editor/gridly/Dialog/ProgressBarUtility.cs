using UnityEditor;

namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor.gridly.Dialog
{
    /// <summary>
    /// Utility class for managing progress bars in the Unity Editor.
    /// Provides functionality to display, update, and cancel progress operations.
    /// </summary>
    public static class ProgressBarUtility
    {
        #region Constants

        private const string DEFAULT_PROGRESS_TITLE = "Processing";
        private const int MINIMUM_TASK_COUNT = 1;

        #endregion

        #region Private Fields

        private static int _totalTasks;
        private static int _currentTask;
        private static bool _isCancelled;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the progress bar with the specified title and total task count.
        /// </summary>
        /// <param name="title">The title to display in the progress bar.</param>
        /// <param name="totalTaskCount">The total number of tasks to process.</param>
        /// <exception cref="System.ArgumentException">Thrown when totalTaskCount is less than 1.</exception>
        public static void InitializeProgressBar(string title, int totalTaskCount)
        {
            ValidateTaskCount(totalTaskCount);
            
            ResetProgressState();
            _totalTasks = totalTaskCount;
            DisplayProgressBar(title);
        }

        /// <summary>
        /// Increments the progress and updates the progress bar with new information.
        /// </summary>
        /// <param name="info">The information to display in the progress bar.</param>
        public static void IncrementProgress(string info)
        {
            _currentTask++;
            DisplayProgressBar(info);
        }

        /// <summary>
        /// Clears the progress bar from the Unity Editor.
        /// </summary>
        public static void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Checks if the current operation has been cancelled by the user.
        /// </summary>
        /// <returns>True if the operation was cancelled, false otherwise.</returns>
        public static bool IsCancelled()
        {
            return _isCancelled;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates that the task count is valid.
        /// </summary>
        /// <param name="totalTaskCount">The total number of tasks to validate.</param>
        /// <exception cref="System.ArgumentException">Thrown when totalTaskCount is less than 1.</exception>
        private static void ValidateTaskCount(int totalTaskCount)
        {
            if (totalTaskCount < MINIMUM_TASK_COUNT)
            {
                throw new System.ArgumentException(
                    $"Total task count must be at least {MINIMUM_TASK_COUNT}. Provided: {totalTaskCount}", 
                    nameof(totalTaskCount));
            }
        }

        /// <summary>
        /// Resets the progress state to initial values.
        /// </summary>
        private static void ResetProgressState()
        {
            _currentTask = 0;
            _isCancelled = false;
        }

        /// <summary>
        /// Displays the progress bar with current progress and cancellation support.
        /// </summary>
        /// <param name="info">The information to display in the progress bar.</param>
        private static void DisplayProgressBar(string info)
        {
            float progress = CalculateProgress();
            _isCancelled = EditorUtility.DisplayCancelableProgressBar(DEFAULT_PROGRESS_TITLE, info, progress);
        }

        /// <summary>
        /// Calculates the current progress as a percentage.
        /// </summary>
        /// <returns>The progress value between 0.0 and 1.0.</returns>
        private static float CalculateProgress()
        {
            return (float)_currentTask / _totalTasks;
        }

        #endregion
    }
}

