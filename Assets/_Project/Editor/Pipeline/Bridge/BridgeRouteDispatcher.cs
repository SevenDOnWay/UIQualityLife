#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UIQualityLife.Editor.Configurators;

namespace UIQualityLife.Editor.Pipeline.Bridge
{
    /// <summary>
    /// Modular HTTP route dispatcher for the UI Editor Bridge Server.
    /// Manages route registration, request routing, thread marshaling, and error handling.
    /// Follows the Open/Closed Principle (OCP) and Single Responsibility Principle (SRP).
    /// </summary>
    public static class BridgeRouteDispatcher
    {
        private static readonly List<IBridgeRouteHandler> s_Handlers = new List<IBridgeRouteHandler>();

        static BridgeRouteDispatcher()
        {
            RegisterHandler(new StatusRouteHandler());
            RegisterHandler(new PresetsRouteHandler());
            RegisterHandler(new BuildComponentRouteHandler());
            RegisterHandler(new BuildThemeRouteHandler());
            RegisterHandler(new BuildAnimationRouteHandler());
            RegisterHandler(new BuildStyleRouteHandler());
            RegisterHandler(new SpawnComponentRouteHandler());
            RegisterHandler(new InspectRouteHandler());
        }

        /// <summary>
        /// Registers a new route handler into the dispatcher.
        /// </summary>
        /// <param name="handler">Handler implementation.</param>
        public static void RegisterHandler(IBridgeRouteHandler handler)
        {
            if (handler != null && !s_Handlers.Contains(handler))
            {
                s_Handlers.Add(handler);
            }
        }

        /// <summary>
        /// Dispatches an incoming HTTP request to matching route handler.
        /// </summary>
        /// <param name="context">Incoming HttpListenerContext.</param>
        /// <param name="enqueueMainThread">Action to schedule a delegate on Unity's main thread.</param>
        public static void Dispatch(HttpListenerContext context, Action<Action> enqueueMainThread)
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
            string method = req.HttpMethod.ToUpperInvariant();

            IBridgeRouteHandler matched = null;
            for (int i = 0; i < s_Handlers.Count; i++)
            {
                var h = s_Handlers[i];
                bool pathMatch = string.Equals(h.Path, path, StringComparison.OrdinalIgnoreCase);
                bool methodMatch = h.Method == "*" || string.Equals(h.Method, method, StringComparison.OrdinalIgnoreCase);
                if (pathMatch && methodMatch)
                {
                    matched = h;
                    break;
                }
            }

            if (matched == null)
            {
                SendJsonResponse(res, 404, "{\"error\":\"Endpoint not found.\"}");
                return;
            }

            // Read body if method has body
            string body = "";
            if (req.HasEntityBody)
            {
                using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                {
                    body = reader.ReadToEnd();
                }
            }

            if (matched.RequiresMainThread)
            {
                enqueueMainThread(() =>
                {
                    try
                    {
                        var (statusCode, json) = matched.Handle(req, body);
                        SendJsonResponse(res, statusCode, json);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UI Bridge Dispatch Error] {ex}");
                        SendJsonResponse(res, 500, $"{{\"success\":false,\"error\":\"{EscapeJson(ex.Message)}\"}}");
                    }
                });
            }
            else
            {
                try
                {
                    var (statusCode, json) = matched.Handle(req, body);
                    SendJsonResponse(res, statusCode, json);
                }
                catch (Exception ex)
                {
                    SendJsonResponse(res, 500, $"{{\"error\":\"{EscapeJson(ex.Message)}\"}}");
                }
            }
        }

        public static void SendJsonResponse(HttpListenerResponse res, int statusCode, string json)
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

        public static string EscapeJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") ?? "";
    }

    #region Default Route Handlers

    internal class StatusRouteHandler : IBridgeRouteHandler
    {
        public string Method => "GET";
        public string Path => "/api/status";
        public bool RequiresMainThread => false;

        public (int statusCode, string responseJson) Handle(HttpListenerRequest req, string body)
        {
            return (200, "{\"status\":\"running\",\"engine\":\"Unity UGUI\",\"port\":" + UIQualityLifeSettings.BridgePort + "}");
        }
    }

    internal class PresetsRouteHandler : IBridgeRouteHandler
    {
        public string Method => "GET";
        public string Path => "/api/presets";
        public bool RequiresMainThread => true;

        public (int statusCode, string responseJson) Handle(HttpListenerRequest req, string body)
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
                sb.Append($"\"description\":\"{BridgeRouteDispatcher.EscapeJson(p.Description)}\"");
                sb.Append("}");
                if (i < presets.Count - 1) sb.Append(",");
            }
            sb.Append("]}");
            return (200, sb.ToString());
        }
    }

    internal class BuildComponentRouteHandler : IBridgeRouteHandler
    {
        public string Method => "POST";
        public string Path => "/api/build";
        public bool RequiresMainThread => true;

        public (int statusCode, string responseJson) Handle(HttpListenerRequest req, string body)
        {
            var result = UIComponentBuilder.BuildFromJson(body);
            string resJson = JsonUtility.ToJson(result);
            return (result.success ? 200 : 400, resJson);
        }
    }

    internal class BuildThemeRouteHandler : IBridgeRouteHandler
    {
        public string Method => "POST";
        public string Path => "/api/build-theme";
        public bool RequiresMainThread => true;

        public (int statusCode, string responseJson) Handle(HttpListenerRequest req, string body)
        {
            var result = UIStylingBuilder.BuildThemeFromJson(body);
            string resJson = JsonUtility.ToJson(result);
            return (result.success ? 200 : 400, resJson);
        }
    }

    internal class BuildAnimationRouteHandler : IBridgeRouteHandler
    {
        public string Method => "POST";
        public string Path => "/api/build-animation";
        public bool RequiresMainThread => true;

        public (int statusCode, string responseJson) Handle(HttpListenerRequest req, string body)
        {
            var result = UIStylingBuilder.BuildAnimationFromJson(body);
            string resJson = JsonUtility.ToJson(result);
            return (result.success ? 200 : 400, resJson);
        }
    }

    internal class BuildStyleRouteHandler : IBridgeRouteHandler
    {
        public string Method => "POST";
        public string Path => "/api/build-style";
        public bool RequiresMainThread => true;

        public (int statusCode, string responseJson) Handle(HttpListenerRequest req, string body)
        {
            var result = UIStylingBuilder.BuildStyleFromJson(body);
            string resJson = JsonUtility.ToJson(result);
            return (result.success ? 200 : 400, resJson);
        }
    }

    internal class SpawnComponentRouteHandler : IBridgeRouteHandler
    {
        public string Method => "POST";
        public string Path => "/api/spawn";
        public bool RequiresMainThread => true;

        public (int statusCode, string responseJson) Handle(HttpListenerRequest req, string body)
        {
            string presetName = body.Trim().Trim('"');
            var presets = UIPresetDatabase.GetPresets();
            var target = presets.Find(p => string.Equals(p.DisplayName, presetName, StringComparison.OrdinalIgnoreCase));

            if (target != null)
            {
                var spawned = SceneSpawner.SpawnPreset(target);
                return (200, $"{{\"success\":true,\"name\":\"{spawned.name}\"}}");
            }

            return (404, $"{{\"success\":false,\"error\":\"Preset '{presetName}' not found.\"}}");
        }
    }

    internal class InspectRouteHandler : IBridgeRouteHandler
    {
        public string Method => "*";
        public string Path => "/api/inspect";
        public bool RequiresMainThread => true;

        public (int statusCode, string responseJson) Handle(HttpListenerRequest req, string body)
        {
            string targetName = req.QueryString["name"];
            if (string.IsNullOrEmpty(targetName) && !string.IsNullOrEmpty(body))
            {
                targetName = body.Trim().Trim('"');
            }

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
                return (404, $"{{\"success\":false,\"error\":\"GameObject '{targetName}' not found in active scene.\"}}");
            }

            string json = InspectGameObjectToJson(target);
            return (200, json);
        }

        private static string InspectGameObjectToJson(GameObject root)
        {
            var sb = new StringBuilder();
            SerializeNode(root, sb, 0);
            return sb.ToString();

            void SerializeNode(GameObject go, StringBuilder builder, int depth)
            {
                builder.Append("{");
                builder.Append($"\"name\":\"{BridgeRouteDispatcher.EscapeJson(go.name)}\",");
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
                    builder.Append($"\"text\":\"{BridgeRouteDispatcher.EscapeJson(tmp.text)}\",");
                    builder.Append($"\"fontSize\":{tmp.fontSize:F1},");
                    builder.Append($"\"alignment\":\"{tmp.alignment}\",");
                    builder.Append($"\"wrapping\":\"{tmp.textWrappingMode}\",");
                    builder.Append($"\"overflow\":\"{tmp.overflowMode}\",");
                    builder.Append($"\"preferredWidth\":{tmp.preferredWidth:F1},");
                    builder.Append($"\"preferredHeight\":{tmp.preferredHeight:F1},");
                    bool isVerticalDistorted = rt != null && rt.rect.width < 35f && tmp.preferredWidth > 40f && tmp.text.Length > 2;
                    builder.Append($"\"isVerticalDistorted\":{isVerticalDistorted.ToString().ToLower()},");
                    builder.Append($"\"color\":\"#{ColorUtility.ToHtmlStringRGBA(tmp.color)}\"");

                    var ancestorBg = NodeFormattingUtils.FindAncestorBackgroundColor(go.transform);
                    if (ancestorBg.HasValue)
                    {
                        float ratio = NodeFormattingUtils.CalculateContrastRatio(tmp.color, ancestorBg.Value);
                        builder.Append($",\"contrastRatio\":{ratio.ToString("F2")},");
                        builder.Append($"\"hasLowContrast\":{(ratio < 3.5f).ToString().ToLower()}");
                    }
                    builder.Append("},");
                }

                var img = go.GetComponent<Image>();
                if (img != null)
                {
                    builder.Append("\"imageInfo\":{");
                    builder.Append($"\"color\":\"#{ColorUtility.ToHtmlStringRGBA(img.color)}\",");
                    builder.Append($"\"type\":\"{img.type}\",");
                    builder.Append($"\"sprite\":\"{BridgeRouteDispatcher.EscapeJson(img.sprite != null ? img.sprite.name : "null")}\"");
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
                    builder.Append($"\"styleClasses\":\"{BridgeRouteDispatcher.EscapeJson(binder.ClassNames)}\",");
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
        }
    }

    #endregion
}
#endif
