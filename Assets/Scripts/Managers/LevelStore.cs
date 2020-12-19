using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace LevelStore
{
    using LevelPaths = List<string>;
    using LevelRevisions = List<string>;

    public delegate void StoreResponseHandle<T>(in StoreResponse<T> response);

    public enum StoreResponseStatus
    {
        Success        = 0,
        BadRequest     = 1,
        NullData       = 2,
        InvalidInput   = 4,
        ServerError    = 5,
        DuplicateData  = 6
    }

    public struct LevelState
    {
        public string path;
        public string level;
        public string revision;
        public string data;
    }

    public struct StoreRequest
    {
        public string path;
        public string revision;
        public string method;
        public string data;
    }

    /// <summary>
    /// When handling a store reponse, the status_code should be checked first.
    /// The result field carried is parsed from server response and is nullable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct StoreResponse<T>
    {
        public int status_code;
        public string status_info;
        public T result;
    }

    /// <summary>
    /// Level State Tunning Store API consumer, supporting:
    /// List avaliable levels
    /// List level revisions (identified by timestamps)
    /// Fetch level with a specific revision
    /// Save new level revision
    /// </summary>
    public class StoreClient
    {

        private string endpoint;

        public StoreClient(string storeUrl)
        {
            endpoint = storeUrl;
        }

        public void SetStoreEndpoint(string address)
        {
            endpoint = address;
        }

        /// <summary>
        /// List available levels, the result carried in response is a list of level paths.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public IEnumerator ListLevels(StoreResponseHandle<LevelPaths> handle)
        {
            var request = new StoreRequest
            {
                method = "list_levels"
            };
            yield return Post<LevelPaths>(request, handle);
        }

        /// <summary>
        /// List level revisions, the handle would receive a list of revisions of the level
        /// </summary>
        /// <param name="path"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        public IEnumerator ListLevelRevisions(string path, StoreResponseHandle<LevelRevisions> handle)
        {
            var request = new StoreRequest
            {
                method = "list_level_revisions",
                path = path
            };
            
            yield return Post<LevelRevisions>(request, handle);
        }

        /// <summary>
        /// Save level revision to store
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        public IEnumerator SaveLevel(string path, string data, StoreResponseHandle<object> handle)
        {
            var request = new StoreRequest
            {
                method = "save_level",
                path = path,
                data = data
            };
            yield return Post<object>(request, handle);
        }

        /// <summary>
        /// Fetch a specific revision of a level, the handler would receive a Level State instance
        /// </summary>
        /// <param name="path"></param>
        /// <param name="revision"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        public IEnumerator FetchLevel(string path, string revision, StoreResponseHandle<LevelState> handle)
        {
            // DateTimeOffset revision_offset = revision;
            var request = new StoreRequest
            {
                method = "fetch_level",
                path = path,
                // revision = revision_offset.ToUnixTimeSeconds().ToString()
                revision = revision
            };
            Console.Write(request);
            yield return Post<LevelState>(request, handle);
        }

        private IEnumerator Post<T>(StoreRequest req, StoreResponseHandle<T> handle)
        {
            if (endpoint == null)
            {
                throw new StoreEndpointNotSet();
            }

            var request = new UnityWebRequest(endpoint);
            var payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(req));

            request.uploadHandler = new UploadHandlerRaw(payload);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.method = UnityWebRequest.kHttpVerbPOST;            
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();


            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log(request.error);
                handle(new StoreResponse<T> {
                    status_code = (int)StoreResponseStatus.ServerError,
                    status_info = "server(" + endpoint + ") unavailable"
                });
            }
            else
            {

                Debug.Log(request.downloadHandler.text);
                var data = JsonUtility.FromJson<StoreResponse<T>>(request.downloadHandler.text);
                handle(in data);
            }
        }

    }

    public class StoreEndpointNotSet : Exception {}
}

