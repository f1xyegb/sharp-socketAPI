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
            else if (func == "request_more_data")
            {
                var payload = new { m = "request_more_data", p = new object[] { chartSession, "sds_1", datasize } };

                ms = JsonConvert.SerializeObject(payload);
            }

            // Call the func to send mssg
            SendMessage(ms, webSocket);

        }

        static List<List<string>> ConvertRawData(string rawdata)
        {

            // Find the part of rawdata that contains the array
            int start = rawdata.IndexOf("\"s\":[") + 5;
            int end = rawdata.IndexOf("}],\"ns\":{\"d\":\"\",\"indexes\":[]}");

            if (start == -1 || end == -1)
            {
                throw new ArgumentException("Invalid rawdata format");
            }

            string dataArray = rawdata.Substring(start, end - start);

            // Split the dataArray into individual dataLine strings
            string[] dataArrayLines = dataArray.Split("},");

            List<List<string>> dataset = new List<List<string>>();

            // Parse and split data from each dataLine
            foreach (string dataLine in dataArrayLines)
            {
                int startIndex = dataLine.IndexOf("[") + 1;
                int endIndex = dataLine.IndexOf("]");

                if (startIndex != -1 && endIndex != -1)
                {
                    string data = dataLine.Substring(startIndex, endIndex - startIndex);
                    List<string> dataFields = new List<string>(data.Split(','));
                    dataset.Add(dataFields);
                }
            }


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

                // Connect

                await webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
                Console.WriteLine("WebSocket connected");


                // Token for connect and data about ticker and timeframe of price chart
                string Token = "eyJhbGciOiJSUzUxMiIsImtpZCI6IkdaeFUiLCJ0eXAiOiJKV1QifQ.eyJ1c2VyX2lkIjoxMjEzMjc5MiwiZXhwIjoxNjk1ODYwNTE2LCJpYXQiOjE2OTU4NDYxMTYsInBsYW4iOiJwcm8iLCJleHRfaG91cnMiOjEsInBlcm0iOiIiLCJzdHVkeV9wZXJtIjoiUFVCO2FVU2MxVTRad3VJZmdYUnQzWlp1Tm9hS0JPN2ZJRnlVLFBVQjtiMjhkYTkxMzI4NzU0YWMxOTgyOTJkYWY0NThlMmZkOCx0di12b2x1bWVieXByaWNlLHR2LWNoYXJ0cGF0dGVybnMiLCJtYXhfc3R1ZGllcyI6NSwibWF4X2Z1bmRhbWVudGFscyI6MCwibWF4X2NoYXJ0cyI6MiwibWF4X2FjdGl2ZV9hbGVydHMiOjIwLCJtYXhfc3R1ZHlfb25fc3R1ZHkiOjEsIm1heF9hY3RpdmVfcHJpbWl0aXZlX2FsZXJ0cyI6MjAsIm1heF9hY3RpdmVfY29tcGxleF9hbGVydHMiOjIwLCJtYXhfY29ubmVjdGlvbnMiOjEwfQ.kaHWV-aMnTBT--vdk5joLc2aOo5etx63k8x5hgvHqCSsGBOk3_Gsr171sLN9hdvzkw99jDTtctbMD0wztLGtapuOs1Bp-xXvHnpSxzqPMDKT7lRddTw7pBOGLpCI7ZIheqga5r5DVMlTja3Af1QeFMprANhyTR0rQQpOdp2k68Q";
                string Symbol = "CAPITALCOM:US500";
                string TimeFrame = "60";

                // Generate session id
                string chartSession = GenerateSessionID("cs");



                // Call the function to set the authentication token
                PrepareMessages(webSocket, "set_auth_token", null, null, Token, null, null, 0);

                // Call the function to create a chart session
                PrepareMessages(webSocket, "chart_create_session", null, chartSession, null, null, null, 0);

                // Call the function to set the symbol
                PrepareMessages(webSocket, "resolve_symbol", null, chartSession, null, Symbol, null, 0);

                // Call the function to create data series
                PrepareMessages(webSocket, "create_series", null, chartSession, null, null, TimeFrame, 5000);

                List<List<string>> data = await GetData(webSocket, chartSession);
            }


        }

    }


}