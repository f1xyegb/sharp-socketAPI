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

        static string GenerateSessionID(string idType)
        {
            // Generate random session id
            int stringLength = 12;
            string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
            Random random = new Random();
            StringBuilder randomString = new StringBuilder();

            for (int i = 0; i < stringLength; i++)
            {
                randomString.Append(letters[random.Next(0, letters.Length)]);
            }

            string sessionID = "cs_" + randomString.ToString();


            return sessionID;
        }

        static async Task SendMessage(string message, ClientWebSocket webSocket)
        {
            
        }

        static void PrepareMessages(ClientWebSocket webSocket, string func, string quoteSession, string chartSession, string token, string symbol, string timeframe, int datasize)
        {
            
        }

        static List<List<string>> ConvertRawData(string rawdata)
        {

            List<List<string>> dataset = new List<List<string>>();

            return dataset;
        }

        static async Task<List<List<string>>> GetData(ClientWebSocket webSocket, string chartSession)
        {

            List<List<string>> data = new List<List<string>>();

   

            return data;
        }

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


        }

    }


}