#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
        private static readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

        static UIEditorBridgeServer()
        {
            EnsureDirectories();
            StartServer();
            EditorApplication.update += OnEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;
        }

        private static void OnEditorUpdate()
        {
            while (mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UI Pipeline Bridge Error] {ex}");
                }
            }

            CheckFileQueue();
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
                    mainThreadActions.Enqueue(() =>
                    {
                        try
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
                        }
                        catch (Exception ex)
                        {
                            SendJsonResponse(res, 500, $"{{\"error\":\"{ex.Message}\"}}");
                        }
                    });
                    return;
                }

                if (path == "/api/build" && req.HttpMethod == "POST")
                {
                    string body;
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    mainThreadActions.Enqueue(() =>
                    {
                        try
                        {
                            var result = UIComponentBuilder.BuildFromJson(body);
                            string resJson = JsonUtility.ToJson(result);
                            SendJsonResponse(res, result.success ? 200 : 400, resJson);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[UI Bridge Build Error] {ex}");
                            SendJsonResponse(res, 500, $"{{\"success\":false,\"error\":\"{ex.Message}\"}}");
                        }
                    });
                    return;
                }

                if (path == "/api/build-theme" && req.HttpMethod == "POST")
                {
                    string body;
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    mainThreadActions.Enqueue(() =>
                    {
                        try
                        {
                            var result = UIStylingBuilder.BuildThemeFromJson(body);
                            string resJson = JsonUtility.ToJson(result);
                            SendJsonResponse(res, result.success ? 200 : 400, resJson);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[UI Bridge Theme Error] {ex}");
                            SendJsonResponse(res, 500, $"{{\"success\":false,\"error\":\"{ex.Message}\"}}");
                        }
                    });
                    return;
                }

                if (path == "/api/build-animation" && req.HttpMethod == "POST")
                {
                    string body;
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    mainThreadActions.Enqueue(() =>
                    {
                        try
                        {
                            var result = UIStylingBuilder.BuildAnimationFromJson(body);
                            string resJson = JsonUtility.ToJson(result);
                            SendJsonResponse(res, result.success ? 200 : 400, resJson);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[UI Bridge Animation Error] {ex}");
                            SendJsonResponse(res, 500, $"{{\"success\":false,\"error\":\"{ex.Message}\"}}");
                        }
                    });
                    return;
                }

                if (path == "/api/build-style" && req.HttpMethod == "POST")
                {
                    string body;
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    mainThreadActions.Enqueue(() =>
                    {
                        try
                        {
                            var result = UIStylingBuilder.BuildStyleFromJson(body);
                            string resJson = JsonUtility.ToJson(result);
                            SendJsonResponse(res, result.success ? 200 : 400, resJson);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[UI Bridge Style Error] {ex}");
                            SendJsonResponse(res, 500, $"{{\"success\":false,\"error\":\"{ex.Message}\"}}");
                        }
                    });
                    return;
                }

                if (path == "/api/spawn" && req.HttpMethod == "POST")
                {
                    string body;
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    mainThreadActions.Enqueue(() =>
                    {
                        try
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
                        }
                        catch (Exception ex)
                        {
                            SendJsonResponse(res, 500, $"{{\"success\":false,\"error\":\"{ex.Message}\"}}");
                        }
                    });
                    return;
                }

                if (path == "/api/inspect" && (req.HttpMethod == "GET" || req.HttpMethod == "POST"))
                {
                    string targetName = req.QueryString["name"];
                    if (string.IsNullOrEmpty(targetName) && req.HttpMethod == "POST")
                    {
                        using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                        {
                            targetName = reader.ReadToEnd()?.Trim().Trim('"');
                        }
                    }

                    mainThreadActions.Enqueue(() =>
                    {
                        try
                        {
                            GameObject target = null;
                            if (!string.IsNullOrEmpty(targetName))
                            {
                                target = GameObject.Find(targetName);
                            }
                            if (target == null)
                            {
                                var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                                if (canvas != null && canvas.transform.childCount > 0)
                                {
                                    target = canvas.transform.GetChild(canvas.transform.childCount - 1).gameObject;
                                }
                            }

                            if (target == null)
                            {
                                SendJsonResponse(res, 404, $"{{\"success\":false,\"error\":\"GameObject '{targetName}' not found in active scene.\"}}");
                                return;
                            }

                            string json = InspectGameObjectToJson(target);
                            SendJsonResponse(res, 200, json);
                        }
                        catch (Exception ex)
                        {
                            SendJsonResponse(res, 500, $"{{\"success\":false,\"error\":\"{ex.Message}\"}}");
                        }
                    });
                    return;
                }

                SendJsonResponse(res, 404, "{\"error\":\"Endpoint not found.\"}");
            }
            catch (Exception ex)
            {
                SendJsonResponse(res, 500, $"{{\"error\":\"{ex.Message}\"}}");
            }
        }

        private static string InspectGameObjectToJson(GameObject root)
        {
            var sb = new StringBuilder();
            SerializeNode(root, sb);
            return sb.ToString();

            void SerializeNode(GameObject go, StringBuilder builder)
            {
                builder.Append("{");
                builder.Append($"\"name\":\"{EscapeJson(go.name)}\",");

                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    var rect = rt.rect;
                    builder.Append($"\"width\":{rect.width:F1},\"height\":{rect.height:F1},");
                    builder.Append($"\"anchoredPosition\":[{rt.anchoredPosition.x:F1},{rt.anchoredPosition.y:F1}],");
                    builder.Append($"\"anchorMin\":[{rt.anchorMin.x:F2},{rt.anchorMin.y:F2}],");
                    builder.Append($"\"anchorMax\":[{rt.anchorMax.x:F2},{rt.anchorMax.y:F2}],");
                }

                var tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    builder.Append($"\"text\":\"{EscapeJson(tmp.text)}\",");
                    builder.Append($"\"fontSize\":{tmp.fontSize:F1},");
                    builder.Append($"\"alignment\":\"{tmp.alignment}\",");
                }

                var lg = go.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (lg != null)
                {
                    builder.Append($"\"layoutType\":\"{lg.GetType().Name}\",");
                    builder.Append($"\"childControlWidth\":{lg.childControlWidth.ToString().ToLower()},");
                    builder.Append($"\"childForceExpandWidth\":{lg.childForceExpandWidth.ToString().ToLower()},");
                }

                var le = go.GetComponent<LayoutElement>();
                if (le != null)
                {
                    builder.Append($"\"flexibleWidth\":{le.flexibleWidth:F1},");
                    builder.Append($"\"preferredWidth\":{le.preferredWidth:F1},");
                }

                var components = go.GetComponents<Component>();
                builder.Append("\"components\":[");
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] != null)
                    {
                        builder.Append($"\"{components[i].GetType().Name}\"");
                        if (i < components.Length - 1) builder.Append(",");
                    }
                }
                builder.Append("],");

                builder.Append("\"children\":[");
                int childCount = go.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    SerializeNode(go.transform.GetChild(i).gameObject, builder);
                    if (i < childCount - 1) builder.Append(",");
                }
                builder.Append("]}");
            }

            string EscapeJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") ?? "";
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
                    string fileName = Path.GetFileName(file);
                    string resultJson = "";

                    if (fileName.StartsWith("theme_", StringComparison.OrdinalIgnoreCase))
                    {
                        var res = UIStylingBuilder.BuildThemeFromJson(json);
                        resultJson = JsonUtility.ToJson(res);
                    }
                    else if (fileName.StartsWith("anim_", StringComparison.OrdinalIgnoreCase))
                    {
                        var res = UIStylingBuilder.BuildAnimationFromJson(json);
                        resultJson = JsonUtility.ToJson(res);
                    }
                    else if (fileName.StartsWith("style_", StringComparison.OrdinalIgnoreCase))
                    {
                        var res = UIStylingBuilder.BuildStyleFromJson(json);
                        resultJson = JsonUtility.ToJson(res);
                    }
                    else
                    {
                        var res = UIComponentBuilder.BuildFromJson(json);
                        resultJson = JsonUtility.ToJson(res);
                    }

                    string dest = $"{FinishedFolder}/{fileName}";
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(file, dest);

                    string resultFile = $"{FinishedFolder}/{Path.GetFileNameWithoutExtension(file)}_result.json";
                    File.WriteAllText(resultFile, resultJson);
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
