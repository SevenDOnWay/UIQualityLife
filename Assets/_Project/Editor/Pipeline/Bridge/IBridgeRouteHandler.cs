#if UNITY_EDITOR
using System.Net;

namespace UIQualityLife.Editor.Pipeline.Bridge
{
    /// <summary>
    /// Strategy interface for handling an individual HTTP endpoint in the UI Bridge Server.
    /// Follows the Open/Closed Principle (OCP).
    /// </summary>
    public interface IBridgeRouteHandler
    {
        /// <summary>HTTP method matched (e.g. "GET", "POST", "*").</summary>
        string Method { get; }

        /// <summary>Absolute path matched (e.g. "/api/status", "/api/build").</summary>
        string Path { get; }

        /// <summary>Whether this handler needs to execute on Unity's main thread.</summary>
        bool RequiresMainThread { get; }

        /// <summary>
        /// Handles the incoming HTTP request and returns an HTTP status code and response payload string.
        /// </summary>
        /// <param name="req">Incoming HTTP request.</param>
        /// <param name="body">Parsed request body string if POST/PUT.</param>
        /// <returns>Tuple of HTTP status code and JSON string response.</returns>
        (int statusCode, string responseJson) Handle(HttpListenerRequest req, string body);
    }
}
#endif
