using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Gridly_loc_package.Editor.Scripts.Gridly.Dialog
{
    public class ErrorDialog : EditorWindow
    {
        private string titleText;
        private string messageText;

        public static void ShowDialog(string title, string message)
        {
            // Create a new window instance
            ErrorDialog window = (ErrorDialog)GetWindow(typeof(ErrorDialog), true, title);
            window.titleText = title;
            window.messageText = message;
            window.minSize = new Vector2(300, 150);
            window.Show();
        }

        private void OnGUI()
        {
            // Display the message
            GUILayout.Label(messageText, EditorStyles.wordWrappedLabel);

            GUILayout.FlexibleSpace();

            // OK Button
            if (GUILayout.Button("OK", GUILayout.Height(40)))
            {
                Close(); // Close the window when OK is clicked
            }
        }
    }
}
