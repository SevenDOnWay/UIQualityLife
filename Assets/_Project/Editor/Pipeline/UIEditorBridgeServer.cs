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
        public static int DefaultPort => UIQualityLifeSettings.BridgePort;
        public static string QueueFolder => UIQualityLifeSettings.QueueFolder;
        public static string FinishedFolder => UIQualityLifeSettings.FinishedFolder;

        private static HttpListener listener;
        private static Thread listenerThread;
        private static bool isRunning;
        private static readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

        static UIEditorBridgeServer()
        {
            EnsureDirectories();
            if (UIQualityLifeSettings.AutoStartBridge)
            {
                StartServer(DefaultPort);
            }
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

        public static void StartServer(int port = -1)
        {
            if (port <= 0) port = DefaultPort;
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
            SerializeNode(root, sb, 0);
            return sb.ToString();

            void SerializeNode(GameObject go, StringBuilder builder, int depth)
            {
                builder.Append("{");
                builder.Append($"\"name\":\"{EscapeJson(go.name)}\",");
                builder.Append($"\"depth\":{depth},");
                builder.Append($"\"siblingIndex\":{go.transform.GetSiblingIndex()},");
                builder.Append($"\"layer\":\"{LayerMask.LayerToName(go.layer)}\",");
                builder.Append($"\"layerId\":{go.layer},");
                builder.Append($"\"active\":{go.activeSelf.ToString().ToLower()},");

                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    var rect = rt.rect;
                    builder.Append($"\"rect\":{{\"x\":{rect.x.ToString("F1")},\"y\":{rect.y.ToString("F1")},\"width\":{rect.width.ToString("F1")},\"height\":{rect.height.ToString("F1")}}},");
                    builder.Append($"\"sizeDelta\":[{rt.sizeDelta.x.ToString("F1")},{rt.sizeDelta.y.ToString("F1")}],");
                    builder.Append($"\"anchoredPosition\":[{rt.anchoredPosition.x.ToString("F1")},{rt.anchoredPosition.y.ToString("F1")}],");
                    builder.Append($"\"anchorMin\":[{rt.anchorMin.x.ToString("F2")},{rt.anchorMin.y.ToString("F2")}],");
                    builder.Append($"\"anchorMax\":[{rt.anchorMax.x.ToString("F2")},{rt.anchorMax.y.ToString("F2")}],");
                    builder.Append($"\"pivot\":[{rt.pivot.x.ToString("F2")},{rt.pivot.y.ToString("F2")}],");
                    builder.Append($"\"offsetMin\":[{rt.offsetMin.x.ToString("F1")},{rt.offsetMin.y.ToString("F1")}],");
                    builder.Append($"\"offsetMax\":[{rt.offsetMax.x.ToString("F1")},{rt.offsetMax.y.ToString("F1")}],");
                }

                var tmp = go.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    builder.Append("\"textInfo\":{");
                    builder.Append($"\"text\":\"{EscapeJson(tmp.text)}\",");
                    builder.Append($"\"fontSize\":{tmp.fontSize:F1},");
                    builder.Append($"\"alignment\":\"{tmp.alignment}\",");
                    builder.Append($"\"wrapping\":\"{tmp.textWrappingMode}\",");
                    builder.Append($"\"overflow\":\"{tmp.overflowMode}\",");
                    builder.Append($"\"preferredWidth\":{tmp.preferredWidth:F1},");
                    builder.Append($"\"preferredHeight\":{tmp.preferredHeight:F1},");
                    bool isVerticalDistorted = rt != null && rt.rect.width < 35f && tmp.preferredWidth > 40f && tmp.text.Length > 2;
                    builder.Append($"\"isVerticalDistorted\":{isVerticalDistorted.ToString().ToLower()},");
                    builder.Append($"\"color\":\"#{ColorUtility.ToHtmlStringRGBA(tmp.color)}\"");
                    builder.Append("},");
                }

                var img = go.GetComponent<Image>();
                if (img != null)
                {
                    builder.Append("\"imageInfo\":{");
                    builder.Append($"\"color\":\"#{ColorUtility.ToHtmlStringRGBA(img.color)}\",");
                    builder.Append($"\"type\":\"{img.type}\",");
                    builder.Append($"\"sprite\":\"{EscapeJson(img.sprite != null ? img.sprite.name : "null")}\"");
                    builder.Append("},");
                }

                var lg = go.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (lg != null)
                {
                    builder.Append("\"layoutGroup\":{");
                    builder.Append($"\"type\":\"{lg.GetType().Name}\",");
                    builder.Append($"\"spacing\":{lg.spacing:F1},");
                    builder.Append($"\"padding\":{{\"left\":{lg.padding.left},\"right\":{lg.padding.right},\"top\":{lg.padding.top},\"bottom\":{lg.padding.bottom}}},");
                    builder.Append($"\"childControlWidth\":{lg.childControlWidth.ToString().ToLower()},");
                    builder.Append($"\"childControlHeight\":{lg.childControlHeight.ToString().ToLower()},");
                    builder.Append($"\"childForceExpandWidth\":{lg.childForceExpandWidth.ToString().ToLower()},");
                    builder.Append($"\"childForceExpandHeight\":{lg.childForceExpandHeight.ToString().ToLower()},");
                    builder.Append($"\"childAlignment\":\"{lg.childAlignment}\"");
                    builder.Append("},");
                }

                var le = go.GetComponent<LayoutElement>();
                if (le != null)
                {
                    builder.Append("\"layoutElement\":{");
                    builder.Append($"\"minWidth\":{le.minWidth:F1},\"minHeight\":{le.minHeight:F1},");
                    builder.Append($"\"preferredWidth\":{le.preferredWidth:F1},\"preferredHeight\":{le.preferredHeight:F1},");
                    builder.Append($"\"flexibleWidth\":{le.flexibleWidth:F1},\"flexibleHeight\":{le.flexibleHeight:F1},");
                    builder.Append($"\"ignoreLayout\":{le.ignoreLayout.ToString().ToLower()}");
                    builder.Append("},");
                }

                var binder = go.GetComponent<UIQualityLife.Styling.UIStyleClassBinder>();
                if (binder != null)
                {
                    builder.Append($"\"styleClasses\":\"{EscapeJson(binder.ClassNames)}\",");
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
                    SerializeNode(go.transform.GetChild(i).gameObject, builder, depth + 1);
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
