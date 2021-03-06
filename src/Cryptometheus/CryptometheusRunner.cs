﻿//
//          Copyright Seth Hendrick 2021.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Collections.Generic;
using System.Net.Http;
using Prometheus;

namespace Cryptometheus
{
    public class CryptometheusRunner : IDisposable
    {
        // ---------------- Fields ----------------

        private const string userAgent = nameof( Cryptometheus );

        private readonly HttpClient client;
        private readonly Settings settings;
        private readonly ApiReader apiReader;
        private bool started;

        private readonly Dictionary<string, IGauge> gagues;
        private readonly ICounter exceptionCount;

        // ---------------- Constructor ----------------

        public CryptometheusRunner( Settings settings )
        {
            this.settings = settings;
            this.client = new HttpClient();
            this.client.DefaultRequestHeaders.Add( "User-Agent", userAgent );

            this.apiReader = new ApiReader( this.client, this.settings );
            this.apiReader.OnSuccessfulRead += this.ApiReader_OnSuccessfulRead;
            this.apiReader.OnError += this.ApiReader_OnError;

            this.gagues = new Dictionary<string, IGauge>();
            foreach( string ticker in settings.Tickers )
            {
                GaugeConfiguration config = new GaugeConfiguration
                {
                    SuppressInitialValue = true
                };

                this.gagues[ticker] = Metrics.CreateGauge(
                    ticker.Replace( '-', '_' ),
                    $"Price of {ticker}",
                    config
                );
            }

            this.exceptionCount = Metrics.CreateCounter(
                "error_count",
                $"How many exceptions has {nameof( Cryptometheus )} reported since the process started?"
            );

            this.started = false;
        }

        // ---------------- Functions ----------------

        public void Start()
        {
            if( this.started )
            {
                throw new InvalidOperationException( "Already started" );
            }

            this.apiReader.Start();
            this.started = true;
        }

        public void Dispose()
        {
            this.apiReader.OnSuccessfulRead -= this.ApiReader_OnSuccessfulRead;
            this.apiReader.OnError -= this.ApiReader_OnError;
            this.apiReader.Dispose();
            this.client.Dispose();
        }

        private void ApiReader_OnSuccessfulRead( CryptonatorResult result )
        {
            string key = result.Ticker;
            if( this.gagues.ContainsKey( key ) == false )
            {
                throw new KeyNotFoundException( $"Can not find ticker '{key}; in gagues" );
            }

            this.gagues[key].Set( result.Price );
        }

        private void ApiReader_OnError( Exception e )
        {
            Console.Error.WriteLine( e.Message );
            this.exceptionCount.Inc();
        }
    }
}
