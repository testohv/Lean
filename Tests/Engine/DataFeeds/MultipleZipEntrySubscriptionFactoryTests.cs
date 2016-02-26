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
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class MultipleZipEntrySubscriptionFactoryTests
    {
        private const string TickZipFile = "20151224_quote_american.zip";
        private const string HourZipFile = "xlre_quote_american.zip";

        [Test]
        public void SynchronizesHourEntries()
        {
            MultipleZipEntrySubscriptionFactory reader;
            var source = CreateReader(out reader, HourZipFile, Resolution.Hour);
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
            var source = CreateReader(out reader, TickZipFile, Resolution.Tick);
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
        public void EmitsBaseDataCollections()
        {
            MultipleZipEntrySubscriptionFactory reader;
            var source = CreateReader(out reader, TickZipFile, Resolution.Tick);
            int count = 0;
            foreach (var data in reader.Read(source))
            {
                count++;
                Assert.That(data, Is.InstanceOf<BaseDataCollection>());
            }
            Assert.AreNotEqual(0, count);
        }

        [Test]
        public void FiltersZipEntries()
        {
            MultipleZipEntrySubscriptionFactory reader;
            var source = CreateReader(out reader, TickZipFile, Resolution.Tick);
            int count = 0;
            reader.SetSymbolFilter(syms => syms.Where(x => x.ID.StrikePrice == 37m));
            foreach (var data in reader.Read(source).OfType<BaseDataCollection>())
            {
                foreach (var d in data.Data) Assert.AreEqual(37m, d.Symbol.ID.StrikePrice);
                count++;
            }
            Assert.AreNotEqual(0, count);
        }

        [Test]
        public void FastForwardsFilteredZipEntries()
        {
            MultipleZipEntrySubscriptionFactory reader;
            var source = CreateReader(out reader, TickZipFile, Resolution.Tick);
            int count = 0;
            var previous = DateTime.MinValue;
            reader.SetSymbolFilter(syms =>
            {
                if (count == 0) return syms;
                if (count%4 == 0) return syms.Where(x => x.ID.StrikePrice == 22m);
                if (count%3 == 0) return syms.Where(x => x.ID.StrikePrice == 21m);
                return syms.Where(x => x.ID.StrikePrice == 37m);
            });
            var symbols = new HashSet<Symbol>();
            foreach (var data in reader.Read(source).OfType<BaseDataCollection>())
            {
                // verifying that time is always moving forward, we fast-forwarded the skipped entries
                Assert.That(data.EndTime, Is.GreaterThanOrEqualTo(previous));
                count++;
                foreach (var d in data.Data) symbols.Add(d.Symbol);
            }
            Assert.AreEqual(3, symbols.Count);
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
