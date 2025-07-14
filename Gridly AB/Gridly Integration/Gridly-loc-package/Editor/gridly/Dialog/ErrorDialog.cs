using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog
{
    /// <summary>
    /// A custom error dialog window for displaying error messages in the Unity Editor.
    /// Provides a simple modal dialog with an OK button to dismiss the error.
    /// </summary>
    public class ErrorDialog : EditorWindow
    {
        #region Constants

        private const float MIN_WINDOW_WIDTH = 300f;
        private const float MIN_WINDOW_HEIGHT = 150f;
        private const float BUTTON_HEIGHT = 40f;
        private const string OK_BUTTON_TEXT = "OK";

        #endregion

        #region Private Fields

        private string _titleText;
        private string _messageText;

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows an error dialog with the specified title and message.
        /// </summary>
        /// <param name="title">The title of the error dialog window.</param>
        /// <param name="message">The error message to display.</param>
        /// <exception cref="ArgumentNullException">Thrown when title or message is null or empty.</exception>
        public static void ShowDialog(string title, string message)
        {
            ValidateInput(title, message);
            
            var window = CreateWindow(title);
            ConfigureWindow(window, title, message);
            window.Show();
        }

        #endregion

        #region Unity Editor Methods

        /// <summary>
        /// Handles the GUI rendering for the error dialog window.
        /// </summary>
        private void OnGUI()
        {
            DrawMessage();
            DrawSpacer();
            DrawOkButton();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the input parameters for the dialog.
        /// </summary>
        /// <param name="title">The title to validate.</param>
        /// <param name="message">The message to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when title or message is null or empty.</exception>
        private static void ValidateInput(string title, string message)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentNullException(nameof(title), "Title cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message), "Message cannot be null or empty.");
            }
        }

        /// <summary>
        /// Creates a new error dialog window instance.
        /// </summary>
        /// <param name="title">The title for the window.</param>
        /// <returns>The created window instance.</returns>
        private static ErrorDialog CreateWindow(string title)
        {
            return (ErrorDialog)GetWindow(typeof(ErrorDialog), true, title);
        }

        /// <summary>
        /// Configures the window properties and content.
        /// </summary>
        /// <param name="window">The window to configure.</param>
        /// <param name="title">The title text.</param>
        /// <param name="message">The message text.</param>
        private static void ConfigureWindow(ErrorDialog window, string title, string message)
        {
            window._titleText = title;
            window._messageText = message;
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
        }

        /// <summary>
        /// Draws the error message in the dialog.
        /// </summary>
        private void DrawMessage()
        {
            GUILayout.Label(_messageText, EditorStyles.wordWrappedLabel);
        }

        /// <summary>
        /// Draws a flexible space to push the OK button to the bottom.
        /// </summary>
        private void DrawSpacer()
        {
            GUILayout.FlexibleSpace();
        }

        /// <summary>
        /// Draws the OK button and handles its click event.
        /// </summary>
        private void DrawOkButton()
        {
            if (GUILayout.Button(OK_BUTTON_TEXT, GUILayout.Height(BUTTON_HEIGHT)))
            {
                Close();
            }
        }

        #endregion
    }
}
