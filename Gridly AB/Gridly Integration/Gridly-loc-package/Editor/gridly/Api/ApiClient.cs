#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Newtonsoft.Json;
using Assets.Gridly_loc_package.Editor.Scripts.Gridly.Model;

namespace GridlyAB.GridlyIntegration.Gridly_loc_package.Editor.gridly.Api
{
    public class ApiClient
    {
        // Method to get a view
        public async Task<View> GetViewAsync(string viewId, string apiKey)
        {
            string url = $"https://api.gridly.com/v1/views/{viewId}";
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                request.SetRequestHeader("Authorization", "ApiKey " + apiKey);
                request.downloadHandler = new DownloadHandlerBuffer();

                await SendWebRequestAsync(request);

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new Exception(request.error);
                }

                string jsonResponse = request.downloadHandler.text;
                return JsonUtility.FromJson<View>(jsonResponse);
            }
        }

        // Method to create a column
        public async Task<string> CreateColumnAsync(string viewId, string apiKey, ColumnCreation columnCreation)
        {
            string url = $"https://api.gridly.com/v1/views/{viewId}/columns";
            string jsonBody = JsonConvert.SerializeObject(columnCreation, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });

            byte[] bytes = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "ApiKey " + apiKey);

                await SendWebRequestAsync(request);

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new Exception(request.error);
                }
                else if (request.responseCode == 400)
                {
                    string errorResponse = request.downloadHandler.text;
                    Debug.LogError("Server Response: " + errorResponse);
                    throw new Exception("HTTP/1.1 400 Bad Request: " + errorResponse);
                }

                return request.downloadHandler.text;
            }
        }

        // Method to upload a CSV file
        public async Task<string> UploadCsvFileAsync(string viewId, string apiKey, string filePath)
        {
            string url = $"https://api.gridly.com/v1/views/{viewId}/import";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            byte[] fileData = File.ReadAllBytes(filePath);
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "text/csv");

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                request.SetRequestHeader("Authorization", "ApiKey " + apiKey);
                request.downloadHandler = new DownloadHandlerBuffer();

                await SendWebRequestAsync(request);

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new Exception(request.error);
                }

                return request.downloadHandler.text;
            }
        }

        // Method to fetch the paths tree
        public async Task<List<string>> GetPathsTreeAsync(string viewId, string apiKey)
        {
            string url = $"https://api.gridly.com/v1/views/{viewId}/paths/tree";

            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                request.SetRequestHeader("Authorization", "ApiKey " + apiKey);
                request.downloadHandler = new DownloadHandlerBuffer();

                await SendWebRequestAsync(request);

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new Exception(request.error);
                }

                string jsonResponse = request.downloadHandler.text;
                string wrappedJson = $"{{\"paths\": {jsonResponse}}}"; // Wrap JSON array for deserialization
                GridlyPathWrapper wrapper = JsonUtility.FromJson<GridlyPathWrapper>(wrappedJson);

                return wrapper.paths.Select(path => path.name).ToList();
            }
        }

        // Method to get records
        public async Task<List<Record>> GetRecordsAsync(string viewId, string apiKey, string fileName, string languageCode)
        {
            int offset = 0;
            int limit = 1000;
            List<Record> allRecords = new List<Record>();

            while (true)
            {
                string query = $"{{\"_path\": {{\"=\": \"{fileName}\" }}, \"{languageCode}\":{{\"!=\": \"\"}} }}";
                string page = $"{{ \"offset\": {offset}, \"limit\": {limit} }}";
                string url = $"https://api.gridly.com/v1/views/{viewId}/records?query={Uri.EscapeDataString(query)}&page={Uri.EscapeDataString(page)}";

                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
                {
                    request.SetRequestHeader("Authorization", "ApiKey " + apiKey);
                    request.downloadHandler = new DownloadHandlerBuffer();

                    await SendWebRequestAsync(request);

                    if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        throw new Exception(request.error);
                    }

                    string jsonResponse = request.downloadHandler.text;
                    List<Record> fetchedRecords = JsonConvert.DeserializeObject<List<Record>>(jsonResponse);
                    allRecords.AddRange(fetchedRecords);

                    string totalCountHeader = request.GetResponseHeader("X-Total-Count");
                    if (totalCountHeader == null || allRecords.Count >= int.Parse(totalCountHeader))
                    {
                        return allRecords
                            .Select(record => new Record
                            {
                                id = record.id,
                                path = record.path,
                                cells = record.cells.Where(cell => cell.columnId == languageCode).ToList()
                            })
                            .Where(record => record.cells.Count > 0)
                            .ToList();
                    }

                    offset += limit;
                }
            }
        }

        // Helper method to send UnityWebRequest asynchronously
        private async Task SendWebRequestAsync(UnityWebRequest request)
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();  // Prevent blocking the main thread
            }

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                throw new Exception($"Request Error: {request.error}");
            }
        }

        [Serializable]
        public class GridlyPathWrapper
        {
            public List<GridlyPath> paths;
        }
    }
#endif
}