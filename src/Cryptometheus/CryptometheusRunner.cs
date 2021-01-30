//
//          Copyright Seth Hendrick 2021.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Net.Http;

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

        // ---------------- Constructor ----------------

        public CryptometheusRunner( Settings settings )
        {
            this.settings = settings;
            this.client = new HttpClient();
            this.client.DefaultRequestHeaders.Add( "User-Agent", userAgent );

            this.apiReader = new ApiReader( this.client, this.settings );
            this.apiReader.OnSuccessfulRead += this.ApiReader_OnSuccessfulRead;
            this.apiReader.OnError += this.ApiReader_OnError;

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

        public void Wait()
        {
            Console.WriteLine( "Press a key to continue" );
            Console.ReadKey();
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
            Console.WriteLine( result );
        }

        private void ApiReader_OnError( Exception e )
        {
            Console.Error.WriteLine( e.Message );
        }
    }
}
