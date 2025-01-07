using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Assets.Gridly_loc_package.Runtime.Scripts.Gridly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class ApiClient : MonoBehaviour
{
    // Existing method to get a view
    public void GetView(string viewId, string apiKey, Action<View> onSuccess, Action<string> onError)
    {
        StartCoroutine(FetchView(viewId, apiKey, onSuccess, onError));
    }

    // New method to create a column
    public void CreateColumn(string viewId, string apiKey, ColumnCreation columnCreation, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendCreateColumnRequest(viewId, apiKey, columnCreation, onSuccess, onError));
    }

    public void UploadCsvFile(string viewId, string apiKey, string filePath, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendUploadCsvRequest(viewId, apiKey, filePath, onSuccess, onError));
    }


    // Coroutine to fetch the paths tree
    public void GetPathsTree(string viewId, string apiKey, Action<List<string>> onSuccess, Action<string> onError)
    {
        StartCoroutine(FetchPathsTree(viewId, apiKey, onSuccess, onError));
    }

    public void GetRecords(string viewId, string apiKey, string fileName, string languageCode, Action<List<Record>> onSuccess, Action<string> onError)
    {
        StartCoroutine(FetchRecords(viewId, apiKey, fileName, languageCode, onSuccess, onError));
    }
       
    private IEnumerator FetchRecords(string viewId, string apiKey, string fileName, string languageCode, Action<List<Record>> onSuccess, Action<string> onError)
    {
        int offset = 0;
        int limit = 1000;
        List<Record> allRecords = new List<Record>();
        List<Record> filteredRecords = new List<Record>();

        while (true)
        {
            string query = $"{{\"_path\": {{\"=\": \"{fileName}\" }}, \"{languageCode}\":{{\"!=\": \"\"}} }}";
            string page = $"{{ \"offset\": {offset}, \"limit\": {limit} }}";
            string url = $"https://api.gridly.com/v1/views/{viewId}/records?query={Uri.EscapeDataString(query)}&page={Uri.EscapeDataString(page)}";

            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.SetRequestHeader("Authorization", "ApiKey " + apiKey);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke(request.error);
                yield break;
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                List<Record> fetchedRecords = JsonConvert.DeserializeObject<List<Record>>(jsonResponse);

                allRecords.AddRange(fetchedRecords);

                string totalCountHeader = request.GetResponseHeader("X-Total-Count");
                if (totalCountHeader == null || allRecords.Count >= int.Parse(totalCountHeader))
                {
                    filteredRecords = allRecords
                        .Select(record => new Record
                        {
                            id = record.id,
                            path = record.path,
                            cells = record.cells.Where(cell => cell.columnId == languageCode).ToList()
                        })
                        .Where(record => record.cells.Count > 0)
                        .ToList();

                    break; // No more records to fetch
                }

                offset += limit;
            }
        }

        onSuccess?.Invoke(filteredRecords); // Return all fetched records
    }



    // Wrapper class for JSON deserialization
    [Serializable]
    public class RecordWrapper
    {
        public List<Record> records = new List<Record>();
    }

    // Coroutine to fetch the paths tree
    private IEnumerator FetchPathsTree(string viewId, string apiKey, Action<List<string>> onSuccess, Action<string> onError)
    {
        string url = $"https://api.gridly.com/v1/views/{viewId}/paths/tree";
        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);

        request.SetRequestHeader("Authorization", "ApiKey " + apiKey);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            onError?.Invoke(request.error);
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;


            // Wrap the JSON array in a fake root object so that JsonUtility can deserialize it
            string wrappedJson = $"{{\"paths\": {jsonResponse}}}";
            GridlyPathWrapper wrapper = JsonUtility.FromJson<GridlyPathWrapper>(wrappedJson);

            // Extract the names
            List<string> names = new List<string>();
            foreach (var path in wrapper.paths)
            {
                names.Add(path.name);
            }

            // Invoke the success callback with the list of names
            onSuccess?.Invoke(names);
        }
    }


    [Serializable]
    public class GridlyPathWrapper
    {
        public List<GridlyPath> paths;
    }

    // Coroutine to fetch a view
    private IEnumerator FetchView(string viewId, string apiKey, Action<View> onSuccess, Action<string> onError)
    {
        Debug.Log("viewId: " + viewId + "apikey: " + apiKey);
        string url = $"https://api.gridly.com/v1/views/{viewId}";
        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);

        request.SetRequestHeader("Authorization", "ApiKey " + apiKey);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            onError?.Invoke(request.error);
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            View view = JsonUtility.FromJson<View>(jsonResponse);
            onSuccess?.Invoke(view);
        }
    }

    // Coroutine to send a POST request to create a column
    private IEnumerator SendCreateColumnRequest(string viewId, string apiKey, ColumnCreation columnCreation, Action<string> onSuccess, Action<string> onError)
    {
        string url = $"https://api.gridly.com/v1/views/{viewId}/columns";

        // Serialize the ColumnCreation object to JSON
        string jsonBody = JsonConvert.SerializeObject(columnCreation, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        });


        // Print the JSON payload to the console
        //Debug.Log("JSON Payload: " + jsonBody);

        // Convert the JSON string to a byte array
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "ApiKey " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke(request.error);
            }
            else if (request.responseCode == 400)
            {
                // Capture the response text for further debugging
                Debug.LogError("Server Response: " + request.downloadHandler.text);
                onError?.Invoke("HTTP/1.1 400 Bad Request");
            }
            else
            {
                string responseContent = request.downloadHandler.text;
                onSuccess?.Invoke(responseContent);
            }
        }
    }
    private IEnumerator SendUploadCsvRequest(string viewId, string apiKey, string filePath, Action<string> onSuccess, Action<string> onError)
    {
        string url = $"https://api.gridly.com/v1/views/{viewId}/import";

        // Check if file exists
        if (!System.IO.File.Exists(filePath))
        {
            onError?.Invoke($"File not found: {filePath}");
            yield break;
        }

        byte[] fileData = System.IO.File.ReadAllBytes(filePath);

        // Create a new WWWForm
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, System.IO.Path.GetFileName(filePath), "text/csv");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            request.SetRequestHeader("Authorization", "ApiKey " + apiKey);

            // Set the download handler
            request.downloadHandler = new DownloadHandlerBuffer();

            // Send the request and wait for the response
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke(request.error);
            }
            else
            {
                string responseContent = request.downloadHandler.text;
                onSuccess?.Invoke(responseContent);
            }
        }
    }



}
