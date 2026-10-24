#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Editor.Pipeline.Bridge;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Background local HTTP bridge and file-queue listener running inside Unity Editor.
    /// Allows the CLI, external scripts, MCP servers, and autonomous agents to communicate
    /// directly with the running Unity Editor to build, spawn, and inspect UI components.
    /// Delegates request routing to <see cref="BridgeRouteDispatcher"/>.
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

        /// <summary>
        /// Starts the local HTTP bridge server listening on the specified port.
        /// </summary>
        /// <param name="port">Port number (defaults to UIQualityLifeSettings.BridgePort).</param>
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

        /// <summary>
        /// Stops the local HTTP bridge server.
        /// </summary>
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
            BridgeRouteDispatcher.Dispatch(context, action => mainThreadActions.Enqueue(action));
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
