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
    /// <summary>
    /// Provides HTTP client functionality for communicating with the Gridly API.
    /// </summary>
    public class ApiClient
    {
        // API Constants
        private const string BaseUrl = "https://api.gridly.com/v1";
        private const string AuthorizationHeader = "Authorization";
        private const string ApiKeyPrefix = "ApiKey ";
        private const string ContentTypeHeader = "Content-Type";
        private const string ApplicationJson = "application/json";
        private const string TextCsv = "text/csv";
        private const string TotalCountHeader = "X-Total-Count";
        
        // HTTP Constants
        private const int DefaultLimit = 1000;
        private const int BadRequestStatusCode = 400;
        
        // Error Messages
        private const string FileNotFoundError = "File not found: {0}";
        private const string RequestErrorFormat = "Request Error: {0}";
        private const string BadRequestErrorFormat = "HTTP/1.1 400 Bad Request: {0}";
        private const string ServerResponseError = "Server Response: {0}";

        /// <summary>
        /// Retrieves a view from the Gridly API.
        /// </summary>
        /// <param name="viewId">The ID of the view to retrieve.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <returns>A View object containing the view data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when viewId or apiKey is null or empty.</exception>
        /// <exception cref="Exception">Thrown when the API request fails.</exception>
        public async Task<View> GetViewAsync(string viewId, string apiKey)
        {
            ValidateApiParameters(viewId, apiKey);
            
            var url = $"{BaseUrl}/views/{viewId}";
            var request = CreateGetRequest(url, apiKey);
            
            try
            {
                await SendWebRequestAsync(request);
                return DeserializeResponse<View>(request);
            }
            finally
            {
                request?.Dispose();
            }
        }

        /// <summary>
        /// Creates a new column in the specified view.
        /// </summary>
        /// <param name="viewId">The ID of the view where the column will be created.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="columnCreation">The column creation data.</param>
        /// <returns>The response from the API as a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when viewId, apiKey, or columnCreation is null.</exception>
        /// <exception cref="Exception">Thrown when the API request fails.</exception>
        public async Task<string> CreateColumnAsync(string viewId, string apiKey, ColumnCreation columnCreation)
        {
            ValidateApiParameters(viewId, apiKey);
            ValidateColumnCreation(columnCreation);
            
            var url = $"{BaseUrl}/views/{viewId}/columns";
            var jsonBody = SerializeColumnCreation(columnCreation);
            var request = CreatePostRequest(url, apiKey, jsonBody);
            
            try
            {
                await SendWebRequestAsync(request);
                HandleBadRequestResponse(request);
                return request.downloadHandler.text;
            }
            finally
            {
                request?.Dispose();
            }
        }

        /// <summary>
        /// Uploads a CSV file to the specified view.
        /// </summary>
        /// <param name="viewId">The ID of the view where the file will be uploaded.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="filePath">The path to the CSV file to upload.</param>
        /// <returns>The response from the API as a string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when viewId, apiKey, or filePath is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
        /// <exception cref="Exception">Thrown when the API request fails.</exception>
        public async Task<string> UploadCsvFileAsync(string viewId, string apiKey, string filePath)
        {
            ValidateApiParameters(viewId, apiKey);
            ValidateFilePath(filePath);
            
            var url = $"{BaseUrl}/views/{viewId}/import";
            var request = CreateFileUploadRequest(url, apiKey, filePath);
            
            try
            {
                await SendWebRequestAsync(request);
                return request.downloadHandler.text;
            }
            finally
            {
                request?.Dispose();
            }
        }

        /// <summary>
        /// Retrieves the paths tree from the specified view.
        /// </summary>
        /// <param name="viewId">The ID of the view to retrieve paths from.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <returns>A list of path names.</returns>
        /// <exception cref="ArgumentNullException">Thrown when viewId or apiKey is null or empty.</exception>
        /// <exception cref="Exception">Thrown when the API request fails.</exception>
        public async Task<List<string>> GetPathsTreeAsync(string viewId, string apiKey)
        {
            ValidateApiParameters(viewId, apiKey);
            
            var url = $"{BaseUrl}/views/{viewId}/paths/tree";
            var request = CreateGetRequest(url, apiKey);
            
            try
            {
                await SendWebRequestAsync(request);
                return DeserializePathsResponse(request);
            }
            finally
            {
                request?.Dispose();
            }
        }

        /// <summary>
        /// Retrieves records from the specified view based on file name and language code.
        /// </summary>
        /// <param name="viewId">The ID of the view to retrieve records from.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="fileName">The name of the file to filter records by.</param>
        /// <param name="languageCode">The language code to filter records by.</param>
        /// <returns>A list of records matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null or empty.</exception>
        /// <exception cref="Exception">Thrown when the API request fails.</exception>
        public async Task<List<Record>> GetRecordsAsync(string viewId, string apiKey, string fileName, string languageCode)
        {
            ValidateApiParameters(viewId, apiKey);
            ValidateRecordParameters(fileName, languageCode);
            
            return await FetchAllRecordsAsync(viewId, apiKey, fileName, languageCode);
        }

        /// <summary>
        /// Validates API parameters for null or empty values.
        /// </summary>
        /// <param name="viewId">The view ID to validate.</param>
        /// <param name="apiKey">The API key to validate.</param>
        private static void ValidateApiParameters(string viewId, string apiKey)
        {
            if (string.IsNullOrEmpty(viewId))
            {
                throw new ArgumentNullException(nameof(viewId), "View ID cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty.");
            }
        }

        /// <summary>
        /// Validates column creation parameters.
        /// </summary>
        /// <param name="columnCreation">The column creation object to validate.</param>
        private static void ValidateColumnCreation(ColumnCreation columnCreation)
        {
            if (columnCreation == null)
            {
                throw new ArgumentNullException(nameof(columnCreation), "Column creation data cannot be null.");
            }
        }

        /// <summary>
        /// Validates file path parameters.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        private static void ValidateFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(string.Format(FileNotFoundError, filePath));
            }
        }

        /// <summary>
        /// Validates record retrieval parameters.
        /// </summary>
        /// <param name="fileName">The file name to validate.</param>
        /// <param name="languageCode">The language code to validate.</param>
        private static void ValidateRecordParameters(string fileName, string languageCode)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName), "File name cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(languageCode))
            {
                throw new ArgumentNullException(nameof(languageCode), "Language code cannot be null or empty.");
            }
        }

        /// <summary>
        /// Creates a GET request with proper headers.
        /// </summary>
        /// <param name="url">The URL for the request.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <returns>A configured UnityWebRequest for GET operations.</returns>
        private static UnityWebRequest CreateGetRequest(string url, string apiKey)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.SetRequestHeader(AuthorizationHeader, ApiKeyPrefix + apiKey);
            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        /// <summary>
        /// Creates a POST request with JSON body.
        /// </summary>
        /// <param name="url">The URL for the request.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="jsonBody">The JSON body to send.</param>
        /// <returns>A configured UnityWebRequest for POST operations.</returns>
        private static UnityWebRequest CreatePostRequest(string url, string apiKey, string jsonBody)
        {
            var bytes = Encoding.UTF8.GetBytes(jsonBody);
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader(ContentTypeHeader, ApplicationJson);
            request.SetRequestHeader(AuthorizationHeader, ApiKeyPrefix + apiKey);
            return request;
        }

        /// <summary>
        /// Creates a file upload request.
        /// </summary>
        /// <param name="url">The URL for the request.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="filePath">The path to the file to upload.</param>
        /// <returns>A configured UnityWebRequest for file upload operations.</returns>
        private static UnityWebRequest CreateFileUploadRequest(string url, string apiKey, string filePath)
        {
            var fileData = File.ReadAllBytes(filePath);
            var form = new WWWForm();
            form.AddBinaryData("file", fileData, Path.GetFileName(filePath), TextCsv);
            
            var request = UnityWebRequest.Post(url, form);
            request.SetRequestHeader(AuthorizationHeader, ApiKeyPrefix + apiKey);
            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        /// <summary>
        /// Serializes a ColumnCreation object to JSON.
        /// </summary>
        /// <param name="columnCreation">The column creation object to serialize.</param>
        /// <returns>The JSON string representation.</returns>
        private static string SerializeColumnCreation(ColumnCreation columnCreation)
        {
            return JsonConvert.SerializeObject(columnCreation, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            });
        }

        /// <summary>
        /// Handles bad request responses from the API.
        /// </summary>
        /// <param name="request">The UnityWebRequest to check.</param>
        private static void HandleBadRequestResponse(UnityWebRequest request)
        {
            if (request.responseCode == BadRequestStatusCode)
            {
                var errorResponse = request.downloadHandler.text;
                Debug.LogError(string.Format(ServerResponseError, errorResponse));
                throw new Exception(string.Format(BadRequestErrorFormat, errorResponse));
            }
        }

        /// <summary>
        /// Deserializes a JSON response to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="request">The UnityWebRequest containing the response.</param>
        /// <returns>The deserialized object.</returns>
        private static T DeserializeResponse<T>(UnityWebRequest request)
        {
            var jsonResponse = request.downloadHandler.text;
            return JsonUtility.FromJson<T>(jsonResponse);
        }

        /// <summary>
        /// Deserializes a paths response and extracts path names.
        /// </summary>
        /// <param name="request">The UnityWebRequest containing the response.</param>
        /// <returns>A list of path names.</returns>
        private static List<string> DeserializePathsResponse(UnityWebRequest request)
        {
            var jsonResponse = request.downloadHandler.text;
            var wrappedJson = $"{{\"paths\": {jsonResponse}}}"; // Wrap JSON array for deserialization
            var wrapper = JsonUtility.FromJson<GridlyPathWrapper>(wrappedJson);
            return wrapper.paths.Select(path => path.name).ToList();
        }

        /// <summary>
        /// Result structure for paginated record fetching.
        /// </summary>
        private class PageResult
        {
            public List<Record> Records { get; set; }
            public int? TotalCount { get; set; }
        }

        /// <summary>
        /// Fetches all records using pagination.
        /// </summary>
        /// <param name="viewId">The view ID.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="fileName">The file name filter.</param>
        /// <param name="languageCode">The language code filter.</param>
        /// <returns>A list of all matching records.</returns>
        private async Task<List<Record>> FetchAllRecordsAsync(string viewId, string apiKey, string fileName, string languageCode)
        {
            var offset = 0;
            var allRecords = new List<Record>();
            int? totalCount = null;

            while (true)
            {
                var pageResult = await FetchRecordsPageAsync(viewId, apiKey, fileName, languageCode, offset);
                var records = pageResult.Records;
                var pageTotalCount = pageResult.TotalCount;

                // Set total count from first page if not already set
                if (totalCount == null && pageTotalCount.HasValue)
                {
                    totalCount = pageTotalCount.Value;
                }

                allRecords.AddRange(records);

                // Stop if we've fetched all records or if the page returned fewer records than the limit (end of data)
                if (ShouldStopFetching(allRecords, totalCount, records.Count))
                {
                    return FilterAndTransformRecords(allRecords, languageCode);
                }

                offset += DefaultLimit;
            }
        }

        /// <summary>
        /// Fetches a single page of records.
        /// </summary>
        /// <param name="viewId">The view ID.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="fileName">The file name filter.</param>
        /// <param name="languageCode">The language code filter.</param>
        /// <param name="offset">The offset for pagination.</param>
        /// <returns>A PageResult containing records and total count from X-Total-Count header.</returns>
        private async Task<PageResult> FetchRecordsPageAsync(string viewId, string apiKey, string fileName, string languageCode, int offset)
        {
            var query = BuildRecordsQuery(fileName, languageCode);
            var page = BuildPageQuery(offset);
            var url = $"{BaseUrl}/views/{viewId}/records?query={Uri.EscapeDataString(query)}&page={Uri.EscapeDataString(page)}";
            
            var request = CreateGetRequest(url, apiKey);
            
            try
            {
                await SendWebRequestAsync(request);
                var jsonResponse = request.downloadHandler.text;
                var records = JsonConvert.DeserializeObject<List<Record>>(jsonResponse);
                
                // Read X-Total-Count header from response (Gridly API provides this for pagination)
                int? totalCount = null;
                var totalCountHeader = request.GetResponseHeader(TotalCountHeader);
                if (!string.IsNullOrEmpty(totalCountHeader) && int.TryParse(totalCountHeader, out var parsedCount))
                {
                    totalCount = parsedCount;
                }

                return new PageResult
                {
                    Records = records ?? new List<Record>(),
                    TotalCount = totalCount
                };
            }
            finally
            {
                request?.Dispose();
            }
        }

        /// <summary>
        /// Builds the query string for record filtering.
        /// </summary>
        /// <param name="fileName">The file name to filter by.</param>
        /// <param name="languageCode">The language code to filter by.</param>
        /// <returns>The JSON query string.</returns>
        private static string BuildRecordsQuery(string fileName, string languageCode)
        {
            return $"{{\"_path\": {{\"=\": \"{fileName}\" }}, \"{languageCode}\":{{\"!=\": \"\"}} }}";
        }

        /// <summary>
        /// Builds the page query string for pagination.
        /// </summary>
        /// <param name="offset">The offset for pagination.</param>
        /// <returns>The JSON page string.</returns>
        private static string BuildPageQuery(int offset)
        {
            return $"{{ \"offset\": {offset}, \"limit\": {DefaultLimit} }}";
        }

        /// <summary>
        /// Determines if record fetching should stop based on total count or page size.
        /// </summary>
        /// <param name="allRecords">The current list of records.</param>
        /// <param name="totalCount">The total count from X-Total-Count header.</param>
        /// <param name="currentPageSize">The number of records returned in the current page.</param>
        /// <returns>True if fetching should stop, false otherwise.</returns>
        private static bool ShouldStopFetching(List<Record> allRecords, int? totalCount, int currentPageSize)
        {
            // Stop if we've fetched all records according to the total count header
            if (totalCount.HasValue && allRecords.Count >= totalCount.Value)
            {
                return true;
            }

            // Stop if the current page returned fewer records than the limit (we've reached the end)
            if (currentPageSize < DefaultLimit)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Filters and transforms records to include only relevant cells.
        /// </summary>
        /// <param name="allRecords">The list of all records.</param>
        /// <param name="languageCode">The language code to filter by.</param>
        /// <returns>The filtered and transformed records.</returns>
        private static List<Record> FilterAndTransformRecords(List<Record> allRecords, string languageCode)
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


        /// <summary>
        /// Sends a UnityWebRequest asynchronously and handles errors.
        /// </summary>
        /// <param name="request">The UnityWebRequest to send.</param>
        /// <exception cref="Exception">Thrown when the request fails.</exception>
        private async Task SendWebRequestAsync(UnityWebRequest request)
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield(); // Prevent blocking the main thread
            }

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                throw new Exception(string.Format(RequestErrorFormat, request.error));
            }
        }

        /// <summary>
        /// Wrapper class for deserializing Gridly path responses.
        /// </summary>
        [Serializable]
        public class GridlyPathWrapper
        {
            public List<GridlyPath> paths;
        }
    }
#endif
}