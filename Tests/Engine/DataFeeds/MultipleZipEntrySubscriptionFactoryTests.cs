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
using System.IO;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class MultipleZipEntrySubscriptionFactoryTests
    {
        [Test]
        public void SynchronizesHourEntries()
        {
            MultipleZipEntrySubscriptionFactory reader;
            var source = CreateReader(out reader, "xlre_quote_american.zip", Resolution.Hour);
            var previous = DateTime.MinValue;
            int count = 0;
            foreach (var data in reader.Read(source))
            {
                count++;
                Assert.That(data.EndTime, Is.GreaterThanOrEqualTo(previous));
                previous = data.EndTime;
            }
            Assert.AreNotEqual(0, count);
        }

        [Test]
        public void SynchronizesTickEntries()
        {
            MultipleZipEntrySubscriptionFactory reader;
            var source = CreateReader(out reader, "20151224_quote_american.zip", Resolution.Tick);
            var previous = DateTime.MinValue;
            int count = 0;
            foreach (var data in reader.Read(source))
            {
                count++;
                Assert.That(data.EndTime, Is.GreaterThanOrEqualTo(previous));
                previous = data.EndTime;
            }
            Assert.AreNotEqual(0, count);
        }

        private static SubscriptionDataSource CreateReader(out MultipleZipEntrySubscriptionFactory reader, string file, Resolution resolution)
        {
            var underlying = Symbol.Create("XLRE", SecurityType.Equity, Market.USA);
            var path = Path.Combine("TestData", file);
            var canonical = Symbol.CreateOption(underlying.Value, Market.USA, OptionStyle.American, OptionRight.Put, 0, SecurityIdentifier.DefaultDate);
            var config = new SubscriptionDataConfig(resolution == Resolution.Tick ? typeof(Tick) : typeof(QuoteBar), canonical, resolution, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false,
                TickType.Quote);
            var source = new SubscriptionDataSource(path, SubscriptionTransportMedium.LocalFile, FileFormat.MultipleTextZipEntry);
            var tmp = SubscriptionFactory.ForSource(source, config, new DateTime(2015, 12, 24), false);
            Assert.IsInstanceOf<MultipleZipEntrySubscriptionFactory>(tmp);
            reader = (MultipleZipEntrySubscriptionFactory)tmp;
            return source;
        }
    }
}
