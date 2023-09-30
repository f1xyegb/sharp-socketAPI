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
            string formattedMessage = message;

            // ping-pong
            if (!message.Contains("~h~"))
            {
                formattedMessage = "~m~" + message.Length + "~m~" + message;
            }
            //to binary and send
            byte[] messageBytes = Encoding.UTF8.GetBytes(formattedMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

        }

        static void PrepareMessages(ClientWebSocket webSocket, string func, string quoteSession, string chartSession, string token, string symbol, string timeframe, int datasize)
        {
            // Generate mssg to understandble to socket format 
            string ms = "";

            if (func == "set_auth_token")
            {
                var payload = new { m = "set_auth_token", p = new string[] { token } };
                ms = JsonConvert.SerializeObject(payload);
            }
            else if (func == "chart_create_session")
            {
                var payload = new { m = "chart_create_session", p = new string[] { chartSession, "" } };
                ms = JsonConvert.SerializeObject(payload);
                ms = JsonConvert.SerializeObject(payload);
            }
            else if (func == "resolve_symbol")
            {
                var payload = new
                {
                    m = "resolve_symbol",
                    p = new string[] { chartSession, "sds_sym_1", $"={{\"adjustment\":\"splits\",\"session\":\"regular\",\"symbol\":\"{symbol}\"}}" }
                };
                ms = JsonConvert.SerializeObject(payload);
            }
            else if (func == "create_series")
            {
                var payload = new
                {
                    m = "create_series",
                    p = new object[] { chartSession, "sds_1", "s1", "sds_sym_1", timeframe, datasize, "" }
                };
                ms = JsonConvert.SerializeObject(payload);
            }

        }

        static List<List<string>> ConvertRawData(string rawdata)
        {

            List<List<string>> dataset = new List<List<string>>();

            return dataset;
        }

        static async Task<List<List<string>>> GetData(ClientWebSocket webSocket, string chartSession)
        {

            // 2mb buffer
            byte[] receiveBuffer = new byte[2097152];
            List<List<string>> data = new List<List<string>>();

            while (webSocket.State == WebSocketState.Open)
            {
                // Read mssg from socket
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);


                    // ping-pong
                    if (receivedMessage.Contains("~h~"))
                    {
                        await SendMessage(receivedMessage, webSocket);
                    }

                    if (receivedMessage.Contains("timescale_update"))
                    {
                        // If find "timescale_update", add data to dataset
                        data.AddRange(ConvertRawData(receivedMessage));
                        PrepareMessages(webSocket, "request_more_data", null, chartSession, null, null, null, 1000);
                    }

                    if (receivedMessage.Contains("data_completed\":\"limit\""))
                    {
                        // If find "data_completed":"limit", end loop
                        break;
                    }


                }
                else if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Received close message. Closing the WebSocket.");
                    break;
                }
            }

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