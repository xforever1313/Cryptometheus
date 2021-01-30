//
//          Copyright Seth Hendrick 2021.
// Distributed under the Boost Software License, Version 1.0.
//    (See accompanying file LICENSE_1_0.txt or copy at
//          http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SethCS.Exceptions;
using SethCS.Extensions;

namespace Cryptometheus
{
    public class CryptonatorResult
    {
        // ---------------- Constructor ----------------

        public CryptonatorResult()
        {
            this.Price = -1;
        }

        // ---------------- Properties ----------------

        /// <summary>
        /// The currency we want to get the price of.
        /// Set to null if <see cref="Success"/> is false.
        /// </summary>
        public string BaseCurrency { get; set; }

        /// <summary>
        /// The currency in which we want to display <see cref="BaseCurrency"/>'s price.
        /// Set to null if <see cref="Success"/> is false.
        /// </summary>
        public string TargetCurrency { get; set; }

        /// <summary>
        /// The ticker that is used.
        /// </summary>
        public string Ticker => $"{this.BaseCurrency ?? string.Empty}-{this.TargetCurrency ?? string.Empty}";

        /// <summary>
        /// How much 1 <see cref="BaseCurrency"/> can be sold for in <see cref="TargetCurrency"/>
        /// Set to 0 if <see cref="Success"/> is false.
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Was the query successful?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The error message.  Set to null if <see cref="Success"/> is true.
        /// </summary>
        public string Error { get; set; }

        // ---------------- Functions ----------------

        public override string ToString()
        {
            if( this.Success == false )
            {
                return $"{nameof( this.Error )}: {this.Error ?? string.Empty}";
            }
            else
            {
                return $"{this.Ticker}: {this.Price}";
            }
        }
    }

    public static class CryptonatorResultExtensions
    {
        public static void FromJson( this CryptonatorResult result, string jsonString )
        {
            // Example success JSON:
            // {
            //     "ticker":
            //      {
            //          "base":"DOGE"
            //          "target":"USD",
            //          "price":"0.02788101",
            //          "volume":"3604478983.83879995",
            //          "change":"-0.00297482"
            //      },
            //      "timestamp":1612025582,
            //      "success":true,
            //      "error":""
            // }
            //
            // Example failure JSON:
            // {
            //     "success":false,
            //     "error":"Pair not found"
            // }

            JObject json = JObject.Parse( jsonString );

            bool success;
            JValue successToken = (JValue)json["success"];
            if( bool.TryParse( successToken.Value.ToString(), out success ) == false )
            {
                throw new ApplicationException(
                    "Could not parse 'success' from response: " + jsonString
                );
            }

            result.Success = success;
            if( result.Success == false )
            {
                JValue errorToken = (JValue)json["error"];
                result.Error = errorToken.Value.ToString();
            }
            else
            {
                JObject tickerObject = (JObject)json["ticker"];
                foreach( JProperty property in tickerObject.Properties() )
                {
                    if( "base".EqualsIgnoreCase( property.Name ) )
                    {
                        result.BaseCurrency = property.Value.ToString();
                    }
                    else if( "target".EqualsIgnoreCase( property.Name ) )
                    {
                        result.TargetCurrency = property.Value.ToString();
                    }
                    else if( "price".EqualsIgnoreCase( property.Name ) )
                    {
                        if( double.TryParse( property.Value.ToString(), out double price ) )
                        {
                            result.Price = price;
                        }
                        else
                        {
                            throw new ApplicationException(
                                "Could not parse 'price' from response: " + jsonString
                            );
                        }
                    }
                }

                IList<string> errors = result.Validate();
                if( errors.IsEmpty() == false )
                {
                    throw new ListedValidationException(
                        $"Errors when parsing response: {jsonString}",
                        errors
                    );
                }
            }
        }

        public static IList<string> Validate( this CryptonatorResult result )
        {
            List<string> errors = new List<string>();
            if( string.IsNullOrWhiteSpace( result.BaseCurrency ) )
            {
                errors.Add( $"{nameof( result.BaseCurrency )} null, empty, or whitespace" );
            }
            else if( string.IsNullOrWhiteSpace( result.TargetCurrency ) )
            {
                errors.Add( $"{nameof( result.TargetCurrency )} null, empty, or whitespace" );
            }
            else if( result.Price < 0 )
            {
                errors.Add( $"{nameof( result.Price )} negative" );
            }

            return errors;
        }
    }
}
