using System;
using System.Collections.Specialized;
using System.Net;
using Newtonsoft.Json;
using QuantConnect.Packets;

namespace QuantConnect.Interfaces
{
    public static class MessagingHandler
    {
        /// <summary>
        /// Response object from the Streaming API.
        /// </summary>
        private class Response
        {
            public Response() { }

            /// <summary>
            /// Type of response from the streaming api.
            /// </summary>
            /// <remarks>success or error</remarks>
            public string Type;

            /// <summary>
            /// Message description of the error or success state.
            /// </summary>
            public string Message;
        }


        /// <summary>
        /// Send a message to the QuantConnect Chart Streaming API.
        /// </summary>
        /// <param name="messaging">Interface we're extending.</param>
        /// <param name="userId">User Id</param>
        /// <param name="apiToken">API token for authentication</param>
        /// <param name="packet">Packet to transmit</param>
        public static void Transmit(this IMessagingHandler messaging, int userId, string apiToken, Packet packet)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var tx = JsonConvert.SerializeObject(packet);
                    if (tx.Length > 10000)
                    {
                        Console.WriteLine("StreamingResultHandler.Send(): Packet too long: " + packet.GetType());
                    }

                    var response = client.UploadValues("http://streaming.quantconnect.com", new NameValueCollection()
                    {
                        { "uid", userId.ToString() },
                        { "token", apiToken},
                        { "tx", tx }
                    });

                    Console.WriteLine("Messaging.Send(): Sending Packet (" + packet.GetType() + ")");

                    //Deserialize the response from the streaming API and throw in error case.
                    var result = JsonConvert.DeserializeObject<Response>(System.Text.Encoding.UTF8.GetString(response));
                    if (result.Type == "error")
                    {
                        throw new Exception(result.Message);
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Messaging.Send(): Error sending packet: " + err.Message);
            }
        }
    }
}