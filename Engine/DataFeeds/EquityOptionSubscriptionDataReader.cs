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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionFactory"/> to handle options
    /// subscriptions. Options are somewhat special in various ways. Firstly, since the trade
    /// volumes are so low, we need both trades and quotes, also, the algorithm may request
    /// only certain strikes relative to the current asset price, so we also need to read the
    /// underlying's trade data
    /// </summary>
    public class EquityOptionSubscriptionDataReader : IEnumerator<BaseData>
    {
        private bool _endOfStream;
        private DateTime _frontier;

        private readonly SubscriptionDataConfig _config;
        private readonly DateTime? _periodStart;
        private readonly DateTime? _periodFinish;
        private readonly MapFileResolver _mapFileResolver;
        private readonly IEnumerator<DateTime> _tradeableDates;
        private readonly bool _isLiveMode;
        private readonly MapFile _mapFile;

        private readonly SubscriptionDataConfig _equityConfig;
        private readonly SubscriptionDataConfig _tradesConfig;
        private readonly SubscriptionDataConfig _quotesConfig;

        private Reader _equityReader;
        private Reader _tradesReader;
        private Reader _quotesReader;

        private readonly BaseData _equityGetSourceFactory;
        private readonly BaseData _tradesGetSourceFactory;
        private readonly BaseData _quotesGetSourceFactory;

        private readonly Option _option;

        // data for option chain
        private BaseData _underlying;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public BaseData Current
        {
            get; private set;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EquityOptionSubscriptionDataReader"/> class
        /// </summary>
        /// <param name="option">The option security</param>
        /// <param name="periodStart">Start time filter, don't emit data before this time</param>
        /// <param name="periodFinish">End time filter, don't emit data after thi time</param>
        /// <param name="mapFileResolver">Map file resolver instance used to resolve symbol changes</param>
        /// <param name="tradeableDates">The tradeable dates to be processed</param>
        /// <param name="isLiveMode">True for live mode, false otherwise</param>
        public EquityOptionSubscriptionDataReader(Option option, DateTime periodStart, DateTime periodFinish, MapFileResolver mapFileResolver, IEnumerator<DateTime> tradeableDates, bool isLiveMode)
        {
            var config = option.SubscriptionDataConfig;
            _config = config;
            _periodStart = periodStart;
            _periodFinish = periodFinish;
            _mapFileResolver = mapFileResolver;
            _tradeableDates = tradeableDates;
            _isLiveMode = isLiveMode;
            _option = option;

            _mapFile = new MapFile(config.Symbol.Value, new List<MapFileRow>());

            // load up the map and factor files for equities
            try
            {
                var mapFile = mapFileResolver.ResolveMapFile(config.Symbol.ID.Symbol, config.Symbol.ID.Date);

                // only take the resolved map file if it has data, otherwise we'll use the empty one we defined above
                if (mapFile.Any()) _mapFile = mapFile;
            }
            catch (Exception err)
            {
                Log.Error(err, "Fetching Price/Map Factors: " + config.Symbol.ID + ": ");
            }

            // create configs for the underlying equity and for the trades/quotes
            var equityType = config.Resolution == Resolution.Tick ? typeof (Tick) : typeof (TradeBar);
            _equityConfig = new SubscriptionDataConfig(config, equityType, Symbol.Create(config.Symbol.ID.Symbol, SecurityType.Equity, config.Symbol.ID.Market));
            if (config.Resolution == Resolution.Tick)
            {
                _tradesConfig = new SubscriptionDataConfig(config, tickType: TickType.Trade);
                _quotesConfig = new SubscriptionDataConfig(config, tickType: TickType.Quote);
            }
            else
            {
                _tradesConfig = new SubscriptionDataConfig(config, typeof (TradeBar), tickType: TickType.Trade);
                _quotesConfig = new SubscriptionDataConfig(config, typeof (QuoteBar), tickType: TickType.Quote);
            }

            // instantiate BaseData instances so we can call GetSource
            _equityGetSourceFactory = (BaseData) Activator.CreateInstance(_equityConfig.Type);
            _tradesGetSourceFactory = (BaseData) Activator.CreateInstance(_tradesConfig.Type);
            _quotesGetSourceFactory = (BaseData) Activator.CreateInstance(_quotesConfig.Type);
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
        public bool MoveNext()
        {
            if (_endOfStream)
            {
                return false;
            }

            if (_equityReader == null && _quotesReader == null && _tradesReader == null)
            {
                if (!AdvanceDate())
                {
                    return false;
                }
            }

            // if we didn't resolve a new frontier by here then we're out of data
            if (_frontier == DateTime.MaxValue)
            {
                _endOfStream = true;
                return false;
            }

            BaseData temp;
            var trades = new BaseDataCollection();
            var contracts = new OptionContracts();
            if (_tradesReader != null)
            {
                if (!_tradesReader.TryGetNextData(_frontier, out temp))
                {
                    _tradesReader = null;
                }
                else if (temp != null)
                {
                    trades = (BaseDataCollection) temp;
                    foreach (var trade in trades.Data)
                    {
                        OptionContract contract;
                        if (!contracts.TryGetValue(trade.Symbol, out contract))
                        {
                            contract = new OptionContract(trade.Symbol, _underlying.Symbol);
                            contracts[trade.Symbol] = contract;
                        }

                        UpdateContractWithTrades(contract, trade);
                    }
                }
            }

            var quotes = new BaseDataCollection();
            if (_quotesReader != null)
            {
                if (!_quotesReader.TryGetNextData(_frontier, out temp))
                {
                    _quotesReader = null;
                }
                else if (temp != null)
                {
                    quotes = (BaseDataCollection) temp;
                    foreach (var quote in quotes.Data)
                    {
                        OptionContract contract;
                        if (!contracts.TryGetValue(quote.Symbol, out contract))
                        {
                            contract = new OptionContract(quote.Symbol, _underlying.Symbol);
                            contracts[quote.Symbol] = contract;
                        }
                        UpdateContractWithQuotes(contract, quote);
                    }
                }
            }

            if (_equityReader != null)
            {
                if (!_equityReader.TryGetNextData(_frontier, out temp))
                {
                    _equityReader = null;
                }
                else
                {
                    if (temp != null) _underlying = temp;
                }
            }

            var chain = new OptionChain(_config.Symbol, _frontier, _underlying, trades.Data, quotes.Data, contracts.Values);
            Current = chain;

            // find the next frontier time
            var nextFrontier = DateTime.MaxValue.Ticks;
            if (_equityReader != null && _equityReader.TryGetNextData(out temp))
            {
                nextFrontier = temp.EndTime.Ticks;
            }
            if (_tradesReader != null && _tradesReader.TryGetNextData(out temp))
            {
                nextFrontier = Math.Min(nextFrontier, temp.EndTime.Ticks);
            }
            if (_quotesReader != null && _quotesReader.TryGetNextData(out temp))
            {
                nextFrontier = Math.Min(nextFrontier, temp.EndTime.Ticks);
            }

            _frontier = new DateTime(nextFrontier);

            return true;
        }

        private bool AdvanceDate()
        {
            if (!_tradeableDates.MoveNext())
            {
                _endOfStream = true;
                return false;
            }
            
            _frontier = DateTime.MaxValue;

            // TODO: On date changes we need to check map files!!

            BaseData temp;
            var date = _tradeableDates.Current;
            _equityReader = CreateReader(_equityGetSourceFactory, _equityConfig, date);
            if (_equityReader.TryGetNextData(out temp))
            {
                _underlying = temp;
            }

            _tradesReader = CreateReader(_tradesGetSourceFactory, _tradesConfig, date);
            if (_tradesReader.TryGetNextData(out temp))
            {
                _frontier = temp.EndTime;
            }

            _quotesReader = CreateReader(_quotesGetSourceFactory, _quotesConfig, date);
            if (_quotesReader.TryGetNextData(out temp))
            {
                _frontier = new DateTime(Math.Min(_frontier.Ticks, temp.EndTime.Ticks));
            }

            // fast forward the equity data to our frontier minus the resolution step
            // we use the close of the previous bar for filtering
            if (_equityReader.TryGetNextData(_frontier - _config.Increment, out temp))
            {
                _underlying = temp;
            }

            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
        public void Reset()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private Reader CreateReader(BaseData getSourceFactory, SubscriptionDataConfig config, DateTime date)
        {
            var source = getSourceFactory.GetSource(config, date, _isLiveMode);
            var subscriptionFactory = SubscriptionFactory.ForSource(source, config, date, _isLiveMode);
            if (subscriptionFactory is MultipleZipEntrySubscriptionFactory)
            {
                var multizip = (MultipleZipEntrySubscriptionFactory) subscriptionFactory;
                multizip.SetSymbolFilter(symbols => _option.ContractFilter.Filter(symbols, _underlying));
            }
            var enumerator = subscriptionFactory.Read(source).GetEnumerator();
            return new Reader(enumerator);
        }

        private void UpdateContractWithTrades(OptionContract contract, BaseData trade)
        {
            var volume = GetTradeVolume(trade);
            contract.Updated = trade.EndTime;
            contract.LastPrice = trade.Price;
            contract.UnderlyingLastPrice = _underlying.Price;
        }

        private void UpdateContractWithQuotes(OptionContract contract, BaseData quote)
        {
            contract.Updated = quote.EndTime;
            if (quote is Tick)
            {
                var tick = (Tick)quote;
                if (tick.AskPrice != 0m)
                {
                    contract.AskPrice = tick.AskPrice;
                    contract.AskSize = tick.AskSize;
                }
                if (tick.BidPrice != 0m)
                {
                    contract.BidPrice = tick.BidPrice;
                    contract.BidSize = tick.BidSize;
                }
            }
            if (quote is QuoteBar)
            {
                var quoteBar = (QuoteBar)quote;
                if (quoteBar.Ask != null && quoteBar.Ask.Close != 0m)
                {
                    contract.AskPrice = quoteBar.Ask.Close;
                    contract.AskSize = quoteBar.LastAskSize;
                }
                if (quoteBar.Bid != null && quoteBar.Bid.Close != 0m)
                {
                    contract.BidPrice = quoteBar.Bid.Close;
                    contract.BidSize = quoteBar.LastBidSize;
                }
            }
        }

        private static long GetTradeVolume(BaseData trade)
        {
            if (trade is Tick)
            {
                return ((Tick)trade).Quantity;
            }
            if (trade is TradeBar)
            {
                return ((TradeBar)trade).Volume;
            }

            return 0L;
        }

        class Reader
        {
            private bool _needsMoveNext;
            private bool _eos;
            private readonly IEnumerator<BaseData> _enumerator;
            public Reader(IEnumerator<BaseData> enumerator)
            {
                _enumerator = enumerator;
                _needsMoveNext = true;
            }

            /// <summary>
            /// Grabs the next piece of data and marks this enumerator as move next = false
            /// </summary>
            public bool TryGetNextData(out BaseData data)
            {
                data = null;
                if (_eos) return false;

                if (_needsMoveNext)
                {
                    if (!_enumerator.MoveNext())
                    {
                        _eos = true;
                        return false;
                    }
                }

                data = _enumerator.Current;
                _needsMoveNext = false;
                return true;
            }

            /// <summary>
            /// Grabs the next piece of data while acknowledging the frontier time,
            /// may return true and still populate data as null
            /// </summary>
            public bool TryGetNextData(DateTime frontier, out BaseData data)
            {
                data = null;
                if (_eos) return false;

                if (_enumerator.Current != null)
                {
                    if (_enumerator.Current.EndTime > frontier)
                    {
                        _needsMoveNext = false;
                        return true;
                    }
                    if (_enumerator.Current.EndTime == frontier)
                    {
                        data = _enumerator.Current;
                        _needsMoveNext = true;
                        return true;
                    }
                }

                if (_needsMoveNext)
                {
                    if (!_enumerator.MoveNext())
                    {
                        _eos = true;
                        return false;
                    }
                }

                while (_enumerator.Current.EndTime <= frontier)
                {
                    data = _enumerator.Current;
                    if (data.EndTime == frontier)
                    {
                        _needsMoveNext = true;
                        return true;
                    }
                    if (!_enumerator.MoveNext())
                    {
                        _eos = true;
                        return true;
                    }
                }

                _needsMoveNext = false;
                return true;
            }
        }
    }
}