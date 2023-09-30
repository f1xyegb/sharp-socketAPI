using System;
using System.Data;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class TVSocketAPI
{
    static async Task Main(string[] args)
    {

        // Data to connect to socket

        string url = "wss://data.tradingview.com/socket.io/websocket";
        string origin = "https://www.tradingview.com";
        string customHeaders = "Host: prodata.tradingview.com\r\n" +
                              "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.110 Safari/537.36";


        using (ClientWebSocket webSocket = new ClientWebSocket())
        {

            // Set headers
            webSocket.Options.SetRequestHeader("Origin", origin);
            foreach (var header in customHeaders.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = header.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    webSocket.Options.SetRequestHeader(parts[0], parts[1]);
                }
            }

            // Connect

            await webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
            Console.WriteLine("WebSocket connected");


        }

    }


}