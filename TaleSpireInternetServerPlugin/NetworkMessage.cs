using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaleSpireUnofficalPlugins
{
    public partial class InternetServerPlugin
    {
        [Serializable]
        public class NetworkMessage
        {
            public NetworkMessage()
            {
            }

            public NetworkMessage(string author, string content)
            {
                this.author = author;
                this.content = content;
                this.timestamp = DateTime.Now;
            }

            public NetworkMessage(DateTime timestamp, string author, string content)
            {
                this.author = author;
                this.content = content;
                this.timestamp = timestamp;
            }

            public DateTime timestamp { get; set; }
            public string author { get; set; }
            public string content { get; set; }
        }

        [Serializable]
        public class NetworkMessages
        {
            public string trans { get; set; }
            public NetworkMessage[] messages { get; set; }

            public NetworkMessages()
            {
            }

            public NetworkMessages(string trans, NetworkMessage[] messages)
            {
                this.trans = trans;
                this.messages = messages;
            }
        }
    }
}
