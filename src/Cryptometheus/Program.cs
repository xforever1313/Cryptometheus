//
//          Copyright Seth Hendrick 2021.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.IO;
using Mono.Options;
using SethCS.Exceptions;

namespace Cryptometheus
{
    class Program
    {
        static int Main( string[] args )
        {
            try
            {
                Settings settings = new Settings();
                settings.ParseFromArguments( args );

                if( settings.PrintHelp )
                {
                    PrintHelp();
                }
                else if( settings.PrintVersion )
                {
                    PrintVersion();
                }
                else if( settings.PrintDisclaimer )
                {
                    PrintDisclaimer();
                }
                else if( settings.PrintCredits )
                {
                    PrintCredits();
                }
                else if( settings.PrintLicense )
                {
                    PrintLicense();
                }
                else
                {
                }
            }
            catch( OptionException e )
            {
                Console.WriteLine( "Invalid Arguments: " + e.Message );
                return 1;
            }
            catch( ListedValidationException e )
            {
                Console.WriteLine( e.Message );
                return 1;
            }
            catch( Exception e )
            {
                Console.WriteLine( "FATAL: Unhandled Exception:" );
                Console.WriteLine( e.Message );
                return -1;
            }

            return 0;
        }

        private static void PrintHelp()
        {
            Settings dummy = new Settings();
            OptionSet options = SettingsExtensions.GetQueryOnlyOptions( dummy );

            Console.WriteLine(
                $"Usage: Cryptometheus.exe [--{SettingsExtensions.TickerArg}=base1-target1;base2-target2] [--{SettingsExtensions.RatelimitArg}=timeInSeconds]"
            );
            Console.WriteLine();
            options.WriteOptionDescriptions( Console.Out );
            Console.WriteLine();
            Console.WriteLine( $"Can also set {SettingsExtensions.TickerArg} and {SettingsExtensions.RatelimitArg} via environment variables" );
            Console.WriteLine( $"(Note, command line arguments take precedence)." );
            Console.WriteLine();
            Console.WriteLine( "Have an issue? Need more help? File an issue: https://github.com/xforever1313/Cryptometheus" );
        }

        private static void PrintVersion()
        {
            Console.WriteLine( typeof( Program ).Assembly.GetName().Version.ToString( 3 ) );
        }

        private static void PrintDisclaimer()
        {
            Console.WriteLine( ReadResource( "Cryptometheus.Disclaimer.md" ) );
        }

        private static void PrintCredits()
        {
            Console.WriteLine( ReadResource( "Cryptometheus.Credits.md" ) );
        }

        private static void PrintLicense()
        {
            Console.WriteLine( ReadResource( "Cryptometheus.LICENSE_1_0.txt" ) );
        }

        private static string ReadResource( string resourceName )
        {
            using( Stream stream = typeof( Program ).Assembly.GetManifestResourceStream( resourceName ) )
            {
                using( StreamReader reader = new StreamReader( stream ) )
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
