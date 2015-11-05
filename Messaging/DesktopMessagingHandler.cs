/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Notifications;
using QuantConnect.Packets;

namespace QuantConnect.Messaging
{
    /// <summary>
    /// Console and Desktop implementation of messaging system for Lean Engine.
    /// </summary>
    public class DesktopMessagingHandler : IMessagingHandler
    {

        private int _userId;
        private string _apiToken;
        private string _algorithmId;
        private bool _transmit = Config.GetBool("enable-charting", true);

        /// <summary>
        /// Class constructor
        /// </summary>
        public DesktopMessagingHandler()
        {
            HasSubscribers = true;
        }

        /// <summary>
        /// The default implementation doesn't send messages, so this does nothing.
        /// </summary>
        public bool HasSubscribers
        {
            get;
            set;
        }

        /// <summary>
        /// Initialize the messaging system
        /// </summary>
        public void Initialize()
        {
            // NOP.
        }

        /// <summary>
        /// Set the messaging channel
        /// </summary>
        public void SetAuthentication(AlgorithmNodePacket job)
        {
            _userId = job.UserId;
            _apiToken = job.Channel;
            _algorithmId = job.AlgorithmId;
        }

        /// <summary>
        /// Send a generic base packet without processing
        /// </summary>
        public void Send(Packet packet)
        {
            //Preprocess debug messages:
            var debug = packet as DebugPacket;
            if (debug != null) Log.Trace("Messaging.Send(): Debug: " + debug.Message);

            //Preprocess error messages:
            var runtimeError = packet as RuntimeErrorPacket;
            if (runtimeError != null) Log.Error("Messaging.Send(): Runtime Error: " + runtimeError.Message + " ST: " + runtimeError.StackTrace);

            //Preprocess handled error messages:
            var handled = packet as HandledErrorPacket;
            if (handled != null) Log.Error("BacktestingResultHandler.Run(): HandledError Packet: " + handled.Message);

            //Process final result packages:
            var result = packet as BacktestResultPacket;
            if (result != null) ProcessResultPacket(result);

            if (_transmit)
            {
                this.Transmit(_userId, _apiToken, packet);
            }
        }

        /// <summary>
        /// If its the final result packet, send a summary:
        /// </summary>
        /// <param name="packet">Result packet</param>
        private void ProcessResultPacket(BacktestResultPacket packet)
        {
            if (packet.Progress == 1)
            {
                // uncomment these code traces to help write regression tests
                //Console.WriteLine("var statistics = new Dictionary<string, string>();");

                // Bleh. Nicely format statistical analysis on your algorithm results. Save to file etc.
                foreach (var pair in packet.Results.Statistics)
                {
                    Log.Trace("STATISTICS:: " + pair.Key + " " + pair.Value);
                    //Console.WriteLine(string.Format("statistics.Add(\"{0}\",\"{1}\");", pair.Key, pair.Value));
                }

                //foreach (var pair in statisticsResults.RollingPerformances) 
                //{
                //    Log.Trace("ROLLINGSTATS:: " + pair.Key + " SharpeRatio: " + Math.Round(pair.Value.PortfolioStatistics.SharpeRatio, 3));
                //}
            }
        }

        /// <summary>
        /// Common notification transmitter
        /// </summary>
        /// <param name="message">Notification packet</param>
        public void SendNotification(Notification message)
        {
            switch (message.GetType().Name)
            {
                case "NotificationEmail":
                    Email(message as NotificationEmail);
                    break;

                case "NotificationSms":
                    Sms(message as NotificationSms);
                    break;

                case "NotificationWeb":
                    Web(message as NotificationWeb);
                    break;

                default:
                    try
                    {
                        message.Send();
                    }
                    catch (Exception err)
                    {
                        Log.Error("Messaging.SendNotification(): Error sending notification: " + err.Message);
                        Send(new HandledErrorPacket(_algorithmId, "Custom send notification: " + err.Message, err.StackTrace));
                    }
                    break;
            }
        }

        /// <summary>
        /// Send a rate limited email notification triggered during live trading from a user algorithm
        /// </summary>
        /// <param name="email"></param>
        private void Email(NotificationEmail email)
        {
            Log.Trace("Messaging.Email(): Email Not Implemented: Subject: " + email.Subject);
        }

        /// <summary>
        /// Send a rate limited SMS notification triggered duing live trading from a user algorithm.
        /// </summary>
        /// <param name="sms"></param>
        private void Sms(NotificationSms sms)
        {
            Log.Trace("Messaging.Sms(): Sms Not Implemented: Message: " + sms.Message);
        }

        /// <summary>
        /// Send a web REST request notification triggered during live trading from a user algorithm.
        /// </summary>
        /// <param name="web"></param>
        private void Web(NotificationWeb web)
        {
            Log.Trace("Messaging.Web(): Web Not Implemented: Url: " + web.Address);
        }
    }
}