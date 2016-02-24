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
using System.IO;
using System.Linq;
using System.Net;
using Ionic.Zip;
using QuantConnect.Data;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionFactory"/> that handles the <see cref="FileFormat.MultipleTextZipEntry"/> format.
    /// This factory will internally synchronize the zip entries.
    /// </summary>
    public class MultipleZipEntrySubscriptionFactory : ISubscriptionFactory
    {
        private readonly DateTime _date;
        private readonly bool _isLiveMode;
        private readonly SubscriptionDataConfig _config;
        private readonly Func<string, Symbol> _zipEntryToSymbol;

        /// <summary>
        /// Event fired when the specified source is considered invalid, this may
        /// be from a missing file or failure to download a remote source
        /// </summary>
        public event EventHandler<InvalidSourceEventArgs> InvalidSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleZipEntrySubscriptionFactory"/> class
        /// </summary>
        /// <param name="config">The configuration</param>
        /// <param name="date">The date to be processed</param>
        /// <param name="isLiveMode">True for live mode, false otherwise</param>
        /// <param name="zipEntryToSymbol">Function to convert zip entry names into symbols</param>
        public MultipleZipEntrySubscriptionFactory(SubscriptionDataConfig config, DateTime date, bool isLiveMode, Func<string, Symbol> zipEntryToSymbol)
        {
            _date = date;
            _config = config;
            _isLiveMode = isLiveMode;
            _zipEntryToSymbol = zipEntryToSymbol;
        }

        /// <summary>
        /// Reads the specified <paramref name="source"/>
        /// </summary>
        /// <param name="source">The source to be read</param>
        /// <returns>An <see cref="IEnumerable{BaseData}"/> that contains the data in the source</returns>
        public IEnumerable<BaseData> Read(SubscriptionDataSource source)
        {
            if (source.Format != FileFormat.MultipleTextZipEntry)
            {
                throw new ArgumentException("Expected source.Format = FileFormat.MultipleZipEntry");
            }

            var file = source.Source;
            if (source.TransportMedium == SubscriptionTransportMedium.RemoteFile)
            {
                try
                {
                    // download to the local cache
                    file = DownloadRemoteSourceFile(source, Constants.Cache);
                }
                catch (Exception err)
                {
                    OnInvalidSource(source, err);
                    yield break;
                }
            }

            // verify the file exists before processing
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists || fileInfo.Length == 0)
            {
                OnInvalidSource(source, new FileNotFoundException("The specified file was not found: " + fileInfo.FullName));
                yield break;
            }

            using (var zip = new ZipFile(file))
            {
                // open readers to each zip entry and prime the pumps to get the initial frontier time
                var readers = zip.Entries
                    // create a reader for each zip entry, parsing the symbol from the zip entry name
                    .Select(x => new ZipEntryReader(x, new SubscriptionDataConfig(_config, symbol: _zipEntryToSymbol(x.FileName)), _date, _isLiveMode))
                    .Where(x => x.MoveNext())
                    .ToDictionary(x => x.ZipEntryName);

                var frontier = readers.Values.Where(x => x.Current != null).Select(x => x.Current.EndTime).DefaultIfEmpty(DateTime.MinValue).Min();

                // if there was no data, break immediately
                if (frontier == DateTime.MinValue)
                {
                    yield break;
                }
                
                var removedZipEntries = new List<string>();
                while (readers.Count > 0)
                {
                    var nextFrontier = DateTime.MaxValue.Ticks;
                    foreach (var reader in readers.Values)
                    {
                        // pull all data before the frontier time
                        while (reader.Current.EndTime <= frontier)
                        {
                            yield return reader.Current;

                            // advance the reader and remove finished zip entries
                            if (!reader.MoveNext())
                            {
                                removedZipEntries.Add(reader.ZipEntryName);
                                break;
                            }
                        }

                        if (reader.Current != null)
                        {
                            nextFrontier = Math.Min(reader.Current.EndTime.Ticks, nextFrontier);
                        }
                    }

                    // if we didn't set a next frontier means we're out of data for all zip entries
                    if (nextFrontier == DateTime.MaxValue.Ticks)
                    {
                        yield break;
                    }

                    frontier = new DateTime(nextFrontier);

                    // remove dead entries
                    if (removedZipEntries.Count != 0)
                    {
                        foreach (var symbol in removedZipEntries)
                        {
                            readers.Remove(symbol);
                        }

                        removedZipEntries.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="InvalidSource"/> event
        /// </summary>
        /// <param name="source">The <see cref="SubscriptionDataSource"/> that was invalid</param>
        /// <param name="exception">The exception if one was raised, otherwise null</param>
        private void OnInvalidSource(SubscriptionDataSource source, Exception exception)
        {
            var handler = InvalidSource;
            if (handler != null) handler(this, new InvalidSourceEventArgs(source, exception));
        }

        /// <summary>
        /// Downloads the specified remote file source to the specified download directory
        /// </summary>
        private string DownloadRemoteSourceFile(SubscriptionDataSource source, string downloadDirectory)
        {
            // clean old files out of the cache
            if (!Directory.Exists(downloadDirectory)) Directory.CreateDirectory(downloadDirectory);
            foreach (var file in Directory.EnumerateFiles(downloadDirectory))
            {
                if (File.GetCreationTime(file) < DateTime.Now.AddHours(-24)) File.Delete(file);
            }

            try
            {
                // create a hash for a new filename
                var url = source.Source;
                var filename = Guid.NewGuid() + url.GetExtension();
                var destination = Path.Combine(downloadDirectory, filename);

                using (var client = new WebClient())
                {
                    client.Proxy = WebRequest.GetSystemWebProxy();
                    client.DownloadFile(url, destination);
                }

                return destination;
            }
            catch (Exception err)
            {
                OnInvalidSource(source, err);
                return null;
            }
        }

        /// <summary>
        /// <see cref="IEnumerator{BaseData}"/> implementation for an individual zip entry
        /// </summary>
        private class ZipEntryReader : IEnumerator<BaseData>
        {
            public readonly string ZipEntryName;
            private readonly DateTime _date;
            private readonly bool _isLiveMode;
            private readonly BaseData _factory;
            private readonly StreamReader _reader;
            private readonly SubscriptionDataConfig _config;

            /// <summary>
            /// Initializes a new instance of the <see cref="ZipEntryReader"/> class
            /// </summary>
            public ZipEntryReader(ZipEntry entry, SubscriptionDataConfig config, DateTime date, bool isLiveMode)
            {
                ZipEntryName = entry.FileName;

                _date = date;
                _isLiveMode = isLiveMode;
                _reader = new StreamReader(entry.OpenReader());
                _config = config;
                _factory = (BaseData)Activator.CreateInstance(config.Type);
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
                if (_reader.EndOfStream)
                {
                    Current = null;
                    return false;
                }

                BaseData current;
                do
                {
                    try
                    {
                        var line = _reader.ReadLine();
                        if (line == null)
                        {
                            Current = null;
                            return false;
                        }
                        current = _factory.Reader(_config, line, _date, _isLiveMode);
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                        current = null;
                    }
                }
                while (current == null || current.EndTime == DateTime.MinValue);

                Current = current;
                return true;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            public void Reset()
            {
                throw new NotImplementedException("Unable to reset ZipEntryReader");
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                _reader.Dispose();
            }

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
        }
    }
}