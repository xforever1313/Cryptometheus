//
//          Copyright Seth Hendrick 2021.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SethCS.Basic;

namespace Cryptometheus
{
    /// <summary>
    /// Reads from the API.
    /// </summary>
    public class ApiReader : IDisposable
    {
        // ---------------- Events ----------------

        /// <summary>
        /// When we are able to read from the server successfully.
        /// </summary>
        public event Action<CryptonatorResult> OnSuccessfulRead;

        /// <summary>
        /// An error has happened.  The exception is passed in.
        /// </summary>
        public event Action<Exception> OnError;

        // ---------------- Fields ----------------

        private readonly HttpClient client;
        private readonly Settings settings;
        private readonly EventExecutor eventQueue;
        private readonly List<string> tickers;

        private bool keepGoing;
        private object keepGoingLock;

        private static readonly Regex notFoundRegex = new Regex(
            @"Pair\s+not\s+found",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture
        );

        // ---------------- Constructor ----------------

        public ApiReader( HttpClient client, Settings settings )
        {
            this.client = client;
            this.settings = settings;
            this.eventQueue = new EventExecutor( nameof( ApiReader ) );
            this.tickers = new List<string>( settings.Tickers );

            this.keepGoingLock = new object();
            this.keepGoing = false;
        }

        // ---------------- Properties ----------------

        public bool KeepGoing
        {
            get
            {
                lock( this.keepGoingLock )
                {
                    return this.keepGoing;
                }
            }
            private set
            {
                lock( this.keepGoingLock )
                {
                    this.keepGoing = value;
                }
            }
        }

        // ---------------- Functions ----------------

        public void Start()
        {
            lock( this.keepGoingLock )
            {
                if( this.keepGoing )
                {
                    throw new InvalidOperationException( "Already started" );
                }
                this.keepGoing = true;
            }

            this.eventQueue.OnError += this.EventQueue_OnError;
            this.eventQueue.Start();
            AddAllTickers();
        }

        public void Dispose()
        {
            lock( this.keepGoingLock )
            {
                if( this.keepGoing == false )
                {
                    return;
                }

                this.keepGoing = true;
            }

            this.eventQueue.Interrupt();
            this.eventQueue.Dispose();
            this.eventQueue.OnError -= this.EventQueue_OnError;
        }

        private void EventQueue_OnError( Exception obj )
        {
            this.OnError?.Invoke( obj );
        }

        private void QueryTicker( string ticker )
        {
            string url = $"https://api.cryptonator.com/api/ticker/{ticker}";
            Task<HttpResponseMessage> responseToken = this.client.GetAsync(
                url
            );

            responseToken.Wait();

            HttpResponseMessage response = responseToken.Result;
            string str = response.Content.ReadAsStringAsync().Result;

            if( response.IsSuccessStatusCode == false )
            {
                throw new HttpRequestException(
                    $"Got response code {response.StatusCode} from {url}." + Environment.NewLine + str
                );
            }

            CryptonatorResult result = new CryptonatorResult();
            result.FromJson( str );
            if( result.Success == false )
            {
                if( notFoundRegex.IsMatch( str ) )
                {
                    // If a ticker is not found, just remove it.
                    this.tickers.Remove( ticker );
                    throw new ArgumentException(
                        $"Invalid ticker pair, removing from future queries: {ticker}",
                        nameof( settings.Tickers )
                    );
                }
                else
                {
                    throw new ApplicationException(
                        $"Error when trying to parse {nameof( CryptonatorResult )}:" + Environment.NewLine + result.Error
                    );
                }
            }

            this.OnSuccessfulRead?.Invoke( result );
        }

        private void AddEvent( string ticker )
        {
            this.eventQueue.AddEvent(
                () =>
                {
                    if( this.KeepGoing )
                    {
                        QueryTicker( ticker );
                    }
                }
            );

            this.eventQueue.AddEvent(
                () =>
                {
                    if( this.KeepGoing )
                    {
                        Thread.Sleep( this.settings.RateLimit );
                    }
                }
            );
        }

        private void AddAllTickers()
        {
            foreach( string ticker in tickers )
            {
                AddEvent( ticker );
            }

            this.eventQueue.AddEvent( AddAllTickers );
        }
    }
}
