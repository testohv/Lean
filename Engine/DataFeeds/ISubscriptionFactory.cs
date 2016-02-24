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
 *
*/

using System;
using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Represents a type responsible for accepting an input <see cref="SubscriptionDataSource"/>
    /// and returning an enumerable of the source's <see cref="BaseData"/>
    /// </summary>
    public interface ISubscriptionFactory
    {
        /// <summary>
        /// Event fired when the specified source is considered invalid, this may
        /// be from a missing file or failure to download a remote source
        /// </summary>
        event EventHandler<InvalidSourceEventArgs> InvalidSource;

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        IEnumerable<BaseData> Read(SubscriptionDataSource source);
    }

    /// <summary>
    /// Provides a factory method for creating <see cref="ISubscriptionFactory"/> instances
    /// </summary>
    public static class SubscriptionFactory
    {
        /// <summary>
        /// Createsa new <see cref="ISubscriptionFactory"/> capable of handling the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The subscription data source to create a factory for</param>
        /// <param name="config">The configuration of the subscription</param>
        /// <param name="date">The date to be processed</param>
        /// <param name="isLiveMode">True for live mode, false otherwise</param>
        /// <returns>A new <see cref="ISubscriptionFactory"/> that can read the specified <paramref name="source"/></returns>
        public static ISubscriptionFactory ForSource(SubscriptionDataSource source, SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            switch (source.Format)
            {
                case FileFormat.Csv:
                    return new TextSubscriptionFactory(config, date, isLiveMode);

                case FileFormat.MultipleTextZipEntry:
                    return new MultipleZipEntrySubscriptionFactory(config, date, isLiveMode, entry => ReadSymbolFromZipEntry(config.SecurityType, config.Resolution, entry));

                default:
                    throw new NotImplementedException("SubscriptionFactory.ForSource(" + source + ") has not been implemented yet.");
            }
        }

        private static Symbol ReadSymbolFromZipEntry(SecurityType securityType, Resolution resolution, string zipEntryName)
        {
            var isHourlyOrDaily = resolution == Resolution.Hour || resolution == Resolution.Daily;
            switch (securityType)
            {
                case SecurityType.Option:
                    var parts = zipEntryName.Replace(".csv", string.Empty).Split('_');
                    if (isHourlyOrDaily)
                    {
                        var style = (OptionStyle) Enum.Parse(typeof (OptionStyle), parts[2], true);
                        var right = (OptionRight) Enum.Parse(typeof (OptionRight), parts[3], true);
                        var strike = decimal.Parse(parts[4])/10000m;
                        var expiry = DateTime.ParseExact(parts[5], DateFormat.EightCharacter, null);
                        return Symbol.CreateOption(parts[0], Market.USA, style, right, strike, expiry);
                    }
                    else
                    {
                        var style = (OptionStyle)Enum.Parse(typeof(OptionStyle), parts[4], true);
                        var right = (OptionRight)Enum.Parse(typeof(OptionRight), parts[5], true);
                        var strike = decimal.Parse(parts[6]) / 10000m;
                        var expiry = DateTime.ParseExact(parts[7], DateFormat.EightCharacter, null);
                        return Symbol.CreateOption(parts[1], Market.USA, style, right, strike, expiry);
                    }

                default:
                    throw new NotImplementedException("ReadSymbolFromZipEntry is not implemented for " + securityType + " " + resolution);
            }
        }
    }
}
