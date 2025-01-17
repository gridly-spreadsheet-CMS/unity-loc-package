using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ServerCommunication : MonoBehaviour
{
    public string serverURL = "https://yourserver.com/upload"; // Replace with your actual server URL

    public void UploadCSV(string filePath)
    {
        if (File.Exists(filePath))
        {
            StartCoroutine(UploadCSVCoroutine(filePath));
        }
        else
        {
            Debug.LogError($"File not found: {filePath}");
        }
    }

    private IEnumerator UploadCSVCoroutine(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        UnityWebRequest www = UnityWebRequest.Put(serverURL, fileData);
        www.SetRequestHeader("Content-Type", "text/csv");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Upload failed: {www.error}");
        }
        else
        {
            Debug.Log("CSV upload complete!");
        }
    }
}
