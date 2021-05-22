# TaleSpire-InternetServerPlugin
Provides messaging between TaleSpire clients including support for historical messages

## Usage

The Internet Server Plugin is intended to be used as a dependency for other plugins. To use it as part of your own plugins, add a reference to the DLL into your Visual Studio project and then add the following to your plugin header:

```C#
    [BepInPlugin(Guid, "My Plugin That Uses The Internet Server Plugin", Version)]
    **[BepInDependency(TaleSpireUnofficalPlugins.InternetServerPlugin.Guid)]**
    public class MyCoolPlugin : BaseUnityPlugin
    {
        private const string Guid = "org.demo.plugins.mycooldemo";
        private const string Version = "1.1.0.0";
        
        void Awake()
        {
        }
        
        void Update()
        {
        }
    }
```

This will give access to the InternetServerPlugin namespace at runtime and ensure that only one InternetServerPlugin instance handles all plugins that need it.

To be able to get or send messages, you create a MessageClient. Typically this can be done in the Awake() function but specific situations may dictated otherwise. This cna be done using:

```C#
    void Awake()
    {
        client = new TaleSpireUnofficalPlugins.InternetServerPlugin.MessageClient();
        client.diagnosticMode = InternetServerPlugin.MessageClient.DiagnosticModes.full;
    }
```

The second line is optional. It sets the diagnostic more. If odd, the plugin does not write processing messages to the console. If set to basic, the plugin writes processing messages to the console. If set to full, the plugin writes processing messages as well as message content to the console. Typically the should be set to off for published plugins and only set to basic or full when testing.

To start message collection we connect, using code such as:

```C#
   string url = "http://talespire.mygamesonline.org/MessageServerSQL.php";
   string session = CampaignSessionManager.Id + "." + BoardSessionManager.CurrentBoardInfo.Id + ".InternetServerDemoPlugin";
   string user = CampaignSessionManager.GetPlayerName(LocalPlayer.Id);
   client.Connect(url, session, user, ReceivedMessages, false, false);
```

The url is the location and page of the Internet Server. The url in the code above is a test server that can be used to test plugins. Once a plugin is published, it is suggested that a dedicated server be created to host the its messages. The code for such a server is available in at: Link TDB

The session is a unique identification for a set of related messages. The session ensures that the server only provides the messages that the plugin is interested in even though the same server handles can messages from many groups and potential for many different plugins. The code above suggest using a concatination of the campaign id, board id and the plugin function. This means different groups using the same plugin will not receive each other group's messages (since campaign and board will separate them) and the appending of the purpose means that different plugins all for the same campaign and board will not interfear with each other. Depending on the plugin the board portion may not be applicable (e.g. if the messages are for the campaign and are regardless of the current board).

The user is a unique identification for the user. It is used to filter out the user's own messages if the ignoreOwn proeprty is true.

The ReceivedMessages is a callback function which takes NetworkMessage[]. It is called when messages arrive from the Internet Server.

The following parameter is ignoreFirst. When true all historical messages read from the Internet Server are ignored and only new messages are pused to the callback. When this property is false, all historical messages obtained from the Internet Server are pushed to the callback in addition to any new messages after that.

The last parameter is ignoreOwn. When true the user's own messages are filtered and not sent to the callback. If false, the user's own messages are also pushed to the callback when read from the server. This can be used as a confirmation of the user's writes to the server.

Once connected, to attempt to get messages is done, typically in the Update() method, with:

```C#
   client.TryGetMessage();
```

This method will try to get messages from the Internet Server if the client is not in the process of any request. If the client is in the process of any requests (either writes or a previous get that has not finished yet) this method will do nothing. This method intentionally does not queue the get request because this method is typically called in the Update() method which will likely be called multiple times before a single get request is finished. Thus queueing the request each time would eventually lead to an overflow. By dropping the request, it will get re-requested on the next Update() cycle (or on whatever future Update cycle that the client is free).

To write messages to the Internet Server, code such as the following is used:

```C#
    client.QueueMessage(new InternetServerPlugin.NetworkMessage(user,"Hello everyone!");
```

There are a total of 4 methods for posting a message to the Internet Server. Two are based on NetworkMessage in which case the user (author) can be specified (and can be other than the user used in the connect) and two are string based which uses the same session and user that was used in the connect. Each of these has a version for one message and a version for multiple messages.

Unlike the TryGetMessage() if the client is busy with previous requests, these requests will be queued and processed when the client is free.

## Dependency Internet Server

This plugin uses an Internet Server to process messages and provide the necessary functionality. In order to avoid firewall issues this server needs to be hosted on the internet (as opposed to local) or the computer hosting the server needs to have post 80 opened for incoming and outgoing traffic.







