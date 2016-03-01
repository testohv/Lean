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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class EquityOptionSubscriptionDataReaderTests
    {
        [Test]
        public void SynchronizesData()
        {
            var start = new DateTime(2015, 12, 24);
            var end = start.AddDays(1);
            var tradeableDates = new List<DateTime>{start}.GetEnumerator();
            var config = CreateConfig();

            var option = new Option(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), config, new Cash("USD", 0, 1m), SymbolProperties.GetDefault("USD"));
            option.Filter = new FuncDerivativeSecurityFilter((syms, underlying) => syms);
            var reader = new EquityOptionSubscriptionDataReader(option, start, end, MapFileResolver.Empty, tradeableDates, false);

            var previous = DateTime.MinValue;
            var stopwatch = Stopwatch.StartNew();
            while (reader.MoveNext())
            {
                Assert.That(reader.Current.EndTime, Is.GreaterThanOrEqualTo(previous));
                previous = reader.Current.EndTime;
            }
            stopwatch.Stop();
            Console.WriteLine("ELAPSED:: " + stopwatch.ElapsedMilliseconds);
        }

        [Test]
        public void FiltersContracts()
        {
            var start = new DateTime(2015, 12, 24);
            var end = start.AddDays(1);
            var tradeableDates = new List<DateTime> { start }.GetEnumerator();
            var config = CreateConfig();

            // require contracts with strikes within 5 dollars of the underlying
            var plusMinusStrikeDollars = 5m;
            var option = new Option(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), config, new Cash("USD", 0, 1m), SymbolProperties.GetDefault("USD"));
            option.Filter = new StrikeExpiryOptionFilter(-2, 2, TimeSpan.Zero, TimeSpan.FromDays(18));
            var reader = new EquityOptionSubscriptionDataReader(option, start, end, MapFileResolver.Empty, tradeableDates, false);

            var stopwatch = Stopwatch.StartNew();
            while (reader.MoveNext())
            {
                var chain = (OptionChain)reader.Current;
                var strikes = chain.Select(x => x.Strike).OrderBy(x => x).ToHashSet();
                if (strikes.Count > 0)
                {
                    var minStrike = strikes.Min();
                    var maxStrike = strikes.Max();
                    Assert.That(maxStrike - minStrike, Is.LessThanOrEqualTo(plusMinusStrikeDollars*2));
                }
            }
            stopwatch.Stop();
            Console.WriteLine("ELAPSED:: " + stopwatch.ElapsedMilliseconds);
        }

        private static SubscriptionDataConfig CreateConfig()
        {
            return new SubscriptionDataConfig(typeof(QuoteBar), Symbols.CanonicalOption.GOOG, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
        }
    }
}
