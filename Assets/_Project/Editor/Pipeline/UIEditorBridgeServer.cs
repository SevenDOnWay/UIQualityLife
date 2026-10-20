#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Background local HTTP bridge and file-queue listener running inside Unity Editor.
    /// Allows the CLI, external scripts, MCP servers, and autonomous agents to communicate
    /// directly with the running Unity Editor to build, spawn, and inspect UI components.
    /// </summary>
    [InitializeOnLoad]
    public static class UIEditorBridgeServer
    {
        public const int DefaultPort = 8765;
        public const string QueueFolder = "Assets/_Project/Pipeline/Queue";
        public const string FinishedFolder = "Assets/_Project/Pipeline/Finished";

        private static HttpListener listener;
        private static Thread listenerThread;
        private static bool isRunning;

        static UIEditorBridgeServer()
        {
            EnsureDirectories();
            StartServer();
            EditorApplication.update += CheckFileQueue;
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;
        }

        public static void StartServer(int port = DefaultPort)
        {
            if (isRunning) return;

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                listener.Start();

                isRunning = true;
                listenerThread = new Thread(ListenLoop) { IsBackground = true };
                listenerThread.Start();

                Debug.Log($"[UI Pipeline Bridge] Server listening on http://127.0.0.1:{port}/");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UI Pipeline Bridge] Could not start HTTP listener on port {port} ({ex.Message}). File-queue fallback will remain active.");
                isRunning = false;
            }
        }

        public static void StopServer()
        {
            if (!isRunning) return;
            isRunning = false;

            try
            {
                listener?.Stop();
                listener?.Close();
            }
            catch { }

            listener = null;
        }

        private static void ListenLoop()
        {
            while (isRunning && listener != null && listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => ProcessRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UI Pipeline Bridge] Listener error: {ex.Message}");
                }
            }
        }

        private static void ProcessRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            // CORS headers for local tools
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 200;
                res.Close();
                return;
            }

            string path = req.Url.AbsolutePath.ToLowerInvariant();

            try
            {
                if (path == "/api/status" && req.HttpMethod == "GET")
                {
                    SendJsonResponse(res, 200, "{\"status\":\"running\",\"engine\":\"Unity UGUI\",\"port\":" + DefaultPort + "}");
                    return;
                }

                if (path == "/api/presets" && req.HttpMethod == "GET")
                {
                    EditorApplication.delayCall += () =>
                    {
                        var presets = UIPresetDatabase.GetPresets();
                        var sb = new StringBuilder();
                        sb.Append("{\"presets\":[");
                        for (int i = 0; i < presets.Count; i++)
                        {
                            var p = presets[i];
                            sb.Append("{");
                            sb.Append($"\"name\":\"{p.DisplayName}\",");
                            sb.Append($"\"category\":\"{p.PrimaryCategory}\",");
                            sb.Append($"\"description\":\"{p.Description}\"");
                            sb.Append("}");
                            if (i < presets.Count - 1) sb.Append(",");
                        }
                        sb.Append("]}");
                        SendJsonResponse(res, 200, sb.ToString());
                    };
                    return;
                }

                if (path == "/api/build" && req.HttpMethod == "POST")
                {
                    string body;
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    // Execute building on Unity Main Thread
                    EditorApplication.delayCall += () =>
                    {
                        var result = UIComponentBuilder.BuildFromJson(body);
                        string resJson = JsonUtility.ToJson(result);
                        SendJsonResponse(res, result.success ? 200 : 400, resJson);
                    };
                    return;
                }

                if (path == "/api/spawn" && req.HttpMethod == "POST")
                {
                    string body;
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    EditorApplication.delayCall += () =>
                    {
                        string presetName = body.Trim().Trim('"');
                        var presets = UIPresetDatabase.GetPresets();
                        var target = presets.Find(p => string.Equals(p.DisplayName, presetName, StringComparison.OrdinalIgnoreCase));

                        if (target != null)
                        {
                            var spawned = SceneSpawner.SpawnPreset(target);
                            SendJsonResponse(res, 200, $"{{\"success\":true,\"name\":\"{spawned.name}\"}}");
                        }
                        else
                        {
                            SendJsonResponse(res, 404, $"{{\"success\":false,\"error\":\"Preset '{presetName}' not found.\"}}");
                        }
                    };
                    return;
                }

                SendJsonResponse(res, 404, "{\"error\":\"Endpoint not found.\"}");
            }
            catch (Exception ex)
            {
                SendJsonResponse(res, 500, $"{{\"error\":\"{ex.Message}\"}}");
            }
        }

        private static void SendJsonResponse(HttpListenerResponse res, int statusCode, string json)
        {
            try
            {
                res.StatusCode = statusCode;
                res.ContentType = "application/json; charset=utf-8";
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                res.ContentLength64 = buffer.Length;
                res.OutputStream.Write(buffer, 0, buffer.Length);
                res.OutputStream.Flush();
                res.Close();
            }
            catch { }
        }

        // =========================================================================
        // FILE QUEUE FALLBACK WATCHER
        // =========================================================================

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(QueueFolder)) Directory.CreateDirectory(QueueFolder);
            if (!Directory.Exists(FinishedFolder)) Directory.CreateDirectory(FinishedFolder);
        }

        private static void CheckFileQueue()
        {
            if (!Directory.Exists(QueueFolder)) return;

            string[] queueFiles = Directory.GetFiles(QueueFolder, "*.json");
            if (queueFiles.Length == 0) return;

            foreach (var file in queueFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var result = UIComponentBuilder.BuildFromJson(json);

                    string fileName = Path.GetFileName(file);
                    string dest = $"{FinishedFolder}/{fileName}";
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(file, dest);

                    string resultFile = $"{FinishedFolder}/{Path.GetFileNameWithoutExtension(file)}_result.json";
                    File.WriteAllText(resultFile, JsonUtility.ToJson(result));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UI Pipeline Queue] Failed processing queue file '{file}': {ex.Message}");
                }
            }
        }
    }
}
#endif
