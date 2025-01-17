using UnityEditor;

public static class ProgressBarUtility
{
    private static int totalTasks;
    private static int currentTask;
    private static bool isCancelled;

    // Initialize the progress bar
    public static void InitializeProgressBar(string title, int totalTaskCount)
    {
        totalTasks = totalTaskCount;
        currentTask = 0;
        isCancelled = false; // Reset the cancellation flag
        DisplayProgressBar(title);
    }

    // Increment progress after each task
    public static void IncrementProgress(string info)
    {
        currentTask++;
        DisplayProgressBar(info);
    }

    // Show the progress bar with the current progress and a cancel button
    private static void DisplayProgressBar(string info)
    {
        float progress = (float)currentTask / totalTasks;

        // Use EditorUtility.DisplayCancelableProgressBar for a progress bar with a cancel button
        isCancelled = EditorUtility.DisplayCancelableProgressBar("Processing", info, progress);
    }

    // Clear the progress bar when complete
    public static void ClearProgressBar()
    {
        EditorUtility.ClearProgressBar();
    }

    // Check if the task has been cancelled
    public static bool IsCancelled()
    {
        
        return isCancelled;
    }
}
