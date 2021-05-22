using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TaleSpireUnofficalPlugins
{
    public partial class InternetServerPlugin
    {
        public class MessageClient
        {
            public enum DiagnosticModes
            {
                off = 0,
                basic,
                full
            }

            public DiagnosticModes diagnosticMode { get; set; } = DiagnosticModes.off;

            private WebClient client = null;
            private string url_base = "";
            private string session = "";
            private string userName = "";
            // Last read transaction. Used in get request to obtain only new mesages
            private string trans = "0"; 
            private bool ignoreOwn = true;
            private bool isFirst = true;
            // Only one request per web cleint can be active at once so we use this boolean to determine if the last request is completed
            private bool ready = true; 
            private ConcurrentQueue<string> requests = new ConcurrentQueue<string>();

            /// <summary>
            /// Starts checking for messages on the provided Internet Server
            /// </summary>
            /// <param name="url">Base url of the Internet Server used for messaging</param>
            /// <param name="session">Unique session identifying messages for the same purpose (e.g. CampainId.BoardId.CustomMiniMessages)</param>
            /// <param name="user">Unique user identification allowing the user to ignore own messages</param>
            /// <param name="callback">Method to handle incoming messages</param>
            public void Connect(string url, string session, string user, Action<NetworkMessage[]> callback, bool ignoreFirst = false, bool ignoreOwn = true)
            {
                // Create new client for communication
                client = new WebClient();
                // Build urls for getting messages, posting messages and requesting a clean
                this.isFirst = true;
                this.url_base = url;
                this.session = session;
                this.userName = user;
                // Subscribe to client completed event for determining when async messages were received
                client.DownloadStringCompleted += (s, e) =>
                {
                    if (diagnosticMode!=DiagnosticModes.off) { UnityEngine.Debug.Log("InternetServerPlugin: Received Response"); }
                    // Validate response
                    if (e.Result != null)
                    {
                        if (diagnosticMode != DiagnosticModes.off) { UnityEngine.Debug.Log("InternetServerPlugin: Received Non-Null Response"); }
                        if (e.Result.IndexOf("{") > -1)
                        {
                            if (diagnosticMode == DiagnosticModes.full) { UnityEngine.Debug.Log("InternetServerPlugin: " + e.Result); }
                            NetworkMessages response = JsonConvert.DeserializeObject<NetworkMessages>(e.Result);
                            // Check that response has a pipe character (all get requests will have at a minimum the transaction id followed by pipe) 
                            trans = response.trans;
                            if (diagnosticMode != DiagnosticModes.off) { UnityEngine.Debug.Log("InternetServerPlugin: Trans updated to " + trans); }
                            if (response.messages.Count() > 0)
                            {
                                if (diagnosticMode != DiagnosticModes.off) { UnityEngine.Debug.Log("InternetServerPlugin: Have " + response.messages.Count() + " Messages (First Pass:" + isFirst + " | IgnoreFirst: " + ignoreFirst + ")"); }
                                // Ignore first set of results if ignoreFirst is true
                                if (isFirst == false || ignoreFirst == false)
                                {
                                    if (diagnosticMode != DiagnosticModes.off) { UnityEngine.Debug.Log("InternetServerPlugin: Pushing " + response.messages.Count() + " Messages To Callback"); }
                                    foreach(NetworkMessage msg in response.messages)
                                    {
                                        msg.content = HttpUtility.UrlDecode(msg.content);
                                    }
                                    callback(response.messages);
                                }
                            }
                            isFirst = false;
                        }
                    }
                    // Indicated that the client is ready for next request
                    ready = true;
                    if (diagnosticMode!=DiagnosticModes.off) { UnityEngine.Debug.Log("InternetServerPlugin: Client Ready"); }
                    // Process queued requests if any
                    SendContent();
                };
                // Try to get initial messages
                if (diagnosticMode!=DiagnosticModes.off) { UnityEngine.Debug.Log("InternetServerPlugin: Getting Initial Messages"); }
                TryGetMessage();
                // Queue a database clean on initial connection
                if (diagnosticMode!=DiagnosticModes.off) { UnityEngine.Debug.Log("InternetServerPlugin: Queuing Clean Request"); }
                QueueClean();
                // Process queue
                if (diagnosticMode!=DiagnosticModes.off) { UnityEngine.Debug.Log("InternetServerPlugin: Processing Queued Content"); }
                SendContent();
            }

            /// <summary>
            /// Method for reading messages from the server. If the client is processing an request then this method does nothing and returns false.
            /// This method intentionally does not queue the request since it will be typically called in the Update() method which is likely to
            /// execute many times while messages are being read. Thus queuing such request would lead to an overflow.
            /// </summary>
            /// <returns>Boolean indicating if the request is being processed (true) or if the request was dropped (false)</returns>
            public bool TryGetMessage()
            {
                if (ready && requests.Count == 0)
                {
                    string exclusion = (ignoreOwn) ? "&user=" + userName : "";
                    string address = url_base + "?session=" + session + exclusion + "&trans=" + trans;
                    requests.Enqueue(address);
                    return (SendContent() && (requests.Count == 1));
                }
                return false;
            }

            /// <summary>
            /// Method for posting messages to the Internet Server. If the client is busy the message is queued and processed when the client
            /// finishes any outstanding requests.
            /// </summary>
            /// <param name="message">Message to be sent</param>
            /// <returns>Boolean indicating if the request is being processed immediately (true) or if the request was queued (false)</returns>
            public bool QueueMessage(NetworkMessage message)
            {
                string address = url_base + "?session=" + session + "&user=" + message.author + "&content=" + System.Web.HttpUtility.UrlEncode(message.content);
                requests.Enqueue(address);
                return (SendContent() && (requests.Count == 1));
            }

            /// <summary>
            /// Method for posting messages to the Internet Server. If the client is busy the message is queued and processed when the client
            /// finishes any outstanding requests.
            /// </summary>
            /// <param name="message">Message to be sent</param>
            /// <returns>Boolean indicating if the request is being processed immediately (true) or if the request was queued (false)</returns>
            public bool QueueMessage(string message)
            {
                string address = url_base + "?session=" + session + "&user=" + userName + "&content=" + System.Web.HttpUtility.UrlEncode(message);
                requests.Enqueue(address);
                return (SendContent() && (requests.Count == 1));
            }

            /// <summary>
            /// Method for posting messages to the Internet Server. If the client is busy the message is queued and processed when the client
            /// finishes any outstanding requests.
            /// </summary>
            /// <param name="message">Message to be sent</param>
            /// <returns>Boolean indicating if the request is being processed immediately (true) or if the request was queued (false)</returns>
            public bool QueueMessages(NetworkMessage[] messages)
            {
                foreach (NetworkMessage message in messages)
                {
                    string address = url_base + "?session=" + session + "&user=" + message.author + "&content=" + System.Web.HttpUtility.UrlEncode(message.content);
                    requests.Enqueue(address);
                }
                return (SendContent() && (requests.Count == 1));
            }

            /// <summary>
            /// Method for posting messages to the Internet Server. If the client is busy the message is queued and processed when the client
            /// finishes any outstanding requests.
            /// </summary>
            /// <param name="message">Message to be sent</param>
            /// <returns>Boolean indicating if the request is being processed immediately (true) or if the request was queued (false)</returns>
            public bool QueueMessages(string[] messages)
            {
                foreach (string message in messages)
                {
                    string address = url_base + "?session=" + session + "&user=" + userName + "&content=" + System.Web.HttpUtility.UrlEncode(message);
                    requests.Enqueue(address);
                }
                return (SendContent() && (requests.Count == 1));
            }

            /// <summary>
            /// Method to queue a datamase maintenance clean 
            /// </summary>
            /// <returns>Boolean indicating if the request is being processed immediately (true) or if the request was queued (false)</returns>
            public bool QueueClean()
            {
                requests.Enqueue(url_base);
                return (SendContent() && (requests.Count == 1));
            }

            /// <summary>
            /// Method to stop checking for messages
            /// </summary>
            public void Disconnect()
            {
                client.Dispose();
            }

            /// <summary>
            /// Private method for carrying out queued requests
            /// </summary>
            /// <returns></returns>
            private bool SendContent()
            {
                if (client == null) { return false; }
                if (ready && (requests.Count > 0))
                {
                    ready = false;
                    string message = "";
                    if (requests.TryDequeue(out message))
                    {
                        if (diagnosticMode!=DiagnosticModes.off) { UnityEngine.Debug.Log("InternetServerPlugin: " + message); }
                        client.DownloadStringAsync(new Uri(message));
                        return true;
                    }
                }
                return false;
            }
        }
    }
}