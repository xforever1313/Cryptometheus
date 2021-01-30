//
//          Copyright Seth Hendrick 2021.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Options;
using SethCS.Exceptions;
using SethCS.Extensions;

namespace Cryptometheus
{
    public class Settings
    {
        // ---------------- Constructor ----------------

        public Settings()
        {
            this.RateLimit = TimeSpan.FromSeconds( 10 );
            this.Tickers = new List<string>();
        }

        // ---------------- Properties ----------------

        /// <summary>
        /// How long to wait between *each* query of the Cryptonator API?
        /// </summary>
        public TimeSpan RateLimit { get; set; }

        /// <summary>
        /// The tickers to watch.
        /// </summary>
        public IList<string> Tickers { get; private set; }

        /// <summary>
        /// Just print help text and exit.
        /// </summary>
        public bool PrintHelp { get; set; }

        /// <summary>
        /// Just print version text and exit.
        /// </summary>
        public bool PrintVersion { get; set; }

        /// <summary>
        /// Just print the credits and exit.
        /// </summary>
        public bool PrintCredits { get; set; }

        /// <summary>
        /// Just print the software license and exit.
        /// </summary>
        public bool PrintLicense { get; set; }

        /// <summary>
        /// Just print the disclaimer and exit.
        /// </summary>
        public bool PrintDisclaimer { get; set; }

        /// <summary>
        /// Returns true if we only want to query some information
        /// about the program and then exit.
        /// </summary>
        public bool QueryCommandOnly =>
            this.PrintHelp ||
            this.PrintCredits ||
            this.PrintVersion ||
            this.PrintLicense ||
            this.PrintDisclaimer;
    }

    public static class SettingsExtensions
    {
        // ---------------- Fields ----------------

        internal const string TickerArg = "tickers";
        private const string tickerHelpMsg = "What tickers to watch for.  Should be in the form of base-target, separated by ';'.  For example, btc-usd;doge-usd;algo-usd";

        internal const string RatelimitArg = "rate_limit";
        private const string rateLimitHelpMsg = "Delay between each API query in seconds.  Can not be 0 or less.";

        // ---------------- Functions ----------------

        public static void ParseFromArguments( this Settings settings, string[] args )
        {
            CheckForQueryCommand( settings, args );
            if( settings.QueryCommandOnly )
            {
                return;
            }

            // Use default for now.
            TimeSpan rateLimit = settings.RateLimit;

            IList<string> tickers = new List<string>();

            // Next, check environment variables.
            string envVarTickerValue = Environment.GetEnvironmentVariable( TickerArg );
            if( string.IsNullOrWhiteSpace( envVarTickerValue ) == false )
            {
                tickers.AddRange( SplitTickers( envVarTickerValue ) );
            }

            string envVarRateLimitValue = Environment.GetEnvironmentVariable( RatelimitArg );
            if( string.IsNullOrWhiteSpace( envVarRateLimitValue ) == false )
            {
                rateLimit = ParseRateLimit( envVarRateLimitValue, "environment variable" );
            }

            bool cleared = false;
            OptionSet options = new OptionSet
            {
                {
                    $"{TickerArg}=",
                    tickerHelpMsg,
                    v =>
                    {
                        if( cleared == false )
                        {
                            // CLI overwrites environment variables.
                            // But, we'll allow multiple --tickers commands to be allowed.
                            tickers.Clear();
                            cleared = true;
                        }
                        tickers.AddRange( SplitTickers( v ) );
                    }
                },
                {
                    $"{RatelimitArg}=",
                    rateLimitHelpMsg,
                    v => { rateLimit = ParseRateLimit( v, "command line argument" ); }
                }
            };

            options.Parse( args );

            settings.Tickers.AddRange( tickers );
            settings.RateLimit = rateLimit;

            settings.Validate();
        }

        private static void CheckForQueryCommand( this Settings settings, string[] args )
        {
            OptionSet options = GetQueryOnlyOptions( settings );

            options.Parse( args );
        }

        public static OptionSet GetQueryOnlyOptions( Settings settings )
        {
            OptionSet options = new OptionSet
            {
                {
                    "h|help",
                    "Shows this message and exits.",
                    v => settings.PrintHelp = ( v != null )
                },
                {
                    "version",
                    "Shows the version and exits.",
                    v => settings.PrintVersion = ( v != null )
                },
                {
                    "print_license",
                    "Shows the license information and exits.",
                    v => settings.PrintLicense = ( v != null )
                },
                {
                    "print_credits",
                    "Shows the credits information and exits.",
                    v => settings.PrintCredits = ( v != null )
                },
                {
                    "print_disclaimer",
                    "Shows the disclaimer and exits.",
                    v => settings.PrintDisclaimer = ( v != null )
                },
                {
                    $"{RatelimitArg}=",
                    rateLimitHelpMsg,
                    v => { }
                },
                {
                    $"{TickerArg}=",
                    tickerHelpMsg,
                    v => { }
                }

            };

            return options;
        }

        private static IEnumerable<string> SplitTickers( string arg )
        {
            return arg.Split( ';', StringSplitOptions.RemoveEmptyEntries )
                .Select( s => s.ToUpper() ); // Cryptonator is all uppercase.  Let's do the same.
        }

        private static TimeSpan ParseRateLimit( string arg, string context )
        {
            if( double.TryParse( arg, out double seconds ) )
            {
                return TimeSpan.FromSeconds( seconds );
            }
            else
            {
                throw new ArgumentException(
                    $"Can not parse value in {RatelimitArg} {context}, not a valid double.",
                    RatelimitArg
                );
            }
        }

        public static void Validate( this Settings settings )
        {
            List<string> errors = new List<string>();

            if( settings.Tickers.IsEmpty() )
            {
                errors.Add( $"{nameof( settings.Tickers )} can not be empty" );
            }

            foreach( string ticker in settings.Tickers )
            {
                if( string.IsNullOrWhiteSpace( ticker ) )
                {
                    errors.Add( "Ticker can not be empty or whitespace" );
                }
            }

            if( settings.RateLimit <= TimeSpan.Zero )
            {
                errors.Add( $"{nameof( settings.RateLimit )} can not be zero or less" );
            }

            if( errors.IsEmpty() == false )
            {
                throw new ListedValidationException(
                    $"Errors when validating {nameof( Settings )}",
                    errors
                );
            }
        }
    }
}
